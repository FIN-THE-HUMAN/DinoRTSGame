using System.Collections.Generic;
using UnityEngine;
using RTSFramework.Buildings;

namespace RTSFramework.Units
{
    public class BuilderComponent : MonoBehaviour
    {
        [SerializeField] private List<BuildingData> buildableBuildings = new List<BuildingData>();

        public IReadOnlyList<BuildingData> BuildableBuildings => buildableBuildings;

        private void Start()
        {
            // Dynamically load all valid buildable buildings from Resources
            // This prevents prefab overrides on pre-placed scene workers from breaking their capability list!
            buildableBuildings.Clear();

            var tower = UnityEngine.Resources.Load<BuildingData>("Buildings/TowerData");
            var artTower = UnityEngine.Resources.Load<BuildingData>("Buildings/ArtilleryTowerData");
            var townHall = UnityEngine.Resources.Load<BuildingData>("Buildings/TownHallData");

            if (tower != null) buildableBuildings.Add(tower);
            if (artTower != null) buildableBuildings.Add(artTower);
            if (townHall != null) buildableBuildings.Add(townHall);
        }
    }
}
