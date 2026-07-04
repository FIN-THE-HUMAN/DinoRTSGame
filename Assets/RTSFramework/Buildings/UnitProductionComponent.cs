using System;
using System.Collections.Generic;
using UnityEngine;
using RTSFramework.Units;
using RTSFramework.Resources;

namespace RTSFramework.Buildings
{
    [RequireComponent(typeof(Building))]
    public class UnitProductionComponent : MonoBehaviour
    {
        [Header("Production Config")]
        [SerializeField] private List<UnitData> trainableUnits = new List<UnitData>();
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private int queueMaxSize = 5;

        private readonly Queue<UnitData> trainingQueue = new Queue<UnitData>();
        private float trainingProgress; // 0 to 1
        private float currentUnitTimeElapsed;

        private Building building;

        public event Action OnQueueChanged;

        public IReadOnlyList<UnitData> TrainableUnits => trainableUnits;
        public int QueueCount => trainingQueue.Count;
        public float TrainingProgress => trainingProgress;
        public UnitData CurrentActiveUnit => trainingQueue.Count > 0 ? trainingQueue.Peek() : null;
        public IReadOnlyCollection<UnitData> QueuedUnits => trainingQueue;

        private void Awake()
        {
            building = GetComponent<Building>();
        }

        private void Update()
        {
            // Cannot train units if building is not fully constructed yet
            if (building == null || !building.IsConstructed)
            {
                return;
            }

            if (trainingQueue.Count > 0)
            {
                UnitData activeUnit = trainingQueue.Peek();
                currentUnitTimeElapsed += Time.deltaTime;
                trainingProgress = Mathf.Clamp01(currentUnitTimeElapsed / activeUnit.TrainingTime);

                if (currentUnitTimeElapsed >= activeUnit.TrainingTime)
                {
                    CompleteTraining();
                }
            }
            else
            {
                trainingProgress = 0f;
                currentUnitTimeElapsed = 0f;
            }
        }

        public bool TryQueueUnit(UnitData unitData)
        {
            if (unitData == null) return false;
            if (trainingQueue.Count >= queueMaxSize)
            {
                Debug.Log("Queue is full!");
                return false;
            }

            // Spend resources
            if (ResourceManager.Instance.SpendResources(unitData.Cost))
            {
                trainingQueue.Enqueue(unitData);
                OnQueueChanged?.Invoke();
                return true;
            }

            Debug.Log("Cannot afford unit: " + unitData.UnitName);
            return false;
        }

        public void CancelLastQueued()
        {
            if (trainingQueue.Count == 0) return;

            // Convert queue to list to manipulate
            List<UnitData> temp = new List<UnitData>(trainingQueue);
            UnitData canceled = temp[temp.Count - 1];
            temp.RemoveAt(temp.Count - 1);

            // Rebuild queue
            trainingQueue.Clear();
            foreach (var item in temp)
            {
                trainingQueue.Enqueue(item);
            }

            // Refund resources
            foreach (var c in canceled.Cost)
            {
                ResourceManager.Instance.AddResource(c.resourceType, c.amount);
            }

            // Reset time if we canceled the current active unit (which was the only one in the queue)
            if (trainingQueue.Count == 0)
            {
                currentUnitTimeElapsed = 0f;
                trainingProgress = 0f;
            }

            OnQueueChanged?.Invoke();
            Debug.Log("Canceled training of: " + canceled.UnitName + " and refunded cost.");
        }

        private void CompleteTraining()
        {
            if (trainingQueue.Count == 0) return;

            UnitData trained = trainingQueue.Dequeue();
            currentUnitTimeElapsed = 0f;
            trainingProgress = 0f;

            SpawnUnit(trained);

            OnQueueChanged?.Invoke();
        }

        private void SpawnUnit(UnitData unitData)
        {
            if (unitData == null || unitData.UnitPrefab == null) return;

            Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.forward * 3f;
            
            // Spawn unit
            GameObject spawned = Instantiate(unitData.UnitPrefab, spawnPos, Quaternion.identity);

            // Propagate Faction
            var controller = spawned.GetComponent<UnitController>();
            if (controller != null)
            {
                controller.SetFaction(building.Faction);
            }

            Debug.Log("Spawned unit: " + unitData.UnitName + " with faction " + (building.Faction != null ? building.Faction.name : "None"));
        }
    }
}
