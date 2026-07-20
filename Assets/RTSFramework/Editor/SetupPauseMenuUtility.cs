#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTSFramework.UI;

namespace RTSFramework.Editor
{
    public static class SetupPauseMenuUtility
    {
        [MenuItem("RTS Debug/Setup Pause Menu")]
        public static void SetupPauseMenu()
        {
            // 1. Find Canvas
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("SetupPauseMenu: Canvas not found in the active scene!");
                return;
            }

            // 2. Re-create PauseMenu root object and clean up rogue dropdowns/toggles under the Canvas
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = canvas.transform.GetChild(i);
                if (child.name.Contains("Dropdown") || child.name == "PauseMenu" || child.name.Contains("ToggleBox") || child.name.Contains("Toggle"))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            GameObject pauseMenuObj = new GameObject("PauseMenu", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pauseMenuObj.transform.SetParent(canvas.transform, false);

            var rootRt = pauseMenuObj.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = Vector2.zero;
            rootRt.sizeDelta = Vector2.zero;

            // Translucent glassmorphism background (disabled by default in editor)
            var rootImg = pauseMenuObj.GetComponent<Image>();
            rootImg.color = new Color(0.04f, 0.05f, 0.08f, 0.82f); // Dark tint vignette
            rootImg.enabled = false;

            // 3. Create menuRoot (centered box containing panels)
            GameObject menuRoot = new GameObject("MenuRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            menuRoot.transform.SetParent(pauseMenuObj.transform, false);
            var boxRt = menuRoot.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.5f, 0.5f);
            boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.pivot = new Vector2(0.5f, 0.5f);
            boxRt.sizeDelta = new Vector2(350f, 520f); // Sleek taller box
            
            var boxImg = menuRoot.GetComponent<Image>();
            boxImg.color = new Color(0.1f, 0.12f, 0.16f, 0.96f); // Premium dark charcoal

            // Add drop shadow and border outline to MenuRoot
            var boxOutline = menuRoot.AddComponent<Outline>();
            boxOutline.effectColor = new Color(0.2f, 0.25f, 0.35f, 0.5f); // Subtle sci-fi blue outline
            boxOutline.effectDistance = new Vector2(1.5f, 1.5f);

            var boxShadow = menuRoot.AddComponent<Shadow>();
            boxShadow.effectColor = new Color(0f, 0f, 0f, 0.5f); // Soft depth shadow
            boxShadow.effectDistance = new Vector2(6f, -6f);

            // 4. Create Main Sub-Panel
            GameObject mainSubPanel = new GameObject("MainSubPanel", typeof(RectTransform));
            mainSubPanel.transform.SetParent(menuRoot.transform, false);
            var mainRt = mainSubPanel.GetComponent<RectTransform>();
            mainRt.anchorMin = Vector2.zero;
            mainRt.anchorMax = Vector2.one;
            mainRt.sizeDelta = new Vector2(-20f, -20f); // 10px margins

            var mainLayout = mainSubPanel.AddComponent<VerticalLayoutGroup>();
            mainLayout.spacing = 15f;
            mainLayout.padding = new RectOffset(10, 10, 25, 25);
            mainLayout.childControlHeight = false;
            mainLayout.childControlWidth = false;
            mainLayout.childForceExpandHeight = false;
            mainLayout.childForceExpandWidth = false;
            mainLayout.childAlignment = TextAnchor.UpperCenter;

            // Main Title
            GameObject mainTitle = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
            mainTitle.transform.SetParent(mainSubPanel.transform, false);
            mainTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 50f);
            var titleText = mainTitle.AddComponent<TextMeshProUGUI>();
            titleText.text = "ПАУЗА";
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAlignmentOptions.Center;

