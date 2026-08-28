using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using MysteryRooms.Game.Data;

public class EgyptianCardPuzzleGenerator : EditorWindow
{
    [Header("Puzzle Configuration")]
    private string puzzleID = "pharaoh_cards_riddle";
    private CardDatabase cardDatabaseAsset;
    private GameObject cardButtonPrefab;
    
    [Header("Visual Settings")]
    private Color primaryGold = new Color(0.85f, 0.65f, 0.13f, 1f);
    private Color secondaryBronze = new Color(0.55f, 0.35f, 0.15f, 1f);
    private Color stoneColor = new Color(0.8f, 0.7f, 0.55f, 1f);
    private Color darkStone = new Color(0.15f, 0.12f, 0.1f, 0.95f);
    
    [Header("Dimensions")]
    private float wallWidth = 6f;
    private float wallHeight = 5.5f;
    private Vector3 wallPosition = new Vector3(0, 2.75f, 0);
    
    [MenuItem("MysteryRooms/Create Egyptian Card Puzzle Wall")]
    public static void ShowWindow()
    {
        EgyptianCardPuzzleGenerator window = GetWindow<EgyptianCardPuzzleGenerator>("Card Puzzle Generator");
        window.minSize = new Vector2(450, 650);
        window.Show();
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Egyptian Card Puzzle Wall Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Generates a 4×4 playing card puzzle wall with a keypad for code input. " +
            "Includes optimized lighting, Egyptian decor, and UI setup.",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        // Configuration Section
        GUILayout.Label("Configuration", EditorStyles.boldLabel);
        puzzleID = EditorGUILayout.TextField("Puzzle ID", puzzleID);
        
        cardDatabaseAsset = (CardDatabase)EditorGUILayout.ObjectField(
            "Card Database", 
            cardDatabaseAsset, 
            typeof(CardDatabase), 
            false
        );
        
        cardButtonPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Card Button Prefab", 
            cardButtonPrefab, 
            typeof(GameObject), 
            false
        );
        
        GUILayout.Space(10);
        
        // Visual Settings Section
        GUILayout.Label("Visual Theme", EditorStyles.boldLabel);
        primaryGold = EditorGUILayout.ColorField("Primary Gold", primaryGold);
        secondaryBronze = EditorGUILayout.ColorField("Secondary Bronze", secondaryBronze);
        stoneColor = EditorGUILayout.ColorField("Stone Color", stoneColor);
        darkStone = EditorGUILayout.ColorField("Dark Stone", darkStone);
        
        GUILayout.Space(10);
        
        // Dimensions Section
        GUILayout.Label("Dimensions", EditorStyles.boldLabel);
        wallWidth = EditorGUILayout.FloatField("Wall Width", wallWidth);
        wallHeight = EditorGUILayout.FloatField("Wall Height", wallHeight);
        wallPosition = EditorGUILayout.Vector3Field("Wall Position", wallPosition);
        
        GUILayout.Space(20);
        
        // Validation
        bool canGenerate = true;
        if (cardDatabaseAsset == null)
        {
            EditorGUILayout.HelpBox("⚠️ Please assign a Card Database!", MessageType.Warning);
            canGenerate = false;
        }
        if (cardButtonPrefab == null)
        {
            EditorGUILayout.HelpBox("⚠️ Please assign a Card Button Prefab! (Click below to create one)", MessageType.Warning);
        }
        
        GUILayout.Space(10);
        
        // Generate Button
        GUI.enabled = canGenerate;
        GUI.backgroundColor = primaryGold;
        if (GUILayout.Button("Generate Card Puzzle Wall", GUILayout.Height(40)))
        {
            GeneratePuzzleWall();
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Create Card Button Prefab", GUILayout.Height(30)))
        {
            cardButtonPrefab = CreateCardButtonPrefab();
        }
    }
    
    private void GeneratePuzzleWall()
    {
        // 1. Root Object
        GameObject wallRoot = new GameObject($"CardPuzzleWall_{puzzleID}");
        wallRoot.transform.position = wallPosition;
        
        CardDeckRiddlePuzzle puzzleScript = wallRoot.AddComponent<CardDeckRiddlePuzzle>();
        
        // 2. Build Structure
        CreateWallStructure(wallRoot.transform);
        GameObject canvas = CreateWorldSpaceCanvas(wallRoot.transform);
        CreateAtmosphericLighting(wallRoot.transform);
        CreateEgyptianDecorations(wallRoot.transform);
        
        // 3. Build UI
        GameObject gridContainer = CreateCardGrid(canvas.transform);
        (GameObject inputPanel, TextMeshProUGUI displayText, Button submitBtn, Button clearBtn) = CreateKeypadUI(canvas.transform);
        
        // 4. Configure Script
        ConfigurePuzzleScript(puzzleScript, gridContainer.transform, inputPanel, displayText, submitBtn, clearBtn);
        
        Selection.activeGameObject = wallRoot;
        
        EditorUtility.DisplayDialog("Success", "Card Puzzle Wall generated successfully!", "OK");
    }
    
