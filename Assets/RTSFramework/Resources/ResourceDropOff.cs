using System.Collections.Generic;
using UnityEngine;

namespace RTSFramework.Resources
{
    public class ResourceDropOff : MonoBehaviour
    {
        [SerializeField] private List<ResourceType> acceptedTypes = new List<ResourceType>();
        
        private static readonly List<ResourceDropOff> allDropOffs = new List<ResourceDropOff>();
        public static IReadOnlyList<ResourceDropOff> AllDropOffs => allDropOffs;

        private void OnEnable()
        {
            if (!allDropOffs.Contains(this))
            {
                allDropOffs.Add(this);
            }
        }

        private void OnDisable()
        {
            allDropOffs.Remove(this);
        }

        public bool Accepts(ResourceType type)
        {
            // If the list is empty, it accepts all resource types by default
            if (acceptedTypes.Count == 0) return true;
            return acceptedTypes.Contains(type);
        }

        public void Deposit(ResourceGatherer gatherer)
        {
            if (gatherer == null || !gatherer.HasCargo) return;
            if (!Accepts(gatherer.CurrentCarriedType)) return;

            ResourceType type = gatherer.CurrentCarriedType;
            int amount = gatherer.DropOff();

            ResourceManager.Instance.AddResource(type, amount);
            Debug.Log($"Deposited {amount} of {type} from {gatherer.gameObject.name} to {gameObject.name}");
        }

        public static ResourceDropOff FindNearest(Vector3 position, ResourceType type)
        {
            ResourceDropOff nearest = null;
            float minDistance = float.MaxValue;

            foreach (var dropOff in allDropOffs)
            {
                if (dropOff == null || !dropOff.Accepts(type)) continue;

                float dist = Vector3.Distance(position, dropOff.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = dropOff;
                }
            }

            return nearest;
        }
    }
}
