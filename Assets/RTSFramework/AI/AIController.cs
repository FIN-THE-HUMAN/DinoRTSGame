using System.Collections.Generic;
using UnityEngine;
using RTSFramework.Units;
using RTSFramework.Combat;

namespace RTSFramework.AI
{
    public class AIController : MonoBehaviour
    {
        [Header("FSM")]
        [SerializeField] private AIState currentState;

        [Header("Configuration")]
        [SerializeField] private float sightRange = 15f;
        [SerializeField] private List<Transform> patrolWaypoints = new List<Transform>();

        private int currentWaypointIndex;
        private Vector3 spawnPosition;

        // Runtime State
        public GameObject Target { get; set; }
        public Vector3 SpawnPosition => spawnPosition;
        public List<Transform> PatrolWaypoints => patrolWaypoints;
        public int CurrentWaypointIndex
        {
            get => currentWaypointIndex;
            set => currentWaypointIndex = value;
        }
        public float SightRange => sightRange;

        // Cached Components
        public UnitController UnitController { get; private set; }
        public CombatComponent Combat { get; private set; }

        private void Awake()
        {
            UnitController = GetComponent<UnitController>();
            Combat = GetComponent<CombatComponent>();
            spawnPosition = transform.position;
        }

        private void Update()
        {
            if (currentState != null)
            {
                currentState.UpdateState(this);
            }
        }

        public void TransitionToState(AIState nextState)
        {
            if (nextState != null)
            {
                currentState = nextState;
                OnExitState();
            }
        }

        private void OnExitState()
        {
            // Reset any temporary state if needed
        }

        public bool FindNearestEnemy(out GameObject enemy)
        {
            enemy = null;
            float minDistance = float.MaxValue;

            // Find all active selectables (which includes all units)
            foreach (var selectable in Selection.SelectionManager.AllSelectables)
            {
                if (selectable == null || selectable.Equals(null)) continue;

                // Check if it's a unit and is an enemy
                if (selectable.GameObject.TryGetComponent<UnitController>(out var unit))
                {
                    // If this AI is player-owned, it targets non-player-owned units.
                    // If this AI is NOT player-owned, it targets player-owned units.
                    bool isOpponent = UnitController != null ? (UnitController.IsPlayerOwned != unit.IsPlayerOwned) : unit.IsPlayerOwned;

                    if (isOpponent)
                    {
                        float dist = Vector3.Distance(transform.position, unit.transform.position);
                        if (dist < minDistance && dist <= sightRange)
                        {
                            // Check if enemy is alive
                            var health = unit.GetComponent<Health>();
                            if (health != null && !health.IsDead)
                            {
                                minDistance = dist;
                                enemy = unit.gameObject;
                            }
                        }
                    }
                }
            }

            return enemy != null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, sightRange);
        }
    }
}
