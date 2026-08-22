using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float gravity = -9.81f;

    [Header("Look Settings")]
    public float mouseSensitivity = 0.1f;
    public Transform playerCamera;

    private CharacterController characterController;
    private Vector3 velocity;
    private float cameraPitch = 0f;
    private bool isGamePaused = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Lock the cursor to the center of the screen and hide it
        LockCursor();

        if (playerCamera == null)
        {
            playerCamera = Camera.main.transform;
        }
    }

    void Update()
    {
        HandlePauseInput();

        // Only allow movement and looking if game is NOT paused
        if (!isGamePaused)
        {
            HandleMouseLook();
            HandleMovement();
        }
    }

    private void HandlePauseInput()
    {
        if (Keyboard.current == null) return;

        // Toggle pause when Escape is pressed
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            isGamePaused = !isGamePaused;

            if (isGamePaused)
            {
                UnlockCursor();
                Debug.Log("Game Paused - Cursor Unlocked");
                // TODO: Show pause menu UI here
            }
            else
            {
                LockCursor();
                Debug.Log("Game Resumed - Cursor Locked");
                // TODO: Hide pause menu UI here
            }
        }
    }

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

    private void HandleMouseLook()
    {
        // Check if mouse is available in new Input System
        if (Mouse.current == null) return;

        // Get mouse delta from new Input System
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;

        // Rotate the camera up/down (pitch)
        cameraPitch -= mouseDelta.y;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f); 
        playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

        // Rotate the player body left/right (yaw)
        transform.Rotate(Vector3.up * mouseDelta.x);
    }

    private void HandleMovement()
    {
        // Check if keyboard is available in new Input System
        if (Keyboard.current == null) return;

        // Get keyboard input manually (W, A, S, D)
        float moveX = 0f;
        float moveZ = 0f;

        if (Keyboard.current.dKey.isPressed) moveX += 1f;
        if (Keyboard.current.aKey.isPressed) moveX -= 1f;
        if (Keyboard.current.wKey.isPressed) moveZ += 1f;
        if (Keyboard.current.sKey.isPressed) moveZ -= 1f;

        // Normalize to prevent diagonal speed boost
        Vector2 inputDir = new Vector2(moveX, moveZ).normalized;

        // Determine current speed (Shift to sprint)
        float currentSpeed = Keyboard.current.leftShiftKey.isPressed ? sprintSpeed : walkSpeed;

        // Calculate movement direction relative to where the player is looking
        Vector3 move = transform.right * inputDir.x + transform.forward * inputDir.y;
        
        // Apply horizontal movement
        characterController.Move(move * currentSpeed * Time.deltaTime);

        // Apply gravity
        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    // Public method so other scripts can check if game is paused
    public bool IsPaused()
    {
        return isGamePaused;
    }
}
