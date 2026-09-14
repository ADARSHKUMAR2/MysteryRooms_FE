using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MysteryRooms.UI
{
    /// <summary>
    /// Drives the in-game Settings Panel.
    /// All UI references are assigned via the Editor Generator.
    /// Button onClick events are wired automatically in Awake.
    /// Persists all values via PlayerPrefs so they survive sessions.
    /// </summary>
    public class SettingsPanelUI : MonoBehaviour
    {
        // ─── PlayerPrefs keys ────────────────────────────────────────────────
        public const string PREF_MOUSE_SENS      = "MouseSensitivity";
        public const string PREF_JOYSTICK_SENS   = "JoystickSensitivity";
        public const string PREF_WALK_SPEED      = "WalkSpeed";

        // ─── Default values (match FirstPersonController defaults) ───────────
        private const float DEFAULT_MOUSE_SENS    = 0.1f;
        private const float DEFAULT_JOYSTICK_SENS = 180f;
        private const float DEFAULT_WALK_SPEED    = 5f;

        [Header("Panel Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Mouse Sensitivity")]
        [SerializeField] private Slider    mouseSensSlider;
        [SerializeField] private TextMeshProUGUI mouseSensLabel;

        [Header("Joystick / Mobile Look Sensitivity")]
        [SerializeField] private Slider    joystickSensSlider;
        [SerializeField] private TextMeshProUGUI joystickSensLabel;

        [Header("Walk Speed")]
        [SerializeField] private Slider    walkSpeedSlider;
        [SerializeField] private TextMeshProUGUI walkSpeedLabel;

        [Header("Buttons")]
        [SerializeField] private Button    closeButton;
        [SerializeField] private Button    resetButton;
        [SerializeField] private Button    leaveGameButton;

        private FirstPersonController playerController;

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            // Automatically wire slider callbacks
            if (mouseSensSlider    != null) mouseSensSlider.onValueChanged.AddListener(OnMouseSensChanged);
            if (joystickSensSlider != null) joystickSensSlider.onValueChanged.AddListener(OnJoystickSensChanged);
            if (walkSpeedSlider    != null) walkSpeedSlider.onValueChanged.AddListener(OnWalkSpeedChanged);

            // Automatically wire button onClick events (no Inspector setup needed!)
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (resetButton != null) resetButton.onClick.AddListener(ResetToDefaults);
            if (leaveGameButton != null) leaveGameButton.onClick.AddListener(LeaveGame);

            // Panel starts hidden
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void Start()
        {
            LoadAndApply();
        }

        // ─── Public API ───────────────────────────────────────────────────────

        /// <summary>Called by the Settings button in the HUD.</summary>
        public void Open()
        {
            // Find the player controller (handles late-spawned networked players)
            playerController = FindFirstObjectByType<FirstPersonController>();

            LoadAndApply();

            if (panelRoot != null) panelRoot.SetActive(true);

            if (playerController != null) playerController.SetPaused(true);
        }

        /// <summary>Called by the Close button or Escape key.</summary>
        public void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);

            if (playerController != null) playerController.SetPaused(false);
        }

        // ─── Slider callbacks ─────────────────────────────────────────────────

        private void OnMouseSensChanged(float value)
        {
            if (mouseSensLabel != null)
                mouseSensLabel.text = $"Mouse Sensitivity: {value:F2}";

            PlayerPrefs.SetFloat(PREF_MOUSE_SENS, value);
            PlayerPrefs.Save();

            if (playerController != null)
                playerController.mouseSensitivity = value;
        }

        private void OnJoystickSensChanged(float value)
        {
            if (joystickSensLabel != null)
                joystickSensLabel.text = $"Joystick Sensitivity: {value:F0}";

            PlayerPrefs.SetFloat(PREF_JOYSTICK_SENS, value);
            PlayerPrefs.Save();

            if (playerController != null)
                playerController.joystickLookSensitivity = value;
        }

        private void OnWalkSpeedChanged(float value)
        {
            if (walkSpeedLabel != null)
                walkSpeedLabel.text = $"Walk Speed: {value:F1}";

            PlayerPrefs.SetFloat(PREF_WALK_SPEED, value);
            PlayerPrefs.Save();

            if (playerController != null)
                playerController.walkSpeed = value;
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private void LoadAndApply()
        {
            if (mouseSensSlider != null)
                mouseSensSlider.value = PlayerPrefs.GetFloat(PREF_MOUSE_SENS, DEFAULT_MOUSE_SENS);

            if (joystickSensSlider != null)
                joystickSensSlider.value = PlayerPrefs.GetFloat(PREF_JOYSTICK_SENS, DEFAULT_JOYSTICK_SENS);

            if (walkSpeedSlider != null)
                walkSpeedSlider.value = PlayerPrefs.GetFloat(PREF_WALK_SPEED, DEFAULT_WALK_SPEED);
        }

        private void ResetToDefaults()
        {
            PlayerPrefs.DeleteKey(PREF_MOUSE_SENS);
            PlayerPrefs.DeleteKey(PREF_JOYSTICK_SENS);
            PlayerPrefs.DeleteKey(PREF_WALK_SPEED);
            PlayerPrefs.Save();
            LoadAndApply();
        }

        /// <summary>Leave the current game and return to main menu</summary>
        private void LeaveGame()
        {
            Debug.Log("🚪 Player is leaving the game...");
            
            // Unpause the player first
            if (playerController != null)
            {
                playerController.SetPaused(false);
            }
            
            // Close the settings panel
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            // Unlock and show cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // Shutdown network if in multiplayer
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Unity.Netcode.NetworkManager.Singleton.Shutdown();
            }

            // Clean up DontDestroyOnLoad objects that shouldn't persist
            CleanupDontDestroyOnLoadObjects();
            
            // Load main menu scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        /// <summary>
        /// Clean up DontDestroyOnLoad objects that were created during gameplay
        /// Keeps only the essential ones needed in MainMenu
        /// </summary>
        private void CleanupDontDestroyOnLoadObjects()
        {
            // List of objects to keep (will exist in MainMenu too)
            // FirebaseAuthManager - Keep (needed for auth in MainMenu)
            // Everything else from game session should be cleaned up
            
            // Destroy MultiplayerSessionManager (will be recreated if needed)
            var sessionManager = FindObjectOfType<MysteryRooms.Multiplayer.Core.MultiplayerSessionManager>();
            if (sessionManager != null)
            {
                Debug.Log("Destroying MultiplayerSessionManager");
                Destroy(sessionManager.gameObject);
            }
            
            // Destroy MultiplayerMysteryCoordinator
            var coordinator = FindObjectOfType<MysteryRooms.Multiplayer.Core.MultiplayerMysteryCoordinator>();
            if (coordinator != null)
            {
                Debug.Log("Destroying MultiplayerMysteryCoordinator");
                Destroy(coordinator.gameObject);
            }
            
            // Destroy MysteryAPIService if it exists
            var apiService = FindObjectOfType<MysteryRooms.Game.Services.MysteryAPIService>();
            if (apiService != null)
            {
                Debug.Log("Destroying MysteryAPIService");
                Destroy(apiService.gameObject);
            }
            
            // Destroy MysteryLoader if it exists
            var mysteryLoader = FindObjectOfType<MysteryRooms.Game.Managers.MysteryLoader>();
            if (mysteryLoader != null)
            {
                Debug.Log("Destroying MysteryLoader");
                Destroy(mysteryLoader.gameObject);
            }
            
            // Destroy Unity Services Vivox
            GameObject[] dontDestroyObjects = GetDontDestroyOnLoadObjects();
            foreach (GameObject obj in dontDestroyObjects)
            {
                // Clean up Vivox objects
                if (obj.name.Contains("Vivox") || obj.name.Contains("Unity.Services"))
                {
                    Debug.Log($"Destroying DontDestroyOnLoad object: {obj.name}");
                    Destroy(obj);
                }
            }
            
            Debug.Log("✅ DontDestroyOnLoad cleanup complete");
        }

        /// <summary>
        /// Get all objects in the DontDestroyOnLoad scene
        /// </summary>
        private GameObject[] GetDontDestroyOnLoadObjects()
        {
            GameObject temp = null;
            try
            {
                temp = new GameObject("Temp");
                DontDestroyOnLoad(temp);
                UnityEngine.SceneManagement.Scene dontDestroyScene = temp.scene;
                DestroyImmediate(temp);
                temp = null;

                return dontDestroyScene.GetRootGameObjects();
            }
            finally
            {
                if (temp != null)
                    DestroyImmediate(temp);
            }
        }
    }
}
