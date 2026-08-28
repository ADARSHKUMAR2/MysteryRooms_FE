using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using MysteryRooms.Game.Data;

public class EgyptianPuzzleWallGenerator : EditorWindow
{
    [Header("Puzzle Configuration")]
    private string puzzleID = "hieroglyph_wall";
    private SymbolDatabase symbolDatabaseAsset; // FIXED: Changed from GameObject to SymbolDatabase
    private GameObject symbolButtonPrefab;
    
    [Header("Visual Settings")]
    private Color primaryGold = new Color(0.85f, 0.65f, 0.13f, 1f); // Egyptian gold
    private Color secondaryBronze = new Color(0.55f, 0.35f, 0.15f, 1f); // Bronze/copper
    private Color stoneColor = new Color(0.8f, 0.7f, 0.55f, 1f); // Sandstone
    private Color darkStone = new Color(0.3f, 0.25f, 0.2f, 1f); // Dark stone
    
    [Header("Dimensions")]
    private float wallWidth = 8f;
    private float wallHeight = 5f;
    private float symbolSize = 0.8f;
    private float symbolSpacing = 0.1f;
    private Vector3 wallPosition = new Vector3(0, 2.5f, 0);
    
    [MenuItem("MysteryRooms/Create Egyptian Puzzle Wall")]
    public static void ShowWindow()
    {
        EgyptianPuzzleWallGenerator window = GetWindow<EgyptianPuzzleWallGenerator>("Puzzle Wall Generator");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Egyptian Symbol Puzzle Wall Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "This tool generates a beautiful Egyptian-themed 3D puzzle wall with an 8×5 grid of symbols. " +
            "The wall includes hieroglyphic decorations, golden borders, and atmospheric lighting.",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        // Configuration Section
        GUILayout.Label("Configuration", EditorStyles.boldLabel);
        puzzleID = EditorGUILayout.TextField("Puzzle ID", puzzleID);
        
        // FIXED: Proper object field types
        symbolDatabaseAsset = (SymbolDatabase)EditorGUILayout.ObjectField(
            "Symbol Database", 
            symbolDatabaseAsset, 
            typeof(SymbolDatabase), 
            false
        );
        
        symbolButtonPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Symbol Button Prefab", 
            symbolButtonPrefab, 
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
        symbolSize = EditorGUILayout.Slider("Symbol Size", symbolSize, 0.3f, 1.5f);
        symbolSpacing = EditorGUILayout.Slider("Symbol Spacing", symbolSpacing, 0.05f, 0.3f);
        wallPosition = EditorGUILayout.Vector3Field("Wall Position", wallPosition);
        
        GUILayout.Space(20);
        
        // Validation
        bool canGenerate = true;
        if (symbolDatabaseAsset == null)
        {
            EditorGUILayout.HelpBox("⚠️ Please assign a Symbol Database!", MessageType.Warning);
            canGenerate = false;
        }
        if (symbolButtonPrefab == null)
        {
            EditorGUILayout.HelpBox("⚠️ Please assign a Symbol Button Prefab! (or click 'Create Symbol Button Prefab' below)", MessageType.Warning);
        }
        
        GUILayout.Space(10);
        
        // Generate Button
        GUI.enabled = canGenerate;
        GUI.backgroundColor = primaryGold;
        if (GUILayout.Button("Generate Egyptian Puzzle Wall", GUILayout.Height(40)))
        {
            GeneratePuzzleWall();
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
        
        GUILayout.Space(10);
        
        // Helper button to create symbol button prefab
        if (GUILayout.Button("Create Symbol Button Prefab", GUILayout.Height(30)))
        {
            symbolButtonPrefab = CreateSymbolButtonPrefab();
        }
        
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Tip: After generation, you can manually adjust lighting, add particle effects (dust, light rays), " +
            "and place torches around the wall for extra atmosphere!",
            MessageType.Info
        );
    }

    
    private void GeneratePuzzleWall()
    {
        // Create root object
        GameObject wallRoot = new GameObject($"SymbolPuzzleWall_{puzzleID}");
        wallRoot.transform.position = wallPosition;
        
        // Add the puzzle script
        SymbolSequencePuzzle puzzleScript = wallRoot.AddComponent<SymbolSequencePuzzle>();
        
        // Create main structure
        GameObject wallStructure = CreateWallStructure(wallRoot.transform);
        GameObject canvas = CreateWorldSpaceCanvas(wallRoot.transform);
        GameObject sequenceDisplay = CreateSequenceDisplay(wallRoot.transform);
        GameObject lighting = CreateAtmosphericLighting(wallRoot.transform);
        GameObject decorations = CreateEgyptianDecorations(wallRoot.transform);
        
        // Setup canvas grid
        GameObject gridContainer = CreateGridContainer(canvas.transform);
        
        // Configure puzzle script
        ConfigurePuzzleScript(puzzleScript, gridContainer.transform);
        
        // Select the created object
        Selection.activeGameObject = wallRoot;
        
        Debug.Log($"✅ Egyptian Puzzle Wall '{puzzleID}' generated successfully!");
        EditorUtility.DisplayDialog("Success", 
            $"Egyptian Puzzle Wall created!\n\n" +
            $"Don't forget to:\n" +
            $"1. Assign Symbol Database in Inspector\n" +
            $"2. Assign Symbol Button Prefab\n" +
            $"3. Test in Play Mode", 
            "OK");
    }
    
