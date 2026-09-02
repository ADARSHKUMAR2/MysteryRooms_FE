using UnityEngine;
using TMPro;
using System.Collections;
using MysteryRooms.Game.Data; // If using IInteractable
using MysteryRooms.Game.Managers; 
public class WallInscription : MonoBehaviour, IInteractable
{
    [Header("Location Data")]
    [Tooltip("Which room is this inscription physically placed in?")]
    public RoomType roomLocation;
    
    [Header("Clue Data")]
    [TextArea(3, 10)]
    public string englishClueText; // Will be set by backend!
    
    [Header("UI References")]
    [SerializeField] private TextMeshPro hieroglyphText;
    [SerializeField] private TextMeshPro englishText;
    [SerializeField] private ParticleSystem magicDustParticles;
    
    [Header("Settings")]
    [SerializeField] private float fadeDuration = 2.5f;
    [SerializeField] private Color glowingGold = new Color(1f, 0.8f, 0.2f, 1f);

    private bool isTranslated = false;
    private AudioSource magicSound;

    private void Awake()
    {
        magicSound = GetComponent<AudioSource>();
        
        // Hide English, show Hieroglyphs
        if (englishText != null)
        {
            englishText.color = new Color(glowingGold.r, glowingGold.g, glowingGold.b, 0f);
            // Hide the text completely to save rendering cost until needed
            englishText.gameObject.SetActive(false); 
        }
        
        if (hieroglyphText != null)
        {
            hieroglyphText.color = new Color(0.1f, 0.08f, 0.05f, 0.9f); // Dark carved stone color
        }
    }

    /// <summary>
    /// Call this when parsing the Backend JSON to inject the AI's clue!
    /// </summary>
    public void SetClueText(string clue)
    {
        englishClueText = clue;
        if (englishText != null)
        {
            englishText.text = clue;
        }
        
        // Make the hieroglyph text roughly the same length so it looks believable
        if (hieroglyphText != null)
        {
            hieroglyphText.text = clue; 
            // Note: Because it uses a Hieroglyph font, English letters will just render as symbols!
        }
    }

    public string GetInteractionPrompt()
    {
        return isTranslated ? "" : "Press E to Translate Ancient Carving";
    }

    public void Interact()
    {
        if (!isTranslated)
        {
            StartCoroutine(TranslateAnimation());
        }
    }

    private IEnumerator TranslateAnimation()
    {
        isTranslated = true;
        
        if (magicSound != null) magicSound.Play();
        if (magicDustParticles != null) magicDustParticles.Play();

        englishText.gameObject.SetActive(true);
        
        float elapsed = 0f;
        Color startHiero = hieroglyphText.color;
        Color endHiero = new Color(startHiero.r, startHiero.g, startHiero.b, 0f);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            float smoothT = Mathf.SmoothStep(0, 1, t);

            // Fade OUT carved stone hieroglyphs
            if (hieroglyphText != null)
                hieroglyphText.color = Color.Lerp(startHiero, endHiero, smoothT);
            
            // Fade IN glowing English translation
            if (englishText != null)
                englishText.color = new Color(glowingGold.r, glowingGold.g, glowingGold.b, smoothT);
            
            yield return null;
        }
        
        // Ensure final state
        if (hieroglyphText != null) hieroglyphText.gameObject.SetActive(false);
        if (englishText != null) englishText.color = glowingGold;
    }
}
