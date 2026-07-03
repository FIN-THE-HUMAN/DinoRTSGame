using UnityEngine;
using UnityEngine.AI;
using RTSFramework.Buildings;
using RTSFramework.Units;

namespace RTSFramework.Commands
{
    public class BuildCommand : Command
    {
        private readonly Building targetBuilding;
        private NavMeshAgent agent;
        private Collider buildingCollider;
        private const float BUILD_RANGE = 2.5f;

        public BuildCommand(Building target)
        {
            this.targetBuilding = target;
        }

        public override void Execute(GameObject unit)
        {
            agent = unit.GetComponent<NavMeshAgent>();
            if (targetBuilding != null)
            {
                buildingCollider = targetBuilding.GetComponent<Collider>();
            }

            var builder = unit.GetComponent<BuilderComponent>();
            if (agent == null || targetBuilding == null || targetBuilding.IsConstructed || builder == null)
            {
                IsFinished = true;
            }
        }

        public override void Update(GameObject unit)
        {
            var builder = unit.GetComponent<BuilderComponent>();
            if (agent == null || targetBuilding == null || targetBuilding.IsConstructed || builder == null)
            {
                IsFinished = true;
                return;
            }

            Vector3 targetPoint = targetBuilding.transform.position;
            if (buildingCollider != null)
            {
                targetPoint = buildingCollider.ClosestPoint(unit.transform.position);
            }

            // Flatten Y to avoid height differences interfering with range checks
            Vector3 unitPos = unit.transform.position;
            unitPos.y = targetPoint.y;

            float distance = Vector3.Distance(unitPos, targetPoint);

            if (distance > BUILD_RANGE)
            {
                if (agent.isActiveAndEnabled)
                {
                    agent.isStopped = false;
                    agent.SetDestination(targetPoint);
                }
            }
            else
            {
                if (agent.isActiveAndEnabled)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                }

                // Channel construction progress
                targetBuilding.Construct(Time.deltaTime / targetBuilding.BuildingData.ConstructionTime);
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
