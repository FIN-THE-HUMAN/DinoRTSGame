using System;
using System.Collections.Generic;
using UnityEngine;
using RTSFramework.Resources;
using RTSFramework.Selection;

namespace RTSFramework.Upgrades
{
    [RequireComponent(typeof(Buildings.Building))]
    public class TechnologyResearchComponent : MonoBehaviour
    {
        [Header("Research Config")]
        [SerializeField] private List<UpgradeData> researchableUpgrades = new List<UpgradeData>();
        [SerializeField] private int queueMaxSize = 5;

        private readonly Queue<UpgradeData> researchQueue = new Queue<UpgradeData>();
        private float researchProgress; // 0 to 1
        private float currentResearchTimeElapsed;

        private Buildings.Building building;

        public event Action OnQueueChanged;

        public IReadOnlyList<UpgradeData> ResearchableUpgrades => researchableUpgrades;
        public int QueueCount => researchQueue.Count;
        public float ResearchProgress => researchProgress;
        public UpgradeData CurrentActiveResearch => researchQueue.Count > 0 ? researchQueue.Peek() : null;
        public IReadOnlyCollection<UpgradeData> QueuedResearch => researchQueue;

        private void Awake()
        {
            building = GetComponent<Buildings.Building>();
        }

        private void Update()
        {
            // Cannot research if building is not fully constructed
            if (building == null || !building.IsConstructed) return;

            if (researchQueue.Count > 0)
            {
                UpgradeData active = researchQueue.Peek();
                currentResearchTimeElapsed += Time.deltaTime;
                researchProgress = Mathf.Clamp01(currentResearchTimeElapsed / active.ResearchTime);

                if (currentResearchTimeElapsed >= active.ResearchTime)
                {
                    CompleteResearch();
                }
            }
            else
            {
                researchProgress = 0f;
                currentResearchTimeElapsed = 0f;
            }
        }

        public bool TryQueueResearch(UpgradeData upgrade)
        {
            if (upgrade == null) return false;
            if (researchQueue.Count >= queueMaxSize)
            {
                Debug.Log("Research queue is full!");
                return false;
            }

            // Verify prerequisites
            if (UpgradeManager.Instance != null && !UpgradeManager.Instance.ArePrerequisitesMet(building.Faction, upgrade))
            {
                Debug.Log($"Prerequisites not met for research: {upgrade.UpgradeName}");
                return false;
            }

            // Verify already completed
            if (UpgradeManager.Instance != null && UpgradeManager.Instance.IsUpgradeCompleted(building.Faction, upgrade))
            {
                Debug.Log($"Research already completed: {upgrade.UpgradeName}");
                return false;
            }

            // Spend resources
            if (ResourceManager.Instance.SpendResources(upgrade.Cost))
            {
                researchQueue.Enqueue(upgrade);
                OnQueueChanged?.Invoke();
                return true;
            }

            Debug.Log("Cannot afford research: " + upgrade.UpgradeName);
            return false;
        }

        public void CancelLastQueued()
        {
            if (researchQueue.Count == 0) return;

            List<UpgradeData> temp = new List<UpgradeData>(researchQueue);
            UpgradeData canceled = temp[temp.Count - 1];
            temp.RemoveAt(temp.Count - 1);

            researchQueue.Clear();
            foreach (var item in temp)
            {
                researchQueue.Enqueue(item);
            }

            // Refund resources
            foreach (var c in canceled.Cost)
            {
                ResourceManager.Instance.AddResource(c.resourceType, c.amount);
            }

            if (researchQueue.Count == 0)
            {
                currentResearchTimeElapsed = 0f;
                researchProgress = 0f;
            }

            OnQueueChanged?.Invoke();
            Debug.Log("Canceled research of: " + canceled.UpgradeName + " and refunded cost.");
        }

        private void CompleteResearch()
        {
            if (researchQueue.Count == 0) return;

            UpgradeData completed = researchQueue.Dequeue();
            currentResearchTimeElapsed = 0f;
            researchProgress = 0f;

            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.CompleteUpgrade(building.Faction, completed);
            }

            OnQueueChanged?.Invoke();
        }
    }
}
