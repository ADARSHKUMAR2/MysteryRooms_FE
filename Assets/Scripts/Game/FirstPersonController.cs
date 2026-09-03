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
    
    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Body Rotation")]
    [SerializeField] private Transform bodyPivot;
    [SerializeField] private float bodyRotationSpeed = 10f;

    private CharacterController characterController;
    private Vector3 velocity;
    private float cameraPitch = 0f;
    private bool isGamePaused = false;
    private Vector3 lastMoveVector;

    // Input Action references
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction sprintAction;
    private float _logTimer = 0f;
    private const float LOG_INTERVAL = 0.5f;

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

        // Debug.Log("[FPC] Animator: " + (animator != null ? "YES -> " + animator.name : "NULL - assign in Inspector!"));
        if (animator != null)
        {
            Debug.Log("[FPC] Controller: " + (animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NONE - no controller set!"));
            var parms = animator.parameters;
            string paramList = "";
            foreach (var p in parms) paramList += "'" + p.name + "' (" + p.type + ")  ";
            Debug.Log("[FPC] Animator parameters (" + parms.Length + "): " + paramList);
        }
        // Debug.Log("[FPC] walkSpeed=" + walkSpeed + "  moveAction bindings=" + moveAction.bindings.Count);

    }

    void Update()
    {
        HandlePauseInput();

        if (!isGamePaused)
        {
            HandleLook();
            HandleMovement();
        }

        UpdateAnimations();

        _logTimer += Time.deltaTime;
        if (_logTimer >= LOG_INTERVAL)
        {
            _logTimer = 0f;
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            Vector3 ccVel = characterController.velocity;
            float hSpeed = new Vector3(ccVel.x, 0f, ccVel.z).magnitude;
            float animSpeed = animator != null ? animator.GetFloat("Speed") : -1f;
            string stateName = animator != null ? animator.GetCurrentAnimatorStateInfo(0).shortNameHash.ToString() : "no animator";
            // Debug.Log("[FPC] input=" + moveInput + "  CC.vel=" + ccVel + "  hSpeed=" + hSpeed.ToString("F2") + "  animSpeed=" + animSpeed.ToString("F2") + "  grounded=" + characterController.isGrounded + "  state=" + stateName + "  paused=" + isGamePaused);
            if (moveInput.magnitude > 0.1f && hSpeed < 0.05f)
                Debug.LogWarning("[FPC] Input received but CC velocity is ~0! Collider stuck or CC disabled?");
            if (moveInput.magnitude > 0.1f && animSpeed < 0.05f && animator != null)
                Debug.LogWarning("[FPC] Input received but animSpeed is ~0! Speed param missing or CC not moving.");
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
        lastMoveVector = move * currentSpeed; // Store the intended movement
        
        characterController.Move(lastMoveVector * Time.deltaTime);

        if (characterController.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);

        // Rotate body pivot toward movement direction (what remote players see)
        RotateBodyTowardMovement(move);
    }

    private void RotateBodyTowardMovement(Vector3 horizontalMove)
    {
        if (bodyPivot == null) return;

        // Only rotate if we're actually moving
        Vector3 flatMove = new Vector3(horizontalMove.x, 0f, horizontalMove.z);
        if (flatMove.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(flatMove, Vector3.up);
        // bodyPivot rotation is in LOCAL space relative to Player root,
        // so we need world-space target converted to local
        bodyPivot.rotation = Quaternion.Slerp(
            bodyPivot.rotation,
            targetRotation,
            bodyRotationSpeed * Time.deltaTime
        );
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

    private void UpdateAnimations()
    {
        if (animator == null) return;

        // Use the intended movement speed, not CharacterController.velocity
        float speed = lastMoveVector.magnitude;

        Debug.Log($"[FPC] UpdateAnimations: intended speed={speed:F2}, CC velocity={characterController.velocity.magnitude:F2}, grounded={characterController.isGrounded}");

        animator.SetFloat("Speed", speed);
        animator.SetBool("IsGrounded", characterController.isGrounded);
    }

    public bool IsPaused() => isGamePaused;
}
