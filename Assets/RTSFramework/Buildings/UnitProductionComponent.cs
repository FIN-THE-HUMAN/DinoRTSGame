using System;
using System.Collections.Generic;
using UnityEngine;
using RTSFramework.Units;
using RTSFramework.Resources;
using RTSFramework.Commands;
using UnityEngine.AI;
using RTSFramework.Selection;

namespace RTSFramework.Buildings
{
    [RequireComponent(typeof(Building))]
    public class UnitProductionComponent : MonoBehaviour
    {
        [Header("Production Config")]
        [SerializeField] private List<UnitData> trainableUnits = new List<UnitData>();
        [SerializeField] private int queueMaxSize = 5;

        [Header("Rally Point Config")]
        [SerializeField] private float defaultRallyDistance = 4f;
        [SerializeField] private float spawnOffsetDistance = 1.5f; // close to entrance

        private readonly Queue<UnitData> trainingQueue = new Queue<UnitData>();
        private float trainingProgress; // 0 to 1
        private float currentUnitTimeElapsed;

        private Building building;
        private Vector3 rallyPoint;
        private bool hasCustomRallyPoint;
        private LineRenderer lineRenderer;

        public event Action OnQueueChanged;

        public IReadOnlyList<UnitData> TrainableUnits => trainableUnits;
        public int QueueCount => trainingQueue.Count;
        public float TrainingProgress => trainingProgress;
        public UnitData CurrentActiveUnit => trainingQueue.Count > 0 ? trainingQueue.Peek() : null;
        public IReadOnlyCollection<UnitData> QueuedUnits => trainingQueue;
        public Vector3 RallyPoint => hasCustomRallyPoint ? rallyPoint : GetDefaultRallyPoint();

        private void Awake()
        {
            building = GetComponent<Building>();
        }

        private void Start()
        {
            ResetRallyPoint();
            SetupLineRenderer();
        }

        private void SetupLineRenderer()
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.05f;
            lineRenderer.endWidth = 0.05f;
            lineRenderer.positionCount = 2;
            
            Shader spritesShader = Shader.Find("Sprites/Default");
            if (spritesShader == null) spritesShader = Shader.Find("Unlit/Color");
            if (spritesShader == null) spritesShader = Shader.Find("Standard");

            if (spritesShader != null)
            {
                lineRenderer.material = new Material(spritesShader);
            }
            
            // Fading green vertical beacon
            lineRenderer.startColor = new Color(0.2f, 0.9f, 0.3f, 0.8f);
            lineRenderer.endColor = new Color(0.2f, 0.9f, 0.3f, 0.0f);
            lineRenderer.enabled = false;
        }

        private void Update()
        {
            // Cannot train units if building is not fully constructed yet
            if (building == null || !building.IsConstructed)
            {
                if (lineRenderer != null) lineRenderer.enabled = false;
                return;
            }

            // Toggle rally line visual based on selection
            if (lineRenderer != null)
            {
                bool isSelected = false;
                if (SelectionManager.Instance != null)
                {
                    for (int i = 0; i < SelectionManager.Instance.SelectedObjects.Count; i++)
                    {
                        if (SelectionManager.Instance.SelectedObjects[i] == (ISelectable)building)
                        {
                            isSelected = true;
                            break;
                        }
                    }
                }
                
                lineRenderer.enabled = isSelected;
                if (isSelected)
                {
                    lineRenderer.SetPosition(0, RallyPoint + Vector3.up * 0.05f); // ground level offset
                    lineRenderer.SetPosition(1, RallyPoint + Vector3.up * 2.5f);  // vertical top
                }
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

        public void ResetRallyPoint()
        {
            hasCustomRallyPoint = false;
            rallyPoint = GetDefaultRallyPoint();
        }

        public void SetRallyPoint(Vector3 worldPosition)
        {
            rallyPoint = worldPosition;
            hasCustomRallyPoint = true;
            Debug.Log($"Rally point updated for {gameObject.name} to {worldPosition}");
        }

        public Vector3 GetDefaultRallyPoint()
        {
            return transform.position + transform.forward * defaultRallyDistance;
        }

        public Vector3 GetSpawnPosition()
        {
            // Spawn close to the entrance (forward offset from the building)
            return transform.position + transform.forward * spawnOffsetDistance;
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

            Vector3 spawnPos = GetSpawnPosition();
            
            // Add a small random offset on the NavMesh so they don't instantiate exactly on top of each other
            Vector2 scatterOffset = UnityEngine.Random.insideUnitCircle * 0.5f;
            Vector3 targetSpawnPos = spawnPos + new Vector3(scatterOffset.x, 0f, scatterOffset.y);
            if (NavMesh.SamplePosition(targetSpawnPos, out NavMeshHit spawnHit, 3.0f, NavMesh.AllAreas))
            {
                targetSpawnPos = spawnHit.position;
            }
            else
            {
                targetSpawnPos = spawnPos;
            }

            // Spawn unit
            GameObject spawned = Instantiate(unitData.UnitPrefab, targetSpawnPos, Quaternion.identity);

            // Propagate Faction
            var controller = spawned.GetComponent<UnitController>();
            if (controller != null)
            {
                controller.SetFaction(building.Faction);

                // Give the unit a command to walk to the rally point
                Vector2 walkOffset = UnityEngine.Random.insideUnitCircle * 1.5f;
                Vector3 exitPos = RallyPoint + new Vector3(walkOffset.x, 0f, walkOffset.y);
                if (NavMesh.SamplePosition(exitPos, out NavMeshHit exitHit, 4.0f, NavMesh.AllAreas))
                {
                    exitPos = exitHit.position;
                }
                controller.GiveCommand(new MoveCommand(exitPos), false);
            }

            Debug.Log("Spawned unit: " + unitData.UnitName + " with faction " + (building.Faction != null ? building.Faction.name : "None"));
        }
    }
}
