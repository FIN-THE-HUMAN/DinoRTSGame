using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTSFramework.Selection;
using RTSFramework.Combat;
using RTSFramework.Fog;

namespace RTSFramework.Input
{
    public class RTSCheatConsole : MonoBehaviour
    {
        public static RTSCheatConsole Instance { get; private set; }

        private GameObject consoleCanvasObj;
        private GameObject consolePanel;
        private TMP_InputField inputField;

        public bool IsOpen { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeOnLoad()
        {
            if (Instance == null)
            {
                new GameObject("RTSCheatConsole", typeof(RTSCheatConsole));
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                CreateConsoleUI();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void CreateConsoleUI()
        {
            // 1. Create Canvas
            GameObject canvasObj = new GameObject("CheatConsoleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.transform.SetParent(transform, false);
            var canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // Render above standard gameplay UI

            // 2. Create Panel Container
            consolePanel = new GameObject("ConsolePanel", typeof(RectTransform), typeof(Image));
            consolePanel.transform.SetParent(canvasObj.transform, false);
            var panelRt = consolePanel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 1f); // Top Center
            panelRt.anchorMax = new Vector2(0.5f, 1f);
            panelRt.pivot = new Vector2(0.5f, 1f);
            panelRt.anchoredPosition = new Vector2(0f, -20f); // 20px margin from top
            panelRt.sizeDelta = new Vector2(400f, 40f); // 400px wide, 40px tall
            
            var panelImage = consolePanel.GetComponent<Image>();
            panelImage.color = new Color(0.08f, 0.08f, 0.08f, 0.88f); // Dark translucent bar

            // 3. Create Input Field GameObject
            GameObject inputFieldObj = new GameObject("InputField", typeof(RectTransform));
            inputFieldObj.transform.SetParent(consolePanel.transform, false);
            var inputRt = inputFieldObj.GetComponent<RectTransform>();
            inputRt.anchorMin = Vector2.zero;
            inputRt.anchorMax = Vector2.one;
            inputRt.sizeDelta = new Vector2(-20f, -10f); // 10px horizontal padding, 5px vertical padding
            inputRt.anchoredPosition = Vector2.zero;

            // 4. Create Text Area Viewport (required by TextMeshPro InputField)
            GameObject viewportObj = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            viewportObj.transform.SetParent(inputFieldObj.transform, false);
            var vpRt = viewportObj.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.sizeDelta = Vector2.zero;

            // 5. Create Text Display
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(viewportObj.transform, false);
            var textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            var textComponent = textObj.GetComponent<TextMeshProUGUI>();
            textComponent.fontSize = 14f;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Left;

            // 6. Create Placeholder Display
            GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderObj.transform.SetParent(viewportObj.transform, false);
            var phRt = placeholderObj.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.sizeDelta = Vector2.zero;

            var placeholderComponent = placeholderObj.GetComponent<TextMeshProUGUI>();
            placeholderComponent.fontSize = 14f;
            placeholderComponent.color = new Color(0.55f, 0.55f, 0.55f, 0.75f);
            placeholderComponent.alignment = TextAlignmentOptions.Left;
            placeholderComponent.text = "Enter cheat code...";

            // 7. Assemble TMP_InputField
            inputField = inputFieldObj.AddComponent<TMP_InputField>();
            inputField.textViewport = vpRt;
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholderComponent;
            inputField.onSubmit.AddListener(OnSubmit);

            // Initially hide
            canvasObj.SetActive(false);
            consoleCanvasObj = canvasObj;
        }

        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;

            // Layout independent Tilde key check (Key.Backquote physical key code)
            if (keyboard.backquoteKey.wasPressedThisFrame)
            {
                ToggleConsole();
            }

            // Close without executing if Escape is pressed while open
            if (IsOpen && keyboard.escapeKey.wasPressedThisFrame)
            {
                CloseConsole();
            }
        }

        public void ToggleConsole()
        {
            if (IsOpen)
                CloseConsole();
            else
                OpenConsole();
        }

        public void OpenConsole()
        {
            if (consoleCanvasObj == null) return;

            IsOpen = true;
            consoleCanvasObj.SetActive(true);
            
            // Focus and select input field
            inputField.text = "";
            inputField.ActivateInputField();
            inputField.Select();
        }

        public void CloseConsole()
        {
            if (consoleCanvasObj == null) return;

            IsOpen = false;
            inputField.DeactivateInputField();
            consoleCanvasObj.SetActive(false);
        }

        private void OnSubmit(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                CloseConsole();
                return;
            }

            string cleanCmd = text.Trim().ToLower();
            Debug.Log($"Cheat Console Command: '{cleanCmd}'");

            if (cleanCmd == "fow" || cleanCmd == "iseedeadpeople")
            {
                if (FogOfWarManager.Instance != null)
                {
                    FogOfWarManager.Instance.ToggleFogCheat();
                }
            }
            else if (cleanCmd.StartsWith("wood ") || cleanCmd.StartsWith("wood"))
            {
                string[] parts = cleanCmd.Split(' ');
                int amount = 1000;
                if (parts.Length > 1 && int.TryParse(parts[1], out int val))
                {
                    amount = val;
                }
                if (Resources.ResourceManager.Instance != null)
                {
                    Resources.ResourceManager.Instance.AddResource(Resources.ResourceType.Wood, amount);
                    Debug.Log($"Cheat: added {amount} Wood.");
                }
            }
            else if (cleanCmd.StartsWith("gold ") || cleanCmd.StartsWith("gold"))
            {
                string[] parts = cleanCmd.Split(' ');
                int amount = 1000;
                if (parts.Length > 1 && int.TryParse(parts[1], out int val))
                {
                    amount = val;
                }
                if (Resources.ResourceManager.Instance != null)
                {
                    Resources.ResourceManager.Instance.AddResource(Resources.ResourceType.Gold, amount);
                    Debug.Log($"Cheat: added {amount} Gold.");
                }
            }
            else if (cleanCmd == "kill")
            {
                // Kill all currently selected units/buildings
                if (SelectionManager.Instance != null)
                {
                    // Copy selection list because elements will be destroyed and unregistered dynamically during loop
                    var selected = new List<ISelectable>(SelectionManager.Instance.SelectedObjects);
                    foreach (var sel in selected)
                    {
                        if (sel != null && !sel.Equals(null))
                        {
                            var health = sel.GameObject.GetComponent<Health>();
                            if (health != null)
                            {
                                health.TakeDamage(99999f, null);
                            }
                        }
                    }
                    Debug.Log("Cheat: killed selected entities.");
                }
            }

            CloseConsole();
        }
    }
}
