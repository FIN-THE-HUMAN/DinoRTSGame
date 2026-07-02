using UnityEngine;
using UnityEngine.AI;

namespace RTSFramework.Commands
{
    public class MoveCommand : Command
    {
        private readonly Vector3 destination;
        private NavMeshAgent agent;

        private float lastYieldCheckTime;
        private const float YIELD_CHECK_INTERVAL = 0.25f;
        private const float YIELD_RADIUS = 1.6f;
        private const float YIELD_TRIGGER_DISTANCE = 3.5f;

        public MoveCommand(Vector3 dest)
        {
            this.destination = dest;
        }

        public override void Execute(GameObject unit)
        {
            agent = unit.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.isStopped = false;
                agent.SetDestination(destination);
                lastYieldCheckTime = Time.time;
            }
            else
            {
                IsFinished = true;
            }
        }

        public override void Update(GameObject unit)
        {
            if (agent == null) return;
            
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

            // Yield/Evade check: nudge blocking friendly idle units near the destination
            if (agent.remainingDistance < YIELD_TRIGGER_DISTANCE && agent.remainingDistance > 0.5f)
            {
                if (Time.time - lastYieldCheckTime > YIELD_CHECK_INTERVAL)
                {
                    lastYieldCheckTime = Time.time;
                    PerformYieldCheck(unit);
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

        private void PerformYieldCheck(GameObject unit)
        {
            var ourController = unit.GetComponent<Units.UnitController>();
            if (ourController == null) return;

            // Perform highly optimized overlap sphere at the destination to find blockages
            Collider[] colliders = Physics.OverlapSphere(destination, YIELD_RADIUS);
            foreach (var col in colliders)
            {
                var otherController = col.GetComponentInParent<Units.UnitController>();
                if (otherController != null && otherController != ourController)
                {
                    // Check if they are friendly and currently idle
                    if (otherController.Faction == ourController.Faction && !otherController.HasActiveCommand)
                    {
                        // Command them to step aside from our destination
                        otherController.EvadeFrom(destination, 1.8f);
                    }
                }
            }
        }
    }
}
