using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTSFramework.Selection;
using RTSFramework.Units;
using RTSFramework.Buildings;
using RTSFramework.Upgrades;

namespace RTSFramework.UI
{
    public class RTSUIManager : MonoBehaviour
    {
        public static RTSUIManager Instance { get; private set; }

        [Header("Selection Panel UI")]
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text unitNameText;
        [SerializeField] private TMP_Text unitCountText;

        [Header("Build HUD UI")]
        [SerializeField] private GameObject buildPanel;
        [SerializeField] private GameObject buildButtonPrefab;

        [Header("Production HUD UI")]
        [SerializeField] private GameObject productionPanel;
        [SerializeField] private GameObject productionButtonPrefab;
        [SerializeField] private Slider productionProgressBar;

        private UnitProductionComponent activeProducer;
        private TechnologyResearchComponent activeResearcher;
        private GameObject commandsPanel;

        private Dictionary<Button, UnitData> unitButtons = new Dictionary<Button, UnitData>();
        private Dictionary<Button, UpgradeData> upgradeButtons = new Dictionary<Button, UpgradeData>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Create and configure commandsPanel
            if (commandsPanel == null && selectionPanel != null)
            {
                commandsPanel = new GameObject("CommandsPanel", typeof(RectTransform));
                commandsPanel.transform.SetParent(selectionPanel.transform, false);
                var grid = commandsPanel.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(55f, 55f);
                grid.spacing = new Vector2(6f, 6f);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 4;
                grid.childAlignment = TextAnchor.UpperLeft;
                
                var rt = commandsPanel.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 0.5f);
                rt.anchorMax = new Vector2(1f, 0.5f);
                rt.pivot = new Vector2(1f, 0.5f);
                rt.anchoredPosition = new Vector2(-300f, 0f); // Middle-Left position
                rt.sizeDelta = new Vector2(120f, 115f);
                rt.localScale = Vector3.one;
                
                commandsPanel.SetActive(false);
            }

            // Configure buildPanel to use a 2-column Grid Layout
            if (buildPanel != null)
            {
                var oldLayout = buildPanel.GetComponent<LayoutGroup>();
                if (oldLayout != null && !(oldLayout is GridLayoutGroup))
                {
                    DestroyImmediate(oldLayout);
                }

                var grid = buildPanel.GetComponent<GridLayoutGroup>();
                if (grid == null)
                {
                    grid = buildPanel.gameObject.AddComponent<GridLayoutGroup>();
                }
                grid.cellSize = new Vector2(55f, 55f);
                grid.spacing = new Vector2(6f, 6f);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 2; // 2 columns
                grid.childAlignment = TextAnchor.UpperLeft;
            }

            // Configure productionPanel to use a 2-column Grid Layout
            if (productionPanel != null)
            {
                var oldLayout = productionPanel.GetComponent<LayoutGroup>();
                if (oldLayout != null && !(oldLayout is GridLayoutGroup))
                {
                    DestroyImmediate(oldLayout);
                }

                var grid = productionPanel.GetComponent<GridLayoutGroup>();
                if (grid == null)
                {
                    grid = productionPanel.gameObject.AddComponent<GridLayoutGroup>();
                }
                grid.cellSize = new Vector2(55f, 55f);
                grid.spacing = new Vector2(6f, 6f);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 2; // 2 columns
                grid.childAlignment = TextAnchor.UpperLeft;
            }

            if (SelectionManager.Instance != null)
            {
                SelectionManager.Instance.OnSelectionChanged += UpdateSelectionUI;
            }

