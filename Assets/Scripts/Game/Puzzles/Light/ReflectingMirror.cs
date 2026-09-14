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

    [Header("Mobile Controls")]
    [SerializeField] private GameObject rotateMirrorButton;

    // Events
    public delegate void MirrorStateChanged(ReflectingMirror mirror, bool isHeld);
    public static event MirrorStateChanged OnMirrorStateChanged;

    private float targetRotationY;
    private bool isRotating = false;
    
    // Holding State
    private bool isHeld = false;
    private Transform playerCamera;
    private AudioSource audioSource;
    
    // Mobile detection
    private bool _isMobileDevice = false;
    [Header("Pause Handling")]
    [SerializeField] private bool ignoreGamePause = true;


    private void Start()
    {
        targetRotationY = transform.eulerAngles.y;
        audioSource = GetComponent<AudioSource>();
        
        // Detect mobile platform - also check if MobileControlToggle forced it on in editor
        bool isMobileRuntime = Application.platform == RuntimePlatform.Android || 
                            Application.platform == RuntimePlatform.IPhonePlayer;
        
        // Check if mobile controls canvas is active (means editor testing is on)
        bool isMobileEditorTest = false;
        #if UNITY_EDITOR
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas.gameObject.name == "MobileControlsCanvas" && canvas.gameObject.activeSelf)
            {
                isMobileEditorTest = true;
                break;
            }
        }
        #endif
        
        _isMobileDevice = isMobileRuntime || isMobileEditorTest;
        
        Debug.Log($"[Mirror] Start() - _isMobileDevice = {_isMobileDevice}, platform = {Application.platform}, editor test = {isMobileEditorTest}");
        
        // Auto-assign layers if not set in inspector
        if (floorLayer == 0)
        {
            floorLayer = LayerMask.GetMask("Default", "Obstacle");
        }
        
        // Auto-find rotate button if not assigned
        if (rotateMirrorButton == null && _isMobileDevice)
        {
            Debug.Log($"[Mirror] Searching for RotateMirrorButton...");
            rotateMirrorButton = FindRotateMirrorButton();
            Debug.Log($"[Mirror] Found button: {(rotateMirrorButton != null ? rotateMirrorButton.name : "NULL")}");
        }
    }

    private void LateUpdate()
    {
        // Check if we should ignore pause
        if (!ignoreGamePause)
        {
            // Check if game is paused via FirstPersonController
            FirstPersonController fpc = FindFirstObjectByType<FirstPersonController>();
            if (fpc != null && fpc.IsPaused())
            {
                return; // Skip update when paused
            }
        }
        
        // Rest of your existing LateUpdate code...
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

                // 3. Allow rotation based on platform
                if (!_isMobileDevice)
                {
                    // Desktop: Use left mouse button or gamepad trigger
                    if (GetDesktopRotateInput())
                    {
                        TriggerRotation();
                    }
                }
                // Mobile: Rotation is handled by MobileRotateMirrorButton.OnPointerDown() -> MobileRotate()
            }
        }
    }

    /// <summary>
    /// Desktop input check for Left Click or Gamepad Right Trigger
    /// </summary>
    private bool GetDesktopRotateInput()
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

    /// <summary>
    /// Called by MobileRotateMirrorButton when the rotate button is pressed on mobile
    /// </summary>
    public void MobileRotate()
    {
        if (isHeld)
        {
            TriggerRotation();
        }
    }

    // --- IInteractable Implementation ---

    public string GetInteractionPrompt()
    {
        if (isHeld)
        {
            if (_isMobileDevice)
                return "Press E to Drop  |  Tap Rotate Button";
            else
                return "Press E to Drop  |  Left Click to Rotate";
        }
        else
            return "Press E to Pick Up Mirror";
    }

    public void Interact()
    {
        Debug.Log($"[Mirror] Interact() called! Current isHeld = {isHeld}");
        
        if (isHeld)
        {
            Debug.Log($"[Mirror] Calling DropMirror()");
            DropMirror();
        }
        else
        {
            Debug.Log($"[Mirror] Calling PickUpMirror()");
            PickUpMirror();
        }
    }

    // --- Core Logic ---

    private void PickUpMirror()
    {
        isHeld = true;
        
        if (Camera.main != null) 
            playerCamera = Camera.main.transform;

        if (audioSource != null)
        {
            audioSource.pitch = 1.2f;
            audioSource.Play();
        }
        
        Debug.Log($"[Mirror] {gameObject.name} picked up");
        
        // Pass 'this' so the button knows EXACTLY which mirror we picked up!
        OnMirrorStateChanged?.Invoke(this, true);
    }

    private void DropMirror()
    {
        isHeld = false;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 10f, floorLayer))
        {
            Vector3 dropPos = transform.position;
            dropPos.y = hit.point.y; 
            transform.position = dropPos;
        }

        if (audioSource != null)
        {
            audioSource.pitch = 0.8f;
            audioSource.Play();
        }
        
        Debug.Log($"[Mirror] {gameObject.name} dropped");
        
        // Pass 'this'
        OnMirrorStateChanged?.Invoke(this, false);
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

    // --- Helper Methods ---

    public bool IsHeld()
    {
        return isHeld;
    }

    private GameObject FindRotateMirrorButton()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            Transform rotateBtnTransform = canvas.transform.Find("RotateMirrorButton");
            if (rotateBtnTransform != null)
            {
                return rotateBtnTransform.gameObject;
            }
        }
        return null;
    }
}
