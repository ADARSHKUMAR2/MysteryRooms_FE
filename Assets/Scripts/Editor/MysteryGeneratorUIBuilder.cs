#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using MysteryRooms.UI;

namespace MysteryRooms.Editor
{
    public class MysteryGeneratorUIBuilder : EditorWindow
    {
        private string panelName = "MysteryGeneratorPanel";
        private Vector2 scrollPosition;
        
        [MenuItem("Mystery Rooms/Create Mystery Generator UI")]
        public static void ShowWindow()
        {
            MysteryGeneratorUIBuilder window = GetWindow<MysteryGeneratorUIBuilder>("UI Builder");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Mystery Generator UI Builder", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This tool will automatically create the complete Mystery Generator UI with all components, animations, and references configured.", MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // Configuration
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            panelName = EditorGUILayout.TextField("Panel Name:", panelName);
            
            EditorGUILayout.Space(20);
            
            // Main Build Button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Generate Complete UI", GUILayout.Height(50)))
            {
                GenerateCompleteUI();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(20);
            
            // Individual Component Buttons
            EditorGUILayout.LabelField("Individual Components", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Create Canvas Only", GUILayout.Height(30)))
            {
                CreateCanvas();
            }
            
            if (GUILayout.Button("Add Components to Selected Panel", GUILayout.Height(30)))
            {
                if (Selection.activeGameObject != null)
                {
                    CreateUIElements(Selection.activeGameObject);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Please select a GameObject first!", "OK");
                }
            }
            
            EditorGUILayout.Space(20);
            
            // Utilities
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Import TextMeshPro Essentials", GUILayout.Height(30)))
            {
                ImportTMPEssentials();
            }
            
            if (GUILayout.Button("Setup Default Canvas Settings", GUILayout.Height(30)))
            {
                SetupCanvasSettings();
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void GenerateCompleteUI()
        {
            if (EditorUtility.DisplayDialog("Generate UI", 
                "This will create a new Mystery Generator UI in your scene. Continue?", 
                "Yes", "Cancel"))
            {
                GameObject canvas = CreateCanvas();
                GameObject panel = CreateMainPanel(canvas);
                CreateUIElements(panel);
                ConfigureReferences(panel);
                
                Selection.activeGameObject = panel;
                EditorGUIUtility.PingObject(panel);
                
                Debug.Log("✅ Mystery Generator UI created successfully!");
                EditorUtility.DisplayDialog("Success", 
                    "Mystery Generator UI has been created!\n\nSelect the panel to see the custom inspector.", 
                    "OK");
            }
        }

        private GameObject CreateCanvas()
        {
            // Check if Canvas already exists
            Canvas existingCanvas = FindObjectOfType<Canvas>();
            if (existingCanvas != null)
            {
                Debug.Log("Using existing Canvas");
                return existingCanvas.gameObject;
            }

            // Create Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
            canvasObj.AddComponent<ResponsiveLayout>();

            // Create EventSystem if it doesn't exist
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
            Debug.Log("✅ Canvas created");
            
            return canvasObj;
        }

        private GameObject CreateMainPanel(GameObject canvas)
        {
            GameObject panelObj = new GameObject(panelName);
            panelObj.transform.SetParent(canvas.transform, false);
            
            RectTransform rectTransform = panelObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            
            CanvasGroup canvasGroup = panelObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            
            MysteryGeneratorPanel panelScript = panelObj.AddComponent<MysteryGeneratorPanel>();
            
            Undo.RegisterCreatedObjectUndo(panelObj, "Create Main Panel");
            Debug.Log("✅ Main Panel created");
            
            return panelObj;
        }

        private void CreateUIElements(GameObject panel)
        {
            // 1. Background
            GameObject background = CreateBackground(panel);
            
            // 2. Title
            GameObject title = CreateTitle(panel);
            
            // 3. Difficulty Section
            GameObject difficultySection = CreateDifficultySection(panel);
            
            // 4. Generate Button
            GameObject generateButton = CreateButton(panel, "GenerateButton", "GENERATE MYSTERY", new Vector2(0.5f, 0.45f), new Vector2(400, 80));
            
            // 5. Loading Spinner
            GameObject loadingSpinner = CreateLoadingSpinner(panel);
            
            // 6. Mystery Display
            GameObject mysteryDisplay = CreateMysteryDisplay(panel);
            
            // 7. Action Buttons
            GameObject openButton = CreateButton(panel, "OpenMysteryButton", "OPEN MYSTERY", new Vector2(0.5f, 0.25f), new Vector2(400, 70));
            GameObject startButton = CreateButton(panel, "StartButton", "START", new Vector2(0.5f, 0.15f), new Vector2(300, 70));
            GameObject backButton = CreateButton(panel, "BackButton", "BACK", new Vector2(0.9f, 0.95f), new Vector2(200, 60));
            
            // 8. Particle Manager
            GameObject particleManager = CreateParticleManager(panel);
            
            Debug.Log("✅ All UI elements created");
        }

        private GameObject CreateBackground(GameObject parent)
        {
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(parent.transform, false);
            
            RectTransform rect = bgObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            
            Image image = bgObj.AddComponent<Image>();
            image.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);
            
            // Add subtle gradient
            GameObject overlay = new GameObject("Overlay");
            overlay.transform.SetParent(bgObj.transform, false);
            RectTransform overlayRect = overlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            
            Image overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0.8f, 0.6f, 0.3f, 0.1f);
            
            Undo.RegisterCreatedObjectUndo(bgObj, "Create Background");
            return bgObj;
        }

        private GameObject CreateTitle(GameObject parent)
        {
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(parent.transform, false);
            
            RectTransform rect = titleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.9f);
            rect.anchorMax = new Vector2(0.5f, 0.9f);
            rect.sizeDelta = new Vector2(800, 100);
            rect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI text = titleObj.AddComponent<TextMeshProUGUI>();
            text.text = "Mystery Generator";
            text.fontSize = 64;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.85f, 0.4f);
            
            // Add outline
            text.outlineWidth = 0.2f;
            text.outlineColor = new Color(0.2f, 0.15f, 0.05f, 1f);
            
            Undo.RegisterCreatedObjectUndo(titleObj, "Create Title");
            return titleObj;
        }

