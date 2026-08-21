using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using MysteryRooms.Authentication; // ADDED THIS

namespace MysteryRooms.Editor
{
    /// <summary>
    /// Editor tool to automatically generate professional login UI
    /// Optimized for mobile with 1920x1080 reference resolution
    /// Menu: Tools → MysteryRooms → Generate Login UI
    /// </summary>
    public class LoginUIGenerator : EditorWindow
    {
        [MenuItem("Tools/MysteryRooms/Generate Login UI")]
        public static void ShowWindow()
        {
            GetWindow<LoginUIGenerator>("Login UI Generator");
        }

        void OnGUI()
        {
            GUILayout.Label("MysteryRooms Login UI Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            GUILayout.Label("This will create a complete login screen optimized for mobile devices.");
            GUILayout.Label("Reference Resolution: 1920 x 1080");
            GUILayout.Space(10);
            
            if (GUILayout.Button("Generate Login UI", GUILayout.Height(40)))
            {
                GenerateLoginUI();
            }
            
            GUILayout.Space(10);
            GUILayout.Label("✓ Includes Email/Password login");
            GUILayout.Label("✓ Includes Google Sign-In button");
            GUILayout.Label("✓ Mobile-responsive design");
            GUILayout.Label("✓ Smooth animations");
            GUILayout.Label("✓ All scripts auto-configured");
        }

        private static void GenerateLoginUI()
        {
            // Create new scene or use current
            bool createNewScene = EditorUtility.DisplayDialog(
                "Create New Scene?",
                "Do you want to create a new scene for the login UI?",
                "Yes, New Scene",
                "No, Use Current"
            );

            if (createNewScene)
            {
                EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            }

            // Generate UI hierarchy
            GameObject canvas = CreateCanvas();
            GameObject eventSystem = CreateEventSystem();
            
            // Create UI elements
            CreateBackground(canvas);
            GameObject loginPanel = CreateLoginPanel(canvas);
            GameObject mainMenuPanel = CreateMainMenuPanel(canvas); // ADDED
            GameObject loadingPanel = CreateLoadingPanel(canvas);

            // ADDED
            GameObject firebaseManager = CreateFirebaseAuthManager();
            WireUpUIReferences(loginPanel, canvas, loadingPanel, mainMenuPanel);
            
            // Save as prefab
            string prefabPath = "Assets/Prefabs/LoginUI.prefab";
            System.IO.Directory.CreateDirectory("Assets/Prefabs");
            PrefabUtility.SaveAsPrefabAsset(canvas, prefabPath);
            
            // Select the canvas
            Selection.activeGameObject = canvas;
            
            EditorUtility.DisplayDialog(
                "Success!",
                "Login UI has been generated successfully!\n\n" +
                "✓ Canvas configured for mobile (1920x1080)\n" +
                "✓ All UI elements created\n" +
                "✓ Scripts attached and configured\n" +
                "✓ Saved as prefab: " + prefabPath,
                "OK"
            );
        }

        #region Canvas Creation
        private static GameObject CreateCanvas()
        {
            GameObject canvasGO = new GameObject("LoginCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); // Mobile reference
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f; // Balance width/height
            scaler.referencePixelsPerUnit = 100;
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            return canvasGO;
        }

        private static GameObject CreateEventSystem()
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            
            return eventSystemGO;
        }
        #endregion

        #region Background
        private static void CreateBackground(GameObject parent)
        {
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(parent.transform, false);
            
            RectTransform rt = bg.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            
            Image img = bg.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.2f); // Dark blue-gray
            
            // Create gradient overlay
            GameObject gradient = new GameObject("GradientOverlay");
            gradient.transform.SetParent(bg.transform, false);
            
            RectTransform gradientRT = gradient.AddComponent<RectTransform>();
            gradientRT.anchorMin = Vector2.zero;
            gradientRT.anchorMax = Vector2.one;
            gradientRT.sizeDelta = Vector2.zero;
            
            Image gradientImg = gradient.AddComponent<Image>();
            // Create gradient texture
            Texture2D gradientTex = CreateGradientTexture();
            gradientImg.sprite = Sprite.Create(
                gradientTex,
                new Rect(0, 0, gradientTex.width, gradientTex.height),
                new Vector2(0.5f, 0.5f)
            );
        }

