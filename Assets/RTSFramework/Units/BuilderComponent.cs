using System.Collections.Generic;
using UnityEngine;
using RTSFramework.Buildings;

namespace RTSFramework.Units
{
    public class BuilderComponent : MonoBehaviour
    {
        [SerializeField] private List<BuildingData> buildableBuildings = new List<BuildingData>();

        public IReadOnlyList<BuildingData> BuildableBuildings => buildableBuildings;
    }
}
