#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RTSFramework.Gameplay;

namespace RTSFramework.Editor
{
    public static class SetupWinLossUtility
    {
        [MenuItem("RTS Debug/Setup Win-Loss Conditions")]
        public static void SetupWinLossConditions()
        {
            // 1. Locate Canvas
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("SetupWinLoss: Canvas object not found in the active scene! Please create a UI Canvas first.");
                return;
            }

            // 2. Create or find GameLoopManager GameObject
            GameObject gmObj = GameObject.Find("GameLoopManager");
            if (gmObj == null)
            {
                gmObj = new GameObject("GameLoopManager");
                Debug.Log("SetupWinLoss: Spawend 'GameLoopManager' GameObject in scene.");
            }

            var gm = gmObj.GetComponent<GameLoopManager>();
            if (gm == null) gm = gmObj.AddComponent<GameLoopManager>();

            // 3. Create Victory Panel Overlay
            GameObject vicPanel = GameObject.Find("VictoryPanelOverlay");
            if (vicPanel != null) Object.DestroyImmediate(vicPanel);

            vicPanel = CreatePanel(canvas.transform, "VictoryPanelOverlay", new Color(0.04f, 0.04f, 0.04f, 0.88f));
            CreateText(vicPanel.transform, "ПОБЕДА", new Color(1.0f, 0.84f, 0.0f), 60, 70f);
            
            Button vicRestartBtn;
            CreateButton(vicPanel.transform, "RestartButton", "Играть снова", -30f, out vicRestartBtn);
            
            Button vicQuitBtn;
            CreateButton(vicPanel.transform, "QuitButton", "Выйти из игры", -90f, out vicQuitBtn);

            // 4. Create Defeat Panel Overlay
            GameObject defPanel = GameObject.Find("DefeatPanelOverlay");
            if (defPanel != null) Object.DestroyImmediate(defPanel);

            defPanel = CreatePanel(canvas.transform, "DefeatPanelOverlay", new Color(0.04f, 0.04f, 0.04f, 0.88f));
            CreateText(defPanel.transform, "ПОРАЖЕНИЕ", new Color(0.9f, 0.22f, 0.18f), 60, 70f);

            Button defRestartBtn;
            CreateButton(defPanel.transform, "RestartButton", "Играть снова", -30f, out defRestartBtn);

            Button defQuitBtn;
            CreateButton(defPanel.transform, "QuitButton", "Выйти из игры", -90f, out defQuitBtn);

            // 5. Connect UI elements to the GameLoopManager inspector properties
            SerializedObject serGm = new SerializedObject(gm);
            serGm.FindProperty("victoryPanel").objectReferenceValue = vicPanel;
            serGm.FindProperty("defeatPanel").objectReferenceValue = defPanel;
            serGm.FindProperty("victoryRestartButton").objectReferenceValue = vicRestartBtn;
            serGm.FindProperty("victoryQuitButton").objectReferenceValue = vicQuitBtn;
            serGm.FindProperty("defeatRestartButton").objectReferenceValue = defRestartBtn;
            serGm.FindProperty("defeatQuitButton").objectReferenceValue = defQuitBtn;
            serGm.ApplyModifiedProperties();

            // Mark Scene and Asset Database dirty to ensure changes save
            EditorUtility.SetDirty(gm);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gmObj.scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("SetupWinLoss: Victory/Defeat overlays constructed and bound to GameLoopManager successfully!");
        }

        private static GameObject CreatePanel(Transform parent, string name, Color bgColor)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);

            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            var img = panel.AddComponent<Image>();
            img.color = bgColor;

            return panel;
        }

        private static GameObject CreateText(Transform parent, string textVal, Color textColor, int fontSize, float yOffset)
        {
            GameObject textObj = new GameObject("TitleText", typeof(RectTransform));
            textObj.transform.SetParent(parent, false);

            var rt = textObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(500f, 100f);
            rt.anchoredPosition = new Vector3(0f, yOffset, 0f);

            var text = textObj.AddComponent<Text>();
            text.text = textVal;
            text.color = textColor;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return textObj;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, float yOffset, out Button buttonComponent)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform));
            btnObj.transform.SetParent(parent, false);

            var rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(200f, 45f);
            rt.anchoredPosition = new Vector3(0f, yOffset, 0f);

            // Add Image component (sleek dark grey button)
            var img = btnObj.AddComponent<Image>();
            img.color = new Color(0.18f, 0.18f, 0.18f, 0.95f);

            // Add Button component
            buttonComponent = btnObj.AddComponent<Button>();

            // Add label text child
            GameObject labelObj = new GameObject("Text", typeof(RectTransform));
            labelObj.transform.SetParent(btnObj.transform, false);

            var labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.sizeDelta = Vector2.zero;
            labelRt.anchoredPosition = Vector2.zero;

            var txt = labelObj.AddComponent<Text>();
            txt.text = label;
            txt.color = Color.white;
            txt.fontSize = 16;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return btnObj;
        }
    }
}
#endif
