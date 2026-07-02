using UnityEngine;
using UnityEngine.AI;
using RTSFramework.Combat;

namespace RTSFramework.Commands
{
    public class AttackCommand : Command
    {
        private readonly GameObject target;
        private Health targetHealth;
        private NavMeshAgent agent;
        private CombatComponent combat;

        public AttackCommand(GameObject target)
        {
            this.target = target;
            if (target != null)
            {
                this.targetHealth = target.GetComponent<Health>();
            }
        }

        public override void Execute(GameObject unit)
        {
            agent = unit.GetComponent<NavMeshAgent>();
            combat = unit.GetComponent<CombatComponent>();

            if (target == null || targetHealth == null || targetHealth.IsDead || combat == null)
            {
                IsFinished = true;
                return;
            }
        }

        public override void Update(GameObject unit)
        {
            if (target == null || targetHealth == null || targetHealth.IsDead || combat == null || agent == null)
            {
                IsFinished = true;
                if (agent != null && agent.isActiveAndEnabled)
                {
                    agent.ResetPath();
                }
                return;
            }

            float distance = Vector3.Distance(unit.transform.position, target.transform.position);

            if (distance <= combat.AttackRange)
            {
                // In range: stop moving and attack
                if (agent.isActiveAndEnabled && !agent.isStopped)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                }

                // Smoothly rotate to face the target
                Vector3 direction = (target.transform.position - unit.transform.position).normalized;
                direction.y = 0;
                if (direction != Vector3.zero)
                {
                    unit.transform.rotation = Quaternion.Slerp(
                        unit.transform.rotation, 
                        Quaternion.LookRotation(direction), 
                        Time.deltaTime * 10f
                    );
                }

                if (combat.CanAttack(target))
                {
                    combat.Attack(target);
                }
            }
            else
            {
                // Out of range: chase the target
                if (agent.isActiveAndEnabled)
                {
                    agent.isStopped = false;
                    agent.SetDestination(target.transform.position);
                }
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
