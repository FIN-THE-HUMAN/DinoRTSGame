using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using RTSFramework.Buildings;
using RTSFramework.Combat;
using RTSFramework.Factions;

namespace RTSFramework.Gameplay
{
    public enum WinConditionType
    {
        DestroyAllBuildings,   // Victory when all enemy buildings are destroyed
        DestroyMainBuildings   // Victory when all enemy Town Halls are destroyed
    }

    public enum LossConditionType
    {
        LoseAllBuildings,      // Defeat when all player buildings are destroyed
        LoseMainBuildings      // Defeat when all player Town Halls are destroyed
    }

    public class GameLoopManager : MonoBehaviour
    {
        public static GameLoopManager Instance { get; private set; }

        [Header("Rules Configuration")]
        [SerializeField] private WinConditionType winCondition = WinConditionType.DestroyMainBuildings;
        [SerializeField] private LossConditionType lossCondition = LossConditionType.LoseMainBuildings;

        [Header("UI Overlays")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;

        [Header("UI Controls")]
        [SerializeField] private Button victoryRestartButton;
        [SerializeField] private Button victoryQuitButton;
        [SerializeField] private Button defeatRestartButton;
        [SerializeField] private Button defeatQuitButton;

        private List<Building> playerBuildings = new List<Building>();
        private List<Building> enemyBuildings = new List<Building>();
        private bool gameEnded = false;

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

            // Bind click listeners dynamically
            if (victoryRestartButton != null) victoryRestartButton.onClick.AddListener(RestartGame);
            if (victoryQuitButton != null) victoryQuitButton.onClick.AddListener(QuitGame);
            if (defeatRestartButton != null) defeatRestartButton.onClick.AddListener(RestartGame);
            if (defeatQuitButton != null) defeatQuitButton.onClick.AddListener(QuitGame);

            // Subscribe to static building lifecycle events
            Building.OnBuildingSpawned += HandleBuildingSpawned;
            Building.OnBuildingDestroyed += HandleBuildingDestroyed;
        }

        private void Start()
        {
            // Hide panels at start
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);

            // Register any buildings that spawned before this script initialized
            var existing = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
            foreach (var b in existing)
            {
                HandleBuildingSpawned(b);
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe to avoid memory leaks
            Building.OnBuildingSpawned -= HandleBuildingSpawned;
            Building.OnBuildingDestroyed -= HandleBuildingDestroyed;
        }

        private void HandleBuildingSpawned(Building b)
        {
            if (b == null) return;

            if (b.Faction != null)
            {
                if (b.Faction.IsPlayerFaction)
                {
                    if (!playerBuildings.Contains(b))
                    {
                        playerBuildings.Add(b);
                    }
                }
                else
                {
                    if (!enemyBuildings.Contains(b))
                    {
                        enemyBuildings.Add(b);
                    }
                }
            }
        }

        private void HandleBuildingDestroyed(Building b)
        {
            if (b == null) return;

            playerBuildings.Remove(b);
            enemyBuildings.Remove(b);

            CheckWinLossConditions();
        }

        private void CheckWinLossConditions()
        {
            if (gameEnded) return;

            bool playerAlive = false;
            bool enemyAlive = false;

            // 1. Verify Player condition
            foreach (var b in playerBuildings)
            {
                if (b == null) continue;

                if (lossCondition == LossConditionType.LoseAllBuildings)
                {
                    playerAlive = true;
                    break;
                }
                else if (lossCondition == LossConditionType.LoseMainBuildings)
                {
                    if (b.BuildingData != null && b.BuildingData.BuildingName == "Town Hall")
                    {
                        var health = b.GetComponent<Health>();
                        if (health != null && !health.IsDead)
                        {
                            playerAlive = true;
                            break;
                        }
                    }
                }
            }

            // 2. Verify Enemy condition
            foreach (var b in enemyBuildings)
            {
                if (b == null) continue;

                if (winCondition == WinConditionType.DestroyAllBuildings)
                {
                    enemyAlive = true;
                    break;
                }
                else if (winCondition == WinConditionType.DestroyMainBuildings)
                {
                    if (b.BuildingData != null && b.BuildingData.BuildingName == "Town Hall")
                    {
                        var health = b.GetComponent<Health>();
                        if (health != null && !health.IsDead)
                        {
                            enemyAlive = true;
                            break;
                        }
                    }
                }
            }

            // 3. Resolve game end
            if (!playerAlive)
            {
                TriggerDefeat();
            }
            else if (!enemyAlive)
            {
                TriggerVictory();
            }
        }

        public void TriggerVictory()
        {
            if (gameEnded) return;
            gameEnded = true;
            Time.timeScale = 0f; // Freeze gameplay logic
            if (victoryPanel != null) victoryPanel.SetActive(true);
            Debug.Log("Game Over: Victory!");
        }

        public void TriggerDefeat()
        {
            if (gameEnded) return;
            gameEnded = true;
            Time.timeScale = 0f; // Freeze gameplay logic
            if (defeatPanel != null) defeatPanel.SetActive(true);
            Debug.Log("Game Over: Defeat!");
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        public void QuitGame()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
