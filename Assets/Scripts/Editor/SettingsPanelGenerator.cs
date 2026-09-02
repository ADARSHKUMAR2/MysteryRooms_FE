using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace MysteryRooms.Editor
{
    public class SettingsPanelGenerator : EditorWindow
    {
        [MenuItem("MysteryRooms/Create Settings Panel UI")]
        public static void ShowWindow()
        {
            GetWindow<SettingsPanelGenerator>("Settings Panel Gen").Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Settings Panel Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This will generate a complete Settings Panel with:\n" +
                "• Mouse Sensitivity Slider (0.01 - 0.5)\n" +
                "• Joystick/Mobile Look Sensitivity Slider (10 - 360)\n" +
                "• Walk Speed Slider (1 - 15)\n" +
                "• Close and Reset buttons\n\n" +
                "All sliders are automatically wired to the SettingsPanelUI script.\n" +
                "Add this panel as a child of your main HUD Canvas.",
                MessageType.Info
            );

            GUILayout.Space(20);

            GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
            if (GUILayout.Button("Generate Settings Panel", GUILayout.Height(40)))
            {
                GenerateSettingsPanel();
            }
            GUI.backgroundColor = Color.white;
        }

        private void GenerateSettingsPanel()
        {
            // 1. Create the root panel GameObject
            GameObject panelRoot = new GameObject("SettingsPanel");
            RectTransform panelRect = panelRoot.AddComponent<RectTransform>();
            
            // Full-screen overlay (anchored to fill parent)
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Semi-transparent dark background
            Image panelBg = panelRoot.AddComponent<Image>();
            panelBg.color = new Color(0, 0, 0, 0.85f);

            // 2. Create the content container (centered box)
            GameObject contentBox = new GameObject("ContentBox");
            contentBox.transform.SetParent(panelRoot.transform, false);
            RectTransform contentRect = contentBox.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(600, 500);
            contentRect.anchoredPosition = Vector2.zero;

            Image contentBg = contentBox.AddComponent<Image>();
            contentBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // 3. Title text
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(contentBox.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(500, 60);
            titleRect.anchoredPosition = new Vector2(0, -40);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "⚙ SETTINGS";
            titleText.fontSize = 36;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
            titleText.fontStyle = FontStyles.Bold;

            // 4. Mouse sensitivity slider
            var (mouseSlider, mouseLabel) = CreateSliderWithLabel(
                contentBox.transform,
                "MouseSensitivity",
                "Mouse Sensitivity: 0.10",
                new Vector2(0, -120),
                0.01f, 0.5f, 0.1f
            );

            // 5. Joystick sensitivity slider
            var (joystickSlider, joystickLabel) = CreateSliderWithLabel(
                contentBox.transform,
                "JoystickSensitivity",
                "Joystick Sensitivity: 180",
                new Vector2(0, -200),
                10f, 360f, 180f
            );

            // 6. Walk speed slider
            var (walkSlider, walkLabel) = CreateSliderWithLabel(
                contentBox.transform,
                "WalkSpeed",
                "Walk Speed: 5.0",
                new Vector2(0, -280),
                1f, 15f, 5f
            );

            // 7. Close button (top-right corner)
            GameObject closeBtn = CreateButton(
                contentBox.transform,
                "CloseButton",
                "✕",
                new Vector2(260, -40),
                new Vector2(60, 60),
                new Color(0.8f, 0.2f, 0.2f, 1f)
            );

            // 8. Reset button (bottom center)
            GameObject resetBtn = CreateButton(
                contentBox.transform,
                "ResetButton",
                "Reset to Defaults",
                new Vector2(0, -380),
                new Vector2(250, 50),
                new Color(0.3f, 0.3f, 0.3f, 1f)
            );

            // 9. Add the SettingsPanelUI component and wire everything automatically
            var settingsScript = panelRoot.AddComponent<MysteryRooms.UI.SettingsPanelUI>();

            SerializedObject so = new SerializedObject(settingsScript);
            so.FindProperty("panelRoot").objectReferenceValue = panelRoot;
            so.FindProperty("mouseSensSlider").objectReferenceValue = mouseSlider;
            so.FindProperty("mouseSensLabel").objectReferenceValue = mouseLabel;
            so.FindProperty("joystickSensSlider").objectReferenceValue = joystickSlider;
            so.FindProperty("joystickSensLabel").objectReferenceValue = joystickLabel;
            so.FindProperty("walkSpeedSlider").objectReferenceValue = walkSlider;
            so.FindProperty("walkSpeedLabel").objectReferenceValue = walkLabel;
            so.FindProperty("closeButton").objectReferenceValue = closeBtn.GetComponent<Button>();
            so.FindProperty("resetButton").objectReferenceValue = resetBtn.GetComponent<Button>();
            so.ApplyModifiedProperties();

            // 10. Select the created panel
            Selection.activeGameObject = panelRoot;
            EditorUtility.DisplayDialog(
                "Success!",
                "Settings Panel created!\n\n" +
                "Next steps:\n" +
                "1. Drag this panel as a child of your main HUD Canvas\n" +
                "2. Create a Settings button on your HUD\n" +
                "3. Wire the button's OnClick to call SettingsPanelUI.Open()\n" +
                "4. Make sure your Player has the updated FirstPersonController script\n\n" +
                "The panel starts hidden — Open() shows it, Close() hides it.",
                "Got it!"
            );
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────────

        private (Slider slider, TextMeshProUGUI label) CreateSliderWithLabel(
            Transform parent,
            string name,
            string labelText,
            Vector2 anchoredPos,
            float minValue,
            float maxValue,
            float defaultValue
        )
        {
            // Container
            GameObject container = new GameObject(name);
            container.transform.SetParent(parent, false);
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.sizeDelta = new Vector2(500, 50);
            containerRect.anchoredPosition = anchoredPos;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(container.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(1, 0.5f);
            labelRect.sizeDelta = new Vector2(0, 30);
            labelRect.anchoredPosition = new Vector2(0, 10);

            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.fontSize = 20;
            label.alignment = TextAlignmentOptions.Left;
            label.color = Color.white;

            // Slider
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(container.transform, false);
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 0);
            sliderRect.anchorMax = new Vector2(1, 0);
            sliderRect.sizeDelta = new Vector2(0, 20);
            sliderRect.anchoredPosition = new Vector2(0, -10);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = defaultValue;
            slider.wholeNumbers = false;

            // Slider background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            // Fill area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.7f, 1f, 1f);

            // Handle slide area
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0, 0.5f);
            handleRect.anchorMax = new Vector2(0, 0.5f);
            handleRect.sizeDelta = new Vector2(20, 20);
            Image handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;

            // Wire slider references
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;

            return (slider, label);
        }

        private GameObject CreateButton(
            Transform parent,
            string name,
            string text,
            Vector2 anchoredPos,
            Vector2 sizeDelta,
            Color color
        )
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.sizeDelta = sizeDelta;
            btnRect.anchoredPosition = anchoredPos;

            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = color;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            // Button text
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmpText = txtObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = 24;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = Color.white;
            tmpText.fontStyle = FontStyles.Bold;

            return btnObj;
        }
    }
}
