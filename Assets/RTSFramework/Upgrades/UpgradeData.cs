using System.Collections.Generic;
using UnityEngine;
using RTSFramework.Buildings;

namespace RTSFramework.Upgrades
{
    [CreateAssetMenu(fileName = "New Upgrade Data", menuName = "RTS/Upgrade Data")]
    public class UpgradeData : ScriptableObject
    {
        [SerializeField] private string upgradeId;
        [SerializeField] private string upgradeName;
        [SerializeField] private float researchTime = 10f;
        [SerializeField] private List<BuildingCost> cost = new List<BuildingCost>();
        [SerializeField] private Sprite icon;
        
        [Header("Prerequisites & Effects")]
        [SerializeField] private List<UpgradeData> prerequisites = new List<UpgradeData>();
        [SerializeField] private List<UpgradeEffect> effects = new List<UpgradeEffect>();

        public string UpgradeId => upgradeId;
        public string UpgradeName => upgradeName;
        public float ResearchTime => researchTime;
        public List<BuildingCost> Cost => cost;
        public Sprite Icon => icon;
        public IReadOnlyList<UpgradeData> Prerequisites => prerequisites;
        public IReadOnlyList<UpgradeEffect> Effects => effects;
    }
}
