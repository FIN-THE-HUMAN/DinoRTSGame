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

        private float baseMaxHealth;
        private RTSFramework.Factions.Faction faction;

        private void Awake()
        {
            currentHealth = maxHealth;

            // Dynamically attach world space health bars to all entities
            if (gameObject.GetComponent<WorldSpaceUIController>() == null)
            {
                gameObject.AddComponent<WorldSpaceUIController>();
            }
        }

        private void Start()
        {
            baseMaxHealth = maxHealth;

            // Resolve Faction
            var unit = GetComponent<Units.UnitController>();
            if (unit != null)
            {
                faction = unit.Faction;
            }
            else
            {
                var building = GetComponent<Buildings.Building>();
                if (building != null)
                {
                    faction = building.Faction;
                }
            }

            if (Upgrades.UpgradeManager.Instance != null)
            {
                Upgrades.UpgradeManager.Instance.OnUpgradeCompleted += HandleUpgradeCompleted;
                RecalculateMaxHealth();
            }
        }

        private void OnDestroy()
        {
            if (Upgrades.UpgradeManager.Instance != null)
            {
                Upgrades.UpgradeManager.Instance.OnUpgradeCompleted -= HandleUpgradeCompleted;
            }
        }

        private void HandleUpgradeCompleted(RTSFramework.Factions.Faction upgradeFaction, Upgrades.UpgradeData upgrade)
        {
            if (upgradeFaction == faction)
            {
                RecalculateMaxHealth();
            }
        }

        private void RecalculateMaxHealth()
        {
            if (faction == null) return;

            string targetTag = "";
            var unit = GetComponent<Units.UnitController>();
            if (unit != null && unit.UnitData != null)
            {
                targetTag = unit.UnitData.UnitName;
            }
            else
            {
                var building = GetComponent<Buildings.Building>();
                if (building != null && building.BuildingData != null)
                {
                    targetTag = building.BuildingData.BuildingName;
                }
            }

            float oldMax = maxHealth;
            maxHealth = Upgrades.UpgradeManager.Instance.GetModifiedValue(faction, targetTag, Upgrades.UpgradeEffectType.MaxHealth, baseMaxHealth);

            if (maxHealth != oldMax)
            {
                float diff = maxHealth - oldMax;
                currentHealth = Mathf.Max(1f, currentHealth + diff);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
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
