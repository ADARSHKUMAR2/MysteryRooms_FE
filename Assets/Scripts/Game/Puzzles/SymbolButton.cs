using UnityEngine;
using MysteryRooms.Game.Data;

[System.Serializable]
public class SymbolButton : MonoBehaviour, IInteractable
{
    public string symbolName;
    public System.Action<string> onSymbolClicked;

    // What the player sees when they look at this specific button
    public string GetInteractionPrompt()
    {
        // Don't show a prompt if the name hasn't been assigned by the backend yet
        if (string.IsNullOrEmpty(symbolName)) return "";
        
        return $"Press E to push '{symbolName}'";
    }

    // Called by your player's InteractionSystem when they press E
    public void Interact()
    {
        OnClick();
    }

    public void OnClick()
    {
        onSymbolClicked?.Invoke(symbolName);
    }
}