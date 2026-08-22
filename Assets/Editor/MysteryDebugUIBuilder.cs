using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using MysteryRooms.UI;
using MysteryRooms.Game.Managers;
using MysteryRooms.Game.Services;
using MysteryRooms.Config;

namespace MysteryRooms.Editor
{
    public class MysteryDebugUIBuilder : EditorWindow
    {
        [MenuItem("MysteryRooms/Build Mystery Debug UI")]
        public static void BuildUI()
        {
            // Check if Canvas already exists
            Canvas existingCanvas = FindObjectOfType<Canvas>();
            if (existingCanvas != null && 
                existingCanvas.GetComponentInChildren<MysteryDebugPanel>() != null)
            {
                if (!EditorUtility.DisplayDialog("UI Already Exists", 
                    "Mystery Debug UI already exists in the scene. Rebuild?", 
                    "Yes, Rebuild", "Cancel"))
                {
                    return;
                }
                DestroyImmediate(existingCanvas.gameObject);
            }

            // Create the complete UI hierarchy
            GameObject canvasObj = CreateCanvas();
            GameObject debugPanel = CreateDebugPanel(canvasObj.transform);
            
            // Wire up the MysteryDebugPanel component
            SetupMysteryDebugPanel(debugPanel);

            Debug.Log("✅ Mystery Debug UI created successfully!");
            
            // FIX: Delay selection to avoid SerializedObjectNotCreatableException
            EditorApplication.delayCall += () => {
                if (canvasObj != null) Selection.activeGameObject = canvasObj;
            };
        }

        private static GameObject CreateCanvas()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("MysteryDebugCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // High priority

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Ensure EventSystem exists
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            return canvasObj;
        }

        private static GameObject CreateDebugPanel(Transform parent)
        {
            // Main Panel
            GameObject panel = new GameObject("MysteryDebugPanel");
            panel.transform.SetParent(parent, false);
            
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(20, -20);
            panelRect.sizeDelta = new Vector2(400, 600);

            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // Add rounded corners (optional)
            panel.AddComponent<Shadow>().effectDistance = new Vector2(3, -3);

            // Create panel content
            CreatePanelHeader(panel.transform);
            CreateDifficultyControls(panel.transform);
            CreateGenerateButton(panel.transform);
            CreateStatusText(panel.transform);
            CreateMysteryInfoPanel(panel.transform);

            // Add the MysteryDebugPanel component
            panel.AddComponent<MysteryDebugPanel>();

            return panel;
        }

        private static void CreatePanelHeader(Transform parent)
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(parent, false);
            
            RectTransform rect = header.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -10);
            rect.sizeDelta = new Vector2(-20, 50);