            if (productionProgressBar != null)
            {
                // Ensure LayoutElement ignores Grid Layout Group
                var layout = productionProgressBar.GetComponent<LayoutElement>();
                if (layout == null) layout = productionProgressBar.gameObject.AddComponent<LayoutElement>();
                layout.ignoreLayout = true;

                // Force horizontal stretch anchored at the bottom of the ProductionPanel
                var rt = productionProgressBar.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 0f);
                    rt.anchorMax = new Vector2(1f, 0f);
                    rt.pivot = new Vector2(0.5f, 0f);
                    rt.anchoredPosition = new Vector2(0f, 2f); // 2px margin from bottom
                    rt.sizeDelta = new Vector2(-10f, 15f); // 15px height, inset by 5px left/right
                    rt.localScale = Vector3.one;
                }

                // Force correct layout for child text (Queue: X) to stretch across the slider
                Transform txtTrans = productionProgressBar.transform.Find("QueueText");
                if (txtTrans != null)
                {
                    var txtRt = txtTrans.GetComponent<RectTransform>();
                    if (txtRt != null)
                    {
                        txtRt.anchorMin = Vector2.zero;
                        txtRt.anchorMax = Vector2.one;
                        txtRt.sizeDelta = Vector2.zero;
                        txtRt.anchoredPosition = Vector2.zero;
                        txtRt.localScale = Vector3.one;
                    }
                }
            }

            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.OnUpgradeCompleted += HandleGlobalUpgradeCompleted;
            }

            // Dynamically activate the MinimapPanel at runtime if it was disabled in the Editor
            if (transform.parent != null)
            {
                Transform minimapTrans = transform.parent.Find("MinimapPanel");
                if (minimapTrans != null)
                {
                    minimapTrans.gameObject.SetActive(true);
                }
            }

            RearrangeHUDGeneralsStyle();
            UpdateSelectionUI();
        }

        private void RearrangeHUDGeneralsStyle()
        {
            if (selectionPanel == null) return;

            // 1. Rearrange main SelectionPanel bottom bar
            var barRt = selectionPanel.GetComponent<RectTransform>();
            if (barRt != null)
            {
                barRt.anchorMin = new Vector2(0.5f, 0f);
                barRt.anchorMax = new Vector2(0.5f, 0f);
                barRt.pivot = new Vector2(0.5f, 0f);
                barRt.anchoredPosition = new Vector2(0f, 15f); // Lifted slightly from bottom edge
                barRt.sizeDelta = new Vector2(750f, 120f); // 750px width, 120px height
            }

            // 2. Position Selection Info (Unit Name, HP, Portrait) on the left
            Transform infoTrans = selectionPanel.transform.Find("SelectionInfoContainer");
            GameObject infoContainer;
            if (infoTrans == null)
            {
                infoContainer = new GameObject("SelectionInfoContainer", typeof(RectTransform));
                infoContainer.transform.SetParent(selectionPanel.transform, false);
            }
            else
            {
                infoContainer = infoTrans.gameObject;
            }

            var infoRt = infoContainer.GetComponent<RectTransform>();
            infoRt.anchorMin = new Vector2(0f, 0.5f);
            infoRt.anchorMax = new Vector2(0f, 0.5f);
            infoRt.pivot = new Vector2(0f, 0.5f);
            infoRt.anchoredPosition = new Vector2(25f, 0f);
            infoRt.sizeDelta = new Vector2(300f, 100f);

            if (portraitImage != null)
            {
                portraitImage.transform.SetParent(infoContainer.transform, false);
                var portRt = portraitImage.GetComponent<RectTransform>();
                if (portRt != null)
                {
                    portRt.anchorMin = new Vector2(0f, 0.5f);
                    portRt.anchorMax = new Vector2(0f, 0.5f);
                    portRt.pivot = new Vector2(0f, 0.5f);
                    portRt.anchoredPosition = new Vector2(0f, 0f);
                    portRt.sizeDelta = new Vector2(90f, 90f);
                }
            }

            if (unitNameText != null)
            {
                unitNameText.transform.SetParent(infoContainer.transform, false);
                var nameRt = unitNameText.GetComponent<RectTransform>();
                if (nameRt != null)
                {
                    nameRt.anchorMin = new Vector2(0f, 0.5f);
                    nameRt.anchorMax = new Vector2(1f, 0.5f);
                    nameRt.pivot = new Vector2(0f, 1f); // top-aligned
                    nameRt.anchoredPosition = new Vector2(105f, 25f);
                    nameRt.sizeDelta = new Vector2(-110f, 35f);
                    unitNameText.alignment = TextAlignmentOptions.Left;
                    unitNameText.fontSize = 22;
                }
            }

            if (unitCountText != null)
            {
                unitCountText.transform.SetParent(infoContainer.transform, false);
                var countRt = unitCountText.GetComponent<RectTransform>();
                if (countRt != null)
                {
                    countRt.anchorMin = new Vector2(0f, 0.5f);
                    countRt.anchorMax = new Vector2(1f, 0.5f);
                    countRt.pivot = new Vector2(0f, 0f); // bottom-aligned
                    countRt.anchoredPosition = new Vector2(105f, -25f);
                    countRt.sizeDelta = new Vector2(-110f, 35f);
                    unitCountText.alignment = TextAlignmentOptions.Left;
                    unitCountText.fontSize = 16;
                }
            }

            // 3. Position panels in different locations on the right side of the bottom bar
            if (buildPanel != null)
            {
                buildPanel.transform.SetParent(selectionPanel.transform, false);
                var bRt = buildPanel.GetComponent<RectTransform>();
                if (bRt != null)
                {
                    bRt.anchorMin = new Vector2(1f, 0.5f);
                    bRt.anchorMax = new Vector2(1f, 0.5f);
                    bRt.pivot = new Vector2(1f, 0.5f);
                    bRt.anchoredPosition = new Vector2(-20f, 0f); // Rightmost
                    bRt.sizeDelta = new Vector2(120f, 115f);
                }
            }

            if (productionPanel != null)
            {
                productionPanel.transform.SetParent(selectionPanel.transform, false);
                var pRt = productionPanel.GetComponent<RectTransform>();
                if (pRt != null)
                {
                    pRt.anchorMin = new Vector2(1f, 0.5f);
                    pRt.anchorMax = new Vector2(1f, 0.5f);
                    pRt.pivot = new Vector2(1f, 0.5f);
                    pRt.anchoredPosition = new Vector2(-160f, 0f); // Middle-Right
                    pRt.sizeDelta = new Vector2(120f, 115f);
                }
            }

            if (commandsPanel != null)
            {
                commandsPanel.transform.SetParent(selectionPanel.transform, false);
                var cRt = commandsPanel.GetComponent<RectTransform>();
                if (cRt != null)
                {
                    cRt.anchorMin = new Vector2(1f, 0.5f);
                    cRt.anchorMax = new Vector2(1f, 0.5f);
                    cRt.pivot = new Vector2(1f, 0.5f);
                    cRt.anchoredPosition = new Vector2(-300f, 0f); // Middle-Left
                    cRt.sizeDelta = new Vector2(120f, 115f);
                }
            }
        }

        private void OnDestroy()
        {
            if (SelectionManager.Instance != null)
            {
                SelectionManager.Instance.OnSelectionChanged -= UpdateSelectionUI;
            }
            if (UpgradeManager.HasInstance)
            {
                UpgradeManager.Instance.OnUpgradeCompleted -= HandleGlobalUpgradeCompleted;
            }
            SubscribeToHealth(null);
        }

        private void Update()
        {
            // Dynamically update selected ResourceSource count if one is selected
            if (SelectionManager.Instance != null && SelectionManager.Instance.SelectedObjects.Count == 1)
            {
                var lead = SelectionManager.Instance.SelectedObjects[0];
                if (lead != null && !lead.Equals(null))
                {
                    var resource = lead.GameObject.GetComponent<Resources.ResourceSource>();
                    if (resource != null && unitCountText != null)
                    {
                        unitCountText.text = $"Amount: {resource.CurrentAmount} / {resource.MaxAmount}";
                    }
                }
            }

            // Hide the old progress bar since we render progress directly on top of the icons
            if (productionProgressBar != null)
            {
                productionProgressBar.gameObject.SetActive(false);
            }

            // Update Unit Button Overlays
            foreach (var kvp in unitButtons)
            {
                var button = kvp.Key;
                var data = kvp.Value;

                if (button == null || data == null) continue;

                int queueCount = 0;
                float progressFraction = 0f;

                if (activeProducer != null)
                {
                    foreach (var u in activeProducer.QueuedUnits)
                    {
                        if (u == data) queueCount++;
                    }

                    if (activeProducer.CurrentActiveUnit == data)
                    {
                        progressFraction = 1f - activeProducer.TrainingProgress;
                    }
                }

                UpdateOverlayValues(button, queueCount, progressFraction);
            }

            // Update Upgrade Button Overlays
            foreach (var kvp in upgradeButtons)
            {
                var button = kvp.Key;
                var data = kvp.Value;

                if (button == null || data == null) continue;

                int queueCount = 0;
                float progressFraction = 0f;

                if (activeResearcher != null)
                {
                    foreach (var upg in activeResearcher.QueuedResearch)
                    {
                        if (upg == data) queueCount++;
                    }

                    if (activeResearcher.CurrentActiveResearch == data)
                    {
                        progressFraction = 1f - activeResearcher.ResearchProgress;
                    }
                }

                UpdateOverlayValues(button, queueCount, progressFraction);
            }
        }

        private void ConfigureButtonVisuals(Button button, Sprite icon, string displayName)
        {
            var btnImage = button.GetComponent<Image>();
            var btnText = button.GetComponentInChildren<TMP_Text>();

            if (icon != null)
            {
                if (btnText != null) btnText.gameObject.SetActive(false); // Hide text name label
                if (btnImage != null)
                {
                    btnImage.sprite = icon;
                    btnImage.color = Color.white;
                }
            }
            else
            {
                if (btnText != null)
                {
                    btnText.gameObject.SetActive(true);
                    btnText.text = displayName;
                    btnText.fontSize = 8f; // Tiny font to fit in a 55x55 square
                    btnText.alignment = TextAlignmentOptions.Center;
                }
                if (btnImage != null)
                {
                    btnImage.sprite = null;
                    btnImage.color = new Color(0.18f, 0.18f, 0.18f, 0.95f);
                }
            }

            // Setup Progress Overlay (Radial Clock-Wipe)
            Transform progressTrans = button.transform.Find("ProgressOverlay");
            if (progressTrans == null)
            {
                GameObject progObj = new GameObject("ProgressOverlay", typeof(RectTransform));
                progObj.transform.SetParent(button.transform, false);

                var rt = progObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;

                var img = progObj.AddComponent<Image>();
                img.sprite = GetWhiteSprite(); // Crucial: Unity UI Image.Type = Filled requires a non-null sprite!
                img.color = new Color(0f, 0f, 0f, 0.65f); // Translucent black overlay
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Radial360;
                img.fillOrigin = (int)Image.Origin360.Top;
                img.fillClockwise = false; // Rotate counter-clockwise (like in Generals / target visual)
                img.raycastTarget = false;

                progressTrans = progObj.transform;
            }

            // Setup Queue Overlay (Number in top-right)
            Transform queueTrans = button.transform.Find("QueueOverlay");
            if (queueTrans == null)
            {
                GameObject queueObj = new GameObject("QueueOverlay", typeof(RectTransform));
                queueObj.transform.SetParent(button.transform, false);

                var rt = queueObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-2f, -2f);
                rt.sizeDelta = new Vector2(20f, 18f); // Small square

                // No image background added to queueObj to leave number cleanly overlayed

                GameObject textObj = new GameObject("Text", typeof(RectTransform));
                textObj.transform.SetParent(queueObj.transform, false);

                var textRt = textObj.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.sizeDelta = Vector2.zero;
                textRt.anchoredPosition = Vector2.zero;

                var txt = textObj.AddComponent<TextMeshProUGUI>();
                txt.fontSize = 11;
                txt.color = new Color(1f, 0.84f, 0f); // Gold text
                txt.alignment = TextAlignmentOptions.Center;
                txt.raycastTarget = false;

                queueTrans = queueObj.transform;
            }
        }

        private void UpdateOverlayValues(Button button, int queueCount, float progressFraction)
        {
            Transform progressTrans = button.transform.Find("ProgressOverlay");
            if (progressTrans != null)
            {
                var img = progressTrans.GetComponent<Image>();
                if (img != null)
                {
                    img.fillAmount = progressFraction;
                    progressTrans.gameObject.SetActive(progressFraction > 0.001f);
                }
            }

            Transform queueTrans = button.transform.Find("QueueOverlay");
            if (queueTrans != null)
            {
                var txt = queueTrans.GetComponentInChildren<TMP_Text>();
                if (txt != null)
                {
                    txt.text = queueCount.ToString();
                }
                queueTrans.gameObject.SetActive(queueCount > 0);
            }
        }

        public void UpdateSelectionUI()
        {
            if (selectionPanel == null) return;

            var selectedObjects = SelectionManager.Instance.SelectedObjects;

            if (selectedObjects.Count == 0)
            {
                selectionPanel.SetActive(false);
                if (buildPanel != null) buildPanel.SetActive(false);
                if (productionPanel != null) productionPanel.SetActive(false);
                activeProducer = null;
                unitButtons.Clear();
                upgradeButtons.Clear();
                return;
            }

            // Find the "lead" selectable object (prefer units over buildings, or just take the first selected)
            ISelectable leadSelectable = null;
            foreach (var sel in selectedObjects)
            {
                if (sel == null || sel.Equals(null)) continue;
                if (sel.GameObject.GetComponent<UnitController>() != null)
                {
                    leadSelectable = sel;
                    break;
                }
            }

            if (leadSelectable == null && selectedObjects.Count > 0)
            {
                leadSelectable = selectedObjects[0];
            }

            if (leadSelectable == null || leadSelectable.Equals(null))
            {
                selectionPanel.SetActive(false);
                if (buildPanel != null) buildPanel.SetActive(false);
                if (productionPanel != null) productionPanel.SetActive(false);
                if (commandsPanel != null) commandsPanel.SetActive(false);
                activeProducer = null;
                unitButtons.Clear();
                upgradeButtons.Clear();
                return;
            }

            selectionPanel.SetActive(true);

            // Determine display details
            string leadName = leadSelectable.GameObject.name;
            Sprite leadIcon = null;

            var unit = leadSelectable.GameObject.GetComponent<UnitController>();
            var building = leadSelectable.GameObject.GetComponent<Building>();
            var resource = leadSelectable.GameObject.GetComponent<Resources.ResourceSource>();
 
            if (unit != null)
            {
                leadName = unit.UnitData != null ? unit.UnitData.UnitName : unit.gameObject.name;
                leadIcon = unit.UnitData != null ? unit.UnitData.UnitIcon : null;
            }
            else if (building != null)
            {
                leadName = building.BuildingData != null ? building.BuildingData.BuildingName : building.gameObject.name;
            }
            else if (resource != null)
            {
                leadName = (resource.ResourceType == Resources.ResourceType.Gold) ? "Gold Mine" : "Forest Tree";
            }
 
            if (unitNameText != null)
            {
                unitNameText.text = leadName;
            }
 
            if (unitCountText != null)
            {
                if (selectedObjects.Count > 1)
                {
                    SubscribeToHealth(null);
                    unitCountText.text = $"x{selectedObjects.Count}";
                }
                else
                {
                    if (resource != null)
                    {
                        SubscribeToHealth(null);
                        unitCountText.text = $"Amount: {resource.CurrentAmount} / {resource.MaxAmount}";
                    }
                    else
                    {
                        var health = leadSelectable.GameObject.GetComponent<Combat.Health>();
                        SubscribeToHealth(health);
                        if (health == null)
                        {
                            unitCountText.text = "";
                        }
                    }
                }
            }

            if (portraitImage != null)
            {
                if (leadIcon != null)
                {
                    portraitImage.gameObject.SetActive(true);
                    portraitImage.sprite = leadIcon;
                }
                else
                {
                    portraitImage.sprite = null;
                    portraitImage.gameObject.SetActive(false);
                }
            }

            // --- DYNAMIC COMMANDS AND BUILD PANEL POPULATION ---
            if (commandsPanel != null)
            {
                foreach (Transform child in commandsPanel.transform)
                {
                    Destroy(child.gameObject);
                }
            }

            if (buildPanel != null)
            {
                foreach (Transform child in buildPanel.transform)
                {
                    Destroy(child.gameObject);
                }
            }

            bool isPlayerOwnedUnit = leadSelectable.IsPlayerOwned && unit != null;
            var builder = leadSelectable.GameObject.GetComponent<BuilderComponent>();

            if (isPlayerOwnedUnit)
            {
                // 1. Show and populate commandsPanel
                if (commandsPanel != null && buildButtonPrefab != null)
                {
                    commandsPanel.SetActive(true);

                    // Action: Attack-Move
                    GameObject btnAttackMove = Instantiate(buildButtonPrefab, commandsPanel.transform);
                    var textAM = btnAttackMove.GetComponentInChildren<TMP_Text>();
                    if (textAM != null)
                    {
                        textAM.text = "Атака в движении";
                        textAM.fontSize = 8f;
                        textAM.alignment = TextAlignmentOptions.Center;
                    }
                    var btnAM = btnAttackMove.GetComponent<Button>();
                    if (btnAM != null)
                    {
                        btnAM.onClick.AddListener(() =>
                        {
                            if (RTSFramework.InputSystem.RTSInputController.Instance != null)
                            {
                                RTSFramework.InputSystem.RTSInputController.Instance.StartAttackMoveMode();
                            }
                        });
                    }

                    // Action: Guard
                    GameObject btnGuard = Instantiate(buildButtonPrefab, commandsPanel.transform);
                    var textG = btnGuard.GetComponentInChildren<TMP_Text>();
                    if (textG != null)
                    {
                        textG.text = "Охрана";
                        textG.fontSize = 8f;
                        textG.alignment = TextAlignmentOptions.Center;
                    }
                    var btnG = btnGuard.GetComponent<Button>();
                    if (btnG != null)
                    {
                        btnG.onClick.AddListener(() =>
                        {
                            if (RTSFramework.InputSystem.RTSInputController.Instance != null)
                            {
                                RTSFramework.InputSystem.RTSInputController.Instance.StartGuardMode();
                            }
                        });
                    }
                }

                // 2. Show and populate buildPanel only if worker/builder
                if (builder != null && buildPanel != null && buildButtonPrefab != null)
                {
                    buildPanel.SetActive(true);
                    foreach (var bData in builder.BuildableBuildings)
                    {
                        if (bData == null) continue;

                        GameObject buttonObj = Instantiate(buildButtonPrefab, buildPanel.transform);
                        var text = buttonObj.GetComponentInChildren<TMP_Text>();
                        if (text != null)
                        {
                            text.text = bData.BuildingName;
                            text.fontSize = 8f;
                            text.alignment = TextAlignmentOptions.Center;
                        }

                        var button = buttonObj.GetComponent<Button>();
                        if (button != null)
                        {
                            var data = bData;
                            button.onClick.AddListener(() =>
                            {
                                if (BuildingSystem.Instance != null)
                                {
                                    BuildingSystem.Instance.StartPlacement(data);
                                }
                            });
                        }
                    }
                }
                else
                {
                    if (buildPanel != null) buildPanel.SetActive(false);
                }
            }
            else
            {
                // Not a player unit (neutral/enemy unit or building)
                if (commandsPanel != null) commandsPanel.SetActive(false);

                if (builder != null && buildPanel != null && buildButtonPrefab != null)
                {
                    buildPanel.SetActive(true);
                    foreach (var bData in builder.BuildableBuildings)
                    {
                        if (bData == null) continue;

                        GameObject buttonObj = Instantiate(buildButtonPrefab, buildPanel.transform);
                        var text = buttonObj.GetComponentInChildren<TMP_Text>();
                        if (text != null)
                        {
                            text.text = bData.BuildingName;
                            text.fontSize = 8f;
                            text.alignment = TextAlignmentOptions.Center;
                        }

                        var button = buttonObj.GetComponent<Button>();
                        if (button != null)
                        {
                            var data = bData;
                            button.onClick.AddListener(() =>
                            {
                                if (BuildingSystem.Instance != null)
                                {
                                    BuildingSystem.Instance.StartPlacement(data);
                                }
                            });
                        }
                    }
                }
                else
                {
                    if (buildPanel != null) buildPanel.SetActive(false);
                }
            }

            // --- PRODUCTION / RESEARCH PANEL POPULATION ---
            var producer = leadSelectable.GameObject.GetComponent<UnitProductionComponent>();
            var researcher = leadSelectable.GameObject.GetComponent<TechnologyResearchComponent>();
            bool isPlayerOwnedBuilding = leadSelectable.IsPlayerOwned && building != null;

            unitButtons.Clear();
            upgradeButtons.Clear();

            if ((producer != null || researcher != null) && isPlayerOwnedBuilding && productionPanel != null && productionButtonPrefab != null)
            {
                activeProducer = producer;
                activeResearcher = researcher;
                productionPanel.SetActive(true);

                // Clear existing buttons
                foreach (Transform child in productionPanel.transform)
                {
                    if (child.gameObject.GetComponent<Button>() != null)
                    {
                        Destroy(child.gameObject);
                    }
                }

                // Populate Trainable Units
                if (producer != null)
                {
                    foreach (var trainable in producer.TrainableUnits)
                    {
                        if (trainable == null) continue;

                        GameObject btnObj = Instantiate(productionButtonPrefab, productionPanel.transform);
                        var button = btnObj.GetComponent<Button>();
                        if (button != null)
                        {
                            var data = trainable;
                            button.onClick.AddListener(() =>
                            {
                                producer.TryQueueUnit(data);
                            });

                            unitButtons.Add(button, trainable);
                            ConfigureButtonVisuals(button, trainable.UnitIcon, trainable.UnitName);
                        }
                    }
                }

                // Populate Researchable Upgrades
                if (researcher != null)
                {
                    foreach (var upgrade in researcher.ResearchableUpgrades)
                    {
                        if (upgrade == null) continue;

                        GameObject btnObj = Instantiate(productionButtonPrefab, productionPanel.transform);
                        var button = btnObj.GetComponent<Button>();
                        if (button != null)
                        {
                            var data = upgrade;
                            button.onClick.AddListener(() =>
                            {
                                researcher.TryQueueResearch(data);
                                UpdateSelectionUI();
                            });

                            upgradeButtons.Add(button, upgrade);
                            ConfigureButtonVisuals(button, upgrade.Icon, upgrade.UpgradeName);

                            if (UpgradeManager.Instance != null)
                            {
                                bool isCompleted = UpgradeManager.Instance.IsUpgradeCompleted(building.Faction, upgrade);
                                bool prereqsMet = UpgradeManager.Instance.ArePrerequisitesMet(building.Faction, upgrade);

                                if (isCompleted)
                                {
                                    button.interactable = false;
                                }
                                else if (!prereqsMet)
                                {
                                    button.interactable = false;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                activeProducer = null;
                activeResearcher = null;
                if (productionPanel != null)
                {
                    productionPanel.SetActive(false);
                }
            }
        }

        private void HandleGlobalUpgradeCompleted(RTSFramework.Factions.Faction faction, UpgradeData upgrade)
        {
            if (SelectionManager.Instance != null && SelectionManager.Instance.SelectedObjects.Count == 1)
            {
                UpdateSelectionUI();
            }
        }

        private Combat.Health currentSelectedHealth;

        private void SubscribeToHealth(Combat.Health newHealth)
        {
            if (currentSelectedHealth != null)
            {
                currentSelectedHealth.OnHealthChanged -= HandleSelectedHealthChanged;
            }

            currentSelectedHealth = newHealth;

            if (currentSelectedHealth != null)
            {
                currentSelectedHealth.OnHealthChanged += HandleSelectedHealthChanged;
                HandleSelectedHealthChanged(currentSelectedHealth.CurrentHealth, currentSelectedHealth.MaxHealth);
            }
        }

        private void HandleSelectedHealthChanged(float current, float max)
        {
            if (unitCountText != null && SelectionManager.Instance != null && SelectionManager.Instance.SelectedObjects.Count == 1)
            {
                unitCountText.text = $"HP: {Mathf.RoundToInt(current)} / {Mathf.RoundToInt(max)}";
            }
        }

        private static Sprite whiteSprite;
        private static Sprite GetWhiteSprite()
        {
            if (whiteSprite == null)
            {
                Texture2D tex = new Texture2D(2, 2);
                for (int y = 0; y < 2; y++)
                {
                    for (int x = 0; x < 2; x++)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                }
                tex.Apply();
                whiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            }
            return whiteSprite;
        }
    }
}
