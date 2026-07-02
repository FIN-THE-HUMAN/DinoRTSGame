using UnityEngine;

namespace RTSFramework.AI
{
    public abstract class AIAction : ScriptableObject
    {
        public abstract void Act(AIController controller);
    }
}
