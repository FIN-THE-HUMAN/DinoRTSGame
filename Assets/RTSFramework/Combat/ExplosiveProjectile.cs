using UnityEngine;
using System.Collections.Generic;
using RTSFramework.CameraSystem;

namespace RTSFramework.Combat
{
    public class ExplosiveProjectile : Projectile
    {
        [Header("Area of Effect (AoE)")]
        [SerializeField] private float aoeRadius = 4f;
        [SerializeField] private bool friendlyFire = false;

        [Header("Camera Shake Profile")]
        [SerializeField] private CameraShakeData shakeData;

        protected override void OnHit()
        {
            // 1. AoE Damage calculation with distance-based falloff
            Collider[] colliders = Physics.OverlapSphere(transform.position, aoeRadius);
            HashSet<Health> uniqueTargets = new HashSet<Health>();

            RTSFramework.Factions.Faction attackerFaction = GetFaction(Attacker);

            foreach (var col in colliders)
            {
                if (col == null || col.isTrigger) continue;

                // Make sure to find component in parent/root to correctly hit units
                Health health = col.GetComponentInParent<Health>();
                if (health != null && !health.IsDead)
                {
                    // If friendly fire is disabled, skip units of the same faction
                    if (!friendlyFire && attackerFaction != null && health.Faction == attackerFaction)
                    {
                        continue;
                    }

                    // Prevent applying damage multiple times to the same unit/building
                    if (uniqueTargets.Add(health))
                    {
                        float dist = Vector3.Distance(transform.position, col.bounds.center);
                        float percent = Mathf.Clamp01(1f - (dist / aoeRadius));
                        float finalDamage = Damage * percent;

                        health.TakeDamage(finalDamage, Attacker);
                    }
                }
            }

            // 2. Play base visual and audio hit effects
            PlayHitEffects();

            // 3. Trigger screen shake using the ScriptableObject configuration
            if (shakeData != null && RTSCameraController.Instance != null)
            {
                RTSCameraController.Instance.TriggerShake(
                    transform.position,
                    shakeData.Intensity,
                    shakeData.Duration,
                    shakeData.ShakeRadius
                );
            }

            // 4. Destroy this projectile
            Destroy(gameObject);
        }

        private RTSFramework.Factions.Faction GetFaction(GameObject obj)
        {
            if (obj == null) return null;

            var health = obj.GetComponent<Health>();
            if (health != null && health.Faction != null) return health.Faction;

            var unit = obj.GetComponent<Units.UnitController>();
            if (unit != null) return unit.Faction;

            var building = obj.GetComponent<Buildings.Building>();
            if (building != null) return building.Faction;

            return null;
        }
    }
}
