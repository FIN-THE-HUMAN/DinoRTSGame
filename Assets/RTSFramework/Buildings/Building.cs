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

        private void Awake()
        {
            health = GetComponent<Health>();
            SetupNavMeshObstacle();
            SetupSelectionVisual();
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

            constructionProgress += amount;
            constructionProgress = Mathf.Clamp01(constructionProgress);

            OnConstructionProgressChanged?.Invoke(constructionProgress);

            if (constructionProgress >= 1f)
            {
                CompleteConstruction();
            }
        }

        private void CompleteConstruction()
        {
            isConstructed = true;
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