            // Main buttons
            Button resumeBtn = CreateButton("ResumeButton", "ПРОДОЛЖИТЬ", mainSubPanel.transform, out TMP_Text _);
            Button settingsBtn = CreateButton("SettingsButton", "НАСТРОЙКИ", mainSubPanel.transform, out TMP_Text _);
            Button restartBtn = CreateButton("RestartButton", "НАЧАТЬ ЗАНОВО", mainSubPanel.transform, out TMP_Text _);
            Button quitBtn = CreateButton("QuitButton", "ВЫЙТИ В WINDOWS", mainSubPanel.transform, out TMP_Text _);


            // 5. Create Settings Sub-Panel
            GameObject settingsSubPanel = new GameObject("SettingsSubPanel", typeof(RectTransform));
            settingsSubPanel.transform.SetParent(menuRoot.transform, false);
            var setRt = settingsSubPanel.GetComponent<RectTransform>();
            setRt.anchorMin = Vector2.zero;
            setRt.anchorMax = Vector2.one;
            setRt.sizeDelta = new Vector2(-20f, -20f);

            var setLayout = settingsSubPanel.AddComponent<VerticalLayoutGroup>();
            setLayout.spacing = 15f;
            setLayout.padding = new RectOffset(10, 10, 25, 25);
            setLayout.childControlHeight = false;
            setLayout.childControlWidth = false;
            setLayout.childForceExpandHeight = false;
            setLayout.childForceExpandWidth = false;
            setLayout.childAlignment = TextAnchor.UpperCenter;

