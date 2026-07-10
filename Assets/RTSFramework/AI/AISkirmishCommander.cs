using UnityEngine;
using System.Collections.Generic;
using RTSFramework.Factions;
using RTSFramework.Units;
using RTSFramework.Buildings;
using RTSFramework.Resources;
using RTSFramework.Commands;
using RTSFramework.Combat;
using UnityEngine.AI;

namespace RTSFramework.AI
{
    public class AISkirmishCommander : MonoBehaviour
    {
        [Header("Faction Config")]
        [SerializeField] private Faction aiFaction;

        [Header("Prefabs/Data Config")]
        [SerializeField] private BuildingData barracksPrefabData;
        [SerializeField] private UnitData workerPrefabData;
        [SerializeField] private UnitData combatUnitPrefabData;

        [Header("AI Logic Settings")]
        [SerializeField] private float evaluationInterval = 1f;
        [SerializeField] private int maxWorkersCount = 6;
        [SerializeField] private int maxCombatUnitsCount = 8;
        [SerializeField] private float buildDistance = 8f;

        private float nextEvaluateTime;

        // Cached Lists
        private readonly List<UnitController> myWorkers = new List<UnitController>();
        private readonly List<UnitController> myCombatUnits = new List<UnitController>();
        private readonly List<Building> myBuildings = new List<Building>();
        private readonly List<Building> myConstructionSites = new List<Building>();

        private void Start()
        {
            if (aiFaction == null)
            {
                Debug.LogError($"AISkirmishCommander: Faction is not assigned on '{gameObject.name}'!");
                enabled = false;
                return;
            }
            
            nextEvaluateTime = Time.time + Random.Range(0f, 1f); // Stagger startup times
        }

        private void Update()
        {
            if (Time.time >= nextEvaluateTime)
            {
                nextEvaluateTime = Time.time + evaluationInterval;
                EvaluateAI();
            }
        }

        private void EvaluateAI()
        {
            AuditAssets();

            // 1. Assign Idle Workers to gather resources
            ManageIdleWorkers();

            // 2. Spend resources to train more workers if below target cap
            ManageWorkerProduction();

            // 3. Spend resources to build barracks if needed and affordable
            ManageBaseBuilding();

            // 4. Spend resources to train combat units if barracks are available
            ManageCombatUnitProduction();

            // 5. Gather combat units and send attack wave if cap is reached
            ManageAttackWaves();
        }

        private void AuditAssets()
        {
            myWorkers.Clear();
            myCombatUnits.Clear();
            myBuildings.Clear();
            myConstructionSites.Clear();

            // Scan units
            var units = FindObjectsOfType<UnitController>();
            foreach (var unit in units)
            {
                if (unit == null || unit.Faction != aiFaction) continue;

                // Identify workers via ResourceGatherer component
                if (unit.GetComponent<ResourceGatherer>() != null)
                {
                    myWorkers.Add(unit);
                }
                else
                {
                    myCombatUnits.Add(unit);
                }
            }

            // Scan buildings
            var buildings = FindObjectsOfType<Building>();
            foreach (var building in buildings)
            {
                if (building == null || building.Faction != aiFaction) continue;

                if (!building.IsConstructed)
                {
                    myConstructionSites.Add(building);
                }
                else
                {
                    myBuildings.Add(building);
                }
            }
        }

        private void ManageIdleWorkers()
        {
            // Gather all resource sources
            var sources = FindObjectsOfType<ResourceSource>();
            if (sources.Length == 0) return;

            foreach (var worker in myWorkers)
            {
                if (worker == null || worker.HasActiveCommand) continue;

                // Find closest non-depleted resource source
                ResourceSource closestSource = null;
                float minDist = float.MaxValue;
                foreach (var source in sources)
                {
                    if (source == null || source.IsDepleted) continue;

                    float d = Vector3.Distance(worker.transform.position, source.transform.position);
                    if (d < minDist)
                    {
                        minDist = d;
                        closestSource = source;
                    }
                }

                if (closestSource != null)
                {
                    worker.GiveCommand(new GatherCommand(closestSource), false);
                    Debug.Log($"AI Commander: Sent worker '{worker.gameObject.name}' to gather '{closestSource.gameObject.name}'.");
                }
            }
        }

