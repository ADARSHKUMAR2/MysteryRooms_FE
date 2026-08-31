using UnityEngine;

// This script sits on the physical bronze globe and forwards interactions to the main puzzle script on the Root.
public class GlobeInteractable : MonoBehaviour, IInteractable
{
    [Tooltip("Drag the Root Astrolabe Puzzle object here")]
    public MapCoordinatesPuzzle parentPuzzle;

    public string GetInteractionPrompt()
    {
        if (parentPuzzle == null) return "";
        return parentPuzzle.GetInteractionPrompt();
    }

    public void Interact()
    {
        if (parentPuzzle != null)
        {
            parentPuzzle.Interact();
        }
    }
}
