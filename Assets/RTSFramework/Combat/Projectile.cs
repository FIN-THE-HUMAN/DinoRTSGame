using UnityEngine;

namespace RTSFramework.Combat
{
    public class Projectile : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float speed = 15f;
        [SerializeField] private float curveArc = 0f; // Height of arc multiplier (0 = straight line)

        [Header("Effects")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private AudioClip hitSound;

        private GameObject target;
        private float damage;
        private GameObject attacker;

        private Vector3 startPosition;
        private Vector3 targetLastPosition;
        private float age;
        private float travelDuration;
        private float initialDistance;
        private bool isInitialized;

        public void Initialize(GameObject target, float damage, GameObject attacker)
        {
            this.target = target;
            this.damage = damage;
            this.attacker = attacker;

            startPosition = transform.position;
            
            // Set initial target center
            if (target != null)
            {
                var col = target.GetComponent<Collider>();
                targetLastPosition = col != null ? col.bounds.center : target.transform.position;
            }
            else
            {
                targetLastPosition = startPosition + transform.forward * 10f;
            }
            
            initialDistance = Vector3.Distance(startPosition, targetLastPosition);
            float dist = Mathf.Max(0.1f, initialDistance);
            travelDuration = dist / speed;
            age = 0f;
            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized) return;

            age += Time.deltaTime;
            float progress = travelDuration > 0f ? Mathf.Clamp01(age / travelDuration) : 1f;

            // Track current target position if target is still active
            Vector3 currentTargetPos = targetLastPosition;
            if (target != null && !target.Equals(null))
            {
                var col = target.GetComponent<Collider>();
                currentTargetPos = col != null ? col.bounds.center : target.transform.position;
                targetLastPosition = currentTargetPos;
            }

            Vector3 currentPos;
            if (curveArc > 0f)
            {
                // Parabolic arc path
                Vector3 horizontalPos = Vector3.Lerp(startPosition, currentTargetPos, progress);
                float arcHeight = Mathf.Sin(progress * Mathf.PI) * curveArc * (initialDistance * 0.2f);
                currentPos = horizontalPos + Vector3.up * arcHeight;
                
                // Aim matching motion direction
                if (progress < 1f)
                {
                    Vector3 nextPos = Vector3.Lerp(startPosition, currentTargetPos, progress + 0.01f);
                    float nextArcHeight = Mathf.Sin((progress + 0.01f) * Mathf.PI) * curveArc * (initialDistance * 0.2f);
                    Vector3 nextWorldPos = nextPos + Vector3.up * nextArcHeight;
                    transform.rotation = Quaternion.LookRotation(nextWorldPos - currentPos);
                }
            }
            else
            {
                // Direct linear path
                currentPos = Vector3.Lerp(startPosition, currentTargetPos, progress);
                if (currentTargetPos != startPosition)
                {
                    transform.rotation = Quaternion.LookRotation(currentTargetPos - startPosition);
                }
            }

            transform.position = currentPos;

            if (progress >= 1f)
            {
                OnHit();
            }
        }

        private void OnHit()
        {
            if (target != null && !target.Equals(null))
            {
                var targetHealth = target.GetComponent<Health>();
                if (targetHealth != null)
                {
                    targetHealth.TakeDamage(damage, attacker);
                }
            }

            // Trigger particles
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }

            // Play sound
            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, transform.position);
            }

            Destroy(gameObject);
        }
    }
}
