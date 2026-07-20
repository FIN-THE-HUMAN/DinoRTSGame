using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RTSFramework.Gameplay;
using System.Collections.Generic;

namespace RTSFramework.UI
{
    [System.Serializable]
    public struct CustomResolution
    {
        public int width;
        public int height;

        public CustomResolution(int w, int h)
        {
            width = w;
            height = h;
        }
    }

    public class RTSPauseMenu : MonoBehaviour
    {
        public static RTSPauseMenu Instance { get; private set; }

        [Header("UI Panels")]
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private GameObject mainSubPanel;
        [SerializeField] private GameObject settingsSubPanel;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;

        [Header("Settings Controls")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Button qualityButton;
        [SerializeField] private TMP_Text qualityText;
        [SerializeField] private Button resolutionButton;
        [SerializeField] private TMP_Text resolutionText;
        [SerializeField] private Button fullscreenButton;
        [SerializeField] private TMP_Text fullscreenText;
        [SerializeField] private Button settingsBackButton;

        private bool isPaused = false;
        private bool isFullscreen = false;
        private List<CustomResolution> resolutionsList = new List<CustomResolution>();
        private int currentQualityIndex = 0;
        private int currentResolutionIndex = 0;

        public bool IsPaused => isPaused;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Hook up main buttons
            if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

            // Hook up settings controls
            if (settingsBackButton != null) settingsBackButton.onClick.AddListener(CloseSettings);
            if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            if (qualityButton != null) qualityButton.onClick.AddListener(CycleQuality);
            if (resolutionButton != null) resolutionButton.onClick.AddListener(CycleResolution);
            if (fullscreenButton != null) fullscreenButton.onClick.AddListener(ToggleFullscreenMode);
        }

        private void Start()
        {
            // Initially close the menu
            if (menuRoot != null) menuRoot.SetActive(false);
            if (mainSubPanel != null) mainSubPanel.SetActive(true);
            if (settingsSubPanel != null) settingsSubPanel.SetActive(false);

            var rootImg = GetComponent<Image>();
            if (rootImg != null) rootImg.enabled = false;

            // Initialize Settings UI values
            InitVolume();
            InitQuality();
            InitResolutions();
            InitFullscreen();
        }

        private void Update()
        {
            // Toggle pause menu on Escape key (with fallback for Old Input System)
            bool escPressed = false;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                escPressed = keyboard.escapeKey.wasPressedThisFrame;
            }
            else
            {
                escPressed = UnityEngine.Input.GetKeyDown(KeyCode.Escape);
            }

            if (escPressed)
            {
                // Verify cheat console isn't handling escape
                if (Input.RTSCheatConsole.Instance == null || !Input.RTSCheatConsole.Instance.IsOpen)
                {
                    // Verify game hasn't ended already
                    if (GameLoopManager.Instance != null && !GameLoopManager.Instance.IsGameEnded)
                    {
                        TogglePause();
                    }
                }
            }
        }

        public void TogglePause()
        {
            isPaused = !isPaused;

            var rootImg = GetComponent<Image>();
            if (rootImg != null) rootImg.enabled = isPaused;

            if (isPaused)
            {
                Time.timeScale = 0f;
                if (menuRoot != null) menuRoot.SetActive(true);
                if (mainSubPanel != null) mainSubPanel.SetActive(true);
                if (settingsSubPanel != null) settingsSubPanel.SetActive(false);
            }
            else
            {
                Time.timeScale = 1f;
                if (menuRoot != null) menuRoot.SetActive(false);
            }
        }

        public void ResumeGame()
        {
            if (isPaused) TogglePause();
        }

        public void OpenSettings()
        {
            if (mainSubPanel != null) mainSubPanel.SetActive(false);
            if (settingsSubPanel != null) settingsSubPanel.SetActive(true);
        }

        public void CloseSettings()
        {
            if (mainSubPanel != null) mainSubPanel.SetActive(true);
            if (settingsSubPanel != null) settingsSubPanel.SetActive(false);
        }

        public void RestartGame()
        {
            if (GameLoopManager.Instance != null)
            {
                GameLoopManager.Instance.RestartGame();
            }
        }

        public void QuitGame()
        {
            if (GameLoopManager.Instance != null)
            {
                GameLoopManager.Instance.QuitGame();
            }
        }