        private GameObject CreateDifficultySection(GameObject parent)
        {
            GameObject sectionObj = new GameObject("DifficultySection");
            sectionObj.transform.SetParent(parent.transform, false);
            
            RectTransform rect = sectionObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.7f);
            rect.anchorMax = new Vector2(0.5f, 0.7f);
            rect.sizeDelta = new Vector2(600, 120);
            rect.anchoredPosition = Vector2.zero;
            
            DifficultySelector selector = sectionObj.AddComponent<DifficultySelector>();
            
            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(sectionObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.6f);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = "Difficulty:";
            labelText.fontSize = 32;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = new Color(0.9f, 0.9f, 0.9f);
            
            // Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(sectionObj.transform, false);
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.1f, 0.1f);
            sliderRect.anchorMax = new Vector2(0.7f, 0.5f);
            sliderRect.sizeDelta = Vector2.zero;
            
            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 1;
            slider.maxValue = 10;
            slider.wholeNumbers = true;
            slider.value = 1;
            
            // Slider Background
            GameObject bgArea = new GameObject("Background");
            bgArea.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRect = bgArea.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            Image bgImage = bgArea.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = new Vector2(-20, 0);
            
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.8f, 0.6f, 0.3f);
            fillImage.type = Image.Type.Filled;
            
            slider.fillRect = fillRect;
            
            // Handle Area
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = new Vector2(-20, 0);
            
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(30, 30);
            
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(1f, 0.8f, 0.4f);
            
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            
            // Value Display
            GameObject valueObj = new GameObject("ValueText");
            valueObj.transform.SetParent(sectionObj.transform, false);
            RectTransform valueRect = valueObj.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.75f, 0.1f);
            valueRect.anchorMax = new Vector2(0.95f, 0.5f);
            valueRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
            valueText.text = "1";
            valueText.fontSize = 48;
            valueText.fontStyle = FontStyles.Bold;
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.color = new Color(0.8f, 0.6f, 0.3f);
            
            Undo.RegisterCreatedObjectUndo(sectionObj, "Create Difficulty Section");
            return sectionObj;
        }

        private GameObject CreateButton(GameObject parent, string name, string buttonText, Vector2 anchorPosition, Vector2 size)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent.transform, false);
            
            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorPosition;
            rect.anchorMax = anchorPosition;
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            
            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.8f, 0.6f, 0.3f);
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            
            Button button = buttonObj.AddComponent<Button>();
            AnimatedButton animButton = buttonObj.AddComponent<AnimatedButton>();
            
            // Button Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = buttonText;
            text.fontSize = 28;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.1f, 0.05f, 0.02f);
            
            button.targetGraphic = image;
            
            Undo.RegisterCreatedObjectUndo(buttonObj, $"Create {name}");
            return buttonObj;
        }

        private GameObject CreateLoadingSpinner(GameObject parent)
        {
            GameObject spinnerObj = new GameObject("LoadingSpinner");
            spinnerObj.transform.SetParent(parent.transform, false);
            
            RectTransform rect = spinnerObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(100, 100);
            rect.anchoredPosition = Vector2.zero;
            
            Image image = spinnerObj.AddComponent<Image>();
            image.color = new Color(1f, 0.8f, 0.4f);
            
            // Add rotation animation
            spinnerObj.AddComponent<LoadingSpinnerRotation>();
            
            spinnerObj.SetActive(false);
            
            Undo.RegisterCreatedObjectUndo(spinnerObj, "Create Loading Spinner");
            return spinnerObj;
        }

        private GameObject CreateMysteryDisplay(GameObject parent)
        {
            GameObject displayObj = new GameObject("MysteryDisplay");
            displayObj.transform.SetParent(parent.transform, false);
            
            RectTransform rect = displayObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(700, 200);
            rect.anchoredPosition = new Vector2(0, 50);
            
            // Mystery Card
            GameObject cardObj = new GameObject("MysteryCard");
            cardObj.transform.SetParent(displayObj.transform, false);
            
            RectTransform cardRect = cardObj.AddComponent<RectTransform>();
            cardRect.anchorMin = Vector2.zero;
            cardRect.anchorMax = Vector2.one;
            cardRect.sizeDelta = Vector2.zero;
            
            CanvasGroup cardGroup = cardObj.AddComponent<CanvasGroup>();
            cardGroup.alpha = 0f;
            
            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.color = new Color(0.15f, 0.1f, 0.05f, 0.95f);
            cardBg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            cardBg.type = Image.Type.Sliced;
            
            MysteryCard mysteryCard = cardObj.AddComponent<MysteryCard>();
            
            // Card Border
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(cardObj.transform, false);
            RectTransform borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = new Vector2(10, 10);
            
            Image borderImage = borderObj.AddComponent<Image>();
            borderImage.color = new Color(0.8f, 0.6f, 0.3f);
            borderImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            borderImage.type = Image.Type.Sliced;
            
            // Title Text
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(cardObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.7f);
            titleRect.anchorMax = new Vector2(0.95f, 0.95f);
            titleRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Mystery Title";
            titleText.fontSize = 32;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(1f, 0.85f, 0.4f);
            
            // Description Text
            GameObject descObj = new GameObject("DescriptionText");
            descObj.transform.SetParent(cardObj.transform, false);
            RectTransform descRect = descObj.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.05f, 0.15f);
            descRect.anchorMax = new Vector2(0.95f, 0.65f);
            descRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI descText = descObj.AddComponent<TextMeshProUGUI>();
            descText.text = "Mystery description will appear here...";
            descText.fontSize = 20;
            descText.alignment = TextAlignmentOptions.TopLeft;
            descText.color = new Color(0.9f, 0.9f, 0.9f);
            descText.enableWordWrapping = true;
            
            // Difficulty Badge
            GameObject badgeObj = new GameObject("DifficultyBadge");
            badgeObj.transform.SetParent(cardObj.transform, false);
            RectTransform badgeRect = badgeObj.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.85f, 0.05f);
            badgeRect.anchorMax = new Vector2(0.95f, 0.15f);
            badgeRect.sizeDelta = Vector2.zero;
            
            Image badgeImage = badgeObj.AddComponent<Image>();
            badgeImage.color = new Color(0.8f, 0.6f, 0.3f);
            
            GameObject badgeTextObj = new GameObject("Text");
            badgeTextObj.transform.SetParent(badgeObj.transform, false);
            RectTransform badgeTextRect = badgeTextObj.AddComponent<RectTransform>();
            badgeTextRect.anchorMin = Vector2.zero;
            badgeTextRect.anchorMax = Vector2.one;
            badgeTextRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI badgeText = badgeTextObj.AddComponent<TextMeshProUGUI>();
            badgeText.text = "1";
            badgeText.fontSize = 24;
            badgeText.fontStyle = FontStyles.Bold;
            badgeText.alignment = TextAlignmentOptions.Center;
            badgeText.color = Color.white;
            
            Undo.RegisterCreatedObjectUndo(displayObj, "Create Mystery Display");
            return displayObj;
        }

        private GameObject CreateParticleManager(GameObject parent)
        {
            GameObject managerObj = new GameObject("ParticleEffectManager");
            managerObj.transform.SetParent(parent.transform, false);
            
            RectTransform rect = managerObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            
            ParticleEffectManager manager = managerObj.AddComponent<ParticleEffectManager>();
            
            // Create ambient particles
            GameObject ambientObj = new GameObject("AmbientParticles");
            ambientObj.transform.SetParent(managerObj.transform, false);
            ambientObj.transform.localPosition = Vector3.zero;
            
            ParticleSystem ps = ambientObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = new Color(1f, 0.8f, 0.4f, 0.3f);
            main.startSize = 0.1f;
            main.startSpeed = 1f;
            main.startLifetime = 3f;
            main.maxParticles = 100;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            var emission = ps.emission;
            emission.rateOverTime = 10f;
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(20f, 15f, 1f);
            
            Undo.RegisterCreatedObjectUndo(managerObj, "Create Particle Manager");
            return managerObj;
        }

        private void ConfigureReferences(GameObject panel)
        {
            MysteryGeneratorPanel panelScript = panel.GetComponent<MysteryGeneratorPanel>();
            if (panelScript == null) return;
            
            // Use SerializedObject for proper Undo support
            SerializedObject so = new SerializedObject(panelScript);
            
            // Find and assign references
            Transform transform = panel.transform;
            
            AssignField(so, "panelCanvasGroup", panel.GetComponent<CanvasGroup>());
            AssignField(so, "panelRectTransform", panel.GetComponent<RectTransform>());
            AssignField(so, "titleText", transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>());
            AssignField(so, "difficultySelector", transform.Find("DifficultySection")?.GetComponent<DifficultySelector>());
            AssignField(so, "difficultySectionRect", transform.Find("DifficultySection")?.GetComponent<RectTransform>());
            AssignField(so, "generateButton", transform.Find("GenerateButton")?.GetComponent<AnimatedButton>());
            AssignField(so, "loadingSpinner", transform.Find("LoadingSpinner")?.gameObject);
            AssignField(so, "mysteryCard", transform.Find("MysteryDisplay/MysteryCard")?.GetComponent<MysteryCard>());
            AssignField(so, "mysteryDisplayRect", transform.Find("MysteryDisplay")?.GetComponent<RectTransform>());
            AssignField(so, "openMysteryButton", transform.Find("OpenMysteryButton")?.GetComponent<AnimatedButton>());
            AssignField(so, "startButton", transform.Find("StartButton")?.GetComponent<AnimatedButton>());
            AssignField(so, "backButton", transform.Find("BackButton")?.GetComponent<AnimatedButton>());
            AssignField(so, "particleManager", transform.Find("ParticleEffectManager")?.GetComponent<ParticleEffectManager>());
            AssignField(so, "backgroundOverlay", transform.Find("Background/Overlay")?.GetComponent<Image>());
            
            so.ApplyModifiedProperties();
            
            Debug.Log("✅ References configured");
        }

        private void AssignField(SerializedObject so, string fieldName, Object value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null && value != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        private void SetupCanvasSettings()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Error", "No Canvas found in the scene!", "OK");
                return;
            }
            
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }
            
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            EditorUtility.SetDirty(canvas);
            Debug.Log("✅ Canvas settings configured");
        }

        private void ImportTMPEssentials()
        {
            EditorUtility.DisplayDialog("TextMeshPro", 
                "Please import TMP Essentials manually:\n\n1. Window → TextMeshPro → Import TMP Essential Resources\n\n" +
                "This only needs to be done once per project.", 
                "OK");
        }
    }
    
    // Helper component for loading spinner rotation
    public class LoadingSpinnerRotation : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed = 180f;
        
        private void Update()
        {
            transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
        }
    }
}
#endif
