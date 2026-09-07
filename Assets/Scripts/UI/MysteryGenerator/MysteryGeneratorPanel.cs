using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System;

namespace MysteryRooms.UI
{
    public class MysteryGeneratorPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private RectTransform panelRectTransform;
        
        [Header("Title")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Animator titleAnimator;
        
        [Header("Difficulty Section")]
        [SerializeField] private DifficultySelector difficultySelector;
        [SerializeField] private RectTransform difficultySectionRect;
        
        [Header("Generate Button")]
        [SerializeField] private AnimatedButton generateButton;
        [SerializeField] private GameObject loadingSpinner;
        
        [Header("Mystery Display")]
        [SerializeField] private MysteryCard mysteryCard;
        [SerializeField] private RectTransform mysteryDisplayRect;
        
        [Header("Action Buttons")]
        [SerializeField] private AnimatedButton openMysteryButton;
        [SerializeField] private AnimatedButton startButton;
        [SerializeField] private AnimatedButton backButton;
        
        [Header("Effects")]
        [SerializeField] private ParticleEffectManager particleManager;
        [SerializeField] private Image backgroundOverlay;
        
        [Header("Animation Settings")]
        [SerializeField] private float panelFadeInDuration = 0.5f;
        [SerializeField] private float elementStaggerDelay = 0.1f;
        [SerializeField] private AnimationCurve entranceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Backend Configuration")]
        [SerializeField] private string backendURL = "http://localhost:8000";
        [SerializeField] private string generateEndpoint = "/api/game/mystery/generate";
        
        private int currentDifficulty = 1;
        private string currentMysteryId;
        private bool isGenerating = false;

        private void Start()
        {
            InitializeUI();
            SetupButtonListeners();
            PlayEntranceAnimation();
        }

        private void InitializeUI()
        {
            // Start with UI hidden
            panelCanvasGroup.alpha = 0f;
            
            if (loadingSpinner != null)
                loadingSpinner.SetActive(false);
            
            if (mysteryCard != null)
                mysteryCard.Hide(true);
            
            if (openMysteryButton != null)
                openMysteryButton.SetInteractable(false);
            
            if (startButton != null)
                startButton.SetInteractable(false);
        }

        private void SetupButtonListeners()
        {
            if (generateButton != null)
                generateButton.OnClick.AddListener(OnGenerateButtonClicked);
            
            if (openMysteryButton != null)
                openMysteryButton.OnClick.AddListener(OnOpenMysteryButtonClicked);
            
            if (startButton != null)
                startButton.OnClick.AddListener(OnStartButtonClicked);
            
            if (backButton != null)
                backButton.OnClick.AddListener(OnBackButtonClicked);
            
            if (difficultySelector != null)
                difficultySelector.OnDifficultyChanged += OnDifficultyChanged;
        }

        private void PlayEntranceAnimation()
        {
            StartCoroutine(EntranceAnimationSequence());
        }

        private IEnumerator EntranceAnimationSequence()
        {
            // Fade in panel
            float elapsed = 0f;
            while (elapsed < panelFadeInDuration)
            {
                elapsed += Time.deltaTime;
                panelCanvasGroup.alpha = entranceCurve.Evaluate(elapsed / panelFadeInDuration);
                yield return null;
            }
            panelCanvasGroup.alpha = 1f;

            // Animate elements in sequence
            yield return new WaitForSeconds(elementStaggerDelay);
            AnimateElement(titleText?.transform, 0);
            
            yield return new WaitForSeconds(elementStaggerDelay);
            AnimateElement(difficultySectionRect, 1);
            
            yield return new WaitForSeconds(elementStaggerDelay);
            AnimateElement(generateButton?.transform, 2);
            
            yield return new WaitForSeconds(elementStaggerDelay);
            AnimateElement(mysteryDisplayRect, 3);
            
            yield return new WaitForSeconds(elementStaggerDelay);
            AnimateElement(openMysteryButton?.transform, 4);
            AnimateElement(startButton?.transform, 4);
            
            yield return new WaitForSeconds(elementStaggerDelay);
            AnimateElement(backButton?.transform, 5);

            // Trigger particle effects
            if (particleManager != null)
                particleManager.PlayAmbientEffect();
        }

        private void AnimateElement(Transform element, int index)
        {
            if (element == null) return;
            
            StartCoroutine(ElementEntranceAnimation(element, index * elementStaggerDelay));
        }

        private IEnumerator ElementEntranceAnimation(Transform element, float delay)
        {
            Vector3 originalScale = element.localScale;
            element.localScale = Vector3.zero;
            
            yield return new WaitForSeconds(delay);
            
            float duration = 0.4f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = entranceCurve.Evaluate(elapsed / duration);
                element.localScale = originalScale * progress;
                yield return null;
            }
            
            element.localScale = originalScale;
        }

