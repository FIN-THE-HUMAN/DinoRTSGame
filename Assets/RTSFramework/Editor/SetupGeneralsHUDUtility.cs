#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RTSFramework.Editor
{
    public static class SetupGeneralsHUDUtility
    {
        [MenuItem("RTS Debug/Setup Generals HUD Layout")]
        public static void SetupGeneralsHUD()
        {
            // Find Canvas first
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("SetupGeneralsHUD: Canvas object not found in the active scene!");
                return;
            }

            // 1. Find SelectionPanel under Canvas (supporting inactive state)
            Transform panelTrans = canvas.transform.Find("SelectionPanel");
            if (panelTrans == null)
            {
                Debug.LogError("SetupGeneralsHUD: SelectionPanel GameObject not found under Canvas!");
                return;
            }
            GameObject selectionPanel = panelTrans.gameObject;

            // Set size and anchors of the main SelectionPanel bottom bar
            var barRt = selectionPanel.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.5f, 0f);
            barRt.anchorMax = new Vector2(0.5f, 0f);
            barRt.pivot = new Vector2(0.5f, 0f);
            barRt.anchoredPosition = new Vector2(0f, 15f); // Lifted slightly from bottom edge
            barRt.sizeDelta = new Vector2(750f, 120f); // 750px width, 120px height

            // 2. Position Selection Info (Unit Name, HP) inside the bar on the left
            Transform nameTrans = selectionPanel.transform.Find("UnitNameText");
            Transform countTrans = selectionPanel.transform.Find("UnitCountText");
            Transform portraitTrans = selectionPanel.transform.Find("PortraitImage");

            // Setup a container for the info text to align them cleanly on the left (supporting inactive state)
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

            if (portraitTrans != null)
            {
                portraitTrans.SetParent(infoContainer.transform, false);
                var portRt = portraitTrans.GetComponent<RectTransform>();
                portRt.anchorMin = new Vector2(0f, 0.5f);
                portRt.anchorMax = new Vector2(0f, 0.5f);
                portRt.pivot = new Vector2(0f, 0.5f);
                portRt.anchoredPosition = new Vector2(0f, 0f);
                portRt.sizeDelta = new Vector2(90f, 90f); // Portrait box
            }

            if (nameTrans != null)
            {
                nameTrans.SetParent(infoContainer.transform, false);
                var nameRt = nameTrans.GetComponent<RectTransform>();
                nameRt.anchorMin = new Vector2(0f, 0.5f);
                nameRt.anchorMax = new Vector2(1f, 0.5f);
                nameRt.pivot = new Vector2(0f, 1f); // top-aligned
                nameRt.anchoredPosition = new Vector2(105f, 25f);
                nameRt.sizeDelta = new Vector2(-110f, 35f);

                var txt = nameTrans.GetComponent<TMP_Text>();
                if (txt != null)
                {
                    txt.alignment = TextAlignmentOptions.Left;
                    txt.fontSize = 24;
                }
            }

            if (countTrans != null)
            {
                countTrans.SetParent(infoContainer.transform, false);
                var countRt = countTrans.GetComponent<RectTransform>();
                countRt.anchorMin = new Vector2(0f, 0.5f);
                countRt.anchorMax = new Vector2(1f, 0.5f);
                countRt.pivot = new Vector2(0f, 0f); // bottom-aligned
                countRt.anchoredPosition = new Vector2(105f, -25f);
                countRt.sizeDelta = new Vector2(-110f, 35f);

                var txt = countTrans.GetComponent<TMP_Text>();
                if (txt != null)
                {
                    txt.alignment = TextAlignmentOptions.Left;
                    txt.fontSize = 18;
                }
            }

            // 3. Position BuildPanel and ProductionPanel on the right side of the bar
            Transform buildTrans = canvas.transform.Find("BuildPanel");
            GameObject buildPanel = buildTrans != null ? buildTrans.gameObject : null;
            if (buildPanel != null)
            {
                buildPanel.transform.SetParent(selectionPanel.transform, false);
                var bRt = buildPanel.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(1f, 0.5f);
                bRt.anchorMax = new Vector2(1f, 0.5f);
                bRt.pivot = new Vector2(1f, 0.5f);
                bRt.anchoredPosition = new Vector2(-25f, 0f);
                bRt.sizeDelta = new Vector2(250f, 100f); // Fits 4 columns of 55x55 cells
            }

            Transform prodTrans = canvas.transform.Find("ProductionPanel");
            GameObject prodPanel = prodTrans != null ? prodTrans.gameObject : null;
            if (prodPanel != null)
            {
                prodPanel.transform.SetParent(selectionPanel.transform, false);
                var pRt = prodPanel.GetComponent<RectTransform>();
                pRt.anchorMin = new Vector2(1f, 0.5f);
                pRt.anchorMax = new Vector2(1f, 0.5f);
                pRt.pivot = new Vector2(1f, 0.5f);
                pRt.anchoredPosition = new Vector2(-25f, 0f);
                pRt.sizeDelta = new Vector2(250f, 100f); // Fits 4 columns of 55x55 cells
            }

            // Remove or reposition the old separate progress bar so it sits neatly above the HUD bar!
            Slider progressBar = Object.FindAnyObjectByType<Slider>();
            if (progressBar != null && progressBar.gameObject.name == "ProductionProgressBar")
            {
                progressBar.transform.SetParent(selectionPanel.transform, false);
                var prRt = progressBar.GetComponent<RectTransform>();
                prRt.anchorMin = new Vector2(0.5f, 1f);
                prRt.anchorMax = new Vector2(0.5f, 1f);
                prRt.pivot = new Vector2(0.5f, 0f);
                prRt.anchoredPosition = new Vector2(0f, 5f); // 5px above selection bar
                prRt.sizeDelta = new Vector2(400f, 15f); // centered, 400px wide progress bar
                
                progressBar.gameObject.SetActive(false); // Disable old slider
            }

            // Hide the panels by default in Edit Mode so the viewport is clean
            selectionPanel.SetActive(false);
            if (buildPanel != null) buildPanel.SetActive(false);
            if (prodPanel != null) prodPanel.SetActive(false);

            EditorUtility.SetDirty(selectionPanel);
            if (buildPanel != null) EditorUtility.SetDirty(buildPanel);
            if (prodPanel != null) EditorUtility.SetDirty(prodPanel);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(selectionPanel.scene);

            Debug.Log("SetupGeneralsHUD: HUD Layout rearranged to C&C Generals command style!");
        }
    }
}
#endif
