using UnityEngine;
using UnityEngine.UI; // Required for UI Image
using System.Collections.Generic;

public class SpriteSequenceDisplay : MonoBehaviour
{
    public enum EgyptianSymbol
    {
        Ankh,
        EyeOfHorus,
        WingedScarab,
        HorusFalcon,
        HieroglyphTablet,
        Pyramid,
        Sphinx,
        PharaohFront,
        AnubisSeated,
        AnubisStanding,
        CrookAndFlail,
        BlueLotus,
        PapyrusFlowers,
        WingedScarabBlue,
        Cobra,
        PharaohSeated,
        HieroglyphBirdTablet,
        AncientScroll,
        EyeOfHorusGold,
        GoldenFeather,
        HieroglyphVase,
        ScarabPendant,
        ScalesOfJustice,
        Hourglass,
        EgyptianTemple,
        EgyptianChest,
        SealedScroll,
        SunDisk,
        HieroglyphStone,
        FlyingFalcon,
        AnubisWalking,
        PharaohProfile,
        ScarabRed,
        CrookAndFlailBlue,
        PalmTree,
        TempleDoorway,
        EgyptianBoat,
        SacredBrazier,
        AnubisStatue,
        SunCompass
    }
    [Header("Your 40 Sliced Sprites")]
    public List<Sprite> atlasSprites = new List<Sprite>();

    [Header("Your 40 UI Image Placeholders")]
    public List<Image> imagePlaceholders = new List<Image>();

    void Start()
    {
        DisplayAllSprites();
    }

    public void DisplayAllSprites()
    {
        // Find out how many items we can safely iterate through 
        // (prevents errors if you have 39 placeholders but 40 sprites)
        int count = Mathf.Min(atlasSprites.Count, imagePlaceholders.Count);

        for (int i = 0; i < count; i++)
        {
            // Assign the sprite at index 'i' to the placeholder at index 'i'
            if (imagePlaceholders[i] != null && atlasSprites[i] != null)
            {
                imagePlaceholders[i].sprite = atlasSprites[i];
            }
        }

        // Just a helpful warning in case the counts don't match
        if (atlasSprites.Count != imagePlaceholders.Count)
        {
            Debug.LogWarning($"Mismatch: You have {atlasSprites.Count} sprites and {imagePlaceholders.Count} placeholders.");
        }
    }
}
