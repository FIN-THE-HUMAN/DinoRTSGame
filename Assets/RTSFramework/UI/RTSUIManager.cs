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

            UpdateSelectionUI();
        }

        private void OnDestroy()
        {
            if (SelectionManager.Instance != null)
            {
                SelectionManager.Instance.OnSelectionChanged -= UpdateSelectionUI;
            }
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.OnUpgradeCompleted -= HandleGlobalUpgradeCompleted;
            }
            SubscribeToHealth(null);
        }

        private void Update()
        {
            if (productionProgressBar == null) return;

            bool showProgress = false;
            float progressVal = 0f;
            string progressText = "";

            if (activeProducer != null && activeProducer.QueueCount > 0)
            {
                showProgress = true;
                progressVal = activeProducer.TrainingProgress;
                progressText = $"Queue: {activeProducer.QueueCount} (Training: {activeProducer.CurrentActiveUnit.UnitName})";
            }
            else if (activeResearcher != null && activeResearcher.QueueCount > 0)
            {
                showProgress = true;
                progressVal = activeResearcher.ResearchProgress;
                progressText = $"Queue: {activeResearcher.QueueCount} (Researching: {activeResearcher.CurrentActiveResearch.UpgradeName})";
            }

            productionProgressBar.gameObject.SetActive(showProgress);
            if (showProgress)
            {
                productionProgressBar.value = progressVal;
                var txt = productionProgressBar.GetComponentInChildren<TMP_Text>();
                if (txt != null)
                {
                    txt.text = progressText;
                }
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
                activeProducer = null;
                return;
            }

            selectionPanel.SetActive(true);

            // Determine display details
            string leadName = leadSelectable.GameObject.name;
            Sprite leadIcon = null;

            var unit = leadSelectable.GameObject.GetComponent<UnitController>();
            var building = leadSelectable.GameObject.GetComponent<Building>();

            if (unit != null)
            {
                leadName = unit.UnitData != null ? unit.UnitData.UnitName : unit.gameObject.name;
                leadIcon = unit.UnitData != null ? unit.UnitData.UnitIcon : null;
            }
            else if (building != null)
            {
                leadName = building.BuildingData != null ? building.BuildingData.BuildingName : building.gameObject.name;
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
                    var health = leadSelectable.GameObject.GetComponent<Combat.Health>();
                    SubscribeToHealth(health);
                    if (health == null)
                    {
                        unitCountText.text = "";
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

            // --- BUILD PANEL POPULATION ---
            if (buildPanel != null)
            {
                foreach (Transform child in buildPanel.transform)
                {
                    Destroy(child.gameObject);
                }
            }

            var builder = leadSelectable.GameObject.GetComponent<BuilderComponent>();
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
                if (buildPanel != null)
                {
                    buildPanel.SetActive(false);
                }
            }

            // --- PRODUCTION / RESEARCH PANEL POPULATION ---
            var producer = leadSelectable.GameObject.GetComponent<UnitProductionComponent>();
            var researcher = leadSelectable.GameObject.GetComponent<TechnologyResearchComponent>();
            bool isPlayerOwnedBuilding = leadSelectable.IsPlayerOwned && building != null;

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
                        var text = btnObj.GetComponentInChildren<TMP_Text>();
                        if (text != null)
                        {
                            text.text = trainable.UnitName;
                        }

                        var button = btnObj.GetComponent<Button>();
                        if (button != null)
                        {
                            var data = trainable;
                            button.onClick.AddListener(() =>
                            {
                                producer.TryQueueUnit(data);
                            });
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
                        var text = btnObj.GetComponentInChildren<TMP_Text>();
                        if (text != null)
                        {
                            text.text = upgrade.UpgradeName;
                        }

                        var button = btnObj.GetComponent<Button>();
                        if (button != null)
                        {
                            var data = upgrade;
                            button.onClick.AddListener(() =>
                            {
                                researcher.TryQueueResearch(data);
                                UpdateSelectionUI();
                            });

                            if (UpgradeManager.Instance != null)
                            {
                                bool isCompleted = UpgradeManager.Instance.IsUpgradeCompleted(building.Faction, upgrade);
                                bool prereqsMet = UpgradeManager.Instance.ArePrerequisitesMet(building.Faction, upgrade);

                                if (isCompleted)
                                {
                                    button.interactable = false;
                                    if (text != null) text.text = $"{upgrade.UpgradeName}\n(Done)";
                                }
                                else if (!prereqsMet)
                                {
                                    button.interactable = false;
                                    if (text != null) text.text = $"{upgrade.UpgradeName}\n(Locked)";
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
    }
}
