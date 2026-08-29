using UnityEngine;
using UnityEngine.UI;

public class CardVisualClue : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The Image component that will show the Suit (Spades, Hearts, etc.)")]
    public Image suitImage;
    
    [Tooltip("The Image component that will show your custom 1-4 number sprite")]
    public Image numberImage;

    [Header("Number Sprites (1 to 4)")]
    [Tooltip("Put your image for 1 in Element 0, image for 2 in Element 1, etc.")]
    public Sprite[] numberSprites = new Sprite[4];

    [Header("Suit Sprites")]
    public Sprite spadesSprite;
    public Sprite heartsSprite;
    public Sprite diamondsSprite;
    public Sprite clubsSprite;

    /// <summary>
    /// Configures this specific image clue based on the AI's rule
    /// </summary>
    public void SetClue(string suitName, int sequenceNumber)
    {
        // 1. Set your custom Number Sprite (sequenceNumber is 1, 2, 3, or 4)
        if (numberImage != null && numberSprites.Length >= 4)
        {
            // Arrays are 0-indexed, so Sequence 1 = Index 0
            int arrayIndex = sequenceNumber - 1;
            
            if (arrayIndex >= 0 && arrayIndex < numberSprites.Length)
            {
                numberImage.sprite = numberSprites[arrayIndex];
                numberImage.color = Color.white; // Ensure it's fully visible
            }
        }

        // 2. Set the correct suit image
        if (suitImage != null)
        {
            switch (suitName.ToLower())
            {
                case "spades":
                    suitImage.sprite = spadesSprite;
                    break;
                case "hearts":
                    suitImage.sprite = heartsSprite;
                    break;
                case "diamonds":
                    suitImage.sprite = diamondsSprite;
                    break;
                case "clubs":
                    suitImage.sprite = clubsSprite;
                    break;
            }
        }
    }
}
