using UnityEngine;

namespace RTSFramework.Resources
{
    public class ResourceSource : MonoBehaviour
    {
        [SerializeField] private ResourceType resourceType;
        [SerializeField] private int maxAmount = 500;
        [SerializeField] private int currentAmount;

        public ResourceType ResourceType => resourceType;
        public int CurrentAmount => currentAmount;
        public bool IsDepleted => currentAmount <= 0;

        private void Awake()
        {
            currentAmount = maxAmount;
        }

        public int Gather(int amountToGather)
        {
            if (IsDepleted) return 0;

            int gathered = Mathf.Min(amountToGather, currentAmount);
            currentAmount -= gathered;

            if (currentAmount <= 0)
            {
                Deplete();
            }

            return gathered;
        }

        private void Deplete()
        {
            // Trigger visual depletion or destroy the resource node
            Destroy(gameObject);
        }
    }
}
