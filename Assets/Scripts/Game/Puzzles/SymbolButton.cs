using UnityEngine;
using MysteryRooms.Game.Data;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class SymbolButton : MonoBehaviour, IInteractable
{
    [Header("Symbol Data")]
    public string symbolName;
    public Image iconImage;
    public System.Action<string> onSymbolClicked;

    [Header("Visual Effects (Optional)")]
    [SerializeField] private Image borderImage;
    [SerializeField] private AudioSource hoverSound;
    [SerializeField] private AudioSource clickSound;
    
    [Header("Effect Settings")]
    // [SerializeField] private Color hoverColor = new Color(1f, 0.9f, 0.5f); // Golden
    [SerializeField] private float hoverScaleFactor = 1.35f;
    [SerializeField] private float animationSpeed = 5f;

    private Vector3 originalScale;
    private Color originalColor;
    private bool isHovered = false;
    private Coroutine hoverCoroutine;

    private void Awake()
    {
        originalScale = transform.localScale;
        if (iconImage != null)
        {
            originalColor = iconImage.color;
        }
    }

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
        // Click animation
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(ClickAnimation());
        }
        
        // Play click sound
        if (clickSound != null)
        {
            clickSound.Play();
        }
        
        // Invoke callback
        onSymbolClicked?.Invoke(symbolName);
    }

    /// <summary>
    /// Called by InteractionSystem when player looks at this symbol
    /// </summary>
    public void OnHoverEnter()
    {
        if (!enabled || !gameObject.activeInHierarchy || isHovered) return;
        
        isHovered = true;
        
        // Stop any existing animation
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }
        
        // Start hover animation
        hoverCoroutine = StartCoroutine(HoverEnterAnimation());
        
        // Play hover sound
        if (hoverSound != null)
        {
            hoverSound.Play();
        }
    }

    /// <summary>
    /// Called by InteractionSystem when player stops looking at this symbol
    /// </summary>
    public void OnHoverExit()
    {
        if (!enabled || !gameObject.activeInHierarchy || !isHovered) return;
        
        isHovered = false;
        
        // Stop any existing animation
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }
        
        // Start exit animation
        hoverCoroutine = StartCoroutine(HoverExitAnimation());
    }

    private IEnumerator HoverEnterAnimation()
    {
        Vector3 targetScale = originalScale * hoverScaleFactor;
        // Color targetColor = hoverColor;
        
        while (Vector3.Distance(transform.localScale, targetScale) > 0.01f)
        {
            // Animate scale
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
            
            // Animate icon brightness
            if (iconImage != null)
            {
                // iconImage.color = Color.Lerp(iconImage.color, targetColor, Time.deltaTime * animationSpeed);
            }
            
            // Animate border
            if (borderImage != null)
            {
                // borderImage.color = Color.Lerp(borderImage.color, hoverColor, Time.deltaTime * animationSpeed);
            }
            
            yield return null;
        }
        
        // Ensure final values
        transform.localScale = targetScale;
        // if (iconImage != null) iconImage.color = targetColor;
        // if (borderImage != null) borderImage.color = hoverColor;
    }

    private IEnumerator HoverExitAnimation()
    {
        Vector3 targetScale = originalScale;
        
        while (Vector3.Distance(transform.localScale, targetScale) > 0.01f)
        {
            // Animate scale back
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
            
            // Animate color back
            if (iconImage != null)
            {
                // iconImage.color = Color.Lerp(iconImage.color, originalColor, Time.deltaTime * animationSpeed);
            }
            
            // Animate border back
            if (borderImage != null)
            {
                // Color goldColor = new Color(0.85f, 0.65f, 0.13f);
                // borderImage.color = Color.Lerp(borderImage.color, goldColor, Time.deltaTime * animationSpeed);
            }
            
            yield return null;
        }
        
        // Ensure final values
        transform.localScale = targetScale;
        // if (iconImage != null) iconImage.color = originalColor;
    }

    private IEnumerator ClickAnimation()
    {
        // Quick punch effect
        Vector3 punchScale = originalScale * 0.85f;
        float duration = 0.1f;
        float elapsed = 0f;
        
        Vector3 startScale = transform.localScale;
        
        // Shrink
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, punchScale, elapsed / duration);
            yield return null;
        }
        
        // Bounce back
        elapsed = 0f;
        Vector3 targetScale = isHovered ? originalScale * hoverScaleFactor : originalScale;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(punchScale, targetScale, elapsed / duration);
            yield return null;
        }
        
        transform.localScale = targetScale;
    }
    
    private void OnDisable()
    {
        // Clean up on disable
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }
        isHovered = false;
    }
}