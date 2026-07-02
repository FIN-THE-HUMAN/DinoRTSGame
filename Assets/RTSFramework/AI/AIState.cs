using System.Collections.Generic;
using UnityEngine;

namespace RTSFramework.AI
{
    [CreateAssetMenu(fileName = "New AI State", menuName = "RTS/AI/State")]
    public class AIState : ScriptableObject
    {
        [SerializeField] private List<AIAction> actions = new List<AIAction>();
        [SerializeField] private List<AITransition> transitions = new List<AITransition>();

        public void UpdateState(AIController controller)
        {
            ExecuteActions(controller);
            CheckTransitions(controller);
        }

        private void ExecuteActions(AIController controller)
        {
            foreach (var action in actions)
            {
                if (action != null) action.Act(controller);
            }
        }

        private void CheckTransitions(AIController controller)
        {
            foreach (var transition in transitions)
            {
                if (transition.decision == null) continue;

                bool decisionSucceeded = transition.decision.Decide(controller);
                if (decisionSucceeded)
                {
                    if (transition.trueState != null && transition.trueState != this)
                    {
                        controller.TransitionToState(transition.trueState);
                        break;
                    }
                }
                else
                {
                    if (transition.falseState != null && transition.falseState != this)
                    {
                        controller.TransitionToState(transition.falseState);
                        break;
                    }
                }
            }
        }
    }
}
