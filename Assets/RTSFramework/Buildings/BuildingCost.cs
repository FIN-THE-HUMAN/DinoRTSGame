using System;
using RTSFramework.Resources;

namespace RTSFramework.Buildings
{
    [Serializable]
    public struct BuildingCost
    {
        public ResourceType resourceType;
        public int amount;
    }
}
