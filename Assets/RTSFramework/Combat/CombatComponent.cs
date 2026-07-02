using UnityEngine;

namespace RTSFramework.Combat
{
    public class CombatComponent : MonoBehaviour
    {
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackCooldown = 1.5f;

        private float nextAttackTime;

        public float AttackRange => attackRange;
        public float AttackDamage => attackDamage;

        public bool CanAttack(GameObject target)
        {
            if (target == null) return false;
            var targetHealth = target.GetComponent<Health>();
            if (targetHealth == null || targetHealth.IsDead) return false;

            return Time.time >= nextAttackTime;
        }

        public void Attack(GameObject target)
        {
            if (!CanAttack(target)) return;

            nextAttackTime = Time.time + attackCooldown;

            var targetHealth = target.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(attackDamage, gameObject);
                
                // Triggers visual attack effects, sounds, or animations here.
                Debug.Log($"{gameObject.name} attacked {target.name} for {attackDamage} damage.");
            }
        }
    }
}
