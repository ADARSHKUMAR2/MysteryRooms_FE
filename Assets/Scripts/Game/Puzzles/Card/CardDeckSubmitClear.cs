using UnityEngine;
using UnityEngine.UI;
using MysteryRooms.Game.Data;
using System.Collections;

[RequireComponent(typeof(Button))]
public class CardDeckSubmitClear : MonoBehaviour, IInteractable
{
    public enum ButtonType { Submit, Clear }
    public ButtonType type;
    
    private Button uiButton;

    private void Start()
    {
        uiButton = GetComponent<Button>();
    }

    public string GetInteractionPrompt()
    {
        return type == ButtonType.Submit ? "Press E to Submit Code" : "Press E to Clear Code";
    }

    public void Interact()
    {
        // Simulate a UI click
        if (uiButton != null && uiButton.interactable)
        {
            uiButton.onClick.Invoke();
            StartCoroutine(FlashButton());
        }
    }
    
    private IEnumerator FlashButton()
    {
        if (uiButton == null) yield break;
        
        Color originalColor = uiButton.colors.normalColor;
        ColorBlock cb = uiButton.colors;
        cb.normalColor = uiButton.colors.pressedColor;
        uiButton.colors = cb;
        
        yield return new WaitForSeconds(0.1f);
        
        cb.normalColor = originalColor;
        uiButton.colors = cb;
    }
}
