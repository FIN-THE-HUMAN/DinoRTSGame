using System;
using UnityEngine;
using RTSFramework.Combat;
using RTSFramework.Selection;
using RTSFramework.Factions;
using UnityEngine.AI;

namespace RTSFramework.Buildings
{
    [RequireComponent(typeof(Health))]
    public class Building : MonoBehaviour, ISelectable
    {
        [Header("Faction & Selection")]
        [SerializeField] private Faction faction;
        [SerializeField] private GameObject selectionVisual;

        [Header("Construction State")]
        [SerializeField] private float constructionProgress; // 0 to 1
        [SerializeField] private bool isConstructed;

        [Header("Config")]
        [SerializeField] private BuildingData buildingData;

        private Health health;

        public event Action<float> OnConstructionProgressChanged; // 0 to 1
        public event Action OnConstructionComplete;

        public BuildingData BuildingData => buildingData;
        public bool IsConstructed => isConstructed;
        public float ConstructionProgress => constructionProgress;

        // Faction and ISelectable Properties
        public Faction Faction => faction;
        public bool IsPlayerOwned => faction != null && faction.IsPlayerFaction;
        public Transform Transform => transform;
        public GameObject GameObject => gameObject;

        private Vector3 originalScale;

        private void Awake()
        {
            health = GetComponent<Health>();
            SetupNavMeshObstacle();
            SetupSelectionVisual();

            if (originalScale == Vector3.zero)
            {
                originalScale = transform.localScale;
            }
        }

        private void SetupSelectionVisual()
        {
            if (selectionVisual == null)
            {
                Transform existing = transform.Find("SelectionVisual");
                if (existing != null)
                {
                    selectionVisual = existing.gameObject;
                }
                else
                {
                    GameObject selVisualObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    selVisualObj.name = "SelectionVisual";
                    selVisualObj.transform.SetParent(transform, false);

                    var boxCol = GetComponent<BoxCollider>();
                    float bottomY = boxCol != null ? (boxCol.center.y - boxCol.size.y / 2f) + 0.02f : 0.02f;
                    selVisualObj.transform.localPosition = new Vector3(0f, bottomY, 0f);
                    selVisualObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                    float sizeVal = buildingData != null ? buildingData.GridSize * 1.5f : 4.5f;
                    selVisualObj.transform.localScale = new Vector3(sizeVal, sizeVal, 1f);

                    Destroy(selVisualObj.GetComponent<Collider>());

                    selectionVisual = selVisualObj;
                }
            }
        }

        public void SetFaction(Faction newFaction)
        {
            this.faction = newFaction;
            ApplyFactionColor();
        }

        private void SetupNavMeshObstacle()
        {
            var obstacle = GetComponent<NavMeshObstacle>();
            if (obstacle == null)
            {
                obstacle = gameObject.AddComponent<NavMeshObstacle>();
            }

            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
            obstacle.shape = NavMeshObstacleShape.Box;

            var boxCol = GetComponent<BoxCollider>();
            if (boxCol != null)
            {
                obstacle.center = boxCol.center;
                obstacle.size = boxCol.size;
            }
            else
            {
                float sizeVal = buildingData != null ? buildingData.GridSize : 3f;
                obstacle.center = Vector3.zero;
                obstacle.size = new Vector3(sizeVal, 2f, sizeVal);
            }
        }

        private void Start()
        {
            SelectionManager.RegisterSelectable(this);
            Deselect();
            ApplyFactionColor();

            // Dynamically attach Fog of War components based on ownership
            if (IsPlayerOwned)
            {
                if (GetComponent<RTSFramework.Fog.FogRevealer>() == null)
                {
                    var rev = gameObject.AddComponent<RTSFramework.Fog.FogRevealer>();
                    rev.SightRange = 15f; // Buildings reveal more area than units
                }
            }
            else
            {
                if (GetComponent<RTSFramework.Fog.FogReceiver>() == null)
                {
                    gameObject.AddComponent<RTSFramework.Fog.FogReceiver>();
                }
            }
        }

