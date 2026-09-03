using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using MysteryRooms.Multiplayer.Network;
using UnityEngine.EventSystems;
public class InteractionSystem : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float interactionRange = 3f;
    public LayerMask interactableLayer;
    public Transform playerCamera;

    [Header("UI References")]
    public TextMeshProUGUI interactionPromptText;

    private IInteractable currentInteractable;
    private SymbolButton currentSymbolButton;

    void Update()
    {
        CheckForInteractable();
        HandleInteractionInput();
    }

    private void CheckForInteractable()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange, interactableLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            SymbolButton symbolButton = hit.collider.GetComponent<SymbolButton>(); // ADD THIS

            if (interactable != null)
            {
                // NEW INTERACTABLE DETECTED
                if (currentInteractable != interactable)
                {
                    // Exit previous hover
                    if (currentSymbolButton != null)
                    {
                        currentSymbolButton.OnHoverExit();
                    }
                    
                    // Enter new hover
                    currentInteractable = interactable;
                    currentSymbolButton = symbolButton;
                    
                    if (currentSymbolButton != null)
                    {
                        currentSymbolButton.OnHoverEnter();
                    }
                    
                    ShowPrompt(interactable.GetInteractionPrompt());
                }
                return;
            }
        }

        // NOT LOOKING AT ANYTHING - EXIT HOVER
        if (currentSymbolButton != null)
        {
            currentSymbolButton.OnHoverExit();
            currentSymbolButton = null;
        }
        
        currentInteractable = null;
        HidePrompt();
    }

    private void HandleInteractionInput()
    {
        // Check if E key was pressed
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            // CRITICAL FIX: Don't process E key if typing in an input field
            if (IsTypingInInputField())
            {
                return;
            }

            if (currentInteractable != null)
            {
                currentInteractable.Interact();
            }
        }
    }

    /// <summary>
    /// Check if the player is currently typing in a UI input field
    /// </summary>
    private bool IsTypingInInputField()
    {
        // Check if EventSystem exists and has a selected GameObject
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            // Check if the selected object is a TMP_InputField
            TMP_InputField inputField = EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>();
            if (inputField != null && inputField.isFocused)
            {
                return true;
            }
        }
        
        return false;
    }

    private void ShowPrompt(string text)
    {
        if (MysteryRooms.UI.GameUIController.Instance != null)
        {
            MysteryRooms.UI.GameUIController.Instance.ShowInteractionPrompt(text);
        }
    }

    private void HidePrompt()
    {
        if (MysteryRooms.UI.GameUIController.Instance != null)
        {
            MysteryRooms.UI.GameUIController.Instance.HideInteractionPrompt();
        }
    }

    void OnInteract(GameObject interactable)
    {
        // ... your existing interaction code ...
        
        // Notify network
        NetworkedPlayerController player = GetComponent<NetworkedPlayerController>();
        if (player != null)
        {
            player.OnInteract(interactable.name, "examine");
        }
    }

    private void OnDisable()
    {
        if (currentSymbolButton != null)
        {
            currentSymbolButton.OnHoverExit();
            currentSymbolButton = null;
        }
    }

}
