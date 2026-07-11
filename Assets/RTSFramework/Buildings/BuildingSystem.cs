using UnityEngine;
using RTSFramework.Resources;
using RTSFramework.Selection;
using RTSFramework.Units;
using RTSFramework.Commands;

namespace RTSFramework.Buildings
{
    public class BuildingSystem : MonoBehaviour
    {
        public static BuildingSystem Instance { get; private set; }

        [Header("Layers")]
        [SerializeField] private LayerMask terrainLayer;
        [SerializeField] private LayerMask obstacleLayer;

        [Header("Placement Settings")]
        [SerializeField] private float gridSnapSize = 0.25f;

        private GameObject ghostInstance;
        private BuildingData currentBuildingData;

        public bool IsPlacing => ghostInstance != null;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void StartPlacement(BuildingData buildingData)
        {
            CancelPlacement();

            if (buildingData == null || buildingData.GhostPrefab == null) return;

            currentBuildingData = buildingData;
            ghostInstance = Instantiate(buildingData.GhostPrefab);
        }

        private void Update()
        {
            if (ghostInstance == null) return;

            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null) return;

            Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, terrainLayer))
            {
                Vector3 snappedPos = SnapToGrid(hit.point, gridSnapSize);
                
                // Add vertical Y offset from the prefab definition to keep the building's base on the ground
                float yOffset = currentBuildingData != null && currentBuildingData.BuildingPrefab != null ? 
                    currentBuildingData.BuildingPrefab.transform.position.y : 0f;
                
                ghostInstance.transform.position = snappedPos + Vector3.up * yOffset;

                bool canPlace = CanPlace(snappedPos, currentBuildingData);
                UpdateGhostVisuals(canPlace);

                var eventSystem = UnityEngine.EventSystems.EventSystem.current;
                bool isOverUI = eventSystem != null && eventSystem.IsPointerOverGameObject();

                if (mouse.leftButton.wasPressedThisFrame && canPlace && !isOverUI)
                {
                    PlaceBuilding(snappedPos + Vector3.up * yOffset);
                }
            }

            if (mouse.rightButton.wasPressedThisFrame || UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CancelPlacement();
            }
        }

        private Vector3 SnapToGrid(Vector3 rawPosition, float size)
        {
            float x = Mathf.Round(rawPosition.x / size) * size;
            float z = Mathf.Round(rawPosition.z / size) * size;
            return new Vector3(x, rawPosition.y, z);
        }

        private bool CanPlace(Vector3 position, BuildingData data)
        {
            // 1. Check if player has enough resources
            if (!ResourceManager.Instance.HasResources(data.Cost)) return false;

            // 2. Check if the spot is blocked by obstacles (units, other buildings)
            float checkSize = data.GridSize * 0.95f;
            Vector3 halfExtents = new Vector3(checkSize / 2f, 0.5f, checkSize / 2f);
            Vector3 checkCenter = position + Vector3.up * 0.5f;

            Collider[] colliders = Physics.OverlapBox(checkCenter, halfExtents, Quaternion.identity, obstacleLayer);
            foreach (var col in colliders)
            {
                // Ignore the ground/terrain
                if (col.gameObject.name.Contains("Ground") || col.gameObject.name.Contains("Terrain") || col.GetComponent<Terrain>() != null)
                {
                    continue;
                }
                // Ignore triggers (selection rings, ghost visual zones)
                if (col.isTrigger)
                {
                    continue;
                }
                // Ignore ourselves (the ghost instance, if it has a collider)
                if (ghostInstance != null && (col.gameObject == ghostInstance || col.transform.IsChildOf(ghostInstance.transform)))
                {
                    continue;
                }
                
                // Found a real obstacle!
                return false;
            }

            return true;
        }

        public Building PlaceBuildingForFaction(BuildingData data, Vector3 position, Factions.Faction faction)
        {
            if (data == null || faction == null) return null;

            // Deduct resources for this specific faction
            if (ResourceManager.Instance.SpendResources(faction, data.Cost))
            {
                float yOffset = data.BuildingPrefab != null ? data.BuildingPrefab.transform.position.y : 0f;
                GameObject buildingObj = Instantiate(data.BuildingPrefab, position + Vector3.up * yOffset, Quaternion.identity);
                Building building = buildingObj.GetComponent<Building>();
                if (building != null)
                {
                    building.Initialize(data);
                    building.SetFaction(faction);
                }
                return building;
            }
            return null;
        }

        private void PlaceBuilding(Vector3 position)
        {
            if (currentBuildingData == null) return;

            Factions.Faction playerFaction = GetPlayerFaction();

            // Deduct resources for player faction
            if (ResourceManager.Instance.SpendResources(playerFaction, currentBuildingData.Cost))
            {
                GameObject buildingObj = Instantiate(currentBuildingData.BuildingPrefab, position, Quaternion.identity);
                Building building = buildingObj.GetComponent<Building>();
                
                if (building != null)
                {
                    building.Initialize(currentBuildingData);
                    
                    if (playerFaction != null)
                    {
                        building.SetFaction(playerFaction);
                    }

                    // Send selected builder units to construct the placed foundation
                    foreach (var selected in SelectionManager.Instance.SelectedObjects)
                    {
                        if (selected == null || selected.Equals(null)) continue;
                        if (selected.GameObject.TryGetComponent<UnitController>(out var unit))
                        {
                            if (unit.IsPlayerOwned && unit.GetComponent<BuilderComponent>() != null)
                            {
                                unit.GiveCommand(new BuildCommand(building), false);
                            }
                        }
                    }
                }

                // If Shift is NOT held, exit placement mode
                var keyboard = UnityEngine.InputSystem.Keyboard.current;
                bool isShiftHeld = keyboard != null && (keyboard.shiftKey.isPressed || keyboard.leftShiftKey.isPressed);
                if (!isShiftHeld)
                {
                    CancelPlacement();
                }
            }
        }

        private void UpdateGhostVisuals(bool canPlace)
        {
            if (ghostInstance == null) return;

            // Simple visual feedback: tint renderers green or red
            Color tintColor = canPlace ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f);

            var renderers = ghostInstance.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                foreach (var mat in r.materials)
                {
                    if (mat.HasProperty("_BaseColor"))
                    {
                        mat.SetColor("_BaseColor", tintColor);
                    }
                    else if (mat.HasProperty("_Color"))
                    {
                        mat.SetColor("_Color", tintColor);
                    }
                }
            }
        }

        public void CancelPlacement()
        {
            if (ghostInstance != null)
            {
                Destroy(ghostInstance);
            }
            currentBuildingData = null;
        }

        private Factions.Faction GetPlayerFaction()
        {
            // 1. Try to find the player faction from existing scene buildings
            foreach (var b in FindObjectsOfType<Building>())
            {
                if (b != null && b.Faction != null && b.Faction.IsPlayerFaction)
                {
                    return b.Faction;
                }
            }
            // 2. Try to find the player faction from units
            foreach (var u in FindObjectsOfType<UnitController>())
            {
                if (u != null && u.Faction != null && u.Faction.IsPlayerFaction)
                {
                    return u.Faction;
                }
            }
            return null;
        }
    }
}
