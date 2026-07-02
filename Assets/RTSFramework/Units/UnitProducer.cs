using System;
using System.Collections.Generic;
using UnityEngine;
using RTSFramework.Resources;
using RTSFramework.Commands;

namespace RTSFramework.Units
{
    public class UnitProducer : MonoBehaviour
    {
        [Header("Production Config")]
        [SerializeField] private List<UnitData> availableUnits = new List<UnitData>();
        [SerializeField] private int maxQueueSize = 5;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform rallyPoint;

        private readonly List<UnitData> trainingQueue = new List<UnitData>();
        private float currentTrainingProgress; // 0 to 1

        private Buildings.Building buildingComponent;

        public event Action OnQueueChanged;
        public event Action<float> OnProgressChanged; // 0 to 1

        public IReadOnlyList<UnitData> TrainingQueue => trainingQueue;
        public List<UnitData> AvailableUnits => availableUnits;
        public float CurrentTrainingProgress => currentTrainingProgress;
        
        public Transform RallyPoint
        {
            get => rallyPoint;
            set => rallyPoint = value;
        }

        private void Awake()
        {
            buildingComponent = GetComponent<Buildings.Building>();
            if (spawnPoint == null) spawnPoint = transform;
            if (rallyPoint == null) rallyPoint = transform;
        }

        private void Update()
        {
            // If this is a building, only train when it's fully constructed
            if (buildingComponent != null && !buildingComponent.IsConstructed)
            {
                currentTrainingProgress = 0f;
                return;
            }

            ProcessQueue();
        }

        private void ProcessQueue()
        {
            if (trainingQueue.Count == 0) return;

            UnitData activeUnit = trainingQueue[0];
            currentTrainingProgress += Time.deltaTime / activeUnit.TrainingTime;
            currentTrainingProgress = Mathf.Clamp01(currentTrainingProgress);

            OnProgressChanged?.Invoke(currentTrainingProgress);

            if (currentTrainingProgress >= 1f)
            {
                SpawnUnit(activeUnit);
                trainingQueue.RemoveAt(0);
                currentTrainingProgress = 0f;
                OnQueueChanged?.Invoke();
                OnProgressChanged?.Invoke(0f);
            }
        }

        public bool QueueUnit(UnitData unitData)
        {
            if (trainingQueue.Count >= maxQueueSize)
            {
                Debug.LogWarning("Training queue is full!");
                return false;
            }

            if (!availableUnits.Contains(unitData))
            {
                Debug.LogWarning($"This producer cannot train {unitData.UnitName}!");
                return false;
            }

            // Deduct resources immediately
            if (ResourceManager.Instance.SpendResources(unitData.Cost))
            {
                trainingQueue.Add(unitData);
                OnQueueChanged?.Invoke();
                Debug.Log($"Queued {unitData.UnitName}. Queue size: {trainingQueue.Count}");
                return true;
            }

            Debug.LogWarning("Insufficient resources to train unit!");
            return false;
        }

        public void CancelUnit(int index)
        {
            if (index < 0 || index >= trainingQueue.Count) return;

            UnitData cancelledUnit = trainingQueue[index];

            // Refund resources
            foreach (var cost in cancelledUnit.Cost)
            {
                ResourceManager.Instance.AddResource(cost.resourceType, cost.amount);
            }

            trainingQueue.RemoveAt(index);

            // If we cancelled the active unit, reset progress
            if (index == 0)
            {
                currentTrainingProgress = 0f;
                OnProgressChanged?.Invoke(0f);
            }

            OnQueueChanged?.Invoke();
            Debug.Log($"Cancelled training of {cancelledUnit.UnitName} at index {index}. Resources refunded.");
        }

        private void SpawnUnit(UnitData unitData)
        {
            if (unitData.UnitPrefab == null) return;

            GameObject unitObj = Instantiate(unitData.UnitPrefab, spawnPoint.position, spawnPoint.rotation);
            unitObj.name = unitData.UnitName;

            // If the unit has a UnitController, send it to the rally point
            var controller = unitObj.GetComponent<UnitController>();
            if (controller != null && rallyPoint != null && rallyPoint != transform)
            {
                controller.GiveCommand(new MoveCommand(rallyPoint.position));
            }
        }
    }
}
