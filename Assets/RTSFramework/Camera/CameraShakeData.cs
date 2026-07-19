using UnityEngine;

namespace RTSFramework.CameraSystem
{
    [CreateAssetMenu(fileName = "NewCameraShakeData", menuName = "RTS/Camera Shake Data")]
    public class CameraShakeData : ScriptableObject
    {
        [Header("Shake Configuration")]
        [SerializeField] private float intensity = 0.5f;
        [SerializeField] private float duration = 0.4f;
        [SerializeField] private float shakeRadius = 20f;

        public float Intensity => intensity;
        public float Duration => duration;
        public float ShakeRadius => shakeRadius;
    }
}
