using System.Collections.Generic;
using UnityEngine;
using RTSFramework.Factions;

namespace RTSFramework.Upgrades
{
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        private Dictionary<Faction, HashSet<UpgradeData>> completedUpgrades = new Dictionary<Faction, HashSet<UpgradeData>>();

        public event System.Action<Faction, UpgradeData> OnUpgradeCompleted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeOnLoad()
        {
            if (Instance == null)
            {
                new GameObject("UpgradeManager", typeof(UpgradeManager));
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void CompleteUpgrade(Faction faction, UpgradeData upgrade)
        {
            if (faction == null || upgrade == null) return;

            if (!completedUpgrades.ContainsKey(faction))
            {
                completedUpgrades[faction] = new HashSet<UpgradeData>();
            }

            if (completedUpgrades[faction].Add(upgrade))
            {
                Debug.Log($"UpgradeManager: Faction '{faction.FactionName}' completed research '{upgrade.UpgradeName}'.");
                OnUpgradeCompleted?.Invoke(faction, upgrade);
            }
        }

        public bool IsUpgradeCompleted(Faction faction, UpgradeData upgrade)
        {
            if (faction == null || upgrade == null) return false;
            
            if (completedUpgrades.TryGetValue(faction, out var set))
            {
                return set.Contains(upgrade);
            }
            return false;
        }

        public bool ArePrerequisitesMet(Faction faction, UpgradeData upgrade)
        {
            if (faction == null || upgrade == null) return false;

            foreach (var req in upgrade.Prerequisites)
            {
                if (req == null) continue;
                if (!IsUpgradeCompleted(faction, req))
                {
                    return false;
                }
            }
            return true;
        }

        public float GetModifiedValue(Faction faction, string targetTag, UpgradeEffectType effectType, float baseValue)
        {
            if (faction == null) return baseValue;

            float flatSum = 0f;
            float percentSum = 0f;

            if (completedUpgrades.TryGetValue(faction, out var set))
            {
                foreach (var upgrade in set)
                {
                    if (upgrade == null) continue;
                    foreach (var effect in upgrade.Effects)
                    {
                        if (effect == null) continue;
                        if (effect.EffectType == effectType)
                        {
                            // Target tag check: empty matches all, otherwise matches specific tag (case-insensitive)
                            if (string.IsNullOrEmpty(effect.TargetUnitTag) || 
                                (!string.IsNullOrEmpty(targetTag) && targetTag.Equals(effect.TargetUnitTag, System.StringComparison.OrdinalIgnoreCase)))
                            {
                                if (effect.IsPercent)
                                {
                                    percentSum += effect.Value;
                                }
                                else
                                {
                                    flatSum += effect.Value;
                                }
                            }
                        }
                    }
                }
            }

            return baseValue * (1f + percentSum) + flatSum;
        }
    }
}
