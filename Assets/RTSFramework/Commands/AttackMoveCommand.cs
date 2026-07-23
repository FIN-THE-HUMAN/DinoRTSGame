using UnityEngine;
using UnityEngine.AI;
using RTSFramework.Combat;
using RTSFramework.Units;

namespace RTSFramework.Commands
{
    public class AttackMoveCommand : Command
    {
        private readonly Vector3 destination;
        private NavMeshAgent agent;
        private CombatComponent combat;
        private UnitController ourController;

        private GameObject currentTarget;
        private Health currentTargetHealth;

        private float lastScanTime;
        private const float SCAN_INTERVAL = 0.3f;
        private const float SIGHT_RANGE = 12f;
        private const float LEASH_RANGE = 16f;

        private float lastYieldCheckTime;
        private const float YIELD_CHECK_INTERVAL = 0.25f;
        private const float YIELD_RADIUS = 1.6f;
        private const float YIELD_TRIGGER_DISTANCE = 3.5f;

        public AttackMoveCommand(Vector3 dest)
        {
            this.destination = dest;
        }

        public override void Execute(GameObject unit)
        {
            agent = unit.GetComponent<NavMeshAgent>();
            combat = unit.GetComponent<CombatComponent>();
            ourController = unit.GetComponent<UnitController>();

            if (agent == null)
            {
                IsFinished = true;
                return;
            }

            agent.isStopped = false;
            agent.SetDestination(destination);
            lastScanTime = Time.time;
            lastYieldCheckTime = Time.time;
        }

        public override void Update(GameObject unit)
        {
            if (agent == null || ourController == null)
            {
                IsFinished = true;
                return;
            }

            // 1. Scan for enemies periodically
            if (Time.time - lastScanTime > SCAN_INTERVAL)
            {
                lastScanTime = Time.time;

                if (currentTarget == null || currentTargetHealth == null || currentTargetHealth.IsDead || 
                    Vector3.Distance(unit.transform.position, currentTarget.transform.position) > LEASH_RANGE)
                {
                    currentTarget = ScanForEnemy(unit);
                    if (currentTarget != null)
                    {
                        currentTargetHealth = currentTarget.GetComponent<Health>();
                    }
                }
            }

            // 2. Execute combat or movement
            if (currentTarget != null && currentTargetHealth != null && !currentTargetHealth.IsDead)
            {
                // Attack / Chase target
                float distance = float.MaxValue;
                Collider targetCollider = currentTarget.GetComponent<Collider>();
                if (targetCollider != null)
                {
                    Vector3 closestPoint = targetCollider.ClosestPoint(unit.transform.position);
                    closestPoint.y = unit.transform.position.y;
                    distance = Vector3.Distance(unit.transform.position, closestPoint);
                }
                else
                {
                    distance = Vector3.Distance(unit.transform.position, currentTarget.transform.position);
                }

                if (combat != null && distance <= combat.AttackRange)
                {
                    if (agent.isActiveAndEnabled && !agent.isStopped)
                    {
                        agent.isStopped = true;
                        agent.ResetPath();
                    }

                    Vector3 direction = (currentTarget.transform.position - unit.transform.position).normalized;
                    direction.y = 0;
                    if (direction != Vector3.zero)
                    {
                        unit.transform.rotation = Quaternion.Slerp(
                            unit.transform.rotation,
                            Quaternion.LookRotation(direction),
                            Time.deltaTime * 10f
                        );
                    }

                    if (combat.CanAttack(currentTarget))
                    {
                        combat.Attack(currentTarget);
                    }
                }
                else
                {
                    if (agent.isActiveAndEnabled)
                    {
                        agent.isStopped = false;
                        agent.SetDestination(currentTarget.transform.position);
                    }
                }
            }
            else
            {
                // No target: resume moving to destination
                if (agent.isActiveAndEnabled)
                {
                    if (agent.isStopped)
                    {
                        agent.isStopped = false;
                        agent.SetDestination(destination);
                    }

                    // Check if arrived
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                    {
                        if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                        {
                            agent.isStopped = true;
                            agent.ResetPath();
                            IsFinished = true;
                            return;
                        }
                    }

                    // Yield checks
                    if (agent.remainingDistance < YIELD_TRIGGER_DISTANCE && agent.remainingDistance > 0.5f)
                    {
                        if (Time.time - lastYieldCheckTime > YIELD_CHECK_INTERVAL)
                        {
                            lastYieldCheckTime = Time.time;
                            PerformYieldCheck(unit);
                        }
                    }
                }
            }
        }

        public override void Cancel(GameObject unit)
        {
            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
            IsFinished = true;
        }

        private GameObject ScanForEnemy(GameObject unit)
        {
            GameObject nearest = null;
            float minDistance = float.MaxValue;

            foreach (var selectable in Selection.SelectionManager.AllSelectables)
            {
                if (selectable == null || selectable.Equals(null)) continue;
                if (selectable.GameObject.TryGetComponent<UnitController>(out var otherUnit))
                {
                    if (otherUnit.IsPlayerOwned != ourController.IsPlayerOwned)
                    {
                        float dist = Vector3.Distance(unit.transform.position, otherUnit.transform.position);
                        if (dist < minDistance && dist <= SIGHT_RANGE)
                        {
                            var health = otherUnit.GetComponent<Health>();
                            if (health != null && !health.IsDead)
                            {
                                minDistance = dist;
                                nearest = otherUnit.gameObject;
                            }
                        }
                    }
                }
                else
                {
                    var building = selectable.GameObject.GetComponent<Buildings.Building>();
                    if (building != null && building.IsPlayerOwned != ourController.IsPlayerOwned)
                    {
                        float dist = Vector3.Distance(unit.transform.position, building.transform.position);
                        if (dist < minDistance && dist <= SIGHT_RANGE)
                        {
                            var health = building.GetComponent<Health>();
                            if (health != null && !health.IsDead)
                            {
                                minDistance = dist;
                                nearest = building.gameObject;
                            }
                        }
                    }
                }
            }

            return nearest;
        }

        private void PerformYieldCheck(GameObject unit)
        {
            Collider[] colliders = Physics.OverlapSphere(destination, YIELD_RADIUS);
            foreach (var col in colliders)
            {
                var otherController = col.GetComponentInParent<UnitController>();
                if (otherController != null && otherController != ourController)
                {
                    if (otherController.Faction == ourController.Faction && !otherController.HasActiveCommand)
                    {
                        otherController.EvadeFrom(destination, 1.8f);
                    }
                }
            }
        }
    }
}
