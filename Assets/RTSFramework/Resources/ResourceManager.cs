using System;
using System.Collections.Generic;
using UnityEngine;
using RTSFramework.Factions;

namespace RTSFramework.Resources
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        private readonly Dictionary<Faction, Dictionary<ResourceType, int>> factionResources = new Dictionary<Faction, Dictionary<ResourceType, int>>();
        private readonly Dictionary<ResourceType, int> defaultPlayerResources = new Dictionary<ResourceType, int>();
        private Faction playerFactionCache;

        public event Action<ResourceType, int> OnResourceChanged; // type, newTotal (invoked for Player HUD)

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
                defaultPlayerResources[type] = 500; // Starting resources
            }
        }

        public Faction GetPlayerFaction()
        {
            if (playerFactionCache != null) return playerFactionCache;

            // Search for player faction in active scene buildings
            var buildings = FindObjectsOfType<Buildings.Building>();
            for (int i = 0; i < buildings.Length; i++)
            {
                if (buildings[i] != null && buildings[i].Faction != null && buildings[i].Faction.IsPlayerFaction)
                {
                    playerFactionCache = buildings[i].Faction;
                    break;
                }
            }

            if (playerFactionCache == null)
            {
                var units = FindObjectsOfType<Units.UnitController>();
                for (int i = 0; i < units.Length; i++)
                {
                    if (units[i] != null && units[i].Faction != null && units[i].Faction.IsPlayerFaction)
                    {
                        playerFactionCache = units[i].Faction;
                        break;
                    }
                }
            }

            // Bind the defaultPlayerResources to the resolved player faction directly
            if (playerFactionCache != null && !factionResources.ContainsKey(playerFactionCache))
            {
                factionResources[playerFactionCache] = defaultPlayerResources;
            }

            return playerFactionCache;
        }

        private Dictionary<ResourceType, int> GetFactionDictionary(Faction faction)
        {
            if (faction == null) faction = GetPlayerFaction();
            if (faction == null)
            {
                return defaultPlayerResources;
            }

            if (!factionResources.ContainsKey(faction))
            {
                var dict = new Dictionary<ResourceType, int>();
                foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
                {
                    dict[type] = 500;
                }
                factionResources[faction] = dict;
            }
            return factionResources[faction];
        }

        // --- NON-FACTION OVERLOADS (Player Faction default) ---
        public int GetResourceAmount(ResourceType type) => GetResourceAmount(null, type);
        public void AddResource(ResourceType type, int amount) => AddResource(null, type, amount);
        public bool SpendResource(ResourceType type, int amount) => SpendResource(null, type, amount);
        public bool HasResources(List<Buildings.BuildingCost> cost) => HasResources(null, cost);
        public bool SpendResources(List<Buildings.BuildingCost> cost) => SpendResources(null, cost);

        // --- FACTION-SPECIFIC API ---
        public int GetResourceAmount(Faction faction, ResourceType type)
        {
            var dict = GetFactionDictionary(faction);
            return dict.TryGetValue(type, out int amount) ? amount : 0;
        }

        public void AddResource(Faction faction, ResourceType type, int amount)
        {
            if (amount <= 0) return;

            var dict = GetFactionDictionary(faction);
            if (dict.ContainsKey(type))
            {
                dict[type] += amount;
            }
            else
            {
                dict[type] = amount;
            }

            // Trigger events if it is the player's faction
            Faction pf = GetPlayerFaction();
            if (faction == null || faction == pf)
            {
                OnResourceChanged?.Invoke(type, dict[type]);
            }
            Debug.Log($"Added {amount} of {type} to faction '{(faction != null ? faction.FactionName : "Player")}'. New total: {dict[type]}");
        }

        public bool SpendResource(Faction faction, ResourceType type, int amount)
        {
            if (amount <= 0) return true;

            var dict = GetFactionDictionary(faction);
            if (dict.TryGetValue(type, out int current) && current >= amount)
            {
                dict[type] = current - amount;
                
                Faction pf = GetPlayerFaction();
                if (faction == null || faction == pf)
                {
                    OnResourceChanged?.Invoke(type, dict[type]);
                }
                Debug.Log($"Spent {amount} of {type} from faction '{(faction != null ? faction.FactionName : "Player")}'. New total: {dict[type]}");
                return true;
            }

            return false;
        }

        public bool HasResources(Faction faction, List<Buildings.BuildingCost> cost)
        {
            if (cost == null) return true;
            foreach (var c in cost)
            {
                if (GetResourceAmount(faction, c.resourceType) < c.amount) return false;
            }
            return true;
        }

        public bool SpendResources(Faction faction, List<Buildings.BuildingCost> cost)
        {
            if (!HasResources(faction, cost)) return false;
            foreach (var c in cost)
            {
                SpendResource(faction, c.resourceType, c.amount);
            }
            return true;
        }
    }
}
