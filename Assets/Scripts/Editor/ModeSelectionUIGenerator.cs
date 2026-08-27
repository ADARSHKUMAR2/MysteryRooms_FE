using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using MysteryRooms.UI; // Make sure this matches the namespace of your ModeSelectionUI.cs

namespace MysteryRooms.UI.Editor
{
    public class ModeSelectionUIGenerator : EditorWindow
    {
        [MenuItem("Mystery Rooms/Generate UI/Mode Selection Panel")]
        public static void ShowWindow()
        {
            GenerateModernModeSelectionUI();
        }

        private static void GenerateModernModeSelectionUI()
        {
            // Find or create Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            // Create Main Panel
            GameObject modeSelectionPanel = new GameObject("ModeSelection_Panel");
            modeSelectionPanel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = modeSelectionPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Background Image
            Image bgImage = modeSelectionPanel.AddComponent<Image>();
            bgImage.color = new Color(0.05f, 0.05f, 0.08f, 1f); // Dark modern background

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(modeSelectionPanel.transform, false);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "SELECT GAME MODE";
            titleText.fontSize = 60;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.8f);
            titleRect.anchorMax = new Vector2(0.5f, 0.9f);
            titleRect.sizeDelta = new Vector2(800, 100);
            titleRect.anchoredPosition = Vector2.zero;

            // Button Container (Horizontal Layout)
            GameObject buttonContainer = new GameObject("Button_Container");
            buttonContainer.transform.SetParent(modeSelectionPanel.transform, false);
            RectTransform containerRect = buttonContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.sizeDelta = new Vector2(1000, 400);
            containerRect.anchoredPosition = new Vector2(0, -50);
            
            HorizontalLayoutGroup hLayout = buttonContainer.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 80; // Gap between the two massive buttons
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlHeight = true;
            hLayout.childControlWidth = true;

            // Generate the Two Large Buttons
            GameObject soloBtnObj = CreateModernButton("Btn_Solo", "SOLO\nMODE", new Color(0.2f, 0.6f, 0.8f, 1f), buttonContainer.transform);
            GameObject multiBtnObj = CreateModernButton("Btn_Multiplayer", "MULTIPLAYER\nMODE", new Color(0.8f, 0.4f, 0.2f, 1f), buttonContainer.transform);

            // Add the Logic Script we discussed earlier
            ModeSelectionUI script = modeSelectionPanel.AddComponent<ModeSelectionUI>();
            
            // Auto-assign the buttons to the script fields via reflection (nice QoL for the editor)
            var serializedObject = new SerializedObject(script);
            serializedObject.FindProperty("modeSelectionPanel").objectReferenceValue = modeSelectionPanel;
            serializedObject.FindProperty("soloModeButton").objectReferenceValue = soloBtnObj.GetComponent<Button>();
            serializedObject.FindProperty("multiplayerModeButton").objectReferenceValue = multiBtnObj.GetComponent<Button>();
            serializedObject.ApplyModifiedProperties();

            Debug.Log("✅ Modern Mode Selection UI Generated! Please link the 'Solo Panel', 'Multiplayer Panel', and 'Back Buttons' in the ModeSelectionUI component inspector.");
            Selection.activeGameObject = modeSelectionPanel;
        }

        private static GameObject CreateModernButton(string name, string text, Color accentColor, Transform parent)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);
            
            // Layout Element to force size in Horizontal Layout
            LayoutElement layout = buttonObj.AddComponent<LayoutElement>();
            layout.preferredWidth = 400;
            layout.preferredHeight = 400;

            // Background Image
            Image btnImage = buttonObj.AddComponent<Image>();
            btnImage.color = new Color(0.12f, 0.12f, 0.15f, 1f); // Dark button base

            // Button Component
            Button btn = buttonObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = accentColor;
            colors.selectedColor = Color.white;
            btn.colors = colors;

            // Add a Colored Accent Bar at the top of the button for a modern feel
            GameObject accentBar = new GameObject("Accent_Top");
            accentBar.transform.SetParent(buttonObj.transform, false);
            Image accentImg = accentBar.AddComponent<Image>();
            accentImg.color = accentColor;
            RectTransform accentRect = accentBar.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0, 0.96f);
            accentRect.anchorMax = new Vector2(1, 1);
            accentRect.offsetMin = Vector2.zero;
            accentRect.offsetMax = Vector2.zero;

            // Button Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = 50;
            tmpText.fontStyle = FontStyles.Bold;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = Color.white;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20, 20);
            textRect.offsetMax = new Vector2(-20, -20);

            return buttonObj;
        }
    }
}
