using UnityEngine;
using UnityEngine.AI;
using RTSFramework.Combat;
using RTSFramework.Units;

namespace RTSFramework.Commands
{
    public class GuardCommand : Command
    {
        private readonly Vector3 guardPosition;
        private NavMeshAgent agent;
        private CombatComponent combat;
        private UnitController ourController;

        private GameObject currentTarget;
        private Health currentTargetHealth;

        private float lastScanTime;
        private const float SCAN_INTERVAL = 0.3f;
        private const float SIGHT_RANGE = 12f;
        private const float LEASH_FROM_GUARD_RANGE = 16f; // Max distance from the guard spot to chase enemies

        public GuardCommand(Vector3 guardPos)
        {
            this.guardPosition = guardPos;
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
            agent.SetDestination(guardPosition);
            lastScanTime = Time.time;
        }

        private bool hasArrivedAtGuardSpot = false;

        public override void Update(GameObject unit)
        {
            if (agent == null || ourController == null)
            {
                IsFinished = true;
                return;
            }

            // Check if we arrived at the guard spot for the first time
            if (!hasArrivedAtGuardSpot)
            {
                if (Vector3.Distance(unit.transform.position, guardPosition) <= 3.0f)
                {
                    hasArrivedAtGuardSpot = true;
                }
            }

            // 1. Scan for enemies periodically (only after arriving at the guard spot)
            if (hasArrivedAtGuardSpot && Time.time - lastScanTime > SCAN_INTERVAL)
            {
                lastScanTime = Time.time;

                // Check if current target is dead, null, or leashed from guard position
                if (currentTarget == null || currentTargetHealth == null || currentTargetHealth.IsDead || 
                    Vector3.Distance(guardPosition, currentTarget.transform.position) > LEASH_FROM_GUARD_RANGE)
                {
                    currentTarget = ScanForEnemy(unit);
                    if (currentTarget != null)
                    {
                        currentTargetHealth = currentTarget.GetComponent<Health>();
                    }
                }
            }

            // 2. Perform combat or guard positioning
            if (currentTarget != null && currentTargetHealth != null && !currentTargetHealth.IsDead)
            {
                // Chase and attack target
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
                // No enemies: return to guard spot
                if (agent.isActiveAndEnabled)
                {
                    if (agent.isStopped)
                    {
                        agent.isStopped = false;
                        agent.SetDestination(guardPosition);
                    }

                    // Check if arrived at guard position
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                    {
                        if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                        {
                            agent.isStopped = true;
                            agent.ResetPath();
                            // Do not set IsFinished = true so the command stays active indefinitely!
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
    }
}
