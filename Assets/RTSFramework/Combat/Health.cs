using System;
using UnityEngine;

namespace RTSFramework.Combat
{
    public class Health : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        public event Action<float, float> OnHealthChanged; // current, max
        public event Action OnDeath;
        public event Action<float, GameObject> OnDamageTaken; // damage, attacker

        // Hook for game-specific damage calculations (e.g. armor/shields in the Game Layer)
        public Func<float, GameObject, float> OnCalculateDamage;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => currentHealth <= 0f;

        private void Awake()
        {
            currentHealth = maxHealth;

            // Dynamically attach world space health bars to all entities
            if (gameObject.GetComponent<WorldSpaceUIController>() == null)
            {
                gameObject.AddComponent<WorldSpaceUIController>();
            }
        }

        public void TakeDamage(float rawDamage, GameObject attacker)
        {
            if (IsDead) return;

            float actualDamage = rawDamage;
            if (OnCalculateDamage != null)
            {
                actualDamage = OnCalculateDamage.Invoke(rawDamage, attacker);
            }

            currentHealth = Mathf.Max(0f, currentHealth - actualDamage);
            
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnDamageTaken?.Invoke(actualDamage, attacker);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Die()
        {
            OnDeath?.Invoke();
            Destroy(gameObject);
        }
    }
}
