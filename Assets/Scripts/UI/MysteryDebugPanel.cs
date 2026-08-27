using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MysteryRooms.Game.Managers;
using MysteryRooms.Game.Data;
using UnityEngine.SceneManagement;
using MysteryRooms.Multiplayer.Core;
using Unity.Netcode;
using System.Collections.Generic;
using MysteryRooms.Game.Services;

namespace MysteryRooms.UI
{
    public class MysteryDebugPanel : MonoBehaviour
    {
        [Header("References")]
        private MysteryLoader mysteryLoader;
        private MysteryAPIService apiService;

        [Header("UI Elements")]
        [SerializeField] private Button generateButton;
        // [SerializeField] private Button submitButton;
        [SerializeField] private TMP_InputField shareCodeInput;
        [SerializeField] private Slider difficultySlider;
        [SerializeField] private TextMeshProUGUI difficultyText;
        [SerializeField] private TextMeshProUGUI mysteryInfoText;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Replay Existing UI")]
        [SerializeField] private TMP_Dropdown recentMysteriesDropdown;
        [SerializeField] private Button replayMysteryButton;
        private List<MysteryConfigData> recentMysteriesCache = new List<MysteryConfigData>();


        private void Start()
        {
            mysteryLoader = MysteryLoader.Instance;
            apiService = FindObjectOfType<MysteryAPIService>();

            if (mysteryLoader == null)
            {
                Debug.LogWarning("MysteryLoader instance not found! The Debug Panel requires a MysteryLoader in the scene.");
            }

            if (generateButton != null)
            {
                generateButton.onClick.AddListener(OnGenerateClicked);
            }

            if (difficultySlider != null)
            {
                difficultySlider.onValueChanged.AddListener(OnDifficultyChanged);
                OnDifficultyChanged(difficultySlider.value);
            }

            // if (submitButton != null && shareCodeInput != null)
            // {
            //     submitButton.onClick.AddListener(OnSubmitCodeClicked);
            // }

            if (replayMysteryButton != null)
            {
                replayMysteryButton.onClick.AddListener(OnReplayMysteryClicked);
            }

            if (mysteryLoader != null)
            {
                mysteryLoader.OnMysteryLoaded += OnMysteryLoaded;
                mysteryLoader.OnMysteryLoadFailed += OnMysteryLoadFailed;
            }

            FetchRecentMysteries();
        }

        private void FetchRecentMysteries()
        {
            if (apiService == null) return;
            
            SetStatus("Fetching recent mysteries...");
            
            StartCoroutine(apiService.GetRecentMysteries(
                limit: 10,
                onSuccess: (mysteries) =>
                {
                    recentMysteriesCache = mysteries;
                    PopulateDropdown();
                    SetStatus("Ready");
                },
                onError: (error) =>
                {
                    SetStatus($"Warning: Failed to fetch recent mysteries: {error}");
                }
            ));
        }

        private void PopulateDropdown()
        {
            if (recentMysteriesDropdown == null) return;
            
            recentMysteriesDropdown.ClearOptions();
            
            if (recentMysteriesCache.Count == 0)
            {
                recentMysteriesDropdown.options.Add(new TMP_Dropdown.OptionData("No recent mysteries found"));
                recentMysteriesDropdown.interactable = false;
                if (replayMysteryButton != null) replayMysteryButton.interactable = false;
                return;
            }
            
            recentMysteriesDropdown.interactable = true;
            if (replayMysteryButton != null) replayMysteryButton.interactable = true;
            
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            foreach (var mystery in recentMysteriesCache)
            {
                string label = $"{mystery.theme} (Diff: {mystery.difficulty}) [{mystery.share_code}]";
                options.Add(new TMP_Dropdown.OptionData(label));
            }
            
            recentMysteriesDropdown.AddOptions(options);
            recentMysteriesDropdown.RefreshShownValue();
        }