        private static Texture2D CreateGradientTexture()
        {
            int width = 256;
            int height = 256;
            Texture2D tex = new Texture2D(width, height);
            
            for (int y = 0; y < height; y++)
            {
                float t = (float)y / height;
                Color color = Color.Lerp(
                    new Color(0.3f, 0.2f, 0.5f, 0.5f), // Purple
                    new Color(0.1f, 0.3f, 0.6f, 0.5f), // Blue
                    t
                );
                
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, color);
                }
            }
            
            tex.Apply();
            return tex;
        }
        #endregion

        #region Login Panel
        private static GameObject CreateLoginPanel(GameObject parent)
        {
            // Main panel container
            GameObject panel = new GameObject("LoginPanel");
            panel.transform.SetParent(parent.transform, false);
            
            RectTransform panelRT = panel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.sizeDelta = new Vector2(700, 1000); // Mobile-optimized size
            panelRT.anchoredPosition = Vector2.zero;
            
            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(1, 1, 1, 0.95f); // Semi-transparent white
            
            // Add shadow effect
            Shadow shadow = panel.AddComponent<Shadow>();
            shadow.effectDistance = new Vector2(5, -5);
            shadow.effectColor = new Color(0, 0, 0, 0.3f);
            
            // Add CanvasGroup for animations
            panel.AddComponent<CanvasGroup>();
            
            // Create panel contents
            CreateTitle(panel);
            CreateEmailInput(panel);
            CreatePasswordInput(panel);
            CreateActionButtons(panel);
            CreateGoogleButton(panel);
            CreateToggleModeButton(panel);
            CreateStatusText(panel);
            CreateErrorToast(parent); // Toast is on canvas, not panel
            
            // Attach controller script
            var controller = panel.AddComponent<MysteryRooms.UI.LoginUIController>();
            
            return panel;
        }

        private static void CreateTitle(GameObject parent)
        {
            GameObject title = new GameObject("Title");
            title.transform.SetParent(parent.transform, false);
            
            RectTransform rt = title.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(600, 120);
            rt.anchoredPosition = new Vector2(0, -100);
            
            TextMeshProUGUI text = title.AddComponent<TextMeshProUGUI>();
            text.text = "Welcome Back";
            text.fontSize = 72;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(0.2f, 0.2f, 0.3f);
            text.alignment = TextAlignmentOptions.Center;
        }

        private static void CreateEmailInput(GameObject parent)
        {
            GameObject emailField = CreateInputField(parent, "EmailInput", "Email", -250);
            // Add email icon placeholder
            CreateInputIcon(emailField, "📧");
        }

        private static void CreatePasswordInput(GameObject parent)
        {
            GameObject passwordField = CreateInputField(parent, "PasswordInput", "Password", -400);
            passwordField.GetComponent<TMP_InputField>().contentType = TMP_InputField.ContentType.Password;
            
            // Add password icon
            CreateInputIcon(passwordField, "🔒");
            
            // Add visibility toggle button
            CreatePasswordToggle(passwordField);
        }

        private static GameObject CreateInputField(GameObject parent, string name, string placeholder, float yPos)
        {
            GameObject inputGO = new GameObject(name);
            inputGO.transform.SetParent(parent.transform, false);
            
            RectTransform rt = inputGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(600, 100); // Large for mobile
            rt.anchoredPosition = new Vector2(0, yPos);
            
            Image img = inputGO.AddComponent<Image>();
            img.color = new Color(0.95f, 0.95f, 0.95f);
            
            TMP_InputField inputField = inputGO.AddComponent<TMP_InputField>();
            
            // Create text area
            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(inputGO.transform, false);
            RectTransform textAreaRT = textArea.AddComponent<RectTransform>();
            textAreaRT.anchorMin = Vector2.zero;
            textAreaRT.anchorMax = Vector2.one;
            textAreaRT.sizeDelta = new Vector2(-20, -20);
            textAreaRT.anchoredPosition = Vector2.zero;
            
            // Create placeholder
            GameObject placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(textArea.transform, false);
            RectTransform placeholderRT = placeholderGO.AddComponent<RectTransform>();
            placeholderRT.anchorMin = Vector2.zero;
            placeholderRT.anchorMax = Vector2.one;
            placeholderRT.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI placeholderText = placeholderGO.AddComponent<TextMeshProUGUI>();
            placeholderText.text = placeholder;
            placeholderText.fontSize = 40;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f);
            placeholderText.alignment = TextAlignmentOptions.Left;
            
            // Create input text
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(textArea.transform, false);
            RectTransform textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
            text.fontSize = 40;
            text.color = Color.black;
            text.alignment = TextAlignmentOptions.Left;
            
            inputField.textViewport = textAreaRT;
            inputField.textComponent = text;
            inputField.placeholder = placeholderText;
            
            return inputGO;
        }

        private static void CreateInputIcon(GameObject parent, string icon)
        {
            GameObject iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(parent.transform, false);
            
            RectTransform rt = iconGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(0, 0.5f);
            rt.sizeDelta = new Vector2(60, 60);
            rt.anchoredPosition = new Vector2(40, 0);
            
            TextMeshProUGUI text = iconGO.AddComponent<TextMeshProUGUI>();
            text.text = icon;
            text.fontSize = 40;
            text.alignment = TextAlignmentOptions.Center;
        }

        private static void CreatePasswordToggle(GameObject parent)
        {
            GameObject toggleGO = CreateButton(parent, "ToggleVisibility", "👁", 60, 60);
            RectTransform rt = toggleGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.anchoredPosition = new Vector2(-40, 0);
        }

        private static void CreateActionButtons(GameObject parent)
        {
            // Login Button
            GameObject loginBtn = CreateButton(parent, "LoginButton", "LOGIN", 600, 100);
            RectTransform loginRT = loginBtn.GetComponent<RectTransform>();
            loginRT.anchorMin = new Vector2(0.5f, 1f);
            loginRT.anchorMax = new Vector2(0.5f, 1f);
            loginRT.anchoredPosition = new Vector2(0, -550);
            
            Image loginImg = loginBtn.GetComponent<Image>();
            loginImg.color = new Color(0.2f, 0.6f, 1f); // Blue
            
            // Register Button (hidden by default, controller manages visibility)
            GameObject registerBtn = CreateButton(parent, "RegisterButton", "REGISTER", 600, 100);
            RectTransform registerRT = registerBtn.GetComponent<RectTransform>();
            registerRT.anchorMin = new Vector2(0.5f, 1f);
            registerRT.anchorMax = new Vector2(0.5f, 1f);
            registerRT.anchoredPosition = new Vector2(0, -550);
            registerBtn.SetActive(false);
            
            Image registerImg = registerBtn.GetComponent<Image>();
            registerImg.color = new Color(0.3f, 0.7f, 0.3f); // Green
        }

        private static void CreateGoogleButton(GameObject parent)
        {
            GameObject googleBtn = CreateButton(parent, "GoogleSignInButton", "🔵 Sign in with Google", 600, 100);
            RectTransform rt = googleBtn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -700);
            
            Image img = googleBtn.GetComponent<Image>();
            img.color = Color.white;
            
            // Change text color to black for white button
            TextMeshProUGUI text = googleBtn.GetComponentInChildren<TextMeshProUGUI>();
            text.color = Color.black;
        }

        private static void CreateToggleModeButton(GameObject parent)
        {
            GameObject toggleBtn = CreateButton(parent, "ToggleModeButton", "Don't have an account? Register", 600, 80);
            RectTransform rt = toggleBtn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -850);
            
            Image img = toggleBtn.GetComponent<Image>();
            img.color = new Color(1, 1, 1, 0); // Transparent
            
            TextMeshProUGUI text = toggleBtn.GetComponentInChildren<TextMeshProUGUI>();
            text.fontSize = 32;
            text.color = new Color(0.3f, 0.3f, 0.3f);
        }

        private static void CreateStatusText(GameObject parent)
        {
            GameObject statusGO = new GameObject("StatusText");
            statusGO.transform.SetParent(parent.transform, false);
            
            RectTransform rt = statusGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(600, 60);
            rt.anchoredPosition = new Vector2(0, 50);
            
            TextMeshProUGUI text = statusGO.AddComponent<TextMeshProUGUI>();
            text.fontSize = 32;
            text.color = Color.red;
            text.alignment = TextAlignmentOptions.Center;
            text.text = "";
        }

        private static GameObject CreateButton(GameObject parent, string name, string label, float width, float height)
        {
            GameObject btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent.transform, false);
            
            RectTransform rt = btnGO.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);
            
            Image img = btnGO.AddComponent<Image>();
            img.color = Color.white;
            
            Button btn = btnGO.AddComponent<Button>();
            
            // Create text
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            
            RectTransform textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 44;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            
            return btnGO;
        }
        #endregion

        #region Main Menu Panel (ADDED)
        private static GameObject CreateMainMenuPanel(GameObject parent)
        {
            GameObject panel = new GameObject("MainMenuPanel");
            panel.transform.SetParent(parent.transform, false);
            
            RectTransform panelRT = panel.AddComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.sizeDelta = Vector2.zero;
            panelRT.anchoredPosition = Vector2.zero;
            
            // Add a title to show it works
            GameObject title = new GameObject("MainMenuTitle");
            title.transform.SetParent(panel.transform, false);
            
            RectTransform titleRT = title.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 0.5f);
            titleRT.anchorMax = new Vector2(0.5f, 0.5f);
            titleRT.sizeDelta = new Vector2(800, 200);
            titleRT.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI text = title.AddComponent<TextMeshProUGUI>();
            text.text = "MAIN MENU\n<size=40>Successfully Logged In!</size>";
            text.fontSize = 72;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;

            // Hide it by default
            panel.SetActive(false);
            
            return panel;
        }
        #endregion

        #region Loading Panel
        private static GameObject CreateLoadingPanel(GameObject parent)
        {
            GameObject loadingGO = new GameObject("LoadingPanel");
            loadingGO.transform.SetParent(parent.transform, false);
            
            RectTransform rt = loadingGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            
            Image img = loadingGO.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.7f);
            
            // Loading text
            GameObject textGO = new GameObject("LoadingText");
            textGO.transform.SetParent(loadingGO.transform, false);
            
            RectTransform textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0.5f, 0.5f);
            textRT.anchorMax = new Vector2(0.5f, 0.5f);
            textRT.sizeDelta = new Vector2(400, 100);
            
            TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "Loading...";
            text.fontSize = 60;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            
            loadingGO.SetActive(false);
            
            return loadingGO;
        }

        private static void CreateErrorToast(GameObject parent)
        {
            GameObject toastGO = new GameObject("ErrorToast");
            toastGO.transform.SetParent(parent.transform, false);
            
            RectTransform rt = toastGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(800, 120);
            rt.anchoredPosition = new Vector2(0, -100);
            
            Image img = toastGO.AddComponent<Image>();
            img.color = new Color(1f, 0.3f, 0.3f);
            
            // Error text
            GameObject textGO = new GameObject("ErrorText");
            textGO.transform.SetParent(toastGO.transform, false);
            
            RectTransform textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = new Vector2(-40, -40);
            
            TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "Error message";
            text.fontSize = 36;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            
            toastGO.SetActive(false);
        }
        #endregion

        #region Firebase & UI Wiring (ADDED)
        /// <summary>
        /// Creates FirebaseAuthManager GameObject in the scene
        /// This handles all authentication with Firebase
        /// </summary>
        private static GameObject CreateFirebaseAuthManager()
        {
            GameObject authManager = new GameObject("FirebaseAuthManager");
            authManager.AddComponent<MysteryRooms.Authentication.FirebaseAuthManager>();
            Debug.Log("✅ FirebaseAuthManager created");
            return authManager;
        }

        /// <summary>
        /// Automatically wires up all UI references to LoginUIController
        /// No manual Inspector setup needed!
        /// </summary>
        private static void WireUpUIReferences(GameObject loginPanel, GameObject canvas, GameObject loadingPanel, GameObject mainMenuPanel)
        {
            var controller = loginPanel.GetComponent<MysteryRooms.UI.LoginUIController>();
            
            // Use SerializedObject to set private fields
            SerializedObject so = new SerializedObject(controller);
            
            // Panels
            so.FindProperty("loginPanel").objectReferenceValue = loginPanel;
            so.FindProperty("loadingPanel").objectReferenceValue = loadingPanel;
            so.FindProperty("mainMenuPanel").objectReferenceValue = mainMenuPanel; // ADDED
            so.FindProperty("loginCanvasGroup").objectReferenceValue = loginPanel.GetComponent<CanvasGroup>();
            
            // Input Fields
            so.FindProperty("emailInput").objectReferenceValue = loginPanel.transform.Find("EmailInput").GetComponent<TMP_InputField>();
            so.FindProperty("passwordInput").objectReferenceValue = loginPanel.transform.Find("PasswordInput").GetComponent<TMP_InputField>();
            so.FindProperty("togglePasswordVisibility").objectReferenceValue = loginPanel.transform.Find("PasswordInput/ToggleVisibility").GetComponent<Button>();
            so.FindProperty("passwordVisibilityIcon").objectReferenceValue = loginPanel.transform.Find("PasswordInput/ToggleVisibility").GetComponent<Image>();
            
            // Buttons
            so.FindProperty("loginButton").objectReferenceValue = loginPanel.transform.Find("LoginButton").GetComponent<Button>();
            so.FindProperty("registerButton").objectReferenceValue = loginPanel.transform.Find("RegisterButton").GetComponent<Button>();
            so.FindProperty("googleSignInButton").objectReferenceValue = loginPanel.transform.Find("GoogleSignInButton").GetComponent<Button>();
            so.FindProperty("toggleModeButton").objectReferenceValue = loginPanel.transform.Find("ToggleModeButton").GetComponent<Button>();
            
            // Text Elements
            so.FindProperty("titleText").objectReferenceValue = loginPanel.transform.Find("Title").GetComponent<TMP_Text>();
            so.FindProperty("statusText").objectReferenceValue = loginPanel.transform.Find("StatusText").GetComponent<TMP_Text>();
            so.FindProperty("toggleModeText").objectReferenceValue = loginPanel.transform.Find("ToggleModeButton/Text").GetComponent<TMP_Text>();
            so.FindProperty("loadingText").objectReferenceValue = loadingPanel.transform.Find("LoadingText").GetComponent<TMP_Text>();
            
            // Visual Elements
            so.FindProperty("backgroundGradient").objectReferenceValue = canvas.transform.Find("Background/GradientOverlay").GetComponent<Image>();
            so.FindProperty("loginCard").objectReferenceValue = loginPanel.GetComponent<RectTransform>();
            so.FindProperty("errorToast").objectReferenceValue = canvas.transform.Find("ErrorToast").gameObject;
            so.FindProperty("errorToastText").objectReferenceValue = canvas.transform.Find("ErrorToast/ErrorText").GetComponent<TMP_Text>();
            
            so.ApplyModifiedProperties();
            Debug.Log("✅ All UI references wired up automatically!");
        }
        #endregion
    }
}
