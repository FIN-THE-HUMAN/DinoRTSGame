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
        public float MaxHealth => finalMaxHealth > 0f ? finalMaxHealth : maxHealth;
        public bool IsDead => currentHealth <= 0f;
        public RTSFramework.Factions.Faction Faction => faction;

        private float baseMaxHealth;
        private float finalMaxHealth;
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
            if (baseMaxHealth == 0f)
            {
                baseMaxHealth = maxHealth;
            }
            if (finalMaxHealth == 0f)
            {
                finalMaxHealth = baseMaxHealth;
            }

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
            if (Upgrades.UpgradeManager.HasInstance)
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
            float finalMax = Upgrades.UpgradeManager.Instance.GetModifiedValue(faction, targetTag, Upgrades.UpgradeEffectType.MaxHealth, baseMaxHealth);

            // Scale if it's a building under construction
            var bComp = GetComponent<Buildings.Building>();
            if (bComp != null && !bComp.IsConstructed)
            {
                float startingMaxHealth = Mathf.Min(10f, finalMax);
                maxHealth = startingMaxHealth + (finalMax - startingMaxHealth) * bComp.ConstructionProgress;
            }
            else
            {
                maxHealth = finalMax;
            }

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
            
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);
            OnDamageTaken?.Invoke(actualDamage, attacker);

            if (Audio.RTSAudioManager.Instance != null)
            {
                bool eitherIsPlayer = (faction != null && faction.IsPlayerFaction);
                if (!eitherIsPlayer && attacker != null)
                {
                    var attHealth = attacker.GetComponent<Health>();
                    if (attHealth != null && attHealth.Faction != null && attHealth.Faction.IsPlayerFaction)
                    {
                        eitherIsPlayer = true;
                    }
                }

                if (eitherIsPlayer)
                {
                    Audio.RTSAudioManager.Instance.NotifyCombatEvent();
                }
            }

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        }

        public void SetConstructionHealth(float currentMax, float currentVal)
        {
            this.maxHealth = currentMax;
            this.currentHealth = Mathf.Clamp(currentVal, 0f, currentMax);
            OnHealthChanged?.Invoke(this.currentHealth, MaxHealth);
        }

        public void SetBaseMaxHealth(float baseMax)
        {
            this.baseMaxHealth = baseMax;
            this.finalMaxHealth = baseMax;
        }

        private void Die()
        {
            if (Audio.RTSAudioManager.Instance != null)
            {
                bool isUnit = GetComponent<Units.UnitController>() != null;
                Audio.RTSAudioManager.Instance.PlayDeathSound(transform.position, isUnit);
            }

            OnDeath?.Invoke();
            Destroy(gameObject);
        }
    }
}
