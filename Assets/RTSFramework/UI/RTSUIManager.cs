using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTSFramework.Selection;
using RTSFramework.Units;
using RTSFramework.Buildings;

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
            UpdateSelectionUI();
        }

        private void OnDestroy()
        {
            if (SelectionManager.Instance != null)
            {
                SelectionManager.Instance.OnSelectionChanged -= UpdateSelectionUI;
            }
        }

        public void UpdateSelectionUI()
        {
            if (selectionPanel == null) return;

            var selectedObjects = SelectionManager.Instance.SelectedObjects;

            // Find all selected UnitControllers
            List<UnitController> selectedUnits = new List<UnitController>();
            foreach (var selected in selectedObjects)
            {
                if (selected == null || selected.Equals(null)) continue;
                if (selected.GameObject.TryGetComponent<UnitController>(out var unit))
                {
                    selectedUnits.Add(unit);
                }
            }

            if (selectedUnits.Count == 0)
            {
                selectionPanel.SetActive(false);
                if (buildPanel != null)
                {
                    buildPanel.SetActive(false);
                }
                return;
            }

            // Find the "lead" unit based on selection priority (highest priority wins)
            UnitController leadUnit = SelectionManager.Instance.GetLeadSelectedUnit();

            // If no unit has UnitData, default to the first one
            if (leadUnit == null)
            {
                leadUnit = selectedUnits[0];
            }

            string leadName = leadUnit.UnitData != null ? leadUnit.UnitData.UnitName : leadUnit.name;

            // Update UI elements
            selectionPanel.SetActive(true);

            if (unitNameText != null)
            {
                unitNameText.text = leadName;
            }

            if (unitCountText != null)
            {
                unitCountText.text = selectedUnits.Count > 1 ? $"x{selectedUnits.Count}" : "";
            }

            if (portraitImage != null)
            {
                if (leadUnit.UnitData != null && leadUnit.UnitData.UnitIcon != null)
                {
                    portraitImage.gameObject.SetActive(true);
                    portraitImage.sprite = leadUnit.UnitData.UnitIcon;
                }
                else
                {
                    // If no icon, hide the portrait container
                    portraitImage.sprite = null;
                    portraitImage.gameObject.SetActive(false);
                }
            }

            // --- BUILD PANEL POPULATION ---
            // Clear existing build buttons
            if (buildPanel != null)
            {
                foreach (Transform child in buildPanel.transform)
                {
                    Destroy(child.gameObject);
                }
            }

            var builder = leadUnit.GetComponent<BuilderComponent>();
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
        }
    }
}
