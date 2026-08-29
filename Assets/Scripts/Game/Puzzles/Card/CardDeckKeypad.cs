using UnityEngine;
using UnityEngine.UI;
using MysteryRooms.Game.Data; 
using System.Collections;

[RequireComponent(typeof(Button))]
public class CardDeckKeypad : MonoBehaviour, IInteractable
{
    [SerializeField] private string digitValue; // "1", "2", "3", "4"
    
    private CardDeckRiddlePuzzle targetPuzzle;
    private Button uiButton;

    private void Start()
    {
        targetPuzzle = GetComponentInParent<CardDeckRiddlePuzzle>();
        uiButton = GetComponent<Button>();
        
        if (targetPuzzle == null)
        {
            Debug.LogError($"[CardDeckKeypad] No CardDeckRiddlePuzzle found in parents!");
            return;
        }

        // Button btn = GetComponent<Button>();
        // btn.onClick.AddListener(OnButtonClicked);

        uiButton.onClick.AddListener(OnButtonClicked);
    }

    public string GetInteractionPrompt()
    {
        return $"Press E to input '{digitValue}'";
    }

    public void Interact()
    {
        // When player presses E while looking at this button
        OnButtonClicked();
        
        // Optional: Flash the button color to show it was pressed
        StartCoroutine(FlashButton());
    }

    // ------------------------------------

    private void OnButtonClicked()
    {
        if (targetPuzzle != null)
        {
            targetPuzzle.OnNumberInput(digitValue);
            
            // Play a click sound if you have one attached
            AudioSource audio = GetComponent<AudioSource>();
            if (audio != null) audio.Play();
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