        private void OnReplayMysteryClicked()
        {
            if (recentMysteriesDropdown == null || recentMysteriesCache.Count == 0) return;
            
            int selectedIndex = recentMysteriesDropdown.value;
            if (selectedIndex >= 0 && selectedIndex < recentMysteriesCache.Count)
            {
                MysteryConfigData selectedMystery = recentMysteriesCache[selectedIndex];
                
                SetStatus($"Loading selected mystery: {selectedMystery.share_code}...");
                mysteryLoader.OnJoinByCodeClicked(selectedMystery.share_code);
            }
        }

        private void OnSubmitCodeClicked()
        {
            if (mysteryLoader == null) return;
            
            string code = shareCodeInput.text.Trim();
            
            if (string.IsNullOrEmpty(code))
            {
                SetStatus("⚠️ Please enter a valid share code.");
                return;
            }

            SetStatus($"🔍 Searching for mystery code: {code}...");
            mysteryLoader.OnJoinByCodeClicked(code);
        }

        private void OnGenerateClicked()
        {
            if (mysteryLoader == null) return;

            int difficulty = (int)difficultySlider.value;
            mysteryLoader.SetDifficulty(difficulty);

            SetStatus("⏳ Generating mystery...");
            mysteryLoader.GenerateNewMystery();
        }

        private void OnDifficultyChanged(float value)
        {
            int diff = (int)value;
            if (difficultyText != null)
            {
                difficultyText.text = $"Difficulty: {diff}";
            }
        }

        private void OnMysteryLoaded(MysteryConfigData mystery)
        {
            SetStatus("✅ Mystery loaded successfully!");
            
            if (mysteryInfoText != null)
            {
                string info = $"<b>Share Code:</b> <color=#00FFFF>{mystery.share_code}</color>\n" +
                             $"<b>Mystery ID:</b> {mystery.mystery_id}\n" +
                             $"<b>Room:</b> {mystery.room}\n" +
                             $"<b>Difficulty:</b> {mystery.difficulty}/5\n" +
                             $"<b>Theme:</b> {mystery.theme}\n" +
                             $"<b>Objective:</b> {mystery.objective}\n" +
                             $"<b>Time Limit:</b> {mystery.time_limit_seconds}s\n" +
                             $"<b>Puzzles:</b> {mystery.puzzles.Count}\n" +
                             $"<b>Clues:</b> {mystery.clues.Count}";
                
                mysteryInfoText.text = info;
            }

            MultiplayerSessionManager mpManager = FindObjectOfType<MultiplayerSessionManager>();

            if (mpManager != null && mpManager.IsConnected)
            {
                if (mpManager.IsHost)
                {
                    Debug.Log("Starting Networked Game Scene...");
                    mpManager.StartNetworkedGame();
                }
                else
                {
                    SetStatus("⏳ Waiting for Host to start game...");
                }
            }
            else
            {
                // THIS IS WHAT FIXES SINGLE-PLAYER!
                Debug.Log("Starting Single-Player Game Scene...");
                if (NetworkManager.Singleton != null)
                {
                    // Start the local server/host
                    NetworkManager.Singleton.StartHost();

                    // Use NetworkSceneManager to load the scene so all NetworkObjects spawn
                    if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Game")
                    {
                        NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
                    }
                    else
                    {
                        Debug.Log("Already in the Game scene for Single-Player. Skipping load.");
                    }
                }
                else
                {
                    Debug.LogError("No NetworkManager found in scene! Cannot start single-player host.");
                }
            }
        }

        private void OnMysteryLoadFailed(string error)
        {
            SetStatus($"❌ Failed: {error}");
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log(message);
        }

        private void OnDestroy()
        {
            if (mysteryLoader != null)
            {
                mysteryLoader.OnMysteryLoaded -= OnMysteryLoaded;
                mysteryLoader.OnMysteryLoadFailed -= OnMysteryLoadFailed;
            }
        }
    }
}
