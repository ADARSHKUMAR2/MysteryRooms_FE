using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

public class InteractionUIBuilder : EditorWindow
{
    [MenuItem("Mystery Rooms/Build Interaction UI Canvas")]
    public static void BuildInteractionUI()
    {
        // Check if Canvas already exists
        Canvas existingCanvas = GameObject.FindObjectOfType<Canvas>();
        if (existingCanvas != null && existingCanvas.gameObject.name == "GameUI_Canvas")
        {
            Debug.LogWarning("GameUI_Canvas already exists! Delete it first if you want to rebuild.");
            return;
        }

        // 1. Create Canvas
        GameObject canvasGO = new GameObject("GameUI_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasGO.AddComponent<GraphicRaycaster>();

        // 2. Create Interaction Prompt TextMeshPro
        GameObject promptGO = new GameObject("InteractionPrompt");
        promptGO.transform.SetParent(canvasGO.transform, false);

        TextMeshProUGUI promptText = promptGO.AddComponent<TextMeshProUGUI>();
        promptText.text = "Press E to Interact";
        promptText.fontSize = 24;
        promptText.color = Color.white;
        promptText.alignment = TextAlignmentOptions.Center;
        
        // Add a subtle shadow for better readability
        promptText.fontStyle = FontStyles.Bold;
        promptText.outlineWidth = 0.2f;
        promptText.outlineColor = Color.black;

        // Position at bottom-center
        RectTransform promptRect = promptGO.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 0f);
        promptRect.anchorMax = new Vector2(0.5f, 0f);
        promptRect.pivot = new Vector2(0.5f, 0f);
        promptRect.anchoredPosition = new Vector2(0, 100); // 100 pixels from bottom
        promptRect.sizeDelta = new Vector2(400, 50);

        // Disable by default (will be enabled by InteractionSystem when needed)
        promptGO.SetActive(false);

        Debug.Log("Interaction UI Canvas created successfully!");
        Debug.Log("Next: Attach InteractionSystem.cs to your Player and drag the InteractionPrompt text to the script's field.");
    }
}