            TextMeshProUGUI text = header.AddComponent<TextMeshProUGUI>();
            text.text = "🎲 Mystery Generator";
            text.fontSize = 24;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.8f, 0.2f);
        }

        private static void CreateDifficultyControls(Transform parent)
        {
            GameObject difficultyGroup = new GameObject("DifficultyGroup");
            difficultyGroup.transform.SetParent(parent, false);
            
            RectTransform rect = difficultyGroup.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -80);
            rect.sizeDelta = new Vector2(-40, 80);

            // Label
            GameObject label = new GameObject("Label");
            label.transform.SetParent(difficultyGroup.transform, false);
            
            RectTransform labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 1);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.pivot = new Vector2(0.5f, 1);
            labelRect.anchoredPosition = new Vector2(0, 0);
            labelRect.sizeDelta = new Vector2(0, 30);

            TextMeshProUGUI labelText = label.AddComponent<TextMeshProUGUI>();
            labelText.text = "Difficulty: 3";
            labelText.fontSize = 18;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = Color.white;
            label.name = "DifficultyText"; // For reference

            // Slider
            GameObject sliderObj = new GameObject("DifficultySlider");
            sliderObj.transform.SetParent(difficultyGroup.transform, false);
            
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 0);
            sliderRect.anchorMax = new Vector2(1, 0);
            sliderRect.pivot = new Vector2(0.5f, 0);
            sliderRect.anchoredPosition = new Vector2(0, 0);
            sliderRect.sizeDelta = new Vector2(0, 30);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 1;
            slider.maxValue = 5;
            slider.value = 3;
            slider.wholeNumbers = true;

            // Slider Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.anchoredPosition = new Vector2(5, 0);
            fillAreaRect.sizeDelta = new Vector2(-10, 0);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.6f, 1f, 1f);

            // Handle Slide Area
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = new Vector2(-10, 0);

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 30);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;

            // Wire up slider
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
        }

        private static void CreateGenerateButton(Transform parent)
        {
            GameObject buttonObj = new GameObject("GenerateButton");
            buttonObj.transform.SetParent(parent, false);
            
            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -180);
            rect.sizeDelta = new Vector2(300, 50);

            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.7f, 0.3f, 1f);

            Button button = buttonObj.AddComponent<Button>();
            
            // Button hover colors
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.7f, 0.3f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.8f, 0.4f, 1f);
            colors.pressedColor = new Color(0.15f, 0.6f, 0.25f, 1f);
            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint; // Ensure transition is set

            // Button Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "🎲 GENERATE MYSTERY";
            text.fontSize = 20;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
        }

        private static void CreateStatusText(Transform parent)
        {
            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(parent, false);
            
            RectTransform rect = statusObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -250);
            rect.sizeDelta = new Vector2(-40, 40);

            TextMeshProUGUI text = statusObj.AddComponent<TextMeshProUGUI>();
            text.text = "Ready to generate";
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            text.fontStyle = FontStyles.Italic;
        }

        private static void CreateMysteryInfoPanel(Transform parent)
        {
            GameObject infoPanel = new GameObject("MysteryInfoPanel");
            infoPanel.transform.SetParent(parent, false);
            
            RectTransform rect = infoPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -310);
            rect.sizeDelta = new Vector2(-40, -320);

            Image image = infoPanel.AddComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Scroll View
            GameObject scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(infoPanel.transform, false);
            
            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.sizeDelta = Vector2.zero;

            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 500);

            // Mystery Info Text
            GameObject infoText = new GameObject("MysteryInfoText");
            infoText.transform.SetParent(content.transform, false);
            
            RectTransform infoRect = infoText.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0, 1);
            infoRect.anchorMax = new Vector2(1, 1);
            infoRect.pivot = new Vector2(0, 1);
            infoRect.anchoredPosition = new Vector2(10, -10);
            infoRect.sizeDelta = new Vector2(-20, 0);

            TextMeshProUGUI infoTextComp = infoText.AddComponent<TextMeshProUGUI>();
            infoTextComp.text = "No mystery loaded yet.\n\nClick 'Generate Mystery' to begin.";
            infoTextComp.fontSize = 14;
            infoTextComp.alignment = TextAlignmentOptions.TopLeft;
            infoTextComp.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            infoTextComp.enableWordWrapping = true;

            ContentSizeFitter fitter = infoText.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Wire up scroll view
            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            // Scrollbar
            GameObject scrollbar = CreateScrollbar(infoPanel.transform);
            scroll.verticalScrollbar = scrollbar.GetComponent<Scrollbar>();
        }

        private static GameObject CreateScrollbar(Transform parent)
        {
            GameObject scrollbarObj = new GameObject("Scrollbar");
            scrollbarObj.transform.SetParent(parent, false);
            
            RectTransform rect = scrollbarObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(20, 0);

            Image image = scrollbarObj.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.1f, 1f);

            Scrollbar scrollbar = scrollbarObj.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            // Handle
            GameObject handleArea = new GameObject("Sliding Area");
            handleArea.transform.SetParent(scrollbarObj.transform, false);
            
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = new Vector2(-10, -10);

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 20);
            
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);

            scrollbar.handleRect = handleRect;

            return scrollbarObj;
        }

        private static void SetupMysteryDebugPanel(GameObject panelObj)
        {
            MysteryDebugPanel debugPanel = panelObj.GetComponent<MysteryDebugPanel>();
            if (debugPanel == null) return;
            
            // Find MysteryLoader or create it
            MysteryLoader mysteryLoader = FindObjectOfType<MysteryLoader>();
            if (mysteryLoader == null)
            {
                GameObject loaderObj = new GameObject("MysterySystem");
                loaderObj.AddComponent<MysteryAPIService>();
                mysteryLoader = loaderObj.AddComponent<MysteryLoader>();
                loaderObj.AddComponent<DynamicPuzzleManager>();
                
                Debug.Log("✅ Created MysterySystem GameObject");
            }

            // Delay the serialized object creation slightly to avoid the Editor error
            EditorApplication.delayCall += () =>
            {
                if (debugPanel == null) return;

                // Find UI elements
                Button generateButton = panelObj.transform.Find("GenerateButton")?.GetComponent<Button>();
                Slider difficultySlider = panelObj.transform.Find("DifficultyGroup/DifficultySlider")?.GetComponent<Slider>();
                TextMeshProUGUI difficultyText = panelObj.transform.Find("DifficultyGroup/Label")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI statusText = panelObj.transform.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI mysteryInfoText = panelObj.transform.Find("MysteryInfoPanel/ScrollView/Viewport/Content/MysteryInfoText")?.GetComponent<TextMeshProUGUI>();

                // Use SerializedObject safely
                try 
                {
                    SerializedObject serializedPanel = new SerializedObject(debugPanel);
                    
                    if (mysteryLoader != null) serializedPanel.FindProperty("mysteryLoader").objectReferenceValue = mysteryLoader;
                    if (generateButton != null) serializedPanel.FindProperty("generateButton").objectReferenceValue = generateButton;
                    if (difficultySlider != null) serializedPanel.FindProperty("difficultySlider").objectReferenceValue = difficultySlider;
                    if (difficultyText != null) serializedPanel.FindProperty("difficultyText").objectReferenceValue = difficultyText;
                    if (mysteryInfoText != null) serializedPanel.FindProperty("mysteryInfoText").objectReferenceValue = mysteryInfoText;
                    if (statusText != null) serializedPanel.FindProperty("statusText").objectReferenceValue = statusText;
                    
                    serializedPanel.ApplyModifiedProperties();
                    EditorUtility.SetDirty(debugPanel);
                    
                    Debug.Log("✅ MysteryDebugPanel wired up successfully!");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Could not auto-wire MysteryDebugPanel properties (you may need to assign them manually in the inspector): {e.Message}");
                }
            };
        }

        [MenuItem("MysteryRooms/Build Mystery System GameObject")]
        public static void BuildMysterySystem()
        {
            // Check if already exists
            if (FindObjectOfType<MysteryLoader>() != null)
            {
                if (!EditorUtility.DisplayDialog("Mystery System Exists", 
                    "MysterySystem already exists. Rebuild?", 
                    "Yes", "Cancel"))
                {
                    return;
                }
            }

            GameObject mysterySystem = new GameObject("MysterySystem");
            
            // Add API Service
            MysteryAPIService apiService = mysterySystem.AddComponent<MysteryAPIService>();
            
            // Find or create BackendConfig
            BackendConfig config = FindBackendConfig();
            
            // Add MysteryLoader
            MysteryLoader loader = mysterySystem.AddComponent<MysteryLoader>();
            
            // Add DynamicPuzzleManager
            mysterySystem.AddComponent<DynamicPuzzleManager>();

            Debug.Log("✅ MysterySystem created successfully!");

            // Delay selection and wiring
            EditorApplication.delayCall += () => {
                if (mysterySystem != null)
                {
                    Selection.activeGameObject = mysterySystem;

                    if (config != null && apiService != null)
                    {
                        try 
                        {
                            SerializedObject serializedAPI = new SerializedObject(apiService);
                            serializedAPI.FindProperty("backendConfig").objectReferenceValue = config;
                            serializedAPI.ApplyModifiedProperties();
                        }
                        catch (System.Exception) { }
                    }
                }
            };
        }

        private static BackendConfig FindBackendConfig()
        {
            string[] guids = AssetDatabase.FindAssets("t:BackendConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<BackendConfig>(path);
            }
            
            Debug.LogWarning("BackendConfig not found! Please create one and assign it manually.");
            return null;
        }
    }
}
