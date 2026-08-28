using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using MysteryRooms.Multiplayer.Network;
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
            if (currentInteractable != null)
            {
                currentInteractable.Interact();
            }
        }
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
