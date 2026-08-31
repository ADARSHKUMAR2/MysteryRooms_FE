using UnityEngine;
using MysteryRooms.Game.Data;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ReflectingMirror : MonoBehaviour, IInteractable
{
    [Header("Rotation Settings")]
    [Tooltip("How many degrees the mirror turns per left-click")]
    public float rotationAngle = 45f;
    public float rotationSpeed = 8f;

    [Header("Movement Settings")]
    [Tooltip("How far in front of the camera the mirror is held")]
    public float holdDistance = 2.5f;
    [Tooltip("How smoothly it follows the camera")]
    public float followSpeed = 10f;
    [Tooltip("Layers it can be dropped onto (e.g., Default, Floor, Obstacle)")]
    public LayerMask floorLayer;

    private float targetRotationY;
    private bool isRotating = false;
    
    // Holding State
    private bool isHeld = false;
    private Transform playerCamera;
    private AudioSource audioSource;

    private void Start()
    {
        targetRotationY = transform.eulerAngles.y;
        audioSource = GetComponent<AudioSource>();
        
        // Auto-assign layers if not set in inspector
        if (floorLayer == 0)
        {
            floorLayer = LayerMask.GetMask("Default", "Obstacle");
        }
    }

    private void Update()
    {
        // 1. Handle Smooth Rotation Animation
        if (isRotating)
        {
            Quaternion targetQuat = Quaternion.Euler(0, targetRotationY, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetQuat, Time.deltaTime * rotationSpeed);

            if (Quaternion.Angle(transform.rotation, targetQuat) < 0.5f)
            {
                transform.rotation = targetQuat;
                isRotating = false;
            }
        }

        // 2. Handle Carrying the Mirror
        if (isHeld)
        {
            // Find camera if we lost it
            if (playerCamera == null && Camera.main != null) 
                playerCamera = Camera.main.transform;

            if (playerCamera != null)
            {
                // Calculate position in front of the player
                Vector3 targetPos = playerCamera.position + (playerCamera.forward * holdDistance);
                
                // Lower it slightly so it doesn't block the center of the screen
                targetPos.y = playerCamera.position.y - 0.5f;

                // Smoothly move it to the hold position
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

                // 3. Allow rotation ONLY while holding it (Left Mouse Button)
                if (GetRotateInput())
                {
                    TriggerRotation();
                }
            }
        }
    }

    /// <summary>
    /// Universal input check for Left Click (works with old and new Input System)
    /// </summary>
    private bool GetRotateInput()
    {
#if ENABLE_INPUT_SYSTEM
        // New Input System: Left Mouse Button
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        
        // Optional: Support Gamepad triggers (Right Trigger)
        if (Gamepad.current != null && Gamepad.current.rightTrigger.wasPressedThisFrame) return true;
        
        return false;
#else
        // Old Input System
        return Input.GetMouseButtonDown(0);
#endif
    }

    // --- IInteractable Implementation ---

    public string GetInteractionPrompt()
    {
        if (isHeld)
            return "Press E to Drop  |  Left Click to Rotate";
        else
            return "Press E to Pick Up Mirror";
    }

    public void Interact()
    {
        if (isHeld)
        {
            DropMirror();
        }
        else
        {
            PickUpMirror();
        }
    }

    // --- Core Logic ---

    private void PickUpMirror()
    {
        isHeld = true;
        
        if (Camera.main != null) 
            playerCamera = Camera.main.transform;

        // Play pickup sound (higher pitch)
        if (audioSource != null)
        {
            audioSource.pitch = 1.2f;
            audioSource.Play();
        }
    }

    private void DropMirror()
    {
        isHeld = false;

        // Snap perfectly to the floor!
        // Shoot a raycast straight down from the mirror's current floating position
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 10f, floorLayer))
        {
            Vector3 dropPos = transform.position;
            dropPos.y = hit.point.y; // Set Y to exactly where the floor is
            transform.position = dropPos;
        }

        // Play drop sound (lower pitch)
        if (audioSource != null)
        {
            audioSource.pitch = 0.8f;
            audioSource.Play();
        }
    }

    private void TriggerRotation()
    {
        if (isRotating) return; // Prevent spam clicking

        targetRotationY += rotationAngle;
        isRotating = true;
        
        // Play stone grinding rotation sound (normal pitch)
        if (audioSource != null)
        {
            audioSource.pitch = 1.0f;
            audioSource.Play();
        }
    }
}
