using UnityEngine;
using MysteryRooms.Game.Data; 

public class MapScrollProp : MonoBehaviour, IInteractable
{
    [Tooltip("Drag the main Astrolabe Globe puzzle object here")]
    public MapCoordinatesPuzzle parentPuzzle;

    private bool isViewing = false;

    public string GetInteractionPrompt()
    {
        return isViewing ? "Press E to Put Away Map" : "Press E to Read Map";
    }

    public void Interact()
    {
        if (parentPuzzle == null) return;

        isViewing = !isViewing;

        if (isViewing)
        {
            parentPuzzle.OpenMap();
        }
        else
        {
            parentPuzzle.CloseMap();
        }
    }
}
