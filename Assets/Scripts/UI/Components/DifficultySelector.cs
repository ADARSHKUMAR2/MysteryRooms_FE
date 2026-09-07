using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

namespace MysteryRooms.UI
{
    public class DifficultySelector : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider difficultySlider;
        [SerializeField] private TextMeshProUGUI difficultyLabel;
        [SerializeField] private TextMeshProUGUI difficultyValue;
        [SerializeField] private Image difficultyIcon;
        [SerializeField] private Image sliderFill;
        
        [Header("Difficulty Settings")]
        [SerializeField] private int minDifficulty = 1;
        [SerializeField] private int maxDifficulty = 10;
        [SerializeField] private int defaultDifficulty = 1;
        
        [Header("Visual Settings")]
        [SerializeField] private Color easyColor = new Color(0.3f, 0.8f, 0.3f);
        [SerializeField] private Color mediumColor = new Color(0.8f, 0.8f, 0.3f);
        [SerializeField] private Color hardColor = new Color(0.8f, 0.3f, 0.3f);
        [SerializeField] private Gradient difficultyGradient;
        
        [Header("Animation")]
        [SerializeField] private float scaleMultiplier = 1.2f;
        [SerializeField] private float animationDuration = 0.2f;
        
        public event Action<int> OnDifficultyChanged;
        
        private int currentDifficulty;

        private void Awake()
        {
            SetupSlider();
            SetupGradient();
        }

        private void Start()
        {
            SetDifficulty(defaultDifficulty, false);
        }

        private void SetupSlider()
        {
            if (difficultySlider == null)
            {
                Debug.LogError("Difficulty slider is not assigned!");
                return;
            }
            
            difficultySlider.minValue = minDifficulty;
            difficultySlider.maxValue = maxDifficulty;
            difficultySlider.wholeNumbers = true;
            difficultySlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        private void SetupGradient()
        {
            if (difficultyGradient == null)
            {
                difficultyGradient = new Gradient();
                
                GradientColorKey[] colorKeys = new GradientColorKey[3];
                colorKeys[0] = new GradientColorKey(easyColor, 0f);
                colorKeys[1] = new GradientColorKey(mediumColor, 0.5f);
                colorKeys[2] = new GradientColorKey(hardColor, 1f);
                
                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
                alphaKeys[0] = new GradientAlphaKey(1f, 0f);
                alphaKeys[1] = new GradientAlphaKey(1f, 1f);
                
                difficultyGradient.SetKeys(colorKeys, alphaKeys);
            }
        }

        private void OnSliderValueChanged(float value)
        {
            int difficulty = Mathf.RoundToInt(value);
            SetDifficulty(difficulty, true);
        }

        public void SetDifficulty(int difficulty, bool notify = true)
        {
            difficulty = Mathf.Clamp(difficulty, minDifficulty, maxDifficulty);
            
            if (currentDifficulty == difficulty && notify)
                return;
            
            currentDifficulty = difficulty;
            
            UpdateUI();
            
            if (notify)
            {
                OnDifficultyChanged?.Invoke(currentDifficulty);
                StartCoroutine(SelectionFeedback());
            }
        }

        private void UpdateUI()
        {
            // Update slider
            if (difficultySlider != null)
                difficultySlider.value = currentDifficulty;
            
            // Update text
            if (difficultyValue != null)
                difficultyValue.text = currentDifficulty.ToString();
            
            if (difficultyLabel != null)
                difficultyLabel.text = GetDifficultyLabel(currentDifficulty);
            
            // Update colors
            float normalizedDifficulty = (float)(currentDifficulty - minDifficulty) / (maxDifficulty - minDifficulty);
            Color difficultyColor = difficultyGradient.Evaluate(normalizedDifficulty);
            
            if (sliderFill != null)
                sliderFill.color = difficultyColor;
            
            if (difficultyValue != null)
                difficultyValue.color = difficultyColor;
            
            if (difficultyIcon != null)
                difficultyIcon.color = difficultyColor;
        }

        private string GetDifficultyLabel(int difficulty)
        {
            float normalizedDifficulty = (float)(difficulty - minDifficulty) / (maxDifficulty - minDifficulty);
            
            if (normalizedDifficulty < 0.33f)
                return "Easy";
            else if (normalizedDifficulty < 0.66f)
                return "Medium";
            else
                return "Hard";
        }

        private IEnumerator SelectionFeedback()
        {
            if (difficultyValue == null) yield break;
            
            Vector3 originalScale = difficultyValue.transform.localScale;
            
            // Scale up
            float elapsed = 0f;
            while (elapsed < animationDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (animationDuration * 0.5f);
                difficultyValue.transform.localScale = Vector3.Lerp(originalScale, originalScale * scaleMultiplier, progress);
                yield return null;
            }
            
            // Scale down
            elapsed = 0f;
            while (elapsed < animationDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / (animationDuration * 0.5f);
                difficultyValue.transform.localScale = Vector3.Lerp(originalScale * scaleMultiplier, originalScale, progress);
                yield return null;
            }
            
            difficultyValue.transform.localScale = originalScale;
        }

        public int GetCurrentDifficulty()
        {
            return currentDifficulty;
        }
    }
}
