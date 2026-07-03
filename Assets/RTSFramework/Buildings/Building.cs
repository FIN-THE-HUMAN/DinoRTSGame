using System;
using UnityEngine;
using RTSFramework.Combat;

namespace RTSFramework.Buildings
{
    [RequireComponent(typeof(Health))]
    public class Building : MonoBehaviour
    {
        private BuildingData buildingData;
        private Health health;

        private float constructionProgress; // 0 to 1
        private bool isConstructed;

        public event Action<float> OnConstructionProgressChanged; // 0 to 1
        public event Action OnConstructionComplete;

        public BuildingData BuildingData => buildingData;
        public bool IsConstructed => isConstructed;
        public float ConstructionProgress => constructionProgress;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        public void Initialize(BuildingData data)
        {
            buildingData = data;
            constructionProgress = 0f;
            isConstructed = false;

            // Disable components until constructed
            ToggleFunctionality(false);
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
