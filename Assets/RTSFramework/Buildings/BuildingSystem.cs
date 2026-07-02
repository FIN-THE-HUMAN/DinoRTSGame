using UnityEngine;
using RTSFramework.Resources;

namespace RTSFramework.Buildings
{
    public class BuildingSystem : MonoBehaviour
    {
        public static BuildingSystem Instance { get; private set; }

        [Header("Layers")]
        [SerializeField] private LayerMask terrainLayer;
        [SerializeField] private LayerMask obstacleLayer;

        private GameObject ghostInstance;
        private BuildingData currentBuildingData;

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
                Vector3 snappedPos = SnapToGrid(hit.point, currentBuildingData.GridSize);
                ghostInstance.transform.position = snappedPos;

                bool canPlace = CanPlace(snappedPos, currentBuildingData);
                UpdateGhostVisuals(canPlace);

                if (mouse.leftButton.wasPressedThisFrame && canPlace)
                {
                    PlaceBuilding(snappedPos);
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
            // We do a box overlap slightly smaller than the grid size to avoid false collisions with adjacent grid cells
            float checkSize = data.GridSize * 0.95f;
            Vector3 halfExtents = new Vector3(checkSize / 2f, 0.5f, checkSize / 2f);
            
            // Check from slightly above the ground
            Vector3 checkCenter = position + Vector3.up * 0.5f;

            Collider[] colliders = Physics.OverlapBox(checkCenter, halfExtents, Quaternion.identity, obstacleLayer);
            return colliders.Length == 0;
        }

        private void PlaceBuilding(Vector3 position)
        {
            if (currentBuildingData == null) return;

            // Deduct resources
            if (ResourceManager.Instance.SpendResources(currentBuildingData.Cost))
            {
                GameObject buildingObj = Instantiate(currentBuildingData.BuildingPrefab, position, Quaternion.identity);
                Building building = buildingObj.GetComponent<Building>();
                
                if (building != null)
                {
                    building.Initialize(currentBuildingData);
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
    }
}
