#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class HUDGeneratorWindow : EditorWindow
{
    private MysteryRooms.UI.GameUIController targetController;
    
    // Theme Colors for a professional Escape Room look
    private Color panelColor = new Color(0.05f, 0.05f, 0.08f, 0.85f); // Deep dark blue/grey
    private Color accentColor = new Color(0.8f, 0.6f, 0.2f, 1f);      // Gold accent for Egypt theme
    private Color textColor = new Color(0.9f, 0.9f, 0.9f, 1f);

    [MenuItem("MysteryRooms/Tools/Generate Professional HUD")]
    public static void ShowWindow()
    {
        GetWindow<HUDGeneratorWindow>("Professional HUD Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Escape Room HUD Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        targetController = (MysteryRooms.UI.GameUIController)EditorGUILayout.ObjectField(
            "Game UI Controller", 
            targetController, 
            typeof(MysteryRooms.UI.GameUIController), 
            true
        );

        GUILayout.Space(20);

        if (GUILayout.Button("Generate Professional HUD", GUILayout.Height(40)))
        {
            if (targetController == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign the GameUIController first!", "OK");
                return;
            }
            GenerateHUD();
        }
    }

    private void GenerateHUD()
    {
        // 1. Create Main Canvas
        GameObject canvasObj = new GameObject("PlayerHUD_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // Keep HUD on top

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. Create Top Left: Objectives Panel
        GameObject objPanel = CreateStyledPanel("ObjectivesPanel", canvasObj.transform, 
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(350, 120));
        
        TextMeshProUGUI titleText = CreateText("TitleText", objPanel.transform, "CURRENT OBJECTIVE", accentColor, 18, FontStyles.Bold);
        SetRect(titleText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -15), new Vector2(0, 30));

        TextMeshProUGUI objDescText = CreateText("DescriptionText", objPanel.transform, "Find the missing artifacts.", textColor, 22, FontStyles.Normal);
        SetRect(objDescText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -50), new Vector2(0, 40));

        TextMeshProUGUI progressText = CreateText("ProgressText", objPanel.transform, "Puzzles Solved: 0 / 5", new Color(0.6f, 0.8f, 0.6f), 16, FontStyles.Italic);
        SetRect(progressText.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 15), new Vector2(0, 30));

        // 3. Create Center: Interaction Prompt & Notifications
        GameObject centerContainer = new GameObject("CenterContainer");
        SetRect(centerContainer.AddComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 200));
        centerContainer.transform.SetParent(canvasObj.transform, false);

        TextMeshProUGUI interactText = CreateText("InteractionPrompt", centerContainer.transform, "[E] Interact", Color.white, 24, FontStyles.Bold);
        interactText.alignment = TextAlignmentOptions.Center;
        SetRect(interactText.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 50), new Vector2(0, 50));
        
        TextMeshProUGUI notifyText = CreateText("NotificationText", centerContainer.transform, "Door Unlocked!", accentColor, 28, FontStyles.Bold);
        notifyText.alignment = TextAlignmentOptions.Center;
        SetRect(notifyText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -50), new Vector2(0, 50));
        notifyText.gameObject.SetActive(false); // Hide by default

        // 4. Create Top Right: Scoreboard Panel
        GameObject scorePanel = CreateStyledPanel("ScoreboardPanel", canvasObj.transform, 
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(250, 150));
        
        TextMeshProUGUI scoreTitle = CreateText("ScoreTitle", scorePanel.transform, "TEAM PROGRESS", accentColor, 18, FontStyles.Bold);
        SetRect(scoreTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -15), new Vector2(0, 30));

        GameObject scoreList = new GameObject("ScoreList");
        scoreList.transform.SetParent(scorePanel.transform, false);
        SetRect(scoreList.AddComponent<RectTransform>(), new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, -35), Vector2.zero); // Offset for title
        VerticalLayoutGroup vlg = scoreList.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 5;

        // Player Score Card Prefab
        GameObject playerCardPrefab = CreateText("PlayerCardPrefab", null, "Player 1: 2 Solved", textColor, 18, FontStyles.Normal).gameObject;
        
        // 5. Create Bottom Center: Hotbar / Inventory
        GameObject invContainer = new GameObject("InventoryContainer");
        invContainer.transform.SetParent(canvasObj.transform, false);
        SetRect(invContainer.AddComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(600, 80));
        
        HorizontalLayoutGroup hlg = invContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 15;

        // Inventory Slot Prefab
        GameObject invSlotPrefab = CreateStyledPanel("InventorySlotPrefab", null, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(70, 70));
        Image border = invSlotPrefab.AddComponent<Outline>().GetComponent<Image>(); // Give it a nice border
        
        GameObject iconObj = new GameObject("ItemIcon");
        iconObj.transform.SetParent(invSlotPrefab.transform, false);
        Image itemIcon = iconObj.AddComponent<Image>();
        itemIcon.color = accentColor; // Placeholder color
        SetRect(itemIcon.rectTransform, new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.9f), Vector2.zero, Vector2.zero); // Top 60%

        TextMeshProUGUI itemName = CreateText("ItemName", invSlotPrefab.transform, "Key", textColor, 12, FontStyles.Bold);
        itemName.alignment = TextAlignmentOptions.Center;
        SetRect(itemName.rectTransform, new Vector2(0, 0), new Vector2(1, 0.3f), Vector2.zero, Vector2.zero); // Bottom 30%

        // 6. Wire everything to the Controller
        SerializedObject serializedObj = new SerializedObject(targetController);
        serializedObj.FindProperty("interactionPromptText").objectReferenceValue = interactText;
        serializedObj.FindProperty("objectiveTitleText").objectReferenceValue = objDescText; // Using desc text for main goal
        serializedObj.FindProperty("puzzleProgressText").objectReferenceValue = progressText;
        serializedObj.FindProperty("recentActionText").objectReferenceValue = notifyText;
        
        serializedObj.FindProperty("inventoryContainer").objectReferenceValue = invContainer.transform;
        serializedObj.FindProperty("scoreboardContainer").objectReferenceValue = scoreList.transform;
        
        // Note: Prefabs usually need to be saved to disk, but we can store them disabled in the scene for now.
        playerCardPrefab.transform.SetParent(canvasObj.transform, false);
        playerCardPrefab.SetActive(false);
        invSlotPrefab.transform.SetParent(canvasObj.transform, false);
        invSlotPrefab.SetActive(false);
        
        serializedObj.FindProperty("playerCardPrefab").objectReferenceValue = playerCardPrefab;
        serializedObj.FindProperty("inventoryItemPrefab").objectReferenceValue = invSlotPrefab;
        
        serializedObj.ApplyModifiedProperties();

        Undo.RegisterCreatedObjectUndo(canvasObj, "Create Professional HUD");
        Debug.Log("✅ Professional HUD Generated and Wired!");
    }

    // --- Helper Methods to construct UI quickly ---

    private GameObject CreateStyledPanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        GameObject panelObj = new GameObject(name);
        if (parent != null) panelObj.transform.SetParent(parent, false);
        
        Image img = panelObj.AddComponent<Image>();
        img.color = panelColor;
        
        // Optional: Add a subtle outline
        Outline outline = panelObj.AddComponent<Outline>();
        outline.effectColor = accentColor;
        outline.effectDistance = new Vector2(2, -2);

        SetRect(panelObj.GetComponent<RectTransform>(), anchorMin, anchorMax, pos, size);
        return panelObj;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string text, Color color, int fontSize, FontStyles style)
    {
        GameObject textObj = new GameObject(name);
        if (parent != null) textObj.transform.SetParent(parent, false);
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = color;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Left;

        // Add soft shadow for readability
        tmp.fontSharedMaterial.EnableKeyword("UNDERLAY_ON");
        
        return tmp;
    }

    private void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.anchoredPosition = pos;
        
        if (size != Vector2.zero) 
            rect.sizeDelta = size;
        else 
            rect.sizeDelta = Vector2.zero; // Stretch to fill
    }
}
#endif
