using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Newtonsoft.Json;
using Google;

namespace MysteryRooms.Authentication
{
    /// <summary>
    /// Manages Firebase authentication and backend API communication.
    /// Singleton pattern ensures one instance throughout the game.
    /// </summary>
    public class FirebaseAuthManager : MonoBehaviour
    {
        #region Singleton
        public static FirebaseAuthManager Instance { get; private set; }
        #endregion

        #region Firebase References
        private FirebaseAuth auth;
        private FirebaseUser currentUser;
        private string cachedGoogleEmail;
        #endregion

        #region Backend Configuration
        [Header("Configuration")]
        [SerializeField] private MysteryRooms.Config.BackendConfig backendConfig;

        // And update the BackendURL property to read from the config:
        private string BackendURL => backendConfig != null ? backendConfig.CurrentURL : "http://localhost:8000";


        #region Google Sign-In Configuration
        [Header("Google Sign-In Configuration")]
        [SerializeField] private string webClientId = "YOUR_WEB_CLIENT_ID.apps.googleusercontent.com";
        #endregion
        
        #endregion

        #region Events
        /// <summary>
        /// Fired when authentication succeeds and user profile is fetched from backend
        /// </summary>
        public event Action<UserProfile> OnAuthenticationSuccess;
        
        /// <summary>
        /// Fired when authentication fails with error message
        /// </summary>
        public event Action<string> OnAuthenticationError;
        
        /// <summary>
        /// Fired when logout is successful
        /// </summary>
        public event Action OnLogoutSuccess;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            // Singleton pattern implementation
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeFirebase();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        #endregion