        private void ManageWorkerProduction()
        {
            if (myWorkers.Count >= maxWorkersCount || workerPrefabData == null) return;

            // Find Town Hall (building that produces units and accepts drop-offs)
            Building townHall = null;
            foreach (var b in myBuildings)
            {
                if (b != null && b.GetComponent<UnitProductionComponent>() != null && b.GetComponent<ResourceDropOff>() != null)
                {
                    townHall = b;
                    break;
                }
            }

            if (townHall != null)
            {
                var prod = townHall.GetComponent<UnitProductionComponent>();
                if (prod != null && prod.QueueCount < 2)
                {
                    // Check resources and try queue
                    prod.TryQueueUnit(workerPrefabData);
                }
            }
        }

        private void ManageBaseBuilding()
        {
            if (barracksPrefabData == null) return;

            // Check if we already have a Barracks or are building one
            bool hasBarracks = false;
            foreach (var b in myBuildings)
            {
                if (b != null && b.BuildingData == barracksPrefabData)
                {
                    hasBarracks = true;
                    break;
                }
            }
            foreach (var b in myConstructionSites)
            {
                if (b != null && b.BuildingData == barracksPrefabData)
                {
                    hasBarracks = true;
                    break;
                }
            }

            if (hasBarracks) return;

            // Do we have enough resources to build a Barracks?
            if (!ResourceManager.Instance.HasResources(aiFaction, barracksPrefabData.Cost)) return;

            // Find Town Hall to build near it
            Building townHall = null;
            foreach (var b in myBuildings)
            {
                if (b != null && b.GetComponent<ResourceDropOff>() != null)
                {
                    townHall = b;
                    break;
                }
            }

            Vector3 basePos = townHall != null ? townHall.transform.position : transform.position;

            // Find a build position on NavMesh near the basePos
            Vector3 targetPos = basePos + new Vector3(Random.Range(-buildDistance, buildDistance), 0f, Random.Range(-buildDistance, buildDistance));
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                targetPos = hit.position;
            }

            // Place building foundation
            Building barracks = BuildingSystem.Instance.PlaceBuildingForFaction(barracksPrefabData, targetPos, aiFaction);
            if (barracks != null)
            {
                Debug.Log($"AI Commander: Programmatically placed Barracks foundation at {targetPos}.");

                // Command a worker to build it
                UnitController builder = GetLeastBusyWorker();
                if (builder != null)
                {
                    builder.GiveCommand(new BuildCommand(barracks), false);
                    Debug.Log($"AI Commander: Ordered worker '{builder.gameObject.name}' to construct Barracks.");
                }
            }
        }

        private void ManageCombatUnitProduction()
        {
            if (myCombatUnits.Count >= maxCombatUnitsCount || combatUnitPrefabData == null) return;

            // Find Barracks
            Building barracks = null;
            foreach (var b in myBuildings)
            {
                if (b != null && b.BuildingData == barracksPrefabData)
                {
                    barracks = b;
                    break;
                }
            }

            if (barracks != null)
            {
                var prod = barracks.GetComponent<UnitProductionComponent>();
                if (prod != null && prod.QueueCount < 2)
                {
                    prod.TryQueueUnit(combatUnitPrefabData);
                }
            }
        }

        private void ManageAttackWaves()
        {
            if (myCombatUnits.Count < maxCombatUnitsCount) return;

            // Find target (any player building or unit)
            GameObject playerTarget = FindPlayerTarget();
            if (playerTarget == null) return;

            Debug.Log($"AI Commander: Launching attack wave with {myCombatUnits.Count} units against '{playerTarget.name}'!");

            foreach (var combatUnit in myCombatUnits)
            {
                if (combatUnit != null)
                {
                    combatUnit.GiveCommand(new AttackCommand(playerTarget), false);
                }
            }
        }

        private UnitController GetLeastBusyWorker()
        {
            if (myWorkers.Count == 0) return null;

            // Prefer idle workers, then gathering workers
            foreach (var w in myWorkers)
            {
                if (w != null && !w.HasActiveCommand) return w;
            }
            return myWorkers[0];
        }

        private GameObject FindPlayerTarget()
        {
            // 1. Try to find a player building
            var buildings = FindObjectsOfType<Building>();
            foreach (var b in buildings)
            {
                if (b != null && b.Faction != null && b.Faction.IsPlayerFaction)
                {
                    var health = b.GetComponent<Health>();
                    if (health != null && !health.IsDead)
                    {
                        return b.gameObject;
                    }
                }
            }

            // 2. Try to find a player unit
            var units = FindObjectsOfType<UnitController>();
            foreach (var u in units)
            {
                if (u != null && u.Faction != null && u.Faction.IsPlayerFaction)
                {
                    var health = u.GetComponent<Health>();
                    if (health != null && !health.IsDead)
                    {
                        return u.gameObject;
                    }
                }
            }

            return null;
        }
    }
}
