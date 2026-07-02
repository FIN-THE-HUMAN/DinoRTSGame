using UnityEngine;

namespace RTSFramework.Resources
{
    public class ResourceGatherer : MonoBehaviour
    {
        [SerializeField] private int capacity = 10;
        [SerializeField] private float gatherRate = 1.5f; // Time between gathers
        [SerializeField] private int gatherAmount = 2; // Amount gathered per tick

        private ResourceType currentCarriedType;
        private int currentCarriedAmount;
        private float nextGatherTime;

        public int Capacity => capacity;
        public float GatherRate => gatherRate;
        public int GatherAmount => gatherAmount;

        public ResourceType CurrentCarriedType => currentCarriedType;
        public int CurrentCarriedAmount => currentCarriedAmount;
        public bool IsFull => currentCarriedAmount >= capacity;
        public bool HasCargo => currentCarriedAmount > 0;

        public bool CanGather(ResourceSource source)
        {
            if (source == null || source.IsDepleted) return false;
            if (IsFull) return false;

            // If carrying something else, must drop off first
            if (HasCargo && currentCarriedType != source.ResourceType) return false;

            return Time.time >= nextGatherTime;
        }

        public void Gather(ResourceSource source)
        {
            if (!CanGather(source)) return;

            nextGatherTime = Time.time + gatherRate;

            int spaceLeft = capacity - currentCarriedAmount;
            int toGather = Mathf.Min(gatherAmount, spaceLeft);
            int gathered = source.Gather(toGather);

            if (gathered > 0)
            {
                currentCarriedType = source.ResourceType;
                currentCarriedAmount += gathered;
                Debug.Log($"{gameObject.name} gathered {gathered} of {currentCarriedType}. Carried: {currentCarriedAmount}/{capacity}");
            }
        }

        public int DropOff()
        {
            int amount = currentCarriedAmount;
            currentCarriedAmount = 0;
            return amount;
        }
    }
}
