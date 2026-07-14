using UnityEngine;
using RTSFramework.Factions;
using RTSFramework.Buildings;

namespace RTSFramework.Combat
{
    [RequireComponent(typeof(CombatComponent))]
    [RequireComponent(typeof(Building))]
    public class DefensiveTower : MonoBehaviour
    {
        [Header("Turret Head Rotation")]
        [SerializeField] private Transform turretHead;
        [SerializeField] private float rotationSpeed = 10f; // Quick rotation to catch fast enemies

        [Header("Targeting Settings")]
        [SerializeField] private float scanInterval = 0.4f;

        private CombatComponent combat;
        private Building building;
        
        private GameObject currentTarget;
        private float nextScanTime;
        private Faction myFaction;

        private void Awake()
        {
            combat = GetComponent<CombatComponent>();
            building = GetComponent<Building>();
        }

        private void Start()
        {
            myFaction = building != null ? building.Faction : null;
        }

        private void Update()
        {
            // Only function if the building is fully constructed!
            if (building == null || !building.IsConstructed) return;

            // Periodically scan for the closest valid target
            if (Time.time >= nextScanTime)
            {
                nextScanTime = Time.time + scanInterval;
                UpdateTarget();
            }

            // Engage current target
            if (currentTarget != null && !currentTarget.Equals(null))
            {
                var targetHealth = currentTarget.GetComponent<Health>();
                if (targetHealth == null || targetHealth.IsDead || Vector3.Distance(transform.position, currentTarget.transform.position) > combat.AttackRange)
                {
                    currentTarget = null;
                    return;
                }

                // Handle optional rotation towards target
                bool isReadyToFire = true;
                if (turretHead != null)
                {
                    Vector3 direction = (currentTarget.transform.position - turretHead.position).normalized;
                    direction.y = 0; // Rotate strictly on Y-axis
                    if (direction != Vector3.zero)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(direction);
                        turretHead.rotation = Quaternion.Slerp(turretHead.rotation, targetRot, Time.deltaTime * rotationSpeed);

                        float angle = Vector3.Angle(turretHead.forward, direction);
                        isReadyToFire = angle < 20f; // Only fire if facing within 20 degrees
                    }
                }

                if (isReadyToFire)
                {
                    combat.Attack(currentTarget);
                }
            }
        }

        private void UpdateTarget()
        {
            // If current target is still valid and in range, stick to it
            if (currentTarget != null && !currentTarget.Equals(null))
            {
                var health = currentTarget.GetComponent<Health>();
                if (health != null && !health.IsDead && Vector3.Distance(transform.position, currentTarget.transform.position) <= combat.AttackRange)
                {
                    return;
                }
            }

            currentTarget = null;
            float closestDist = float.MaxValue;

            // Query colliders in range
            Collider[] colliders = Physics.OverlapSphere(transform.position, combat.AttackRange);
            foreach (var col in colliders)
            {
                if (col == null || col.isTrigger) continue;

                // Resolve entity controller to identify faction
                Faction targetFaction = null;
                var unit = col.GetComponentInParent<Units.UnitController>();
                if (unit != null)
                {
                    targetFaction = unit.Faction;
                }
                else
                {
                    var bld = col.GetComponentInParent<Building>();
                    if (bld != null)
                    {
                        targetFaction = bld.Faction;
                    }
                }

                // Target must belong to a different, hostile/unallied faction
                if (targetFaction == myFaction) continue;

                // Validate target health
                var health = col.GetComponentInParent<Health>();
                if (health == null || health.IsDead) continue;

                float dist = Vector3.Distance(transform.position, health.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    currentTarget = health.gameObject;
                }
            }
        }
    }
}
