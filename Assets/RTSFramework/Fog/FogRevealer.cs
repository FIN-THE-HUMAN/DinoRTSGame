using UnityEngine;

namespace RTSFramework.Fog
{
    public class FogRevealer : MonoBehaviour
    {
        [Header("Sight Settings")]
        [SerializeField] private float sightRange = 10f;

        public float SightRange
        {
            get => sightRange;
            set => sightRange = value;
        }

        private void OnEnable()
        {
            if (FogOfWarManager.Instance != null)
            {
                FogOfWarManager.Instance.RegisterRevealer(this);
            }
        }

        private void Start()
        {
            if (FogOfWarManager.Instance != null)
            {
                FogOfWarManager.Instance.RegisterRevealer(this);
            }
        }

        private void OnDisable()
        {
            if (FogOfWarManager.Instance != null)
            {
                FogOfWarManager.Instance.UnregisterRevealer(this);
            }
        }
    }
}
