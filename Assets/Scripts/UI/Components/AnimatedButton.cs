using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using TMPro;
using System.Collections;

namespace MysteryRooms.UI
{
    [RequireComponent(typeof(Button))]
    public class AnimatedButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("References")]
        [SerializeField] private Button button;
        [SerializeField] private Image buttonImage;
        [SerializeField] private TextMeshProUGUI buttonText;
        [SerializeField] private Image glowImage;
        [SerializeField] private ParticleSystem clickParticles;
        
        [Header("Animation Settings")]
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float pressScale = 0.95f;
        [SerializeField] private float animationSpeed = 0.15f;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Color Settings")]
        [SerializeField] private Color normalColor = new Color(0.8f, 0.6f, 0.3f, 1f);
        [SerializeField] private Color hoverColor = new Color(1f, 0.8f, 0.4f, 1f);
        [SerializeField] private Color pressColor = new Color(0.6f, 0.4f, 0.2f, 1f);
        [SerializeField] private Color disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        
        [Header("Glow Settings")]
        [SerializeField] private bool useGlow = true;
        [SerializeField] private float glowIntensity = 0.5f;
        [SerializeField] private float glowPulseSpeed = 2f;
        
        [Header("Sound Settings")]
        [SerializeField] private AudioClip hoverSound;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private float soundVolume = 0.5f;
        
        public UnityEvent OnClick = new UnityEvent();
        
        private Vector3 originalScale;
        private Coroutine currentAnimation;
        private bool isHovering = false;
        private bool isPressed = false;
        private AudioSource audioSource;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            
            if (buttonImage == null)
                buttonImage = GetComponent<Image>();
            
            originalScale = transform.localScale;
            
            // Setup audio
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = soundVolume;
            
            // Setup button click listener
            button.onClick.AddListener(() => OnClick?.Invoke());
            
            // Initialize colors
            if (buttonImage != null)
                buttonImage.color = normalColor;
            
            if (glowImage != null)
            {
                glowImage.gameObject.SetActive(useGlow);
                if (useGlow)
                    StartCoroutine(GlowPulseCoroutine());
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!button.interactable) return;
            
            isHovering = true;
            PlayHoverSound();
            AnimateButton(hoverScale, hoverColor);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!button.interactable) return;
            
            isHovering = false;
            if (!isPressed)
                AnimateButton(1f, normalColor);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!button.interactable) return;
            
            isPressed = true;
            AnimateButton(pressScale, pressColor);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!button.interactable) return;
            
            isPressed = false;
            PlayClickSound();
            PlayClickParticles();
            
            if (isHovering)
                AnimateButton(hoverScale, hoverColor);
            else
                AnimateButton(1f, normalColor);
        }

        private void AnimateButton(float targetScale, Color targetColor)
        {
            if (currentAnimation != null)
                StopCoroutine(currentAnimation);
            
            currentAnimation = StartCoroutine(AnimateCoroutine(targetScale, targetColor));
        }

        private IEnumerator AnimateCoroutine(float targetScale, Color targetColor)
        {
            Vector3 startScale = transform.localScale;
            Color startColor = buttonImage != null ? buttonImage.color : normalColor;
            
            float elapsed = 0f;
            
            while (elapsed < animationSpeed)
            {
                elapsed += Time.deltaTime;
                float progress = scaleCurve.Evaluate(elapsed / animationSpeed);
                
                transform.localScale = Vector3.Lerp(startScale, originalScale * targetScale, progress);
                
                if (buttonImage != null)
                    buttonImage.color = Color.Lerp(startColor, targetColor, progress);
                
                yield return null;
            }
            
            transform.localScale = originalScale * targetScale;
            if (buttonImage != null)
                buttonImage.color = targetColor;
        }

        private IEnumerator GlowPulseCoroutine()
        {
            if (glowImage == null) yield break;
            
            float time = 0f;
            Color glowColor = glowImage.color;
            
            while (true)
            {
                time += Time.deltaTime * glowPulseSpeed;
                float alpha = (Mathf.Sin(time) + 1f) * 0.5f * glowIntensity;
                glowColor.a = alpha;
                glowImage.color = glowColor;
                
                yield return null;
            }
        }

        private void PlayHoverSound()
        {
            if (hoverSound != null && audioSource != null)
                audioSource.PlayOneShot(hoverSound, soundVolume);
        }

        private void PlayClickSound()
        {
            if (clickSound != null && audioSource != null)
                audioSource.PlayOneShot(clickSound, soundVolume);
        }

        private void PlayClickParticles()
        {
            if (clickParticles != null)
                clickParticles.Play();
        }

        public void SetInteractable(bool interactable)
        {
            button.interactable = interactable;
            
            if (!interactable)
            {
                transform.localScale = originalScale;
                if (buttonImage != null)
                    buttonImage.color = disabledColor;
                isHovering = false;
                isPressed = false;
            }
            else
            {
                if (buttonImage != null)
                    buttonImage.color = normalColor;
            }
        }

        private void OnValidate()
        {
            if (button == null)
                button = GetComponent<Button>();
            
            if (buttonImage == null)
                buttonImage = GetComponent<Image>();
        }
    }
}