    private GameObject CreateWallStructure(Transform parent)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "WallStructure";
        wall.transform.SetParent(parent);
        wall.transform.localPosition = Vector3.zero;
        wall.transform.localScale = new Vector3(wallWidth + 1f, wallHeight + 1f, 0.2f);
        
        // Configure material
        Material wallMat = new Material(Shader.Find("Standard"));
        wallMat.color = stoneColor;
        wallMat.SetFloat("_Metallic", 0f);
        wallMat.SetFloat("_Glossiness", 0.2f);
        wall.GetComponent<Renderer>().material = wallMat;
        
        // Add texture (you can assign a sandstone texture later)
        wallMat.mainTextureScale = new Vector2(2, 2);
        
        return wall;
    }
    
    private GameObject CreateWorldSpaceCanvas(Transform parent)
    {
        GameObject canvasObj = new GameObject("PuzzleCanvas");
        canvasObj.transform.SetParent(parent);
        canvasObj.transform.localPosition = new Vector3(0, 0, -0.11f); // Slightly in front of wall
        canvasObj.transform.localRotation = Quaternion.identity;
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;
        
        GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
        
        // Set canvas size to match wall
        RectTransform rectTransform = canvasObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(wallWidth * 100, wallHeight * 100);
        rectTransform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        
        return canvasObj;
    }
    
    private GameObject CreateGridContainer(Transform canvasTransform)
    {
        GameObject gridPanel = new GameObject("GridContainer");
        gridPanel.transform.SetParent(canvasTransform);
        
        RectTransform gridRect = gridPanel.AddComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.pivot = new Vector2(0.5f, 0.5f);
        gridRect.anchoredPosition = Vector2.zero;
        gridRect.sizeDelta = new Vector2(wallWidth * 90, wallHeight * 90);
        
        // Add background image
        Image bgImage = gridPanel.AddComponent<Image>();
        bgImage.color = new Color(darkStone.r, darkStone.g, darkStone.b, 0.95f);
        
        // Add GridLayoutGroup for 8×5 layout
        GridLayoutGroup grid = gridPanel.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 8;
        
        float cellSize = (wallWidth * 90f - symbolSpacing * 100 * 7) / 8f;
        grid.cellSize = new Vector2(cellSize, cellSize);
        grid.spacing = new Vector2(symbolSpacing * 100, symbolSpacing * 100);
        grid.padding = new RectOffset(10, 10, 10, 10);
        grid.childAlignment = TextAnchor.MiddleCenter;
        
        // Add decorative border
        CreateBorder(gridPanel.transform, "TopBorder", new Vector2(0, 0.5f), wallWidth * 95, 20);
        CreateBorder(gridPanel.transform, "BottomBorder", new Vector2(0, -0.5f), wallWidth * 95, 20);
        CreateBorder(gridPanel.transform, "LeftBorder", new Vector2(-0.5f, 0), 20, wallHeight * 95);
        CreateBorder(gridPanel.transform, "RightBorder", new Vector2(0.5f, 0), 20, wallHeight * 95);
        
        return gridPanel;
    }
    
    private void CreateBorder(Transform parent, string name, Vector2 anchorPos, float width, float height)
    {
        GameObject border = new GameObject(name);
        border.transform.SetParent(parent);
        
        RectTransform rect = border.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchorPos * (name.Contains("Top") || name.Contains("Bottom") ? wallHeight * 100 : wallWidth * 100);
        rect.sizeDelta = new Vector2(width, height);
        
        Image img = border.AddComponent<Image>();
        img.color = primaryGold;
    }
    
    private GameObject CreateSequenceDisplay(Transform parent)
    {
        GameObject displayRoot = new GameObject("SequenceDisplay");
        displayRoot.transform.SetParent(parent);
        displayRoot.transform.localPosition = new Vector3(0, wallHeight / 2 + 0.8f, -0.11f);
        
        // Create canvas for sequence display
        GameObject canvas = new GameObject("SequenceCanvas");
        canvas.transform.SetParent(displayRoot.transform);
        canvas.transform.localPosition = Vector3.zero;
        
        Canvas canvasComp = canvas.AddComponent<Canvas>();
        canvasComp.renderMode = RenderMode.WorldSpace;
        
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(500, 120);
        canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        
        // Background panel
        GameObject bgPanel = new GameObject("Background");
        bgPanel.transform.SetParent(canvas.transform);
        
        RectTransform bgRect = bgPanel.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        Image bgImage = bgPanel.AddComponent<Image>();
        bgImage.color = new Color(darkStone.r, darkStone.g, darkStone.b, 0.9f);
        
        // Title text
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(bgPanel.transform);
        
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.6f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "SACRED SEQUENCE";
        titleText.fontSize = 24;
        titleText.color = primaryGold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.fontStyle = FontStyles.Bold;
        
        // Create 4 placeholder slots
        GameObject slotsContainer = new GameObject("PlaceholderSlots");
        slotsContainer.transform.SetParent(bgPanel.transform);
        
        RectTransform slotsRect = slotsContainer.AddComponent<RectTransform>();
        slotsRect.anchorMin = new Vector2(0, 0);
        slotsRect.anchorMax = new Vector2(1, 0.6f);
        slotsRect.offsetMin = new Vector2(20, 10);
        slotsRect.offsetMax = new Vector2(-20, -10);
        
        HorizontalLayoutGroup layout = slotsContainer.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        
        for (int i = 0; i < 4; i++)
        {
            GameObject slot = new GameObject($"Placeholder_{i + 1}");
            slot.transform.SetParent(slotsContainer.transform);
            
            Image slotImage = slot.AddComponent<Image>();
            slotImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            // Add golden border
            GameObject border = new GameObject("Border");
            border.transform.SetParent(slot.transform);
            
            RectTransform borderRect = border.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-2, -2);
            borderRect.offsetMax = new Vector2(2, 2);
            
            Image borderImage = border.AddComponent<Image>();
            borderImage.color = primaryGold;
            
            // Move the slot image to front
            slot.transform.SetAsLastSibling();
        }
        
        return displayRoot;
    }
    
    private GameObject CreateAtmosphericLighting(Transform parent)
    {
        GameObject lightingRoot = new GameObject("Lighting");
        lightingRoot.transform.SetParent(parent);
        lightingRoot.transform.localPosition = Vector3.zero;
        
        // Main spotlight
        GameObject spotlight = new GameObject("MainSpotlight");
        spotlight.transform.SetParent(lightingRoot.transform);
        spotlight.transform.localPosition = new Vector3(0, 2, -3);
        spotlight.transform.localRotation = Quaternion.Euler(30, 0, 0);
        
        Light mainLight = spotlight.AddComponent<Light>();
        mainLight.type = LightType.Spot;
        mainLight.color = new Color(1f, 0.9f, 0.7f); // Warm torch light
        mainLight.intensity = 3f;
        mainLight.range = 10f;
        mainLight.spotAngle = 60f;
        mainLight.shadows = LightShadows.Soft;
        
        // Rim lights for atmosphere
        CreateRimLight(lightingRoot.transform, "LeftRim", new Vector3(-wallWidth / 2 - 1, 0, -1), new Color(0.8f, 0.5f, 0.2f));
        CreateRimLight(lightingRoot.transform, "RightRim", new Vector3(wallWidth / 2 + 1, 0, -1), new Color(0.8f, 0.5f, 0.2f));
        
        // Ambient glow (Point light)
        GameObject ambientGlow = new GameObject("AmbientGlow");
        ambientGlow.transform.SetParent(lightingRoot.transform);
        ambientGlow.transform.localPosition = new Vector3(0, 0, -0.5f);
        
        Light glowLight = ambientGlow.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = primaryGold;
        glowLight.intensity = 1.5f;
        glowLight.range = wallWidth;
        
        return lightingRoot;
    }
    
    private void CreateRimLight(Transform parent, string name, Vector3 position, Color color)
    {
        GameObject rim = new GameObject(name);
        rim.transform.SetParent(parent);
        rim.transform.localPosition = position;
        rim.transform.LookAt(parent);
        
        Light light = rim.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = color;
        light.intensity = 2f;
        light.range = 8f;
        light.spotAngle = 45f;
    }
    
    private GameObject CreateEgyptianDecorations(Transform parent)
    {
        GameObject decorRoot = new GameObject("Decorations");
        decorRoot.transform.SetParent(parent);
        decorRoot.transform.localPosition = Vector3.zero;
        
        // Top decorative element (Eye of Horus)
        CreateDecorativeElement(decorRoot.transform, "TopDecor", new Vector3(0, wallHeight / 2 + 0.3f, -0.15f), 0.5f);
        
        // Side pillars (optional - can be simple cubes)
        CreatePillar(decorRoot.transform, "LeftPillar", new Vector3(-wallWidth / 2 - 0.7f, 0, 0));
        CreatePillar(decorRoot.transform, "RightPillar", new Vector3(wallWidth / 2 + 0.7f, 0, 0));
        
        // Hieroglyphic strips (decorative lines)
        CreateHieroglyphicStrip(decorRoot.transform, "TopStrip", new Vector3(0, wallHeight / 2 + 0.6f, -0.12f));
        CreateHieroglyphicStrip(decorRoot.transform, "BottomStrip", new Vector3(0, -wallHeight / 2 - 0.6f, -0.12f));
        
        return decorRoot;
    }
    
    private void CreateDecorativeElement(Transform parent, string name, Vector3 position, float size)
    {
        GameObject decor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        decor.name = name;
        decor.transform.SetParent(parent);
        decor.transform.localPosition = position;
        decor.transform.localScale = Vector3.one * size;
        
        Material decorMat = new Material(Shader.Find("Standard"));
        decorMat.color = primaryGold;
        decorMat.SetFloat("_Metallic", 0.8f);
        decorMat.SetFloat("_Glossiness", 0.6f);
        decorMat.EnableKeyword("_EMISSION");
        decorMat.SetColor("_EmissionColor", primaryGold * 0.5f);
        
        decor.GetComponent<Renderer>().material = decorMat;
    }
    
    private void CreatePillar(Transform parent, string name, Vector3 position)
    {
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.name = name;
        pillar.transform.SetParent(parent);
        pillar.transform.localPosition = position;
        pillar.transform.localScale = new Vector3(0.3f, wallHeight / 2, 0.3f);
        
        Material pillarMat = new Material(Shader.Find("Standard"));
        pillarMat.color = secondaryBronze;
        pillarMat.SetFloat("_Metallic", 0.3f);
        pillarMat.SetFloat("_Glossiness", 0.4f);
        
        pillar.GetComponent<Renderer>().material = pillarMat;
    }
    
    private void CreateHieroglyphicStrip(Transform parent, string name, Vector3 position)
    {
        GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        strip.name = name;
        strip.transform.SetParent(parent);
        strip.transform.localPosition = position;
        strip.transform.localScale = new Vector3(wallWidth - 0.5f, 0.1f, 0.05f);
        
        Material stripMat = new Material(Shader.Find("Standard"));
        stripMat.color = new Color(secondaryBronze.r, secondaryBronze.g, secondaryBronze.b, 0.8f);
        stripMat.SetFloat("_Metallic", 0.5f);
        stripMat.SetFloat("_Glossiness", 0.3f);
        
        strip.GetComponent<Renderer>().material = stripMat;
    }
    
        private void ConfigurePuzzleScript(SymbolSequencePuzzle script, Transform gridContainer)
    {
        // Use SerializedObject to set private fields
        SerializedObject serializedScript = new SerializedObject(script);
        
        serializedScript.FindProperty("puzzleID").stringValue = puzzleID;
        serializedScript.FindProperty("gridContainer").objectReferenceValue = gridContainer;
        
        // FIXED: Properly assign the ScriptableObject
        serializedScript.FindProperty("symbolDatabase").objectReferenceValue = symbolDatabaseAsset;
        serializedScript.FindProperty("symbolButtonPrefab").objectReferenceValue = symbolButtonPrefab;
        
        // Initialize the placeholder list with 4 elements
        SerializedProperty placeholders = serializedScript.FindProperty("sequenceAttemptPlaceholders");
        
        // Find the placeholder images we created
        Transform sequenceDisplay = script.transform.Find("SequenceDisplay/SequenceCanvas/Background/PlaceholderSlots");
        if (sequenceDisplay != null)
        {
            placeholders.ClearArray();
            for (int i = 0; i < 4; i++)
            {
                Transform placeholder = sequenceDisplay.Find($"Placeholder_{i + 1}");
                if (placeholder != null)
                {
                    placeholders.InsertArrayElementAtIndex(i);
                    placeholders.GetArrayElementAtIndex(i).objectReferenceValue = placeholder.GetComponent<Image>();
                }
            }
        }
        
        serializedScript.ApplyModifiedProperties();
        
        EditorUtility.SetDirty(script);
    }

        /// <summary>
    /// Creates a properly configured SymbolButton prefab
    /// </summary>
    private GameObject CreateSymbolButtonPrefab()
    {
        // Create the root button object
        GameObject buttonObj = new GameObject("SymbolButton");
        
        // Add RectTransform
        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100, 100);
        
        // Add Image component for the symbol sprite
        Image symbolImage = buttonObj.AddComponent<Image>();
        symbolImage.color = Color.white;
        symbolImage.raycastTarget = true;
        
        // Add background panel
        GameObject background = new GameObject("Background");
        background.transform.SetParent(buttonObj.transform);
        
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.12f, 0.1f, 0.9f); // Dark stone background
        background.transform.SetAsFirstSibling(); // Put it behind the symbol
        
        // Add border/frame
        GameObject border = new GameObject("Border");
        border.transform.SetParent(buttonObj.transform);
        
        RectTransform borderRect = border.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-3, -3);
        borderRect.offsetMax = new Vector2(3, 3);
        
        Image borderImage = border.AddComponent<Image>();
        borderImage.color = primaryGold;
        border.transform.SetAsFirstSibling(); // Put it behind everything
        
        // Add hover effect (optional - makes it glow on mouse over)
        GameObject hoverGlow = new GameObject("HoverGlow");
        hoverGlow.transform.SetParent(buttonObj.transform);
        
        RectTransform glowRect = hoverGlow.AddComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-5, -5);
        glowRect.offsetMax = new Vector2(5, 5);
        
        Image glowImage = hoverGlow.AddComponent<Image>();
        glowImage.color = new Color(primaryGold.r, primaryGold.g, primaryGold.b, 0f); // Transparent by default
        hoverGlow.SetActive(false); // Will be enabled on hover
        
        // Add the SymbolButton script
        SymbolButton symbolButton = buttonObj.AddComponent<SymbolButton>();
        
        // Use SerializedObject to assign the iconImage field
        SerializedObject serializedButton = new SerializedObject(symbolButton);
        serializedButton.FindProperty("iconImage").objectReferenceValue = symbolImage;
        serializedButton.ApplyModifiedProperties();
        
        // Optional: Add Button component for Unity UI events
        Button uiButton = buttonObj.AddComponent<Button>();
        uiButton.targetGraphic = symbolImage;
        uiButton.transition = Selectable.Transition.ColorTint;
        
        ColorBlock colors = uiButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.95f, 0.8f); // Slight golden tint on hover
        colors.pressedColor = primaryGold;
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        uiButton.colors = colors;
        
        // Add BoxCollider for 3D interaction (world space canvas)
        BoxCollider collider = buttonObj.AddComponent<BoxCollider>();
        collider.size = new Vector3(100, 100, 1);
        collider.center = Vector3.zero;
        
        // Save as prefab
        string prefabPath = "Assets/Prefabs/Puzzles/SymbolButton.prefab";
        
        // Create directory if it doesn't exist
        string directory = System.IO.Path.GetDirectoryName(prefabPath);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }
        
        // Save the prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(buttonObj, prefabPath);
        
        // Clean up the temporary object
        DestroyImmediate(buttonObj);
        
        Debug.Log($"✅ SymbolButton prefab created at: {prefabPath}");
        EditorUtility.DisplayDialog("Success", 
            $"SymbolButton prefab created!\n\nLocation: {prefabPath}\n\nThe prefab has been automatically assigned to the generator.", 
            "OK");
        
        return prefab;
    }
}


