using UnityEngine;
using RTSFramework.Commands;

namespace RTSFramework.AI.Actions
{
    [CreateAssetMenu(fileName = "ChaseAndAttackAction", menuName = "RTS/AI/Actions/Chase And Attack")]
    public class ChaseAndAttackAction : AIAction
    {
        public override void Act(AIController controller)
        {
            if (controller.UnitController == null || controller.Target == null) return;

            // If the unit is already executing a command, let it proceed.
            // The AttackCommand will automatically handle chasing and attacking.
            if (controller.UnitController.HasActiveCommand) return;

            controller.UnitController.GiveCommand(new AttackCommand(controller.Target));
        }
    }
}
