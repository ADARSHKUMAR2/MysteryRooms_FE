using UnityEngine;
using UnityEngine.InputSystem;
using MysteryRooms.UI;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float gravity = -9.81f;

    [Header("Look Settings")]
    public float mouseSensitivity = 0.1f;
    public float joystickLookSensitivity = 180f;
    public Transform playerCamera;

    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;

    private CharacterController characterController;
    private Vector3 velocity;
    private float cameraPitch = 0f;
    private bool isGamePaused = false;

    // Input Action references
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction sprintAction;

    void Awake()
    {
        var playerMap = inputActions.FindActionMap("Player", throwIfNotFound: true);

        moveAction   = playerMap.FindAction("Move",   throwIfNotFound: true);
        lookAction   = playerMap.FindAction("Look",   throwIfNotFound: true);
        sprintAction = playerMap.FindAction("Sprint", throwIfNotFound: true);

        // Apply any previously saved sensitivity settings immediately on spawn
        ApplySavedSettings();
    }

    void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        sprintAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        sprintAction.Disable();
    }

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        LockCursor();

        if (playerCamera == null)
            playerCamera = Camera.main.transform;
    }

    void Update()
    {
        HandlePauseInput();

        if (!isGamePaused)
        {
            HandleLook();
            HandleMovement();
        }
    }

    // ─── Public API (called by SettingsPanelUI) ───────────────────────────────

    /// <summary>
    /// Lets external systems (e.g. SettingsPanelUI) pause or resume the player.
    /// </summary>
    public void SetPaused(bool paused)
    {
        isGamePaused = paused;

        if (isGamePaused)
            UnlockCursor();
        else
            LockCursor();
    }

    // ─── Pause (keyboard Escape) ──────────────────────────────────────────────

    private void HandlePauseInput()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // If the settings panel is open, Escape should close it, not toggle pause.
            // SettingsPanelUI.Close() will call SetPaused(false) itself.
            SettingsPanelUI settingsPanel = FindFirstObjectByType<SettingsPanelUI>();
            if (settingsPanel != null && settingsPanel.gameObject.activeSelf)
            {
                settingsPanel.Close();
                return;
            }

            SetPaused(!isGamePaused);
            Debug.Log(isGamePaused ? "Game Paused" : "Game Resumed");
        }
    }

    // ─── Cursor ───────────────────────────────────────────────────────────────

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ─── Look ─────────────────────────────────────────────────────────────────

    private void HandleLook()
    {
        Vector2 lookDelta = lookAction.ReadValue<Vector2>();

        // Gamepad / OnScreenStick outputs normalised values (magnitude <= 1).
        // Mouse outputs raw pixel deltas which are almost always > 1.
        bool isJoystickInput = lookDelta.magnitude <= 1.0f && lookDelta.magnitude > 0f;

        if (isJoystickInput)
            lookDelta *= joystickLookSensitivity * Time.deltaTime;
        else
            lookDelta *= mouseSensitivity;

        cameraPitch -= lookDelta.y;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);
        playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

        transform.Rotate(Vector3.up * lookDelta.x);
    }

    // ─── Movement ─────────────────────────────────────────────────────────────

    private void HandleMovement()
    {
        Vector2 inputDir = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);

        float currentSpeed = sprintAction.IsPressed() ? sprintSpeed : walkSpeed;

        Vector3 move = transform.right * inputDir.x + transform.forward * inputDir.y;
        characterController.Move(move * currentSpeed * Time.deltaTime);

        if (characterController.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    // ─── Settings ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads PlayerPrefs written by SettingsPanelUI and applies them.
    /// Called in Awake so values are live from the first frame.
    /// </summary>
    private void ApplySavedSettings()
    {
        mouseSensitivity      = PlayerPrefs.GetFloat(SettingsPanelUI.PREF_MOUSE_SENS,    mouseSensitivity);
        joystickLookSensitivity = PlayerPrefs.GetFloat(SettingsPanelUI.PREF_JOYSTICK_SENS, joystickLookSensitivity);
        walkSpeed             = PlayerPrefs.GetFloat(SettingsPanelUI.PREF_WALK_SPEED,    walkSpeed);
    }

    public bool IsPaused() => isGamePaused;
}