        // --- Settings Management ---

        private void InitVolume()
        {
            if (volumeSlider != null)
            {
                volumeSlider.value = AudioListener.volume;
            }
        }

        private void OnVolumeChanged(float value)
        {
            AudioListener.volume = value;
        }

        private void InitQuality()
        {
            currentQualityIndex = QualitySettings.GetQualityLevel();
            UpdateQualityButtonText();
        }

        private void CycleQuality()
        {
            string[] names = QualitySettings.names;
            if (names.Length == 0) return;

            currentQualityIndex = (currentQualityIndex + 1) % names.Length;
            QualitySettings.SetQualityLevel(currentQualityIndex, true);
            UpdateQualityButtonText();
        }

        private void UpdateQualityButtonText()
        {
            string[] names = QualitySettings.names;
            if (qualityText != null && currentQualityIndex >= 0 && currentQualityIndex < names.Length)
            {
                qualityText.text = $"ГРАФИКА: {names[currentQualityIndex].ToUpper()}";
            }
        }

        private void InitFullscreen()
        {
            isFullscreen = Screen.fullScreen;
            UpdateFullscreenButtonText(isFullscreen);
        }

        private void ToggleFullscreenMode()
        {
            isFullscreen = !isFullscreen;
            Screen.fullScreen = isFullscreen;
            UpdateFullscreenButtonText(isFullscreen);
        }

        private void UpdateFullscreenButtonText(bool isFull)
        {
            if (fullscreenText != null)
            {
                fullscreenText.text = isFull ? "ЭКРАН: ПОЛНОЭКРАННЫЙ" : "ЭКРАН: ОКОННЫЙ";
            }
        }

        private void InitResolutions()
        {
            resolutionsList.Clear();
            Resolution[] allResolutions = Screen.resolutions;
            HashSet<string> seen = new HashSet<string>();

            // Always provide standard fallbacks in Editor or if physical screen count is too low
            bool useStandardFallback = false;
            #if UNITY_EDITOR
            useStandardFallback = true;
            #endif
            if (allResolutions == null || allResolutions.Length < 3)
            {
                useStandardFallback = true;
            }

            if (useStandardFallback)
            {
                resolutionsList.Add(new CustomResolution(640, 480));
                resolutionsList.Add(new CustomResolution(800, 600));
                resolutionsList.Add(new CustomResolution(1024, 768));
                resolutionsList.Add(new CustomResolution(1280, 720));
                resolutionsList.Add(new CustomResolution(1366, 768));
                resolutionsList.Add(new CustomResolution(1600, 900));
                resolutionsList.Add(new CustomResolution(1920, 1080));
                resolutionsList.Add(new CustomResolution(2560, 1440));
                resolutionsList.Add(new CustomResolution(3840, 2160));
            }
            else
            {
                for (int i = 0; i < allResolutions.Length; i++)
                {
                    Resolution r = allResolutions[i];
                    string resText = $"{r.width} x {r.height}";
                    if (seen.Add(resText))
                    {
                        resolutionsList.Add(new CustomResolution(r.width, r.height));
                    }
                }
            }

            // Find current resolution index
            currentResolutionIndex = resolutionsList.Count - 1; // Default fallback to highest
            for (int i = 0; i < resolutionsList.Count; i++)
            {
                if (resolutionsList[i].width == Screen.width && resolutionsList[i].height == Screen.height)
                {
                    currentResolutionIndex = i;
                    break;
                }
            }

            UpdateResolutionButtonText();
        }

        private void CycleResolution()
        {
            if (resolutionsList.Count == 0) return;

            currentResolutionIndex = (currentResolutionIndex + 1) % resolutionsList.Count;
            CustomResolution r = resolutionsList[currentResolutionIndex];
            Screen.SetResolution(r.width, r.height, isFullscreen);
            UpdateResolutionButtonText();
        }

        private void UpdateResolutionButtonText()
        {
            if (resolutionText != null && currentResolutionIndex >= 0 && currentResolutionIndex < resolutionsList.Count)
            {
                CustomResolution r = resolutionsList[currentResolutionIndex];
                resolutionText.text = $"РАЗРЕШЕНИЕ: {r.width} X {r.height}";
            }
        }
    }
}
