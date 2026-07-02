using UnityEngine;

namespace RTSFramework.AI
{
    public abstract class AIDecision : ScriptableObject
    {
        public abstract bool Decide(AIController controller);
    }
}
