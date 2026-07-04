using System;
using UnityEngine;
using RTSFramework.Combat;
using RTSFramework.Selection;
using RTSFramework.Factions;

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

        private BuildingData buildingData;
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
        }

        private void Start()
        {
            SelectionManager.RegisterSelectable(this);
            Deselect();
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
