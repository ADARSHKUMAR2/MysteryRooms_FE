using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class InteractionSystem : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float interactionRange = 3f;
    public LayerMask interactableLayer;
    public Transform playerCamera;

    [Header("UI References")]
    public TextMeshProUGUI interactionPromptText;

    private IInteractable currentInteractable;

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
            // Check if the object we hit has an IInteractable component
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                // We are looking at something interactable
                currentInteractable = interactable;
                ShowPrompt(interactable.GetInteractionPrompt());
                return;
            }
        }

        // If we reach here, we are not looking at anything interactable
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
        if (interactionPromptText != null)
        {
            interactionPromptText.text = text;
            interactionPromptText.gameObject.SetActive(true);
        }
    }

    private void HidePrompt()
    {
        if (interactionPromptText != null)
        {
            interactionPromptText.gameObject.SetActive(false);
        }
    }
}
