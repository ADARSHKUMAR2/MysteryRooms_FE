using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MysteryRooms.Multiplayer.Core;
using MysteryRooms.Game.Data;
using System.Collections.Generic;

namespace MysteryRooms.Multiplayer.UI
{
    /// <summary>
    /// UI Controller for multiplayer menu (Host/Join)
    /// </summary>
    public class MultiplayerUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MultiplayerMysteryCoordinator coordinator;
        [SerializeField] private MultiplayerSessionManager sessionManager;

        [Header("UI Panels")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private GameObject loadingPanel;

        [Header("Menu UI")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private TMP_InputField joinCodeInput;
        [SerializeField] private TMP_Dropdown roomDropdown;
        [SerializeField] private Slider difficultySlider;
        [SerializeField] private TextMeshProUGUI difficultyText;
        [SerializeField] private TMP_InputField playerCountInput;

        [Header("Lobby UI")]
        [SerializeField] private TextMeshProUGUI joinCodeDisplay;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button disconnectButton;
        [SerializeField] private Button startGameButton;


        [Header("Loading UI")]
        [SerializeField] private TextMeshProUGUI loadingText;

        [Header("Replay Existing UI")]
        [SerializeField] private TMP_Dropdown recentMysteriesDropdown;
        [SerializeField] private Button replayMysteryButton;
        
        private List<MysteryConfigData> recentMysteriesCache = new List<MysteryConfigData>();


        private string selectedRoom = "mummy_tomb";
        private int selectedDifficulty = 3;
        private int selectedPlayerCount = 4;

        #region Initialization

        private void Start()
        {
            SetupUI();
            ShowMenuPanel();

            // Subscribe to coordinator events
            if (coordinator != null)
            {
                coordinator.OnMysteryReady += OnMysteryReady;
                coordinator.OnError += OnError;
            }

            // Subscribe to session events
            if (sessionManager != null)
            {
                sessionManager.OnSessionCreated += OnSessionCreated;
                sessionManager.OnSessionJoined += OnSessionJoined;
                sessionManager.OnPlayerJoined += OnPlayerJoined;
                sessionManager.OnPlayerLeft += OnPlayerLeft;
            }

            // Fetch recent mysteries for the replay dropdown
            FetchRecentMysteries();
        }

        private void SetupUI()
        {
            // Host button
            if (hostButton != null)
            {
                hostButton.onClick.AddListener(OnHostButtonClicked);
            }

            // Join button
            if (joinButton != null)
            {
                joinButton.onClick.AddListener(OnJoinButtonClicked);
            }

            // Disconnect button
            if (disconnectButton != null)
            {
                disconnectButton.onClick.AddListener(OnDisconnectButtonClicked);
            }

            // Start Game button (Host only)
            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(OnStartGameButtonClicked);
                // Hide it by default until we know the player is the host
                startGameButton.gameObject.SetActive(false);
            }

            // Difficulty slider
            if (difficultySlider != null)
            {
                difficultySlider.onValueChanged.AddListener(OnDifficultyChanged);
                OnDifficultyChanged(difficultySlider.value);
            }

            // Room dropdown
            if (roomDropdown != null)
            {
                roomDropdown.onValueChanged.AddListener(OnRoomChanged);
                SetupRoomDropdown();
            }

            // Player count
            if (playerCountInput != null)
            {
                playerCountInput.text = selectedPlayerCount.ToString();
                playerCountInput.onEndEdit.AddListener(OnPlayerCountChanged);
            }

            if (replayMysteryButton != null)
            {
                replayMysteryButton.onClick.AddListener(OnReplayMysteryClicked);
            }
        }

        private void FetchRecentMysteries()
        {
            if (recentMysteriesDropdown == null) return;
            
            recentMysteriesDropdown.ClearOptions();
            recentMysteriesDropdown.options.Add(new TMP_Dropdown.OptionData("Loading recent mysteries..."));
            recentMysteriesDropdown.RefreshShownValue();

            // Requires MysteryAPIService reference (you can grab it from mystery loader or add a reference)
            var apiService = FindObjectOfType<MysteryRooms.Game.Services.MysteryAPIService>();
            if (apiService != null)
            {
                StartCoroutine(apiService.GetRecentMysteries(
                    limit: 10,
                    onSuccess: mysteries => 
                    {
                        recentMysteriesCache = mysteries;
                        PopulateRecentMysteriesDropdown();
                    },
                    onError: error => 
                    {
                        recentMysteriesDropdown.ClearOptions();
                        recentMysteriesDropdown.options.Add(new TMP_Dropdown.OptionData("Failed to load mysteries"));
                        recentMysteriesDropdown.RefreshShownValue();
                    }
                ));
            }
        }

        private void PopulateRecentMysteriesDropdown()
        {
            recentMysteriesDropdown.ClearOptions();
            
            if (recentMysteriesCache.Count == 0)
            {
                recentMysteriesDropdown.options.Add(new TMP_Dropdown.OptionData("No mysteries found"));
                replayMysteryButton.interactable = false;
                return;
            }

            List<string> options = new List<string>();
            foreach (var mystery in recentMysteriesCache)
            {
                // Example format: "mummy_tomb (Diff 3) - Break the Pharaoh's curse"
                string label = $"{mystery.room} (Diff {mystery.difficulty}) - {mystery.objective}";
                options.Add(label);
            }

            recentMysteriesDropdown.AddOptions(options);
            replayMysteryButton.interactable = true;
        }

        private void OnReplayMysteryClicked()
        {
            Debug.Log("🎮 Replay existing mystery button clicked");

            if (recentMysteriesCache == null || recentMysteriesCache.Count == 0) return;

            // Get the mystery that is currently selected in the dropdown
            int selectedIndex = recentMysteriesDropdown.value;
            MysteryConfigData selectedMystery = recentMysteriesCache[selectedIndex];

            // Validate player count
            if (selectedPlayerCount < 1 || selectedPlayerCount > 8)
            {
                ShowError("Player count must be between 1 and 8");
                return;
            }

            ShowLoadingPanel("Setting up existing mystery session...");

            // Call the new Host method!
            coordinator.HostExistingMystery(selectedMystery, selectedPlayerCount);
        }


        private void SetupRoomDropdown()
        {
            roomDropdown.ClearOptions();
            roomDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "mummy_tomb",
                "haunted_mansion",
                "pirate_ship",
                "space_station"
            });
        }

        #endregion

        #region Button Callbacks

        private void OnHostButtonClicked()
        {
            Debug.Log("🎮 Host button clicked");

            // Validate inputs
            if (selectedPlayerCount < 1 || selectedPlayerCount > 8)
            {
                ShowError("Player count must be between 1 and 8");
                return;
            }

            ShowLoadingPanel("Generating mystery and creating session...");

            // Start host flow
            coordinator.HostNewMystery(selectedRoom, selectedDifficulty, selectedPlayerCount);
        }

        private void OnJoinButtonClicked()
        {
            Debug.Log("🎮 Join button clicked");

            string code = joinCodeInput.text.Trim().ToUpper();

            if (string.IsNullOrEmpty(code))
            {
                ShowError("Please enter a join code");
                return;
            }

            if (code.Length != 6)
            {
                ShowError("Join code must be 6 characters");
                return;
            }

            ShowLoadingPanel($"Joining session {code}...");

            // Start join flow
            coordinator.JoinMysteryByCode(code);
        }

        private async void OnDisconnectButtonClicked()
        {
            Debug.Log("Disconnecting...");
            await sessionManager.DisconnectAsync();
            ShowMenuPanel();
        }

        private void OnStartGameButtonClicked()
        {
            Debug.Log("📢 UI: Host clicked Start Game!");
            
            // Hide the button to prevent double clicks
            if (startGameButton != null)
            {
                startGameButton.interactable = false;
            }

            if (statusText != null)
            {
                statusText.text = "Starting game for all players...";
            }

            // Tell the session manager to load the game scene for everyone
            if (sessionManager != null)
            {
                sessionManager.StartNetworkedGame();
            }
        }


        #endregion

        #region Input Callbacks

        private void OnDifficultyChanged(float value)
        {
            selectedDifficulty = Mathf.RoundToInt(value);
            if (difficultyText != null)
            {
                difficultyText.text = $"Difficulty: {selectedDifficulty}/5";
            }
        }

        private void OnRoomChanged(int index)
        {
            selectedRoom = roomDropdown.options[index].text;
            Debug.Log($"Room selected: {selectedRoom}");
        }

        private void OnPlayerCountChanged(string value)
        {
            if (int.TryParse(value, out int count))
            {
                selectedPlayerCount = Mathf.Clamp(count, 1, 8);
                playerCountInput.text = selectedPlayerCount.ToString();
            }
        }

        #endregion

        #region Event Handlers

        private void OnSessionCreated(string joinCode)
        {
            Debug.Log($"📢 UI: Session created with code {joinCode}");
            
            if (joinCodeDisplay != null)
            {
                joinCodeDisplay.text = $"Share Code: {joinCode}";
            }

            UpdatePlayerCount();
        }

        private void OnSessionJoined()
        {
            Debug.Log("📢 UI: Session joined");
            
            if (joinCodeDisplay != null)
            {
                joinCodeDisplay.text = $"Joined: {sessionManager.CurrentSession?.joinCode}";
            }

            UpdatePlayerCount();
        }

        private void OnMysteryReady(MysteryConfigData mystery)
        {
            Debug.Log($"📢 UI: Mystery ready - {mystery.objective}");
            
            ShowLobbyPanel();
            
            if (statusText != null)
            {
                statusText.text = $"Mystery Loaded!\n{mystery.objective}";
            }

            // Show Start button ONLY if this player is the host
            if (startGameButton != null && sessionManager != null)
            {
                startGameButton.gameObject.SetActive(sessionManager.IsHost);
            }

            // Mystery is loaded, game can start
            // You might hide the UI here and transition to gameplay
        }

        private void OnError(string error)
        {
            Debug.LogError($"📢 UI: Error - {error}");
            ShowError(error);
            ShowMenuPanel();
        }

        private void OnPlayerJoined(string playerId)
        {
            Debug.Log($"📢 UI: Player joined - {playerId}");
            UpdatePlayerCount();
        }

        private void OnPlayerLeft(string playerId)
        {
            Debug.Log($"📢 UI: Player left - {playerId}");
            UpdatePlayerCount();
        }

        #endregion

        #region UI State Management

        private void ShowMenuPanel()
        {
            SetPanelActive(menuPanel, true);
            SetPanelActive(lobbyPanel, false);
            SetPanelActive(loadingPanel, false);
        }

        private void ShowLobbyPanel()
        {
            SetPanelActive(menuPanel, false);
            SetPanelActive(lobbyPanel, true);
            SetPanelActive(loadingPanel, false);
        }

        private void ShowLoadingPanel(string message)
        {
            SetPanelActive(menuPanel, false);
            SetPanelActive(lobbyPanel, false);
            SetPanelActive(loadingPanel, true);

            if (loadingText != null)
            {
                loadingText.text = message;
            }
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }

        private void ShowError(string message)
        {
            if (statusText != null)
            {
                statusText.text = $"<color=red>Error: {message}</color>";
            }
            Debug.LogError(message);
        }

        private void UpdatePlayerCount()
        {
            if (sessionManager.CurrentSession != null && playerCountText != null)
            {
                playerCountText.text = $"Players: {sessionManager.CurrentSession.currentPlayerCount}/{sessionManager.CurrentSession.maxPlayers}";
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            if (coordinator != null)
            {
                coordinator.OnMysteryReady -= OnMysteryReady;
                coordinator.OnError -= OnError;
            }

            if (sessionManager != null)
            {
                sessionManager.OnSessionCreated -= OnSessionCreated;
                sessionManager.OnSessionJoined -= OnSessionJoined;
                sessionManager.OnPlayerJoined -= OnPlayerJoined;
                sessionManager.OnPlayerLeft -= OnPlayerLeft;
            }
        }

        #endregion
    }
}
