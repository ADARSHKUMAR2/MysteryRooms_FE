#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace MysteryRooms.Multiplayer.UI.Editor
{
    /// <summary>
    /// Editor utility to automatically generate the Multiplayer UI hierarchy
    /// Usage: Right-click in Hierarchy -> Multiplayer -> Generate Multiplayer UI
    /// </summary>
    public class MultiplayerUIGenerator : EditorWindow
    {
        [MenuItem("GameObject/Multiplayer/Generate Multiplayer UI", false, 0)]
        public static void GenerateMultiplayerUI()
        {
            // Create root canvas if it doesn't exist
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                
                // Configure Canvas Scaler for responsive UI
                CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                Debug.Log("✅ Created Canvas");
            }

            // Create Multiplayer UI root
            GameObject multiplayerUIRoot = new GameObject("MultiplayerUI");
            multiplayerUIRoot.transform.SetParent(canvas.transform, false);
            
            RectTransform rootRect = multiplayerUIRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;

            MultiplayerUI uiScript = multiplayerUIRoot.AddComponent<MultiplayerUI>();

            // Create the three panels
            GameObject menuPanel = CreateMenuPanel(multiplayerUIRoot.transform);
            GameObject lobbyPanel = CreateLobbyPanel(multiplayerUIRoot.transform);
            GameObject loadingPanel = CreateLoadingPanel(multiplayerUIRoot.transform);

            // Assign references using SerializedObject
            SerializedObject serializedUI = new SerializedObject(uiScript);

            // Assign panels
            serializedUI.FindProperty("menuPanel").objectReferenceValue = menuPanel;
            serializedUI.FindProperty("lobbyPanel").objectReferenceValue = lobbyPanel;
            serializedUI.FindProperty("loadingPanel").objectReferenceValue = loadingPanel;

            // Assign menu UI references
            serializedUI.FindProperty("hostButton").objectReferenceValue = 
                menuPanel.transform.Find("Content/HostButton").GetComponent<Button>();
            serializedUI.FindProperty("joinButton").objectReferenceValue = 
                menuPanel.transform.Find("Content/JoinButton").GetComponent<Button>();
            serializedUI.FindProperty("joinCodeInput").objectReferenceValue = 
                menuPanel.transform.Find("Content/JoinCodeInput").GetComponent<TMP_InputField>();
            serializedUI.FindProperty("roomDropdown").objectReferenceValue = 
                menuPanel.transform.Find("Content/RoomDropdown").GetComponent<TMP_Dropdown>();
            serializedUI.FindProperty("difficultySlider").objectReferenceValue = 
                menuPanel.transform.Find("Content/DifficultySlider").GetComponent<Slider>();
            serializedUI.FindProperty("difficultyText").objectReferenceValue = 
                menuPanel.transform.Find("Content/DifficultyText").GetComponent<TextMeshProUGUI>();
            serializedUI.FindProperty("playerCountInput").objectReferenceValue = 
                menuPanel.transform.Find("Content/PlayerCountInput").GetComponent<TMP_InputField>();
                
            // Assign REPLAY existing UI references
            serializedUI.FindProperty("recentMysteriesDropdown").objectReferenceValue = 
                menuPanel.transform.Find("Content/RecentMysteriesDropdown").GetComponent<TMP_Dropdown>();
            serializedUI.FindProperty("replayMysteryButton").objectReferenceValue = 
                menuPanel.transform.Find("Content/ReplayMysteryButton").GetComponent<Button>();

            // Assign lobby UI references
            serializedUI.FindProperty("joinCodeDisplay").objectReferenceValue = 
                lobbyPanel.transform.Find("Content/JoinCodeBackground/JoinCodeDisplay").GetComponent<TextMeshProUGUI>();
            serializedUI.FindProperty("playerCountText").objectReferenceValue = 
                lobbyPanel.transform.Find("Content/PlayerCountText").GetComponent<TextMeshProUGUI>();
            serializedUI.FindProperty("statusText").objectReferenceValue = 
                lobbyPanel.transform.Find("Content/StatusText").GetComponent<TextMeshProUGUI>();
            serializedUI.FindProperty("disconnectButton").objectReferenceValue = 
                lobbyPanel.transform.Find("Content/DisconnectButton").GetComponent<Button>();

            // Assign loading UI references
            serializedUI.FindProperty("loadingText").objectReferenceValue = 
                loadingPanel.transform.Find("Content/LoadingText").GetComponent<TextMeshProUGUI>();

            serializedUI.ApplyModifiedProperties();

            // Set initial states
            lobbyPanel.SetActive(false);
            loadingPanel.SetActive(false);

            Selection.activeGameObject = multiplayerUIRoot;
            Debug.Log("✅ Multiplayer UI Generated Successfully!");
            Debug.Log("⚠️ Remember to assign MultiplayerSessionManager and MultiplayerMysteryCoordinator references in the Inspector!");
        }

        #region Menu Panel

        private static GameObject CreateMenuPanel(Transform parent)
        {
            GameObject panel = CreatePanel("MenuPanel", parent, new Color(0.1f, 0.1f, 0.1f, 0.95f));
            
            GameObject content = CreateVerticalLayoutGroup("Content", panel.transform, 30f);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(600, 900);

            // Title
            CreateText("Title", content.transform, "MYSTERY ROOMS", 48, TextAlignmentOptions.Center);

            // --- SECTION: HOST NEW ---
            CreateText("HostNewHeader", content.transform, "- GENERATE NEW -", 20, TextAlignmentOptions.Center);

            CreateText("RoomLabel", content.transform, "Select Room:", 20, TextAlignmentOptions.Left);
            CreateDropdown("RoomDropdown", content.transform, new string[] { "mummy_tomb", "haunted_mansion" });

            CreateText("DifficultyLabel", content.transform, "Difficulty:", 20, TextAlignmentOptions.Left);
            CreateSlider("DifficultySlider", content.transform, 1, 5, 3);
            CreateText("DifficultyText", content.transform, "Difficulty: 3/5", 16, TextAlignmentOptions.Center);

            CreateText("PlayerCountLabel", content.transform, "Max Players:", 20, TextAlignmentOptions.Left);
            CreateInputField("PlayerCountInput", content.transform, "4", TMP_InputField.ContentType.IntegerNumber);

            CreateButton("HostButton", content.transform, "GENERATE & HOST", new Color(0.2f, 0.8f, 0.2f));

            // --- SECTION: REPLAY EXISTING ---
            CreateText("Separator1", content.transform, "- OR REPLAY PAST -", 20, TextAlignmentOptions.Center);
            
            CreateText("RecentMysteriesLabel", content.transform, "Recent Mysteries:", 20, TextAlignmentOptions.Left);
            CreateDropdown("RecentMysteriesDropdown", content.transform, new string[] { "Loading..." });
            
            CreateButton("ReplayMysteryButton", content.transform, "HOST SELECTED MYSTERY", new Color(0.8f, 0.5f, 0.2f));

            // --- SECTION: JOIN ---
            CreateText("Separator2", content.transform, "- OR JOIN FRIEND -", 20, TextAlignmentOptions.Center);

            CreateText("JoinCodeLabel", content.transform, "Enter Join Code:", 20, TextAlignmentOptions.Left);
            CreateInputField("JoinCodeInput", content.transform, "Enter 6-character code", TMP_InputField.ContentType.Alphanumeric);

            CreateButton("JoinButton", content.transform, "JOIN MYSTERY", new Color(0.2f, 0.5f, 0.9f));

            return panel;
        }

        #endregion

        #region Lobby Panel

        private static GameObject CreateLobbyPanel(Transform parent)
        {
            GameObject panel = CreatePanel("LobbyPanel", parent, new Color(0.1f, 0.1f, 0.15f, 0.95f));
            
            GameObject content = CreateVerticalLayoutGroup("Content", panel.transform, 30f);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(700, 600);

            // Title
            CreateText("Title", content.transform, "LOBBY", 48, TextAlignmentOptions.Center);

            // Join Code Display (Large and prominent)
            GameObject joinCodeBg = CreatePanel("JoinCodeBackground", content.transform, new Color(0.2f, 0.2f, 0.3f));
            RectTransform joinCodeBgRect = joinCodeBg.GetComponent<RectTransform>();
            joinCodeBgRect.sizeDelta = new Vector2(600, 120);
            
            TextMeshProUGUI joinCodeDisplay = CreateText("JoinCodeDisplay", joinCodeBg.transform, 
                "Share Code: XXXXXX", 56, TextAlignmentOptions.Center);
            joinCodeDisplay.color = new Color(0.3f, 1f, 1f); // Cyan
            joinCodeDisplay.fontStyle = FontStyles.Bold;

            // Player Count
            CreateText("PlayerCountText", content.transform, "Players: 1/4", 32, TextAlignmentOptions.Center);

            // Status Text
            TextMeshProUGUI statusText = CreateText("StatusText", content.transform, 
                "Waiting for players...", 28, TextAlignmentOptions.Center);
            statusText.color = new Color(1f, 1f, 0.5f); // Yellow

            // Spacer
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(content.transform, false);
            RectTransform spacerRect = spacer.AddComponent<RectTransform>();
            spacerRect.sizeDelta = new Vector2(0, 50);

            // Disconnect Button
            CreateButton("DisconnectButton", content.transform, "DISCONNECT", new Color(0.8f, 0.2f, 0.2f));

            return panel;
        }

        #endregion

        #region Loading Panel

        private static GameObject CreateLoadingPanel(Transform parent)
        {
            GameObject panel = CreatePanel("LoadingPanel", parent, new Color(0, 0, 0, 0.9f));
            
            GameObject content = CreateVerticalLayoutGroup("Content", panel.transform, 30f);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(600, 200);

            // Loading Text
            TextMeshProUGUI loadingText = CreateText("LoadingText", content.transform, 
                "Loading...", 36, TextAlignmentOptions.Center);
            loadingText.color = new Color(0.5f, 0.8f, 1f);

            return panel;
        }

        #endregion

        #region UI Helper Methods

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            Image image = panel.AddComponent<Image>();
            image.color = color;

            return panel;
        }

        private static GameObject CreateVerticalLayoutGroup(string name, Transform parent, float spacing)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            
            RectTransform rect = obj.AddComponent<RectTransform>();
            
            VerticalLayoutGroup layout = obj.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(50, 50, 50, 50);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;

            ContentSizeFitter fitter = obj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return obj;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, int fontSize, 
            TextAlignmentOptions alignment)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, fontSize + 20);
            
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;

            return tmp;
        }

        private static Button CreateButton(string name, Transform parent, string text, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 60); // Made buttons slightly smaller to fit
            
            Image image = obj.AddComponent<Image>();
            image.color = color;
            
            Button button = obj.AddComponent<Button>();

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;

            return button;
        }

        private static TMP_InputField CreateInputField(string name, Transform parent, string placeholder, 
            TMP_InputField.ContentType contentType)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 50);
            
            Image image = obj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f);
            
            TMP_InputField inputField = obj.AddComponent<TMP_InputField>();
            inputField.contentType = contentType;

            // Text Area
            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(obj.transform, false);
            RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.sizeDelta = new Vector2(-20, -20);

            // Placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(textArea.transform, false);
            RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholderText.text = placeholder;
            placeholderText.fontSize = 20;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f);
            placeholderText.fontStyle = FontStyles.Italic;

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(textArea.transform, false);
            RectTransform textObjRect = textObj.AddComponent<RectTransform>();
            textObjRect.anchorMin = Vector2.zero;
            textObjRect.anchorMax = Vector2.one;
            textObjRect.sizeDelta = Vector2.zero;
            
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.fontSize = 20;
            textComponent.color = Color.white;

            inputField.textViewport = textAreaRect;
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholderText;

            return inputField;
        }

        private static Slider CreateSlider(string name, Transform parent, float min, float max, float value)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 30);
            
            Slider slider = obj.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.wholeNumbers = true;

            // Background
            GameObject background = new GameObject("Background");
            background.transform.SetParent(obj.transform, false);
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.25f);
            bgRect.anchorMax = new Vector2(1, 0.75f);
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f);

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(obj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.sizeDelta = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.7f, 1f);

            // Handle Slide Area
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(obj.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = Vector2.zero;

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;

            return slider;
        }

                private static TMP_Dropdown CreateDropdown(string name, Transform parent, string[] options)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 50);
            
            Image image = obj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f);
            
            TMP_Dropdown dropdown = obj.AddComponent<TMP_Dropdown>();

            // Label
            GameObject label = new GameObject("Label");
            label.transform.SetParent(obj.transform, false);
            RectTransform labelRect = label.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = new Vector2(-40, 0);
            labelRect.anchoredPosition = new Vector2(-10, 0);
            
            TextMeshProUGUI labelText = label.AddComponent<TextMeshProUGUI>();
            labelText.text = options.Length > 0 ? options[0] : "Option";
            labelText.fontSize = 18;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.Left;
            labelText.enableWordWrapping = false;
            labelText.overflowMode = TextOverflowModes.Ellipsis;

            // Arrow
            GameObject arrow = new GameObject("Arrow");
            arrow.transform.SetParent(obj.transform, false);
            RectTransform arrowRect = arrow.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.sizeDelta = new Vector2(20, 20);
            arrowRect.anchoredPosition = new Vector2(-20, 0);
            Image arrowImage = arrow.AddComponent<Image>();
            arrowImage.color = Color.white;

            // Template (The dropdown list container)
            GameObject template = new GameObject("Template");
            template.transform.SetParent(obj.transform, false);
            RectTransform templateRect = template.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.anchoredPosition = new Vector2(0, 2);
            templateRect.sizeDelta = new Vector2(0, 150); // Height of the dropdown list
            
            Image templateImage = template.AddComponent<Image>();
            templateImage.color = new Color(0.15f, 0.15f, 0.15f);
            
            ScrollRect scrollRect = template.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Viewport (Masks the overflowing content)
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(template.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = new Vector2(-20, 0); // Leave room for scrollbar
            viewportRect.anchoredPosition = new Vector2(-10, 0);
            
            Image viewportImage = viewport.AddComponent<Image>();
            Mask viewportMask = viewport.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            // Content (Holds the actual items)
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 28);
            
            // Add vertical layout to auto-stack the items
            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childControlHeight = true;
            
            ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            
            // Item (The template for each row)
            GameObject item = new GameObject("Item");
            item.transform.SetParent(content.transform, false);
            RectTransform itemRect = item.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.sizeDelta = new Vector2(0, 30); // Fixed height per item
            
            // --- NEW CODE: Force the layout group to recognize the height ---
            LayoutElement itemLayout = item.AddComponent<LayoutElement>();
            itemLayout.minHeight = 30;
            itemLayout.preferredHeight = 30;
            // ----------------------------------------------------------------
            
            // Background color for item
            Image itemImage = item.AddComponent<Image>();
            itemImage.color = new Color(0.15f, 0.15f, 0.15f, 1f); 
            
            Toggle itemToggle = item.AddComponent<Toggle>();
            itemToggle.targetGraphic = itemImage;
            
            // Hover/Selection colors
            ColorBlock colors = itemToggle.colors;
            colors.normalColor = new Color(0.15f, 0.15f, 0.15f, 0f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            colors.pressedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            colors.selectedColor = new Color(0.2f, 0.5f, 0.9f, 1f); // Blue when selected
            itemToggle.colors = colors;
            
            // Item Label
            GameObject itemLabel = new GameObject("Item Label");
            itemLabel.transform.SetParent(item.transform, false);
            RectTransform itemLabelRect = itemLabel.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.sizeDelta = new Vector2(-10, 0); // Padding
            
            TextMeshProUGUI itemLabelText = itemLabel.AddComponent<TextMeshProUGUI>();
            itemLabelText.fontSize = 16;
            itemLabelText.color = Color.white;
            itemLabelText.alignment = TextAlignmentOptions.Left;
            itemLabelText.enableWordWrapping = false;
            itemLabelText.overflowMode = TextOverflowModes.Ellipsis; // Cut off long text with ...

            // Scrollbar (Optional but helpful)
            GameObject scrollbarObj = new GameObject("Scrollbar");
            scrollbarObj.transform.SetParent(template.transform, false);
            RectTransform scrollbarRect = scrollbarObj.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.pivot = new Vector2(1, 0.5f);
            scrollbarRect.sizeDelta = new Vector2(20, 0);
            
            Image scrollbarBg = scrollbarObj.AddComponent<Image>();
            scrollbarBg.color = new Color(0.1f, 0.1f, 0.1f);
            
            Scrollbar scrollbar = scrollbarObj.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            
            GameObject slidingArea = new GameObject("Sliding Area");
            slidingArea.transform.SetParent(scrollbarObj.transform, false);
            RectTransform slidingAreaRect = slidingArea.AddComponent<RectTransform>();
            slidingAreaRect.anchorMin = Vector2.zero;
            slidingAreaRect.anchorMax = Vector2.one;
            slidingAreaRect.sizeDelta = new Vector2(-4, -4); // Padding
            
            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(slidingArea.transform, false);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(0, 0);
            
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.4f, 0.4f, 0.4f);
            
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            scrollRect.verticalScrollbar = scrollbar;

            // Final assignments to Dropdown component
            dropdown.targetGraphic = image;
            dropdown.template = templateRect;
            dropdown.captionText = labelText;
            dropdown.itemText = itemLabelText;

            template.SetActive(false);

            // Add options
            dropdown.ClearOptions();
            dropdown.AddOptions(new System.Collections.Generic.List<string>(options));

            return dropdown;
        }


        #endregion
    }
}
#endif
