using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MysteryRooms.Game.Managers;
using MysteryRooms.Game.Data;
using UnityEngine.SceneManagement;
using MysteryRooms.Multiplayer.Core;
using Unity.Netcode;

namespace MysteryRooms.UI
{
    public class MysteryDebugPanel : MonoBehaviour
    {
        [Header("References")]
        private MysteryLoader mysteryLoader;

        [Header("UI Elements")]
        [SerializeField] private Button generateButton;
        [SerializeField] private Button submitButton;
        [SerializeField] private TMP_InputField shareCodeInput;
        [SerializeField] private Slider difficultySlider;
        [SerializeField] private TextMeshProUGUI difficultyText;
        [SerializeField] private TextMeshProUGUI mysteryInfoText;
        [SerializeField] private TextMeshProUGUI statusText;

        private void Start()
        {
            mysteryLoader = MysteryLoader.Instance;

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

            if (submitButton != null && shareCodeInput != null)
            {
                submitButton.onClick.AddListener(OnSubmitCodeClicked);
            }

            if (mysteryLoader != null)
            {
                mysteryLoader.OnMysteryLoaded += OnMysteryLoaded;
                mysteryLoader.OnMysteryLoadFailed += OnMysteryLoadFailed;
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
                    NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
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
