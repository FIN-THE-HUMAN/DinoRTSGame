using System;

namespace RTSFramework.AI
{
    [Serializable]
    public struct AITransition
    {
        public AIDecision decision;
        public AIState trueState;
        public AIState falseState;
    }
}
