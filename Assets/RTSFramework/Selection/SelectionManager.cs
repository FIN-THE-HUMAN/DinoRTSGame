using System.Collections.Generic;
using UnityEngine;
using RTSFramework.Units;
using RTSFramework.Combat;

namespace RTSFramework.Selection
{
    public class SelectionManager : MonoBehaviour
    {
        public static SelectionManager Instance { get; private set; }

        [SerializeField] private LayerMask selectableLayer;
        public LayerMask SelectableLayer => selectableLayer;
        
        private static readonly List<ISelectable> allSelectables = new List<ISelectable>();
        public static IReadOnlyList<ISelectable> AllSelectables => allSelectables;

        private readonly List<ISelectable> selectedObjects = new List<ISelectable>();
        public IReadOnlyList<ISelectable> SelectedObjects => selectedObjects;

        public static void RegisterSelectable(ISelectable selectable)
        {
            if (!allSelectables.Contains(selectable))
            {
                allSelectables.Add(selectable);
            }
        }

        public static void UnregisterSelectable(ISelectable selectable)
        {
            allSelectables.Remove(selectable);
        }

        public event System.Action OnSelectionChanged;

        private readonly Dictionary<ISelectable, System.Action> deathSubscriptions = new Dictionary<ISelectable, System.Action>();

        private readonly List<ISelectable>[] controlGroups = new List<ISelectable>[10];
        private float[] lastRecallTimes = new float[10];
        private const float doubleTapTimeThreshold = 0.3f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                for (int i = 0; i < 10; i++)
                {
                    controlGroups[i] = new List<ISelectable>();
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void Select(ISelectable selectable, bool clearExisting = true)
        {
            bool changed = false;
            
            if (clearExisting && selectedObjects.Count > 0)
            {
                foreach (var s in selectedObjects)
                {
                    if (s != null && !s.Equals(null))
                    {
                        UnsubscribeFromDeath(s);
                        s.Deselect();
                    }
                }
                selectedObjects.Clear();
                changed = true;
            }
            
            if (!selectedObjects.Contains(selectable))
            {
                selectedObjects.Add(selectable);
                selectable.Select();
                SubscribeToDeath(selectable);
                changed = true;
            }

            if (changed)
            {
                OnSelectionChanged?.Invoke();
            }
        }

        public void Deselect(ISelectable selectable)
        {
            if (selectedObjects.Remove(selectable))
            {
                UnsubscribeFromDeath(selectable);
                selectable.Deselect();
                OnSelectionChanged?.Invoke();
            }
        }

        public void ClearSelection()
        {
            if (selectedObjects.Count == 0) return;

            foreach (var selectable in selectedObjects)
            {
                if (selectable != null && !selectable.Equals(null))
                {
                    UnsubscribeFromDeath(selectable);
                    selectable.Deselect();
                }
            }
            selectedObjects.Clear();
            OnSelectionChanged?.Invoke();
        }

        public UnitController GetLeadSelectedUnit()
        {
            UnitController lead = null;
            int highestPriority = int.MinValue;

            foreach (var selected in selectedObjects)
            {
                if (selected == null || selected.Equals(null)) continue;
                if (selected.GameObject.TryGetComponent<UnitController>(out var unit))
                {
                    if (unit.UnitData != null && unit.UnitData.SelectionPriority > highestPriority)
                    {
                        highestPriority = unit.UnitData.SelectionPriority;
                        lead = unit;
                    }
                }
            }
            return lead;
        }

        private void SubscribeToDeath(ISelectable selectable)
        {
            if (deathSubscriptions.ContainsKey(selectable)) return;

            if (selectable.GameObject.TryGetComponent<Health>(out var health))
            {
                System.Action deathHandler = () => HandleSelectedUnitDeath(selectable);
                health.OnDeath += deathHandler;
                deathSubscriptions[selectable] = deathHandler;
            }
        }

        private void UnsubscribeFromDeath(ISelectable selectable)
        {
            if (deathSubscriptions.TryGetValue(selectable, out var deathHandler))
            {
                if (selectable != null && !selectable.Equals(null))
                {
                    if (selectable.GameObject.TryGetComponent<Health>(out var health))
                    {
                        health.OnDeath -= deathHandler;
                    }
                }
                deathSubscriptions.Remove(selectable);
            }
        }

        private void HandleSelectedUnitDeath(ISelectable selectable)
        {
            UnitController oldLead = GetLeadSelectedUnit();

            Deselect(selectable);

            UnitController newLead = GetLeadSelectedUnit();

            if (oldLead != newLead && newLead != null)
            {
                if (newLead.UnitData != null && newLead.UnitData.SelectVoices.Count > 0)
                {
                    if (Audio.RTSAudioManager.Instance != null)
                    {
                        Audio.RTSAudioManager.Instance.PlayVoice(newLead.UnitData.SelectVoices, false);
                    }
                }
            }
        }

        public void AssignControlGroup(int groupIndex)
        {
            if (groupIndex < 0 || groupIndex >= 10) return;

            controlGroups[groupIndex].Clear();
            foreach (var selectable in selectedObjects)
            {
                if (selectable != null && !selectable.Equals(null))
                {
                    controlGroups[groupIndex].Add(selectable);
                }
            }

            Debug.Log($"Control Group {groupIndex + 1} assigned with {controlGroups[groupIndex].Count} objects.");
        }

        public void RecallControlGroup(int groupIndex)
        {
            if (groupIndex < 0 || groupIndex >= 10) return;

            // Remove null/destroyed references
            controlGroups[groupIndex].RemoveAll(item => item == null || item.Equals(null));

            if (controlGroups[groupIndex].Count == 0) return;

            // Select group
            ClearSelection();
            foreach (var selectable in controlGroups[groupIndex])
            {
                Select(selectable, false);
            }

            // Double tap camera focus check
            float timeSinceLastRecall = Time.time - lastRecallTimes[groupIndex];
            if (timeSinceLastRecall < doubleTapTimeThreshold)
            {
                Vector3 averagePos = Vector3.zero;
                int validCount = 0;
                foreach (var selectable in controlGroups[groupIndex])
                {
                    if (selectable != null && !selectable.Equals(null))
                    {
                        averagePos += selectable.Transform.position;
                        validCount++;
                    }
                }

                if (validCount > 0)
                {
                    averagePos /= validCount;
                    if (RTSFramework.CameraSystem.RTSCameraController.Instance != null)
                    {
                        RTSFramework.CameraSystem.RTSCameraController.Instance.FocusOn(averagePos);
                    }
                }
            }

            lastRecallTimes[groupIndex] = Time.time;
        }
    }
}
