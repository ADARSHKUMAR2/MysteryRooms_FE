#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace MysteryRooms.Multiplayer.UI.Editor
{
    /// <summary>
    /// Editor utility to automatically generate the Host Disconnected Popup UI
    /// Usage: Right-click in Hierarchy -> Multiplayer -> Generate Host Disconnected Popup
    /// </summary>
    public class HostDisconnectedPopupGenerator : EditorWindow
    {
        [MenuItem("GameObject/Multiplayer/Generate Host Disconnected Popup", false, 1)]
        public static void GenerateHostDisconnectedPopup()
        {
            // Find or create Canvas
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

            // Create root popup GameObject
            GameObject popupRoot = new GameObject("HostDisconnectedPopup");
            popupRoot.transform.SetParent(canvas.transform, false);
            
            // Add and configure RectTransform for full screen overlay
            RectTransform popupRect = popupRoot.AddComponent<RectTransform>();
            popupRect.anchorMin = Vector2.zero;
            popupRect.anchorMax = Vector2.one;
            popupRect.sizeDelta = Vector2.zero;
            popupRect.anchoredPosition = Vector2.zero;

            // Add semi-transparent black background
            Image bgImage = popupRoot.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.8f);

            // Create dialog box panel
            GameObject dialogBox = new GameObject("DialogBox");
            dialogBox.transform.SetParent(popupRoot.transform, false);
            
            RectTransform dialogRect = dialogBox.AddComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.sizeDelta = new Vector2(600, 400);
            dialogRect.anchoredPosition = Vector2.zero;

            Image dialogImage = dialogBox.AddComponent<Image>();
            dialogImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Create title text
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(dialogBox.transform, false);
            
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.sizeDelta = new Vector2(-40, 80);
            titleRect.anchoredPosition = new Vector2(0, -40);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Host Disconnected";
            titleText.fontSize = 32;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(1f, 0.3f, 0.3f, 1f); // Red color
            titleText.alignment = TextAlignmentOptions.Center;

            // Create message text
            GameObject messageObj = new GameObject("MessageText");
            messageObj.transform.SetParent(dialogBox.transform, false);
            
            RectTransform messageRect = messageObj.AddComponent<RectTransform>();
            messageRect.anchorMin = new Vector2(0, 0.5f);
            messageRect.anchorMax = new Vector2(1, 0.5f);
            messageRect.sizeDelta = new Vector2(-60, 150);
            messageRect.anchoredPosition = new Vector2(0, 0);

            TextMeshProUGUI messageText = messageObj.AddComponent<TextMeshProUGUI>();
            messageText.text = "The host has disconnected from the game.\n\nYou will be returned to the main menu.";
            messageText.fontSize = 24;
            messageText.color = Color.white;
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.enableWordWrapping = true;

            // Create return to menu button
            GameObject buttonObj = new GameObject("ReturnToMenuButton");
            buttonObj.transform.SetParent(dialogBox.transform, false);
            
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0);
            buttonRect.anchorMax = new Vector2(0.5f, 0);
            buttonRect.sizeDelta = new Vector2(350, 70);
            buttonRect.anchoredPosition = new Vector2(0, 60);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.8f, 0.3f, 0.3f, 1f); // Red button

            Button button = buttonObj.AddComponent<Button>();
            
            // Configure button colors
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.8f, 0.3f, 0.3f, 1f);
            colors.highlightedColor = new Color(1f, 0.4f, 0.4f, 1f);
            colors.pressedColor = new Color(0.6f, 0.2f, 0.2f, 1f);
            colors.selectedColor = new Color(0.8f, 0.3f, 0.3f, 1f);
            button.colors = colors;

            // Create button text
            GameObject buttonTextObj = new GameObject("Text");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);
            
            RectTransform buttonTextRect = buttonTextObj.AddComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.sizeDelta = Vector2.zero;
            buttonTextRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = "Return to Main Menu";
            buttonText.fontSize = 24;
            buttonText.fontStyle = FontStyles.Bold;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;

            // Add the HostDisconnectedPopup script
            HostDisconnectedPopup popupScript = popupRoot.AddComponent<HostDisconnectedPopup>();

            // Assign references using SerializedObject
            SerializedObject serializedPopup = new SerializedObject(popupScript);
            serializedPopup.FindProperty("popupPanel").objectReferenceValue = popupRoot;
            serializedPopup.FindProperty("messageText").objectReferenceValue = messageText;
            serializedPopup.FindProperty("returnToMenuButton").objectReferenceValue = button;
            serializedPopup.FindProperty("mainMenuSceneName").stringValue = "MainMenu";
            serializedPopup.FindProperty("defaultMessage").stringValue = "The host has disconnected from the game.";
            serializedPopup.ApplyModifiedProperties();

            // Disable popup by default
            popupRoot.SetActive(false);

            // Select the created object
            Selection.activeGameObject = popupRoot;

            Debug.Log("✅ Host Disconnected Popup UI created successfully!");
            
            EditorUtility.DisplayDialog(
                "Host Disconnected Popup Created", 
                "The popup UI has been created successfully!\n\n" +
                "Next step:\n" +
                "Right-click in Hierarchy → Multiplayer → Generate Game Session Manager\n\n" +
                "This will create the manager and automatically link it to the popup.",
                "OK"
            );
        }
    }
}
#endif