    private GameObject CreateWallStructure(Transform parent)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "WallStructure";
        wall.transform.SetParent(parent);
        wall.transform.localPosition = Vector3.zero;
        wall.transform.localScale = new Vector3(wallWidth, wallHeight, 0.2f);
        
        DestroyImmediate(wall.GetComponent<Collider>());
        MeshCollider mc = wall.AddComponent<MeshCollider>();
        mc.convex = false;
        
        Material wallMat = new Material(Shader.Find("Standard"));
        wallMat.color = stoneColor;
        wallMat.SetFloat("_Glossiness", 0.1f);
        wall.GetComponent<Renderer>().material = wallMat;
        wall.isStatic = true;
        
        return wall;
    }
    
    private GameObject CreateWorldSpaceCanvas(Transform parent)
    {
        GameObject canvasObj = new GameObject("PuzzleCanvas");
        canvasObj.transform.SetParent(parent);
        canvasObj.transform.localPosition = new Vector3(0, 0, -0.11f);
        canvasObj.transform.localRotation = Quaternion.identity;
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        RectTransform rectTransform = canvasObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(wallWidth * 100, wallHeight * 100);
        rectTransform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        
        return canvasObj;
    }
    
    private GameObject CreateCardGrid(Transform canvasTransform)
    {
        GameObject gridPanel = new GameObject("GridContainer");
        gridPanel.transform.SetParent(canvasTransform);
        
        RectTransform gridRect = gridPanel.AddComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 1f);
        gridRect.anchorMax = new Vector2(0.5f, 1f);
        gridRect.pivot = new Vector2(0.5f, 1f);
        gridRect.anchoredPosition = new Vector2(0, -30);
        gridRect.sizeDelta = new Vector2(520, 760); // Space for 4x4 cards
        
        Image bgImage = gridPanel.AddComponent<Image>();
        bgImage.color = darkStone;
        
        GridLayoutGroup grid = gridPanel.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.cellSize = new Vector2(120, 180); // Playing card aspect ratio
        grid.spacing = new Vector2(10, 10);
        grid.padding = new RectOffset(10, 10, 10, 10);
        grid.childAlignment = TextAnchor.MiddleCenter;
        
        // Add golden border
        CreateBorder(gridPanel.transform, "Border", Vector2.zero, 530, 770);
        
        return gridPanel;
    }

    private (GameObject, TextMeshProUGUI, Button, Button) CreateKeypadUI(Transform canvasTransform)
    {
        // Main Panel
        GameObject inputPanel = new GameObject("CodeInputPanel");
        inputPanel.transform.SetParent(canvasTransform);
        
        RectTransform panelRect = inputPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0, 30);
        panelRect.sizeDelta = new Vector2(400, 180);
        
        Image bgImage = inputPanel.AddComponent<Image>();
        bgImage.color = darkStone;
        CreateBorder(inputPanel.transform, "Border", Vector2.zero, 410, 190);

        // Display Text
        GameObject textObj = new GameObject("CodeDisplay");
        textObj.transform.SetParent(inputPanel.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0.6f);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI displayText = textObj.AddComponent<TextMeshProUGUI>();
        displayText.text = "_ _ _ _";
        displayText.fontSize = 48;
        displayText.color = primaryGold;
        displayText.alignment = TextAlignmentOptions.Center;
        displayText.fontStyle = FontStyles.Bold;

        // Keypad Grid
        GameObject keypadObj = new GameObject("KeypadGrid");
        keypadObj.transform.SetParent(inputPanel.transform);
        RectTransform keypadRect = keypadObj.AddComponent<RectTransform>();
        keypadRect.anchorMin = new Vector2(0, 0);
        keypadRect.anchorMax = new Vector2(1, 0.6f);
        keypadRect.offsetMin = new Vector2(10, 10);
        keypadRect.offsetMax = new Vector2(-10, -10);

        GridLayoutGroup layout = keypadObj.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(55, 45);
        layout.spacing = new Vector2(8, 8);
        layout.childAlignment = TextAnchor.MiddleCenter;

        // Digits 1-4
        for (int i = 1; i <= 4; i++)
        {
            CreateKeypadButton(keypadObj.transform, i.ToString(), true);
        }
        
        // Clear & Submit
        Button clearBtn = CreateKeypadButton(keypadObj.transform, "CLR", false).GetComponent<Button>();
        Button submitBtn = CreateKeypadButton(keypadObj.transform, "ENT", false).GetComponent<Button>();

        return (inputPanel, displayText, submitBtn, clearBtn);
    }

    private GameObject CreateKeypadButton(Transform parent, string label, bool isDigit)
    {
        GameObject btnObj = new GameObject($"Btn_{label}");
        btnObj.transform.SetParent(parent);
        
        Image bg = btnObj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f);
        
        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = primaryGold;
        cb.pressedColor = new Color(0.6f, 0.4f, 0.1f);
        btn.colors = cb;

        // Add 3D Raycast collider
        BoxCollider col = btnObj.AddComponent<BoxCollider>();
        col.size = new Vector3(55, 45, 1);

        // Add CardDeckKeypad if it's a digit
        if (isDigit)
        {
            CardDeckKeypad keypad = btnObj.AddComponent<CardDeckKeypad>();
            SerializedObject so = new SerializedObject(keypad);
            so.FindProperty("digitValue").stringValue = label;
            so.ApplyModifiedProperties();
        }

        // Text label
        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform);
        RectTransform txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = txtRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI text = txtObj.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 20;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        return btnObj;
    }
    
    private void CreateBorder(Transform parent, string name, Vector2 anchorPos, float width, float height)
    {
        GameObject border = new GameObject(name);
        border.transform.SetParent(parent);
        border.transform.SetAsFirstSibling();
        
        RectTransform rect = border.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchorPos;
        rect.sizeDelta = new Vector2(width, height);
        
        Image img = border.AddComponent<Image>();
        img.color = primaryGold;
        img.raycastTarget = false;
    }
    
    private void ConfigurePuzzleScript(CardDeckRiddlePuzzle script, Transform grid, GameObject panel, TextMeshProUGUI display, Button submit, Button clear)
    {
        SerializedObject so = new SerializedObject(script);
        
        so.FindProperty("puzzleID").stringValue = puzzleID;
        so.FindProperty("cardDatabase").objectReferenceValue = cardDatabaseAsset;
        so.FindProperty("cardButtonPrefab").objectReferenceValue = cardButtonPrefab;
        so.FindProperty("gridContainer").objectReferenceValue = grid;
        so.FindProperty("codeInputPanel").objectReferenceValue = panel;
        so.FindProperty("codeDisplayText").objectReferenceValue = display;
        so.FindProperty("submitButton").objectReferenceValue = submit;
        so.FindProperty("clearButton").objectReferenceValue = clear;
        
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(script);
    }
    
    private void CreateAtmosphericLighting(Transform parent)
    {
        GameObject lightRoot = new GameObject("Lighting");
        lightRoot.transform.SetParent(parent);
        lightRoot.transform.localPosition = Vector3.zero;
        
        GameObject mainLight = new GameObject("Spotlight");
        mainLight.transform.SetParent(lightRoot.transform);
        mainLight.transform.localPosition = new Vector3(0, 2, -2);
        mainLight.transform.localRotation = Quaternion.Euler(45, 0, 0);
        
        Light l = mainLight.AddComponent<Light>();
        l.type = LightType.Spot;
        l.color = new Color(1f, 0.9f, 0.7f);
        l.intensity = 2f;
        l.range = 10f;
        l.shadows = LightShadows.Hard;
        l.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Low;
    }
    
    private void CreateEgyptianDecorations(Transform parent)
    {
        GameObject decor = new GameObject("Decorations");
        decor.transform.SetParent(parent);
        decor.transform.localPosition = Vector3.zero;
        
        // Strip
        GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        strip.name = "TopStrip";
        strip.transform.SetParent(decor.transform);
        strip.transform.localPosition = new Vector3(0, wallHeight / 2 + 0.3f, -0.1f);
        strip.transform.localScale = new Vector3(wallWidth - 0.5f, 0.1f, 0.05f);
        DestroyImmediate(strip.GetComponent<Collider>());
        
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = secondaryBronze;
        strip.GetComponent<Renderer>().material = mat;
    }

    private GameObject CreateCardButtonPrefab()
    {
        GameObject buttonObj = new GameObject("CardButton");
        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120, 180); // Playing card aspect ratio
        
        // Border
        CreateBorder(buttonObj.transform, "Border", Vector2.zero, 126, 186);
        
        // Glow (hidden)
        GameObject glow = new GameObject("Glow");
        glow.transform.SetParent(buttonObj.transform);
        RectTransform glowRect = glow.AddComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero; glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-8, -8); glowRect.offsetMax = new Vector2(8, 8);
        Image glowImg = glow.AddComponent<Image>();
        glowImg.color = new Color(1, 0.9f, 0.5f, 0f);
        glowImg.raycastTarget = false;
        glow.SetActive(false);
        
        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(buttonObj.transform);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = Color.white;
        bgImg.raycastTarget = false;

        // Card Face
        Image faceImg = buttonObj.AddComponent<Image>();
        faceImg.color = Color.white;
        faceImg.raycastTarget = true;
        
        // Components
        BoxCollider col = buttonObj.AddComponent<BoxCollider>();
        col.size = new Vector3(120, 180, 1);
        
        CardButton cardBtn = buttonObj.AddComponent<CardButton>();
        SerializedObject so = new SerializedObject(cardBtn);
        so.FindProperty("cardImage").objectReferenceValue = faceImg;
        so.FindProperty("borderImage").objectReferenceValue = buttonObj.transform.Find("Border").GetComponent<Image>();
        so.ApplyModifiedProperties();
        
        // Save
        string path = "Assets/Prefabs/Puzzles/CardButton.prefab";
        System.IO.Directory.CreateDirectory("Assets/Prefabs/Puzzles");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(buttonObj, path);
        DestroyImmediate(buttonObj);
        
        EditorUtility.DisplayDialog("Created", $"CardButton prefab saved at: {path}", "OK");
        return prefab;
    }
}
