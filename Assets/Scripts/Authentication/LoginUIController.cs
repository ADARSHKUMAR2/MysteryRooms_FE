using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MysteryRooms.Authentication;
using UnityEngine.SceneManagement;

namespace MysteryRooms.UI
{
    /// <summary>
    /// Controls the login/registration UI with mobile-optimized layout
    /// Reference Resolution: 1920x1080 (scales to all mobile devices)
    /// </summary>
    public class LoginUIController : MonoBehaviour
    {
        #region UI References - Assign in Inspector
        [Header("Panels")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private CanvasGroup loginCanvasGroup;

        [SerializeField] private GameObject mainMenuPanel;
        
        [Header("Input Fields")]
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Button togglePasswordVisibility;
        [SerializeField] private Image passwordVisibilityIcon;
        
        [Header("Buttons")]
        [SerializeField] private Button loginButton;
        [SerializeField] private Button registerButton;
        [SerializeField] private Button googleSignInButton;
        [SerializeField] private Button toggleModeButton;
        
        [Header("Text Elements")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text toggleModeText;
        [SerializeField] private TMP_Text loadingText;
        
        [Header("Visual Elements")]
        [SerializeField] private Image backgroundGradient;
        [SerializeField] private RectTransform loginCard;
        [SerializeField] private GameObject errorToast;
        [SerializeField] private TMP_Text errorToastText;
        
        [Header("Animation Settings")]
        [SerializeField] private float fadeSpeed = 2f;
        [SerializeField] private float cardAnimSpeed = 1.5f;
        #endregion

        #region Private Variables
        private bool isLoginMode = true;
        private bool isPasswordVisible = false;
        private bool isProcessing = false;
        #endregion

        #region Unity Lifecycle
        void Start()
        {
            // Subscribe to authentication events
            if (FirebaseAuthManager.Instance != null)
            {
                FirebaseAuthManager.Instance.OnAuthenticationSuccess += OnLoginSuccess;
                FirebaseAuthManager.Instance.OnAuthenticationError += OnLoginError;
            }
            
            // Setup button listeners
            loginButton.onClick.AddListener(OnLoginButtonClicked);
            registerButton.onClick.AddListener(OnRegisterButtonClicked);
            googleSignInButton.onClick.AddListener(OnGoogleSignInClicked);
            toggleModeButton.onClick.AddListener(ToggleLoginRegisterMode);
            togglePasswordVisibility.onClick.AddListener(TogglePasswordVisibility);
            
            // Initial setup
            SetLoginMode(true);
            HideLoading();
            HideErrorToast();

            // Hide main menu if it exists
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(false);
            
            // Animate UI on start
            StartCoroutine(AnimateLoginCardIn());
        }

        void OnDestroy()
        {
            // Unsubscribe from events
            if (FirebaseAuthManager.Instance != null)
            {
                FirebaseAuthManager.Instance.OnAuthenticationSuccess -= OnLoginSuccess;
                FirebaseAuthManager.Instance.OnAuthenticationError -= OnLoginError;
            }
        }
        #endregion

        #region Button Handlers
        /// <summary>
        /// Handles login button click
        /// </summary>
        private void OnLoginButtonClicked()
        {
            if (isProcessing) return;
            
            string email = emailInput.text.Trim();
            string password = passwordInput.text;
            
            // Validate inputs
            if (!ValidateInputs(email, password)) return;
            
            // Show loading
            ShowLoading("Logging in...");
            isProcessing = true;
            
            // Call Firebase Auth Manager
            FirebaseAuthManager.Instance.LoginUser(email, password);
        }
        
        /// <summary>
        /// Handles register button click
        /// </summary>
        private void OnRegisterButtonClicked()
        {
            if (isProcessing) return;
            
            string email = emailInput.text.Trim();
            string password = passwordInput.text;
            
            // Validate inputs
            if (!ValidateInputs(email, password)) return;
            
            // Extra validation for registration
            if (password.Length < 6)
            {
                ShowError("Password must be at least 6 characters");
                return;
            }
            
            // Show loading
            ShowLoading("Creating account...");
            isProcessing = true;
            
            // Call Firebase Auth Manager
            FirebaseAuthManager.Instance.RegisterUser(email, password);
        }
        
        /// <summary>
        /// Handles Google Sign-In button click
        /// </summary>
        private void OnGoogleSignInClicked()
        {
            if (isProcessing) return;
            
            ShowLoading("Signing in with Google...");
            isProcessing = true;
            
            FirebaseAuthManager.Instance.SignInWithGoogle();
        }
        
        /// <summary>
        /// Toggles between Login and Register modes
        /// </summary>
        private void ToggleLoginRegisterMode()
        {
            if (isProcessing) return;
            
            SetLoginMode(!isLoginMode);
            
            // Animate mode change
            StartCoroutine(AnimateModeChange());
        }
        
        /// <summary>
        /// Toggles password visibility (show/hide)
        /// </summary>
        private void TogglePasswordVisibility()
        {
            isPasswordVisible = !isPasswordVisible;
            passwordInput.contentType = isPasswordVisible ? 
                TMP_InputField.ContentType.Standard : 
                TMP_InputField.ContentType.Password;
            passwordInput.ForceLabelUpdate();
            
            // Update icon (you can swap sprites here)
            Color iconColor = passwordVisibilityIcon.color;
            iconColor.a = isPasswordVisible ? 1f : 0.5f;
            passwordVisibilityIcon.color = iconColor;
        }
        #endregion

        #region Authentication Callbacks
        /// <summary>
        /// Called when authentication succeeds
        /// </summary>
        private void OnLoginSuccess(UserProfile profile)
        {
            isProcessing = false;
            HideLoading();
            
            Debug.Log($"✅ Login successful! Welcome {profile.email}");
            
            // Show success message
            statusText.text = $"Welcome, {profile.email}!";
            statusText.color = Color.green;
            
            // Animate out and load game scene
            StartCoroutine(TransitionToGameScene());
        }
        
        /// <summary>
        /// Called when authentication fails
        /// </summary>
        private void OnLoginError(string error)
        {
            isProcessing = false;
            HideLoading();
            
            Debug.LogError($"❌ Login error: {error}");
            ShowError(error);
        }
        #endregion

        #region UI State Management
        /// <summary>
        /// Sets the UI to Login or Register mode
        /// </summary>
        private void SetLoginMode(bool login)
        {
            isLoginMode = login;
            
            if (isLoginMode)
            {
                titleText.text = "Welcome Back";
                loginButton.gameObject.SetActive(true);
                registerButton.gameObject.SetActive(false);
                toggleModeText.text = "Don't have an account? <color=#4FC3F7>Register</color>";
            }
            else
            {
                titleText.text = "Create Account";
                loginButton.gameObject.SetActive(false);
                registerButton.gameObject.SetActive(true);
                toggleModeText.text = "Already have an account? <color=#4FC3F7>Login</color>";
            }
            
            // Clear inputs
            ClearInputs();
        }
        
        /// <summary>
        /// Shows loading overlay
        /// </summary>
        private void ShowLoading(string message = "Loading...")
        {
            loadingPanel.SetActive(true);
            loadingText.text = message;
            
            // Disable interaction
            loginButton.interactable = false;
            registerButton.interactable = false;
            googleSignInButton.interactable = false;
            toggleModeButton.interactable = false;
        }
        
        /// <summary>
        /// Hides loading overlay
        /// </summary>
        private void HideLoading()
        {
            loadingPanel.SetActive(false);
            
            // Enable interaction
            loginButton.interactable = true;
            registerButton.interactable = true;
            googleSignInButton.interactable = true;
            toggleModeButton.interactable = true;
        }
        
        /// <summary>
        /// Shows error toast message
        /// </summary>
        private void ShowError(string error)
        {
            errorToastText.text = error;
            errorToast.SetActive(true);
            
            // Auto-hide after 3 seconds
            StartCoroutine(HideErrorToastAfterDelay(3f));
        }
        
        /// <summary>
        /// Hides error toast
        /// </summary>
        private void HideErrorToast()
        {
            errorToast.SetActive(false);
        }
        #endregion

        #region Input Validation
        /// <summary>
        /// Validates email and password inputs
        /// </summary>
        private bool ValidateInputs(string email, string password)
        {
            if (string.IsNullOrEmpty(email))
            {
                ShowError("Please enter your email");
                return false;
            }
            
            if (string.IsNullOrEmpty(password))
            {
                ShowError("Please enter your password");
                return false;
            }
            
            if (!IsValidEmail(email))
            {
                ShowError("Please enter a valid email address");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Validates email format
        /// </summary>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Clears all input fields
        /// </summary>
        private void ClearInputs()
        {
            emailInput.text = "";
            passwordInput.text = "";
            statusText.text = "";
        }
        #endregion

        #region Animations - Mobile Optimized
        /// <summary>
        /// Animates login card sliding in from bottom
        /// </summary>
        private IEnumerator AnimateLoginCardIn()
        {
            Vector2 startPos = loginCard.anchoredPosition;
            startPos.y = -1200f; // Start below screen
            loginCard.anchoredPosition = startPos;
            
            float elapsed = 0f;
            float duration = 0.5f;
            Vector2 targetPos = new Vector2(0, 0);
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                loginCard.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                yield return null;
            }
            
            loginCard.anchoredPosition = targetPos;
        }
        
        /// <summary>
        /// Animates mode change with fade effect
        /// </summary>
        private IEnumerator AnimateModeChange()
        {
            // Fade out
            float elapsed = 0f;
            float duration = 0.2f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                loginCanvasGroup.alpha = 1f - (elapsed / duration);
                yield return null;
            }
            
            // Change mode
            yield return new WaitForSeconds(0.1f);
            
            // Fade in
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                loginCanvasGroup.alpha = elapsed / duration;
                yield return null;
            }
            
            loginCanvasGroup.alpha = 1f;
        }
        
        /// <summary>
        /// Hides error toast after delay
        /// </summary>
        private IEnumerator HideErrorToastAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            HideErrorToast();
        }
        
        /// <summary>
        /// Transitions to game scene after successful login
        /// </summary>
        private IEnumerator TransitionToGameScene()
        {
            yield return new WaitForSeconds(1f);
    
            // Fade out login canvas
            float elapsed = 0f;
            float duration = 0.5f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                loginCanvasGroup.alpha = 1f - (elapsed / duration);
                yield return null;
            }
            
            loginPanel.SetActive(false); // Hide login panel
            
            if (mainMenuPanel != null)
            {
                // mainMenuPanel.SetActive(true); // Show main menu
                SceneManager.LoadScene("Game");
            }
            else
            {
                Debug.LogWarning("Main Menu Panel is not assigned in the Inspector!");
            }
        }
        #endregion
    }
}
