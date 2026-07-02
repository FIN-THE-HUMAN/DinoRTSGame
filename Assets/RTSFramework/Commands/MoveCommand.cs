using UnityEngine;
using UnityEngine.AI;

namespace RTSFramework.Commands
{
    public class MoveCommand : Command
    {
        private readonly Vector3 destination;
        private NavMeshAgent agent;

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
                    IsFinished = true;
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
    }
}
