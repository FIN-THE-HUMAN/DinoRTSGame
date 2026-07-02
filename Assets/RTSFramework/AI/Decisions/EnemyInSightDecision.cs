using UnityEngine;

namespace RTSFramework.AI.Decisions
{
    [CreateAssetMenu(fileName = "EnemyInSightDecision", menuName = "RTS/AI/Decisions/Enemy In Sight")]
    public class EnemyInSightDecision : AIDecision
    {
        public override bool Decide(AIController controller)
        {
            if (controller.FindNearestEnemy(out GameObject enemy))
            {
                controller.Target = enemy;
                return true;
            }
            return false;
        }
    }
}
