#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class KeypadGeneratorWindow : EditorWindow
{
    private CombinationLockPuzzle targetPuzzle;
    private bool generateWorldSpace = false;

    [MenuItem("MysteryRooms/Tools/Generate UI Keypad")]
    public static void ShowWindow()
    {
        GetWindow<KeypadGeneratorWindow>("UI Keypad Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Keypad UI Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        targetPuzzle = (CombinationLockPuzzle)EditorGUILayout.ObjectField(
            "Target Lock Script", 
            targetPuzzle, 
            typeof(CombinationLockPuzzle), 
            true
        );

        generateWorldSpace = EditorGUILayout.Toggle("Generate as World Space?", generateWorldSpace);

        GUILayout.Space(20);

        if (GUILayout.Button("Generate Keypad", GUILayout.Height(40)))
        {
            if (targetPuzzle == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a CombinationLockPuzzle target first!", "OK");
                return;
            }

            GenerateKeypadUI();
        }
    }

    private void GenerateKeypadUI()
    {
        // 1. Create Canvas
        GameObject canvasObj = new GameObject("KeypadCanvas");
        canvasObj.transform.SetParent(targetPuzzle.transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        if (generateWorldSpace)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 500);
            canvas.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
            canvas.transform.localPosition = new Vector3(0, 1f, 0); // Hover above puzzle
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        // 2. Create Background Panel
        GameObject panelObj = new GameObject("KeypadPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f); // Dark grey semi-transparent
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(300, 450);

        // 3. Create Display Text
        GameObject displayObj = new GameObject("DisplayText");
        displayObj.transform.SetParent(panelObj.transform, false);
        TextMeshProUGUI displayText = displayObj.AddComponent<TextMeshProUGUI>();
        displayText.text = "_ _ _ _";
        displayText.alignment = TextAlignmentOptions.Center;
        displayText.fontSize = 36;
        displayText.color = Color.green;
        RectTransform displayRect = displayObj.GetComponent<RectTransform>();
        displayRect.anchoredPosition = new Vector2(0, 170);
        displayRect.sizeDelta = new Vector2(250, 50);

        // 4. Create Grid Layout for Buttons
        GameObject gridObj = new GameObject("ButtonGrid");
        gridObj.transform.SetParent(panelObj.transform, false);
        RectTransform gridRect = gridObj.AddComponent<RectTransform>();
        gridRect.anchoredPosition = new Vector2(0, -20);
        gridRect.sizeDelta = new Vector2(250, 300);
        
        GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(70, 70);
        grid.spacing = new Vector2(10, 10);
        grid.childAlignment = TextAnchor.MiddleCenter;

        // 5. Generate 1-9 Buttons
        for (int i = 1; i <= 9; i++)
        {
            CreateButtonWithHandler(i.ToString(), gridObj.transform, KeypadButtonType.Digit, i.ToString(), Color.white);
        }

        // 6. Generate Bottom Row (Clear, 0, Submit)
        CreateButtonWithHandler("Clear", gridObj.transform, KeypadButtonType.Clear, "", Color.red);
        CreateButtonWithHandler("0", gridObj.transform, KeypadButtonType.Digit, "0", Color.white);
        CreateButtonWithHandler("Submit", gridObj.transform, KeypadButtonType.Submit, "", Color.green);

        // 7. Create Close Button (Top Right)
        GameObject closeObj = CreateButtonWithHandler("X", panelObj.transform, KeypadButtonType.Close, "", Color.red);
        RectTransform closeRect = closeObj.GetComponent<RectTransform>();
        closeRect.anchorMax = new Vector2(1, 1);
        closeRect.anchorMin = new Vector2(1, 1);
        closeRect.anchoredPosition = new Vector2(-20, -20);
        closeRect.sizeDelta = new Vector2(30, 30);

        // 8. Wire up the references in the Target Puzzle Script
        SerializedObject serializedPuzzle = new SerializedObject(targetPuzzle);
        serializedPuzzle.FindProperty("keypadUIPanel").objectReferenceValue = panelObj;
        serializedPuzzle.FindProperty("displayText").objectReferenceValue = displayText;
        serializedPuzzle.ApplyModifiedProperties();

        // 9. Hide panel by default
        panelObj.SetActive(false);
        
        // Register Undo
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create Keypad UI");
        Debug.Log("✅ Keypad UI Generated! Handlers automatically attached.");
    }

    private GameObject CreateButtonWithHandler(string text, Transform parent, KeypadButtonType type, string digitValue, Color textColor)
    {
        GameObject btnObj = new GameObject($"Button_{text}");
        btnObj.transform.SetParent(parent, false);
        
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.3f);
        
        // Adding our new Handler Script
        KeypadButtonHandler handler = btnObj.AddComponent<KeypadButtonHandler>();
        handler.buttonType = type;
        handler.digitValue = digitValue;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 24;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        return btnObj;
    }
}
#endif
