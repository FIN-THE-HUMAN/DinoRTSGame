using UnityEngine;
using UnityEngine.AI;
using RTSFramework.Commands;
using RTSFramework.Resources;
using RTSFramework.Buildings;
using RTSFramework.Combat;

namespace RTSFramework.Units
{
    public class RTSUnitAnimationController : MonoBehaviour
    {
        private Animator animator;
        private NavMeshAgent agent;
        private UnitController unitController;
        private Health health;
        private bool isDead;

        private void Start()
        {
            animator = GetComponentInChildren<Animator>();
            agent = GetComponent<NavMeshAgent>();
            unitController = GetComponent<UnitController>();
            health = GetComponent<Health>();
        }

        private void Update()
        {
            if (isDead || animator == null) return;

            // 1. Check if dead
            if (health != null && health.IsDead)
            {
                TriggerDeath();
                return;
            }

            // 2. Default State: 0 (Idle)
            int newState = 0;

            // 3. Check movement speed
            if (agent != null && agent.velocity.magnitude > 0.15f)
            {
                newState = 1; // Walk
            }
            else if (unitController != null && unitController.CurrentCommand != null)
            {
                Command cmd = unitController.CurrentCommand;

                if (cmd is GatherCommand gatherCmd)
                {
                    // Check targetSource
                    var targetSource = GetField<ResourceSource>(gatherCmd, "targetSource");
                    if (targetSource != null && Vector3.Distance(transform.position, targetSource.transform.position) <= 2.8f)
                    {
                        if (targetSource.ResourceType == ResourceType.Wood)
                        {
                            newState = 2; // Chopping (Wood)
                        }
                        else
                        {
                            newState = 3; // Mining (Gold/Stone)
                        }
                    }
                }
                else if (cmd is BuildCommand buildCmd)
                {
                    // Check targetBuilding
                    var targetBuilding = GetField<Building>(buildCmd, "targetBuilding");
                    if (targetBuilding != null && Vector3.Distance(transform.position, targetBuilding.transform.position) <= 3.5f)
                    {
                        newState = 4; // Hammering
                    }
                }
                else if (cmd is AttackCommand attackCmd)
                {
                    // Check target
                    var target = GetField<GameObject>(attackCmd, "target");
                    if (target != null && Vector3.Distance(transform.position, target.transform.position) <= 3.0f)
                    {
                        newState = 2; // Chopping (used for attack)
                    }
                }
            }

            animator.SetInteger("AnimationState", newState);
        }

        public void TriggerDeathExternal()
        {
            if (isDead) return;
            TriggerDeath();
        }

        private void TriggerDeath()
        {
            isDead = true;
            if (animator != null)
            {
                animator.SetInteger("AnimationState", 5); // Death
            }

            // Disable components so it becomes static and non-interactive
            if (agent != null) agent.enabled = false;
            
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            var sel = GetComponent<UnitController>();
            if (sel != null)
            {
                Selection.SelectionManager.Instance.Deselect(sel);
                sel.enabled = false;
            }

            // Let the game destroy it after the animation completes
            Destroy(gameObject, 2.5f);
        }

        private T GetField<T>(object obj, string fieldName)
        {
            if (obj == null) return default;
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                return (T)field.GetValue(obj);
            }
            return default;
        }
    }
}
