using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using RTSFramework.Selection;
using RTSFramework.Units;
using RTSFramework.Commands;
using RTSFramework.Buildings;
using RTSFramework.Combat;
using RTSFramework.Resources;

namespace RTSFramework.InputSystem
{
    public class RTSInputController : MonoBehaviour
    {
        [Header("Layers")]
        [SerializeField] private LayerMask terrainLayer;

        [Header("Selection Box Visuals")]
        [SerializeField] private Color boxColor = new Color(0.8f, 1f, 0.8f, 0.15f);
        [SerializeField] private Color borderColor = new Color(0.8f, 1f, 0.8f, 0.8f);
        [SerializeField] private float borderThickness = 2f;

        private Camera mainCamera;
        private Vector2 dragStartPosition;
        private bool isDragging;

        private static Texture2D whiteTexture;
        private static Texture2D WhiteTexture
        {
            get
            {
                if (whiteTexture == null)
                {
                    whiteTexture = new Texture2D(1, 1);
                    whiteTexture.SetPixel(0, 0, Color.white);
                    whiteTexture.Apply();
                }
                return whiteTexture;
            }
        }

        private void Start()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            HandleSelectionAndCommands();
        }

        private void HandleSelectionAndCommands()
        {
            if (mainCamera == null) return;

            // Placement mode: disable selection and command input
            if (BuildingSystem.Instance != null && BuildingSystem.Instance.IsPlacing)
            {
                return;
            }

            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (mouse == null) return;

            // --- SELECTION (Left Click & Drag) ---
            if (mouse.leftButton.wasPressedThisFrame)
            {
                var eventSystem = UnityEngine.EventSystems.EventSystem.current;
                if (eventSystem == null || !eventSystem.IsPointerOverGameObject())
                {
                    isDragging = true;
                    dragStartPosition = mouse.position.ReadValue();
                }
            }

            if (mouse.leftButton.wasReleasedThisFrame && isDragging)
            {
                isDragging = false;
                Vector2 dragEndPosition = mouse.position.ReadValue();

                bool isAccumulating = keyboard != null && (keyboard.ctrlKey.isPressed || keyboard.leftCtrlKey.isPressed);

                if (Vector2.Distance(dragStartPosition, dragEndPosition) < 5f)
                {
                    // Single Selection
                    Ray ray = mainCamera.ScreenPointToRay(dragEndPosition);
                    if (Physics.Raycast(ray, out RaycastHit hit, 1000f, SelectionManager.Instance.SelectableLayer))
                    {
                        ISelectable selectable = hit.collider.GetComponentInParent<ISelectable>();
                        if (selectable != null)
                        {
                            if (!selectable.IsPlayerOwned)
                            {
                                SelectionManager.Instance.Select(selectable, true);
                            }
                            else
                            {
                                // If player object is selected, but we currently have an enemy selected, we must clear it.
                                bool hasEnemySelected = false;
                                foreach (var sel in SelectionManager.Instance.SelectedObjects)
                                {
                                    if (!sel.IsPlayerOwned)
                                    {
                                        hasEnemySelected = true;
                                        break;
                                    }
                                }

                                SelectionManager.Instance.Select(selectable, !isAccumulating || hasEnemySelected);
                            }
                        }
                        else
                        {
                            if (!isAccumulating) SelectionManager.Instance.ClearSelection();
                        }
                    }
                    else
                    {
                        if (!isAccumulating) SelectionManager.Instance.ClearSelection();
                    }
                }
                else
                {
                    // Box Selection (Only targets player-owned units)
                    Bounds viewportBounds = GetViewportBounds(mainCamera, dragStartPosition, dragEndPosition);
                    
                    if (!isAccumulating)
                    {
                        SelectionManager.Instance.ClearSelection();
                    }

                    foreach (var selectable in SelectionManager.AllSelectables)
                    {
                        if (selectable == null || selectable.Equals(null)) continue;
                        
                        Vector3 viewportPos = mainCamera.WorldToViewportPoint(selectable.Transform.position);
                        if (viewportBounds.Contains(viewportPos))
                        {
                            if (selectable.GameObject.TryGetComponent<UnitController>(out var unit))
                            {
                                if (unit.IsPlayerOwned)
                                {
                                    SelectionManager.Instance.Select(selectable, false);
                                }
                            }
                        }
                    }
                }

                // Play selection voice response for the lead selected unit
                PlayLeadVoice(false);
            }

            // --- COMMANDS (Right Click) ---
            if (mouse.rightButton.wasPressedThisFrame)
            {
                var eventSystem = UnityEngine.EventSystems.EventSystem.current;
                if (eventSystem != null && eventSystem.IsPointerOverGameObject())
                {
                    return;
                }
                Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    bool isQueueing = keyboard != null && (keyboard.shiftKey.isPressed || keyboard.leftShiftKey.isPressed);

                    // Check if we hit a resource source
                    ResourceSource targetSource = hit.collider.GetComponentInParent<ResourceSource>();
                    // Check if we hit a unit/entity with a Health component
                    Health targetHealth = hit.collider.GetComponentInParent<Health>();
                    // Check if we hit a building
                    Building targetBuilding = hit.collider.GetComponentInParent<Building>();

                    bool commandIssued = false;

                    // Gather all player-controlled units from selection
                    List<UnitController> movingUnits = new List<UnitController>();
                    foreach (var selected in SelectionManager.Instance.SelectedObjects)
                    {
                        if (selected == null || selected.Equals(null)) continue;
                        if (selected.GameObject.TryGetComponent<UnitController>(out var unit))
                        {
                            if (unit.IsPlayerOwned)
                            {
                                movingUnits.Add(unit);
                            }
                        }
                    }

                    if (movingUnits.Count > 0)
                    {
                        if (targetSource != null)
                        {
                            // Gather resource
                            foreach (var unit in movingUnits)
                            {
                                unit.GiveCommand(new GatherCommand(targetSource), isQueueing);
                                commandIssued = true;
                            }
                        }
                        else if (targetBuilding != null && !targetBuilding.IsConstructed)
                        {
                            // Construct under-construction building
                            foreach (var unit in movingUnits)
                            {
                                if (unit.GetComponent<BuilderComponent>() != null)
                                {
                                    unit.GiveCommand(new BuildCommand(targetBuilding), isQueueing);
                                    commandIssued = true;
                                }
                                else
                                {
                                    // Move near the building for non-builders to stand guard
                                    unit.GiveCommand(new MoveCommand(hit.point), isQueueing);
                                    commandIssued = true;
                                }
                            }
                        }
                        else if (targetHealth != null)
                        {
                            // Attack target
                            foreach (var unit in movingUnits)
                            {
                                if (targetHealth.gameObject != unit.gameObject)
                                {
                                    unit.GiveCommand(new AttackCommand(targetHealth.gameObject), isQueueing);
                                    commandIssued = true;
                                }
                            }
                        }
                        else
                        {
                            // Move to point (use spiral offsets to prevent crowding/pushing)
                            for (int i = 0; i < movingUnits.Count; i++)
                            {
                                Vector3 targetPos = hit.point;

                                if (movingUnits.Count > 1)
                                {
                                    Vector3 offset = GetSpiralOffset(i, 1.8f);
                                    Vector3 candidatePos = hit.point + offset;

                                    if (NavMesh.SamplePosition(candidatePos, out NavMeshHit navHit, 3.0f, NavMesh.AllAreas))
                                    {
                                        targetPos = navHit.position;
                                    }
                                    else
                                    {
                                        targetPos = hit.point;
                                    }
                                }

                                movingUnits[i].GiveCommand(new MoveCommand(targetPos), isQueueing);
                                commandIssued = true;
                            }
                        }
                    }

                    if (commandIssued)
                    {
                        PlayLeadVoice(true);
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (isDragging)
            {
                var mouse = Mouse.current;
                if (mouse == null) return;

                Vector2 currentMousePos = mouse.position.ReadValue();
                Rect rect = GetScreenRect(dragStartPosition, currentMousePos);

                // Draw filled rectangle
                DrawScreenRect(rect, boxColor);
                // Draw border
                DrawScreenRectBorder(rect, borderThickness, borderColor);
            }
        }

        private void PlayLeadVoice(bool isCommand)
        {
            var selectedObjects = SelectionManager.Instance.SelectedObjects;
            if (selectedObjects.Count == 0) return;

            UnitController leadUnit = null;
            int highestPriority = int.MinValue;

            foreach (var selected in selectedObjects)
            {
                if (selected == null || selected.Equals(null)) continue;
                if (selected.GameObject.TryGetComponent<UnitController>(out var unit))
                {
                    if (!unit.IsPlayerOwned) continue;

                    if (unit.UnitData != null && unit.UnitData.SelectionPriority > highestPriority)
                    {
                        highestPriority = unit.UnitData.SelectionPriority;
                        leadUnit = unit;
                    }
                }
            }

            if (leadUnit != null && leadUnit.UnitData != null)
            {
                var clips = isCommand ? leadUnit.UnitData.CommandVoices : leadUnit.UnitData.SelectVoices;
                if (Audio.RTSAudioManager.Instance != null)
                {
                    Audio.RTSAudioManager.Instance.PlayVoice(clips);
                }
            }
        }

        private Vector3 GetSpiralOffset(int index, float spacing = 1.8f)
        {
            if (index == 0) return Vector3.zero;

            // Golden angle in radians (approx 137.5 degrees)
            float goldenAngle = 137.5f * Mathf.Deg2Rad;
            // Radius grows with square root of index to distribute units evenly in area
            float radius = spacing * Mathf.Sqrt(index);
            float theta = index * goldenAngle;

            return new Vector3(Mathf.Cos(theta) * radius, 0f, Mathf.Sin(theta) * radius);
        }

        #region Helper Methods

        private static Rect GetScreenRect(Vector2 screenPosition1, Vector2 screenPosition2)
        {
            // Move origin from bottom left (Unity Screen) to top left (GUI)
            screenPosition1.y = Screen.height - screenPosition1.y;
            screenPosition2.y = Screen.height - screenPosition2.y;

            var topLeft = Vector2.Min(screenPosition1, screenPosition2);
            var bottomRight = Vector2.Max(screenPosition1, screenPosition2);

            return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
        }

        private static Bounds GetViewportBounds(Camera camera, Vector2 screenPosition1, Vector2 screenPosition2)
        {
            Vector3 v1 = camera.ScreenToViewportPoint(screenPosition1);
            Vector3 v2 = camera.ScreenToViewportPoint(screenPosition2);

            Vector3 min = Vector3.Min(v1, v2);
            Vector3 max = Vector3.Max(v1, v2);

            min.z = camera.nearClipPlane;
            max.z = camera.farClipPlane;

            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }

        private static void DrawScreenRect(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, WhiteTexture);
            GUI.color = Color.white;
        }

        private static void DrawScreenRectBorder(Rect rect, float thickness, Color color)
        {
            // Top
            DrawScreenRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
            // Left
            DrawScreenRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
            // Right
            DrawScreenRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
            // Bottom
            DrawScreenRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
        }

        #endregion
    }
}
