using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSFramework.Resources
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        private readonly Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();

        public event Action<ResourceType, int> OnResourceChanged; // type, newTotal

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeResources();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeResources()
        {
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                resources[type] = 0;
            }
        }

        public int GetResourceAmount(ResourceType type)
        {
            return resources.TryGetValue(type, out int amount) ? amount : 0;
        }

        public void AddResource(ResourceType type, int amount)
        {
            if (amount <= 0) return;

            if (resources.ContainsKey(type))
            {
                resources[type] += amount;
            }
            else
            {
                resources[type] = amount;
            }

            OnResourceChanged?.Invoke(type, resources[type]);
            Debug.Log($"Added {amount} of {type}. New total: {resources[type]}");
        }

        public bool SpendResource(ResourceType type, int amount)
        {
            if (amount <= 0) return true;

            if (GetResourceAmount(type) >= amount)
            {
                resources[type] -= amount;
                OnResourceChanged?.Invoke(type, resources[type]);
                Debug.Log($"Spent {amount} of {type}. New total: {resources[type]}");
                return true;
            }

            return false;
        }

        public bool HasResources(Dictionary<ResourceType, int> cost)
        {
            if (cost == null) return true;

            foreach (var pair in cost)
            {
                if (GetResourceAmount(pair.Key) < pair.Value)
                {
                    return false;
                }
            }

            return true;
        }

        public bool SpendResources(Dictionary<ResourceType, int> cost)
        {
            if (!HasResources(cost)) return false;

            if (cost != null)
            {
                foreach (var pair in cost)
                {
                    SpendResource(pair.Key, pair.Value);
                }
            }

            return true;
        }

        public bool HasResources(List<Buildings.BuildingCost> cost)
        {
            if (cost == null) return true;
            foreach (var c in cost)
            {
                if (GetResourceAmount(c.resourceType) < c.amount) return false;
            }
            return true;
        }

        public bool SpendResources(List<Buildings.BuildingCost> cost)
        {
            if (!HasResources(cost)) return false;
            foreach (var c in cost)
            {
                SpendResource(c.resourceType, c.amount);
            }
            return true;
        }
    }
}
