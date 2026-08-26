using UnityEngine;
using MysteryRooms.Game.Data;
using UnityEngine.UI;

[System.Serializable]
public class SymbolButton : MonoBehaviour, IInteractable
{
    public string symbolName;
    public Image iconImage;
    public System.Action<string> onSymbolClicked;

    // What the player sees when they look at this specific button
    public string GetInteractionPrompt()
    {
        // Don't show a prompt if the name hasn't been assigned by the backend yet
        if (string.IsNullOrEmpty(symbolName)) return "";
        
        return $"Press E to push '{symbolName}'";
    }

    public void SetSprite(Sprite newSprite)
    {
        if (iconImage != null)
        {
            iconImage.sprite = newSprite;
        }
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