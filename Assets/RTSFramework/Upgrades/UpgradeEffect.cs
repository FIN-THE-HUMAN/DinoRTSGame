using System;
using UnityEngine;

namespace RTSFramework.Upgrades
{
    public enum UpgradeEffectType
    {
        MaxHealth,
        AttackDamage,
        AttackRange,
        MoveSpeed,
        GatherSpeed
    }

    [Serializable]
    public class UpgradeEffect
    {
        [SerializeField] private UpgradeEffectType effectType;
        [SerializeField] private float value;
        [SerializeField] private bool isPercent;
        [SerializeField] private string targetUnitTag; // e.g. "Raptor", or empty for all units

        public UpgradeEffectType EffectType => effectType;
        public float Value => value;
        public bool IsPercent => isPercent;
        public string TargetUnitTag => targetUnitTag;

        public float Apply(float baseValue)
        {
            if (isPercent)
            {
                return baseValue * (1f + value);
            }
            else
            {
                return baseValue + value;
            }
        }
    }
}