        private void ApplyFactionColor()
        {
            if (selectionVisual == null) return;
            var renderer = selectionVisual.GetComponent<Renderer>();
            if (renderer == null) return;

            Shader spritesShader = Shader.Find("Sprites/Default");
            if (spritesShader == null) spritesShader = Shader.Find("Unlit/Color");
            if (spritesShader == null) spritesShader = Shader.Find("Standard");

            if (spritesShader != null)
            {
                Material mat = new Material(spritesShader);
                mat.color = IsPlayerOwned ? Color.green : Color.red;
                renderer.sharedMaterial = mat;
            }
        }

        private void OnDestroy()
        {
            SelectionManager.UnregisterSelectable(this);
        }

        public void Initialize(BuildingData data)
        {
            buildingData = data;
            constructionProgress = 0f;
            isConstructed = false;

            if (originalScale == Vector3.zero)
            {
                originalScale = transform.localScale;
            }
            // Set flat foundation scale (10% height)
            transform.localScale = new Vector3(originalScale.x, originalScale.y * 0.1f, originalScale.z);

            // Set dynamic health values for foundation (starting at 10 HP)
            if (health != null && data != null)
            {
                health.SetBaseMaxHealth(data.MaxHealth);
                float startingMaxHealth = Mathf.Min(10f, data.MaxHealth);
                health.SetConstructionHealth(startingMaxHealth, startingMaxHealth);
            }

            // Disable components until constructed
            ToggleFunctionality(false);
        }

        // ISelectable Methods
        public void Select()
        {
            if (selectionVisual != null)
            {
                selectionVisual.SetActive(true);
            }
        }

        public void Deselect()
        {
            if (selectionVisual != null)
            {
                selectionVisual.SetActive(false);
            }
        }

        public void Construct(float amount)
        {
            if (isConstructed) return;

            float oldProgress = constructionProgress;
            constructionProgress += amount;
            constructionProgress = Mathf.Clamp01(constructionProgress);

            // Update height scale based on construction progress
            transform.localScale = new Vector3(originalScale.x, originalScale.y * (0.1f + 0.9f * constructionProgress), originalScale.z);

            // Update health values dynamically during construction
            if (health != null && buildingData != null)
            {
                float finalMaxHealth = buildingData.MaxHealth;
                if (Upgrades.UpgradeManager.Instance != null && faction != null)
                {
                    string targetTag = buildingData.BuildingName;
                    finalMaxHealth = Upgrades.UpgradeManager.Instance.GetModifiedValue(faction, targetTag, Upgrades.UpgradeEffectType.MaxHealth, finalMaxHealth);
                }

                float startingMaxHealth = Mathf.Min(10f, finalMaxHealth);
                float oldMaxHealth = startingMaxHealth + (finalMaxHealth - startingMaxHealth) * oldProgress;
                float newMaxHealth = startingMaxHealth + (finalMaxHealth - startingMaxHealth) * constructionProgress;
                
                // Add the difference in maximum health to the current health
                float addedMaxHealth = newMaxHealth - oldMaxHealth;
                float newCurrentHealth = health.CurrentHealth + addedMaxHealth;

                health.SetConstructionHealth(newMaxHealth, newCurrentHealth);
            }

            OnConstructionProgressChanged?.Invoke(constructionProgress);

            if (constructionProgress >= 1f)
            {
                CompleteConstruction();
            }
        }

        private void CompleteConstruction()
        {
            isConstructed = true;
            transform.localScale = originalScale; // Reset to full scale

            // Ensure health is set to final values (retaining any damage taken)
            if (health != null && buildingData != null)
            {
                float finalMaxHealth = buildingData.MaxHealth;
                if (Upgrades.UpgradeManager.Instance != null && faction != null)
                {
                    string targetTag = buildingData.BuildingName;
                    finalMaxHealth = Upgrades.UpgradeManager.Instance.GetModifiedValue(faction, targetTag, Upgrades.UpgradeEffectType.MaxHealth, finalMaxHealth);
                }
                health.SetConstructionHealth(finalMaxHealth, health.CurrentHealth);
            }

            ToggleFunctionality(true);
            OnConstructionComplete?.Invoke();
            Debug.Log($"{gameObject.name} construction complete!");
        }

        private void ToggleFunctionality(bool enable)
        {
            // Enable or disable components like ResourceDropOff, UnitProduction, etc.
            var dropOff = GetComponent<Resources.ResourceDropOff>();
            if (dropOff != null)
            {
                dropOff.enabled = enable;
            }
            
            // In the future: enable recruitment, defensive towers, etc.
        }
    }
}
