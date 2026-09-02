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
    }
}
