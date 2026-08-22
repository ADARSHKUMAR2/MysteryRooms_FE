using UnityEngine;

public class InteractableDoor : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    public bool isLocked = true;

    public string GetInteractionPrompt()
    {
        if (isLocked)
            return "Press E to Try Door (Locked)";
        else
            return "Press E to Open Door";
    }

    public void Interact()
    {
        if (isLocked)
        {
            Debug.Log("The door is locked! You need to solve the puzzles first.");
            // TODO: Play a locked door sound effect
        }
        else
        {
            Debug.Log("Door unlocked! You escaped!");
            // TODO: Trigger victory screen / level complete
        }
    }

    // This will be called by your puzzle system when all puzzles are solved
    public void UnlockDoor()
    {
        isLocked = false;
        Debug.Log("Door has been unlocked!");
    }
}