        private void OnDifficultyChanged(int difficulty)
        {
            currentDifficulty = difficulty;
            Debug.Log($"Difficulty changed to: {difficulty}");
            
            // Play feedback
            if (particleManager != null)
                particleManager.PlaySelectionEffect(difficultySectionRect.position);
        }

        private void OnGenerateButtonClicked()
        {
            if (isGenerating) return;
            
            StartCoroutine(GenerateMysteryCoroutine());
        }

        private IEnumerator GenerateMysteryCoroutine()
        {
            isGenerating = true;
            
            // Show loading state
            if (generateButton != null)
                generateButton.SetInteractable(false);
            
            if (loadingSpinner != null)
                loadingSpinner.SetActive(true);
            
            if (mysteryCard != null)
                mysteryCard.Hide(false);
            
            // Call backend API
            string url = $"{backendURL}{generateEndpoint}";
            string jsonBody = $"{{\"difficulty\": {currentDifficulty}}}";
            
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    HandleSuccessResponse(request.downloadHandler.text);
                }
                else
                {
                    Debug.LogError($"Failed to generate mystery: {request.error}");
                    ShowError("Failed to generate mystery. Please try again.");
                }
            }
            
            // Hide loading state
            if (loadingSpinner != null)
                loadingSpinner.SetActive(false);
            
            if (generateButton != null)
                generateButton.SetInteractable(true);
            
            isGenerating = false;
        }

        private void HandleSuccessResponse(string responseText)
        {
            Debug.Log($"Mystery generated: {responseText}");
            
            // Parse response
            MysteryResponse response = null;
            bool parseSuccess = false;
            
            try
            {
                response = JsonUtility.FromJson<MysteryResponse>(responseText);
                parseSuccess = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse mystery response: {e.Message}");
                ShowError("Failed to parse mystery data");
                return;
            }
            
            if (parseSuccess && response != null)
            {
                currentMysteryId = response.mystery_id;
                
                // Display mystery with delay for anticipation
                StartCoroutine(ShowMysteryWithDelay(response));
                
                // Enable action buttons
                if (openMysteryButton != null)
                    openMysteryButton.SetInteractable(true);
                
                if (startButton != null)
                    startButton.SetInteractable(true);
                
                // Play success effect
                if (particleManager != null)
                    particleManager.PlaySuccessEffect(mysteryDisplayRect.position);
            }
        }

        private IEnumerator ShowMysteryWithDelay(MysteryResponse response)
        {
            yield return new WaitForSeconds(0.5f); // Brief pause for anticipation
            
            if (mysteryCard != null)
            {
                mysteryCard.Show(response.name, response.description, currentDifficulty);
            }
        }


        private void ShowError(string message)
        {
            Debug.LogError(message);
            // TODO: Show error UI
        }

        private void OnOpenMysteryButtonClicked()
        {
            if (string.IsNullOrEmpty(currentMysteryId))
            {
                Debug.LogWarning("No mystery to open");
                return;
            }
            
            Debug.Log($"Opening mystery: {currentMysteryId}");
            // TODO: Navigate to mystery details scene
            
            if (particleManager != null)
                particleManager.PlaySuccessEffect(openMysteryButton.transform.position);
        }

        private void OnStartButtonClicked()
        {
            if (string.IsNullOrEmpty(currentMysteryId))
            {
                Debug.LogWarning("No mystery to start");
                return;
            }
            
            Debug.Log($"Starting mystery: {currentMysteryId}");
            // TODO: Start the game with current mystery
            
            if (particleManager != null)
                particleManager.PlaySuccessEffect(startButton.transform.position);
        }

        private void OnBackButtonClicked()
        {
            Debug.Log("Back button clicked");
            StartCoroutine(ExitAnimationSequence());
        }

        private IEnumerator ExitAnimationSequence()
        {
            // Disable buttons
            if (generateButton != null) generateButton.SetInteractable(false);
            if (openMysteryButton != null) openMysteryButton.SetInteractable(false);
            if (startButton != null) startButton.SetInteractable(false);
            if (backButton != null) backButton.SetInteractable(false);

            // Fade out
            float duration = 0.3f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                panelCanvasGroup.alpha = 1f - (elapsed / duration);
                yield return null;
            }
            
            panelCanvasGroup.alpha = 0f;
            
            // TODO: Navigate to previous scene or menu
            Debug.Log("Navigating back...");
        }

        [Serializable]
        private class MysteryResponse
        {
            public string mystery_id;
            public string name;
            public string description;
            public int difficulty;
        }
    }
}
