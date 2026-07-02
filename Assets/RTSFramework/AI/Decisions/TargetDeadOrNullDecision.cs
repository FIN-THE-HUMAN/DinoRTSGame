using UnityEngine;
using RTSFramework.Combat;

namespace RTSFramework.AI.Decisions
{
    [CreateAssetMenu(fileName = "TargetDeadOrNullDecision", menuName = "RTS/AI/Decisions/Target Dead Or Null")]
    public class TargetDeadOrNullDecision : AIDecision
    {
        public override bool Decide(AIController controller)
        {
            if (controller.Target == null) return true;

            var health = controller.Target.GetComponent<Health>();
            if (health == null || health.IsDead)
            {
                controller.Target = null;
                controller.UnitController.CancelAllCommands();
                return true;
            }

            // Check if the target has escaped beyond sight range (with a 20% buffer to prevent state flickering)
            float dist = Vector3.Distance(controller.transform.position, controller.Target.transform.position);
            if (dist > controller.SightRange * 1.2f)
            {
                controller.Target = null;
                controller.UnitController.CancelAllCommands();
                return true;
            }

            return false;
        }
    }
}