            // Settings Title
            GameObject settingsTitle = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer));
            settingsTitle.transform.SetParent(settingsSubPanel.transform, false);
            settingsTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 40f);
            var sTitleText = settingsTitle.AddComponent<TextMeshProUGUI>();
            sTitleText.text = "НАСТРОЙКИ";
            sTitleText.fontSize = 24;
            sTitleText.fontStyle = FontStyles.Bold;
            sTitleText.color = Color.white;
            sTitleText.alignment = TextAlignmentOptions.Center;

            // Settings Controls
            Slider volumeSlider = CreateSlider("VolumeSetting", "ГРОМКОСТЬ", settingsSubPanel.transform);
            
            // Cycling Buttons
            Button qualityBtn = CreateButton("QualitySettingButton", "ГРАФИКА: ...", settingsSubPanel.transform, out TMP_Text qualityTextObj);
            Button resolutionBtn = CreateButton("ResolutionSettingButton", "РАЗРЕШЕНИЕ: ...", settingsSubPanel.transform, out TMP_Text resolutionTextObj);
            Button fullscreenBtn = CreateButton("FullscreenSettingButton", "ЭКРАН: ...", settingsSubPanel.transform, out TMP_Text fullscreenTextObj);

            // Back Button
            Button backBtn = CreateButton("BackButton", "НАЗАД", settingsSubPanel.transform, out TMP_Text _);

            // 6. Attach RTSPauseMenu and assign references
            var script = pauseMenuObj.AddComponent<RTSPauseMenu>();
            SerializedObject serialized = new SerializedObject(script);

            serialized.FindProperty("menuRoot").objectReferenceValue = menuRoot;
            serialized.FindProperty("mainSubPanel").objectReferenceValue = mainSubPanel;
            serialized.FindProperty("settingsSubPanel").objectReferenceValue = settingsSubPanel;

            serialized.FindProperty("resumeButton").objectReferenceValue = resumeBtn;
            serialized.FindProperty("settingsButton").objectReferenceValue = settingsBtn;
            serialized.FindProperty("restartButton").objectReferenceValue = restartBtn;
            serialized.FindProperty("quitButton").objectReferenceValue = quitBtn;

            serialized.FindProperty("volumeSlider").objectReferenceValue = volumeSlider;
            serialized.FindProperty("qualityButton").objectReferenceValue = qualityBtn;
            serialized.FindProperty("qualityText").objectReferenceValue = qualityTextObj;
            serialized.FindProperty("resolutionButton").objectReferenceValue = resolutionBtn;
            serialized.FindProperty("resolutionText").objectReferenceValue = resolutionTextObj;
            serialized.FindProperty("fullscreenButton").objectReferenceValue = fullscreenBtn;
            serialized.FindProperty("fullscreenText").objectReferenceValue = fullscreenTextObj;
            serialized.FindProperty("settingsBackButton").objectReferenceValue = backBtn;

            serialized.ApplyModifiedProperties();

            // Set off by default in Edit Mode
            menuRoot.SetActive(false);
            pauseMenuObj.SetActive(true);

            EditorUtility.SetDirty(pauseMenuObj);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(pauseMenuObj.scene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("SetupPauseMenu: Redesigned Pause & Settings menu built successfully!");
        }

        private static Button CreateButton(string name, string labelText, Transform parent, out TMP_Text textComponent)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            btnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, 40f); // Spaced premium width

            var img = btnObj.GetComponent<Image>();
            img.color = new Color(0.18f, 0.2f, 0.26f, 1f); 

            // Outline around button for premium look
            var btnOutline = btnObj.AddComponent<Outline>();
            btnOutline.effectColor = new Color(0.25f, 0.3f, 0.4f, 0.3f);
            btnOutline.effectDistance = new Vector2(1f, 1f);

            var btn = btnObj.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.18f, 0.2f, 0.26f, 1f);
            colors.highlightedColor = new Color(0.26f, 0.32f, 0.45f, 1f); // Sleek hover highlight
            colors.pressedColor = new Color(0.12f, 0.14f, 0.18f, 1f);
            colors.selectedColor = colors.normalColor;
            btn.colors = colors;
            
            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
            txtObj.transform.SetParent(btnObj.transform, false);
            var txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            var tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.fontSize = 14;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;

            textComponent = tmp;
            return btn;
        }

        private static Slider CreateSlider(string name, string labelText, Transform parent)
        {
            GameObject container = new GameObject(name, typeof(RectTransform));
            container.transform.SetParent(parent, false);
            container.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, 50f); // Space for label above

            // Label above the slider
            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
            labelObj.transform.SetParent(container.transform, false);
            var labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0.6f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.pivot = new Vector2(0.5f, 0.5f);
            labelRt.anchoredPosition = Vector2.zero;
            labelRt.sizeDelta = Vector2.zero;

            var tmp = labelObj.AddComponent<TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.fontSize = 12;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.7f, 0.75f, 0.82f, 1f); // Slate label
            tmp.alignment = TextAlignmentOptions.Center;

            // Slider track
            GameObject sliderObj = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(container.transform, false);
            var sliderRt = sliderObj.GetComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(0.05f, 0.1f);
            sliderRt.anchorMax = new Vector2(0.95f, 0.4f);
            sliderRt.pivot = new Vector2(0.5f, 0.5f);
            sliderRt.anchoredPosition = Vector2.zero;
            sliderRt.sizeDelta = Vector2.zero;

            var slider = sliderObj.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(sliderObj.transform, false);
            var bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            bgObj.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 1f);

            GameObject fillAreaObj = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            var faRt = fillAreaObj.GetComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero;
            faRt.anchorMax = Vector2.one;
            faRt.sizeDelta = Vector2.zero;

            GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            var fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.sizeDelta = Vector2.zero;
            fillObj.GetComponent<Image>().color = new Color(0.0f, 0.85f, 0.6f, 1f); // Emerald neon fill!

            slider.fillRect = fillRt;

            GameObject handleAreaObj = new GameObject("Handle Area", typeof(RectTransform));
            handleAreaObj.transform.SetParent(sliderObj.transform, false);
            var haRt = handleAreaObj.GetComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero;
            haRt.anchorMax = Vector2.one;
            haRt.sizeDelta = Vector2.zero;

            GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleObj.transform.SetParent(handleAreaObj.transform, false);
            var handleRt = handleObj.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(16f, 16f);
            handleObj.GetComponent<Image>().color = Color.white;

            slider.handleRect = handleRt;

            return slider;
        }
    }
}
#endif
