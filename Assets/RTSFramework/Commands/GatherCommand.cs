using UnityEngine;
using UnityEngine.AI;
using RTSFramework.Resources;

namespace RTSFramework.Commands
{
    public class GatherCommand : Command
    {
        private readonly ResourceSource targetSource;
        private NavMeshAgent agent;
        private ResourceGatherer gatherer;

        private ResourceDropOff currentDropOff;
        private bool isReturningToDropOff;

        public GatherCommand(ResourceSource source)
        {
            this.targetSource = source;
        }

        public override void Execute(GameObject unit)
        {
            agent = unit.GetComponent<NavMeshAgent>();
            gatherer = unit.GetComponent<ResourceGatherer>();

            if (targetSource == null || targetSource.IsDepleted || gatherer == null)
            {
                IsFinished = true;
                return;
            }

            isReturningToDropOff = gatherer.IsFull;
        }

        public override void Update(GameObject unit)
        {
            if (gatherer == null || agent == null)
            {
                IsFinished = true;
                return;
            }

            if (isReturningToDropOff)
            {
                // Find or update nearest drop-off
                if (currentDropOff == null)
                {
                    currentDropOff = ResourceDropOff.FindNearest(unit.transform.position, gatherer.CurrentCarriedType);
                }

                if (currentDropOff == null)
                {
                    Debug.LogWarning("No drop-off point found!");
                    IsFinished = true;
                    if (agent.isActiveAndEnabled) agent.ResetPath();
                    return;
                }

                float distToDropOff = Vector3.Distance(unit.transform.position, currentDropOff.transform.position);
                
                if (distToDropOff <= 2.5f) // Adjust range as needed
                {
                    // Arrived at drop-off
                    if (agent.isActiveAndEnabled && !agent.isStopped)
                    {
                        agent.isStopped = true;
                        agent.ResetPath();
                    }

                    currentDropOff.Deposit(gatherer);
                    isReturningToDropOff = false;
                    currentDropOff = null;
                }
                else
                {
                    // Move to drop-off
                    if (agent.isActiveAndEnabled)
                    {
                        agent.isStopped = false;
                        agent.SetDestination(currentDropOff.transform.position);
                    }
                }
            }
            else
            {
                // Gathering state
                if (targetSource == null || targetSource.IsDepleted)
                {
                    // Resource is depleted! If we have cargo, drop it off. Otherwise, finish.
                    if (gatherer.HasCargo)
                    {
                        isReturningToDropOff = true;
                    }
                    else
                    {
                        IsFinished = true;
                        if (agent.isActiveAndEnabled) agent.ResetPath();
                    }
                    return;
                }

                float distToSource = Vector3.Distance(unit.transform.position, targetSource.transform.position);

                if (distToSource <= 2.5f) // Adjust range as needed
                {
                    // Arrived at resource
                    if (agent.isActiveAndEnabled && !agent.isStopped)
                    {
                        agent.isStopped = true;
                        agent.ResetPath();
                    }

                    // Rotate towards resource
                    Vector3 direction = (targetSource.transform.position - unit.transform.position).normalized;
                    direction.y = 0;
                    if (direction != Vector3.zero)
                    {
                        unit.transform.rotation = Quaternion.Slerp(
                            unit.transform.rotation, 
                            Quaternion.LookRotation(direction), 
                            Time.deltaTime * 10f
                        );
                    }

                    if (gatherer.CanGather(targetSource))
                    {
                        gatherer.Gather(targetSource);
                    }

                    if (gatherer.IsFull)
                    {
                        isReturningToDropOff = true;
                    }
                }
                else
                {
                    // Move to resource
                    if (agent.isActiveAndEnabled)
                    {
                        agent.isStopped = false;
                        agent.SetDestination(targetSource.transform.position);
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
    }
}
