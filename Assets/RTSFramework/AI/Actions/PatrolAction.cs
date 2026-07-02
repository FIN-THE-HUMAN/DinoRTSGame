using UnityEngine;
using RTSFramework.Commands;

namespace RTSFramework.AI.Actions
{
    [CreateAssetMenu(fileName = "PatrolAction", menuName = "RTS/AI/Actions/Patrol")]
    public class PatrolAction : AIAction
    {
        public override void Act(AIController controller)
        {
            if (controller.UnitController == null) return;

            // If the unit is already executing a move command, let it finish
            if (controller.UnitController.HasActiveCommand) return;

            if (controller.PatrolWaypoints.Count == 0)
            {
                // No waypoints: return to spawn position
                float dist = Vector3.Distance(controller.transform.position, controller.SpawnPosition);
                if (dist > 2f)
                {
                    controller.UnitController.GiveCommand(new MoveCommand(controller.SpawnPosition));
                }
                return;
            }

            // Patrol waypoints logic
            Transform targetWaypoint = controller.PatrolWaypoints[controller.CurrentWaypointIndex];
            
            // Move to the current waypoint
            controller.UnitController.GiveCommand(new MoveCommand(targetWaypoint.position));

            // Cycle waypoints for the next check
            controller.CurrentWaypointIndex = (controller.CurrentWaypointIndex + 1) % controller.PatrolWaypoints.Count;
        }
    }
}
