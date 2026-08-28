using UnityEngine;
using MysteryRooms.Game.Data;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class CardButton : MonoBehaviour, IInteractable
{
    [Header("Card Data")]
    public CardData cardData;
    public Image cardImage;
    
    [Header("Visual Effects")]
    [SerializeField] private Image borderImage;
    [SerializeField] private AudioSource hoverSound;
    [SerializeField] private AudioSource clickSound;
    
    [Header("Effect Settings")]
    [SerializeField] private Color hoverColor = new Color(1f, 0.9f, 0.5f);
    [SerializeField] private float hoverScaleFactor = 1.1f;
    [SerializeField] private float animationSpeed = 5f;

    private Vector3 originalScale;
    private bool isHovered = false;
    private Coroutine hoverCoroutine;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public string GetInteractionPrompt()
    {
        if (cardData == null) return "";
        return $"Card: {cardData.rank} of {cardData.suit}";
    }

    public void SetCardData(CardData data, Sprite sprite)
    {
        cardData = data;
        if (cardImage != null)
        {
            cardImage.sprite = sprite;
        }
    }

    public void Interact()
    {
        // Cards are view-only, no interaction needed
        // Or you could add a flip animation here
    }

    /// <summary>
    /// Called by InteractionSystem when player looks at this card
    /// </summary>
    public void OnHoverEnter()
    {
        if (!enabled || !gameObject.activeInHierarchy || isHovered) return;
        
        isHovered = true;
        
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }
        
        hoverCoroutine = StartCoroutine(HoverEnterAnimation());
        
        if (hoverSound != null)
        {
            hoverSound.Play();
        }
    }

    /// <summary>
    /// Called by InteractionSystem when player stops looking at this card
    /// </summary>
    public void OnHoverExit()
    {
        if (!enabled || !gameObject.activeInHierarchy || !isHovered) return;
        
        isHovered = false;
        
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }
        
        hoverCoroutine = StartCoroutine(HoverExitAnimation());
    }

    private IEnumerator HoverEnterAnimation()
    {
        Vector3 targetScale = originalScale * hoverScaleFactor;
        
        while (Vector3.Distance(transform.localScale, targetScale) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
            
            if (borderImage != null)
            {
                borderImage.color = Color.Lerp(borderImage.color, hoverColor, Time.deltaTime * animationSpeed);
            }
            
            yield return null;
        }
        
        transform.localScale = targetScale;
        if (borderImage != null) borderImage.color = hoverColor;
    }

    private IEnumerator HoverExitAnimation()
    {
        Vector3 targetScale = originalScale;
        Color goldColor = new Color(0.85f, 0.65f, 0.13f);
        
        while (Vector3.Distance(transform.localScale, targetScale) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
            
            if (borderImage != null)
            {
                borderImage.color = Color.Lerp(borderImage.color, goldColor, Time.deltaTime * animationSpeed);
            }
            
            yield return null;
        }
        
        transform.localScale = targetScale;
    }
    
    private void OnDisable()
    {
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
        }
        isHovered = false;
    }
}
