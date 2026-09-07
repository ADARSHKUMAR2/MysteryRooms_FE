using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace MysteryRooms.UI
{
    public class MysteryCard : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform cardRect;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image difficultyBadge;
        [SerializeField] private TextMeshProUGUI difficultyText;
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image cardBorder;
        
        [Header("Animation Settings")]
        [SerializeField] private AnimationType animationType = AnimationType.SlideAndFade;
        [SerializeField] private float animationDuration = 0.5f;
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Difficulty Colors")]
        [SerializeField] private Gradient difficultyGradient;
        
        public enum AnimationType
        {
            Fade,
            SlideAndFade,
            Scale,
            Flip
        }
        
        private Vector3 originalPosition;
        private Vector3 originalScale;
        private bool isVisible = false;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            
            if (cardRect == null)
                cardRect = GetComponent<RectTransform>();
            
            originalPosition = cardRect.anchoredPosition;
            originalScale = cardRect.localScale;
            
            SetupDifficultyGradient();
        }

        private void SetupDifficultyGradient()
        {
            if (difficultyGradient == null)
            {
                difficultyGradient = new Gradient();
                
                GradientColorKey[] colorKeys = new GradientColorKey[3];
                colorKeys[0] = new GradientColorKey(new Color(0.3f, 0.8f, 0.3f), 0f);
                colorKeys[1] = new GradientColorKey(new Color(0.8f, 0.8f, 0.3f), 0.5f);
                colorKeys[2] = new GradientColorKey(new Color(0.8f, 0.3f, 0.3f), 1f);
                
                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
                alphaKeys[0] = new GradientAlphaKey(1f, 0f);
                alphaKeys[1] = new GradientAlphaKey(1f, 1f);
                
                difficultyGradient.SetKeys(colorKeys, alphaKeys);
            }
        }

        public void Show(string title, string description, int difficulty)
        {
            if (isVisible) return;
            
            // Set content
            if (titleText != null)
                titleText.text = title;
            
            if (descriptionText != null)
                descriptionText.text = description;
            
            if (difficultyText != null)
                difficultyText.text = $"Difficulty: {difficulty}";
            
            // Set difficulty color
            float normalizedDifficulty = (difficulty - 1f) / 9f; // Assuming 1-10 scale
            Color diffColor = difficultyGradient.Evaluate(normalizedDifficulty);
            
            if (difficultyBadge != null)
                difficultyBadge.color = diffColor;
            
            if (cardBorder != null)
                cardBorder.color = diffColor;
            
            // Play animation
            StopAllCoroutines();
            StartCoroutine(ShowAnimation());
        }

        public void Hide(bool immediate = false)
        {
            if (!isVisible && !immediate) return;
            
            if (immediate)
            {
                canvasGroup.alpha = 0f;
                gameObject.SetActive(false);
                isVisible = false;
            }
            else
            {
                StopAllCoroutines();
                StartCoroutine(HideAnimation());
            }
        }

        private IEnumerator ShowAnimation()
        {
            gameObject.SetActive(true);
            isVisible = true;
            
            switch (animationType)
            {
                case AnimationType.Fade:
                    yield return FadeAnimation(0f, 1f);
                    break;
                
                case AnimationType.SlideAndFade:
                    yield return SlideAndFadeAnimation(true);
                    break;
                
                case AnimationType.Scale:
                    yield return ScaleAnimation(true);
                    break;
                
                case AnimationType.Flip:
                    yield return FlipAnimation(true);
                    break;
            }
        }

        private IEnumerator HideAnimation()
        {
            switch (animationType)
            {
                case AnimationType.Fade:
                    yield return FadeAnimation(1f, 0f);
                    break;
                
                case AnimationType.SlideAndFade:
                    yield return SlideAndFadeAnimation(false);
                    break;
                
                case AnimationType.Scale:
                    yield return ScaleAnimation(false);
                    break;
                
                case AnimationType.Flip:
                    yield return FlipAnimation(false);
                    break;
            }
            
            gameObject.SetActive(false);
            isVisible = false;
        }

        private IEnumerator FadeAnimation(float from, float to)
        {
            float elapsed = 0f;
            
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float progress = animationCurve.Evaluate(elapsed / animationDuration);
                canvasGroup.alpha = Mathf.Lerp(from, to, progress);
                yield return null;
            }
            
            canvasGroup.alpha = to;
        }

        private IEnumerator SlideAndFadeAnimation(bool show)
        {
            Vector3 startPos = show ? originalPosition + Vector3.right * 500f : originalPosition;
            Vector3 endPos = show ? originalPosition : originalPosition + Vector3.right * 500f;
            
            float startAlpha = show ? 0f : 1f;
            float endAlpha = show ? 1f : 0f;
            
            float elapsed = 0f;
            
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float progress = animationCurve.Evaluate(elapsed / animationDuration);
                
                cardRect.anchoredPosition = Vector3.Lerp(startPos, endPos, progress);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
                
                yield return null;
            }
            
            cardRect.anchoredPosition = endPos;
            canvasGroup.alpha = endAlpha;
        }

        private IEnumerator ScaleAnimation(bool show)
        {
            Vector3 startScale = show ? Vector3.zero : originalScale;
            Vector3 endScale = show ? originalScale : Vector3.zero;
            
            float startAlpha = show ? 0f : 1f;
            float endAlpha = show ? 1f : 0f;
            
            float elapsed = 0f;
            
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float progress = animationCurve.Evaluate(elapsed / animationDuration);
                
                cardRect.localScale = Vector3.Lerp(startScale, endScale, progress);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
                
                yield return null;
            }
            
            cardRect.localScale = endScale;
            canvasGroup.alpha = endAlpha;
        }

        private IEnumerator FlipAnimation(bool show)
        {
            float startRotation = show ? 90f : 0f;
            float midRotation = show ? 0f : 90f;
            
            float startAlpha = show ? 0f : 1f;
            float endAlpha = show ? 1f : 0f;
            
            // First half - rotate to 90 degrees
            float elapsed = 0f;
            float halfDuration = animationDuration * 0.5f;
            
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / halfDuration;
                
                float rotation = Mathf.Lerp(startRotation, 90f, progress);
                cardRect.localRotation = Quaternion.Euler(0f, rotation, 0f);
                
                if (!show)
                    canvasGroup.alpha = Mathf.Lerp(1f, 0.5f, progress);
                
                yield return null;
            }
            
            // Second half - rotate to end position
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / halfDuration;
                
                float rotation = Mathf.Lerp(90f, midRotation, progress);
                cardRect.localRotation = Quaternion.Euler(0f, rotation, 0f);
                
                if (show)
                    canvasGroup.alpha = Mathf.Lerp(0.5f, 1f, progress);
                else
                    canvasGroup.alpha = Mathf.Lerp(0.5f, 0f, progress);
                
                yield return null;
            }
            
            cardRect.localRotation = Quaternion.Euler(0f, midRotation, 0f);
            canvasGroup.alpha = endAlpha;
        }
    }
}
