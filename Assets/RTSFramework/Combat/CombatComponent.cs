using UnityEngine;

namespace RTSFramework.Combat
{
    public class CombatComponent : MonoBehaviour
    {
        [Header("Weapon Stats")]
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackCooldown = 1.5f;

        [Header("Ranged Setup")]
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private Transform launchPoint;

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
                if (projectilePrefab != null)
                {
                    // Ranged Attack: Launch Projectile
                    Vector3 spawnPos = launchPoint != null ? launchPoint.position : transform.position + Vector3.up * 1f;
                    Quaternion spawnRot = launchPoint != null ? launchPoint.rotation : transform.rotation;

                    GameObject projObj = Instantiate(projectilePrefab.gameObject, spawnPos, spawnRot);
                    Projectile proj = projObj.GetComponent<Projectile>();
                    if (proj != null)
                    {
                        proj.Initialize(target, attackDamage, gameObject);
                    }
                    Debug.Log($"{gameObject.name} fired projectile at {target.name}.");
                }
                else
                {
                    // Melee Attack: Apply Damage Instantly
                    targetHealth.TakeDamage(attackDamage, gameObject);
                    Debug.Log($"{gameObject.name} melee attacked {target.name} for {attackDamage} damage.");
                }
            }
        }
    }
}
