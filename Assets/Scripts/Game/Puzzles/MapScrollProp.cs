using UnityEngine;
using MysteryRooms.Game.Data; 
using System.Collections;

public class MapScrollProp : MonoBehaviour, IInteractable
{
    [Tooltip("Drag the main Astrolabe Globe puzzle object here")]
    public MapCoordinatesPuzzle parentPuzzle;
    [Tooltip("Time in seconds before the map auto-closes")]
    [SerializeField] private float autoCloseDelay = 5f;

    private bool isViewing = false;
    private Coroutine autoCloseCoroutine;

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
            // Start auto-close timer
            if (autoCloseCoroutine != null)
            {
                StopCoroutine(autoCloseCoroutine);
            }
            autoCloseCoroutine = StartCoroutine(AutoCloseMapAfterDelay());
        }
        else
        {
            // Manual close - cancel the auto-close timer
            if (autoCloseCoroutine != null)
            {
                StopCoroutine(autoCloseCoroutine);
                autoCloseCoroutine = null;
            }
            parentPuzzle.CloseMap();
        }
    }

    private IEnumerator AutoCloseMapAfterDelay()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        
        // Auto-close the map
        if (isViewing)
        {
            isViewing = false;
            parentPuzzle.CloseMap();
            autoCloseCoroutine = null;
        }
    }

    private void OnDisable()
    {
        // Clean up coroutine if this object is disabled
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }
    }
}