        #region Firebase Initialization
        /// <summary>
        /// Initializes Firebase SDK and checks dependencies
        /// </summary>
        private void InitializeFirebase()
        {
            Debug.Log("🔥 Initializing Firebase...");
            
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                DependencyStatus dependencyStatus = task.Result;
                
                if (dependencyStatus == DependencyStatus.Available)
                {
                    auth = FirebaseAuth.DefaultInstance;
                    Debug.Log("✅ Firebase initialized successfully");
                    
                    // Configure Google Sign-In (ADD THIS)
                    GoogleSignIn.Configuration = new GoogleSignInConfiguration
                    {
                        RequestIdToken = true,
                        RequestEmail = true,  
                        WebClientId = webClientId
                    };
                    Debug.Log("✅ Google Sign-In configured");
                    
                    // Check if user already signed in
                    if (auth.CurrentUser != null)
                    {
                        currentUser = auth.CurrentUser;
                        Debug.Log($"👤 User already signed in: {currentUser.Email}");
                        GetTokenAndVerifyWithBackend();
                    }
                }
                else
                {
                    Debug.LogError($"❌ Firebase dependency error: {dependencyStatus}");
                    OnAuthenticationError?.Invoke($"Firebase initialization failed: {dependencyStatus}");
                }
            });
        }
        #endregion


        #region Email/Password Authentication
        /// <summary>
        /// Registers a new user with email and password
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <param name="password">User's password (min 6 characters)</param>
        public void RegisterUser(string email, string password)
        {
            Debug.Log($"📝 Registering user: {email}");
            
            auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogWarning("⚠️ Registration was canceled");
                    OnAuthenticationError?.Invoke("Registration canceled");
                    return;
                }
                
                if (task.IsFaulted)
                {
                    Debug.LogError($"❌ Registration failed: {task.Exception}");
                    string errorMessage = ParseFirebaseError(task.Exception);
                    OnAuthenticationError?.Invoke(errorMessage);
                    return;
                }
                
                // Success!
                AuthResult result = task.Result;
                currentUser = result.User;
                Debug.Log($"✅ User registered successfully: {currentUser.Email}");
                
                // Verify with backend and create MongoDB profile
                GetTokenAndVerifyWithBackend();
            });
        }
        
        /// <summary>
        /// Logs in an existing user with email and password
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <param name="password">User's password</param>
        public void LoginUser(string email, string password)
        {
            Debug.Log($"🔐 Logging in user: {email}");
            
            auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogWarning("⚠️ Login was canceled");
                    OnAuthenticationError?.Invoke("Login canceled");
                    return;
                }
                
                if (task.IsFaulted)
                {
                    Debug.LogError($"❌ Login failed: {task.Exception}");
                    string errorMessage = ParseFirebaseError(task.Exception);
                    OnAuthenticationError?.Invoke(errorMessage);
                    return;
                }
                
                // Success!
                AuthResult result = task.Result;
                currentUser = result.User;
                Debug.Log($"✅ User logged in successfully: {currentUser.Email}");
                
                // Verify with backend
                GetTokenAndVerifyWithBackend();
            });
        }
        #endregion

        #region Google Sign-In
        /// <summary>
        /// Signs in user with Google - Web-based OAuth flow
        /// Works on ALL platforms including mobile
        /// </summary>
        public void SignInWithGoogle()
        {
            Debug.Log("🔵 Starting Google Sign-In...");
            StartCoroutine(SignInWithGoogleCoroutine());
        }

        private IEnumerator SignInWithGoogleCoroutine()
        {
            // 1. Prompt Google Login
            var signInTask = GoogleSignIn.DefaultInstance.SignIn();
            
            yield return new WaitUntil(() => signInTask.IsCompleted);
            
            if (signInTask.IsFaulted)
            {
                Debug.LogError($"❌ Google Sign-In failed: {signInTask.Exception}");
                OnAuthenticationError?.Invoke("Google Sign-In failed. Please try again.");
                yield break;
            }
            
            if (signInTask.IsCanceled)
            {
                Debug.LogWarning("⚠️ Google Sign-In canceled");
                OnAuthenticationError?.Invoke("Sign-in canceled");
                yield break;
            }
            
            GoogleSignInUser googleUser = signInTask.Result;
            cachedGoogleEmail = googleUser.Email;
            Debug.Log($"✅ Google Sign-In successful: {googleUser.Email}");
            
            // 2. Pass Google ID Token to Firebase
            Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);
            
            var authTask = auth.SignInAndRetrieveDataWithCredentialAsync(credential);
            yield return new WaitUntil(() => authTask.IsCompleted);
            
            if (authTask.IsFaulted || authTask.IsCanceled)
            {
                Debug.LogError($"❌ Firebase authentication failed");
                OnAuthenticationError?.Invoke("Authentication failed");
                yield break;
            }
            
            // 3. Success!
            AuthResult authResult = authTask.Result;
            currentUser = authResult.User;
            
            Debug.Log($"✅ Firebase Auth Success!");
            Debug.Log($"   Email: {currentUser.Email}");
            Debug.Log($"   Display Name: {currentUser.DisplayName}");
            
            // 4. Verify with backend
            GetTokenAndVerifyWithBackend();
        }

        /// <summary>
        /// Signs out from both Firebase and Google
        /// </summary>
        public void Logout()
        {
            if (auth != null)
            {
                auth.SignOut();
            }
            
            if (GoogleSignIn.DefaultInstance != null)
            {
                GoogleSignIn.DefaultInstance.SignOut();
            }
            
            currentUser = null;
            ClearLocalData();
            Debug.Log("✅ User logged out from Firebase and Google");
            OnLogoutSuccess?.Invoke();
        }
        #endregion

        #region Backend Communication
        /// <summary>
        /// Gets Firebase ID token and sends it to backend for verification
        /// Backend creates/updates user profile in MongoDB
        /// </summary>
        private void GetTokenAndVerifyWithBackend()
        {
            if (currentUser == null)
            {
                Debug.LogError("❌ No user logged in");
                OnAuthenticationError?.Invoke("No user logged in");
                return;
            }
            
            Debug.Log("🎫 Getting Firebase ID token...");
            
            currentUser.TokenAsync(false).ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("❌ Failed to get Firebase token");
                    OnAuthenticationError?.Invoke("Failed to get authentication token");
                    return;
                }
                
                string idToken = task.Result;
                Debug.Log($"✅ Got Firebase token (first 30 chars): {idToken.Substring(0, 30)}...");
                
                // Send token to backend
                StartCoroutine(VerifyTokenWithBackend(idToken));
            });
        }
        
        /// <summary>
        /// Sends Firebase token to your backend for verification
        /// Backend validates token and returns user profile with game data
        /// </summary>
        private IEnumerator VerifyTokenWithBackend(string firebaseToken)
        {
            string url = $"{BackendURL}/auth/verify";
            PlayerPrefs.SetString("FirebaseToken", firebaseToken);
            
            // Create request body matching your backend's TokenVerifyRequest model
            var requestBody = new TokenVerifyRequest
            {
                firebase_token = firebaseToken,
                email = !string.IsNullOrEmpty(cachedGoogleEmail) ? cachedGoogleEmail : currentUser?.Email,                 
                display_name = currentUser?.DisplayName   
            };
            
            string json = JsonConvert.SerializeObject(requestBody);
            Debug.Log($"📤 Sending token to backend: {url}");
            
            // Create UnityWebRequest
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                
                // Send request
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"✅ Backend response received");
                    
                    try
                    {
                        // Parse user profile from backend
                        UserProfile profile = JsonConvert.DeserializeObject<UserProfile>(request.downloadHandler.text);
                        
                        // Store locally for offline access
                        SaveUserProfileLocally(profile);

                        // --- NEW: Initialize UserSession ---
                        if (UserSession.Instance == null)
                        {
                            GameObject sessionObj = new GameObject("UserSession");
                            sessionObj.AddComponent<UserSession>();
                        }

                        // Determine display name (fallback to email prefix if null)
                        string displayName = !string.IsNullOrEmpty(profile.display_name) 
                            ? profile.display_name 
                            : profile.email.Split('@')[0];

                        // Store data in singleton so the Game scene can use it
                        UserSession.Instance.SetUserData(
                            userId: profile.firebase_uid, 
                            username: displayName, 
                            email: profile.email, 
                            token: firebaseToken
                        );
                        // ------------------------------------

                        // Notify listeners
                        OnAuthenticationSuccess?.Invoke(profile);
                        
                        Debug.Log($"🎮 User Profile Loaded:");
                        Debug.Log($"   Email: {profile.email}");
                        Debug.Log($"   Coins: {profile.coins}");
                        Debug.Log($"   Games Played: {profile.games_played}");
                        Debug.Log($"   Games Won: {profile.games_won}");

                        // --- NEW: Load Game Scene ---
                        Debug.Log("🚀 Loading Game Scene...");
                        UnityEngine.SceneManagement.SceneManager.LoadScene("Game"); // Ensure "Game" is in your Build Settings!
                    
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"❌ Failed to parse backend response: {e.Message}");
                        OnAuthenticationError?.Invoke("Failed to load user profile");
                    }
                }
                else
                {
                    Debug.LogError($"❌ Backend error: {request.error}");
                    Debug.LogError($"   Response: {request.downloadHandler.text}");
                    OnAuthenticationError?.Invoke($"Backend error: {request.error}");
                }
            }
        }
        
        /// <summary>
        /// Gets updated user profile from backend
        /// Use this after gameplay to refresh coins, stats, etc.
        /// </summary>
        public IEnumerator GetUserProfile(Action<UserProfile> onSuccess, Action<string> onError)
        {
            if (currentUser == null)
            {
                onError?.Invoke("No user logged in");
                yield break;
            }
            
            // Get fresh token
            var tokenTask = currentUser.TokenAsync(false);
            yield return new WaitUntil(() => tokenTask.IsCompleted);
            
            if (tokenTask.IsFaulted || tokenTask.IsCanceled)
            {
                onError?.Invoke("Failed to get authentication token");
                yield break;
            }
            
            string idToken = tokenTask.Result;
            string url = $"{BackendURL}/auth/profile";
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", $"Bearer {idToken}");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    UserProfile profile = JsonConvert.DeserializeObject<UserProfile>(request.downloadHandler.text);
                    SaveUserProfileLocally(profile);
                    onSuccess?.Invoke(profile);
                }
                else
                {
                    onError?.Invoke(request.error);
                }
            }
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Checks if a user is currently logged in
        /// </summary>
        public bool IsUserLoggedIn()
        {
            return currentUser != null;
        }
        
        /// <summary>
        /// Gets the current user's email
        /// </summary>
        public string GetUserEmail()
        {
            return currentUser?.Email ?? "Not logged in";
        }
        
        /// <summary>
        /// Gets the current user's Firebase UID
        /// </summary>
        public string GetUserUID()
        {
            return currentUser?.UserId ?? "";
        }
        
        /// <summary>
        /// Parses Firebase exceptions into user-friendly error messages
        /// </summary>
        private string ParseFirebaseError(AggregateException exception)
        {
            if (exception.InnerException is FirebaseException firebaseEx)
            {
                switch (firebaseEx.ErrorCode)
                {
                    case (int)AuthError.EmailAlreadyInUse:
                        return "This email is already registered";
                    case (int)AuthError.InvalidEmail:
                        return "Invalid email address";
                    case (int)AuthError.WeakPassword:
                        return "Password is too weak (minimum 6 characters)";
                    case (int)AuthError.WrongPassword:
                        return "Incorrect password";
                    case (int)AuthError.UserNotFound:
                        return "No account found with this email";
                    default:
                        return $"Authentication error: {firebaseEx.Message}";
                }
            }
            
            return exception.Message;
        }
        
        /// <summary>
        /// Saves user profile to PlayerPrefs for offline access
        /// </summary>
        private void SaveUserProfileLocally(UserProfile profile)
        {
            PlayerPrefs.SetString("user_email", profile.email);
            PlayerPrefs.SetString("user_uid", profile.firebase_uid);
            PlayerPrefs.SetInt("user_coins", profile.coins);
            PlayerPrefs.SetInt("user_games_played", profile.games_played);
            PlayerPrefs.SetInt("user_games_won", profile.games_won);
            PlayerPrefs.SetInt("user_total_score", profile.total_score);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Clears all local user data
        /// </summary>
        private void ClearLocalData()
        {
            PlayerPrefs.DeleteKey("user_email");
            PlayerPrefs.DeleteKey("user_uid");
            PlayerPrefs.DeleteKey("user_coins");
            PlayerPrefs.DeleteKey("user_games_played");
            PlayerPrefs.DeleteKey("user_games_won");
            PlayerPrefs.DeleteKey("user_total_score");
            PlayerPrefs.Save();
        }
        #endregion
    }

    #region Data Models
    /// <summary>
    /// Request model for backend token verification
    /// Matches your backend's TokenVerifyRequest in auth_routes.py
    /// </summary>
    [Serializable]
    public class TokenVerifyRequest
    {
        public string firebase_token;
        public string email;                   
        public string display_name; 
    }

    /// <summary>
    /// User profile model matching your backend's UserResponse
    /// Includes game-specific data from MongoDB
    /// </summary>
    [Serializable]
    public class UserProfile
    {
        public string firebase_uid;
        public string email;
        public string display_name;
        public int coins;
        public int games_played;
        public int games_won;
        public int total_score;
    }
    #endregion
}
