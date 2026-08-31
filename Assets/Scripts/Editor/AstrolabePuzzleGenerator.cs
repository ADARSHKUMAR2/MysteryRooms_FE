using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class AstrolabePuzzleGenerator : EditorWindow
{
    private string puzzleID = "map_room";
    
    [Header("Visual Theme")]
    private Color parchmentColor = new Color(0.9f, 0.85f, 0.7f, 1f); // Aged paper
    private Color bronzeColor = new Color(0.6f, 0.4f, 0.1f, 1f); // Astrolabe metal
    private Color darkInkColor = new Color(0.2f, 0.15f, 0.1f, 1f); // Faded ink
    
    [Header("Sprites (Optional)")]
    private Sprite mapBackgroundSprite; // A drawn map of Egypt
    private Sprite astrolabeBackgroundSprite; // A bronze gear/dial background

    [MenuItem("MysteryRooms/Create Map and Astrolabe Puzzle")]
    public static void ShowWindow()
    {
        GetWindow<AstrolabePuzzleGenerator>("Astrolabe Gen").Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Map & Astrolabe Puzzle Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Generates two objects:\n" +
            "1. A physical Papyrus Map that opens a beautiful full-screen UI.\n" +
            "2. A physical Bronze Globe that opens a coordinate input dial UI.", 
            MessageType.Info
        );
        GUILayout.Space(10);

        puzzleID = EditorGUILayout.TextField("Puzzle ID", puzzleID);
        
        GUILayout.Space(10);
        GUILayout.Label("Optional Sprites (Makes it look amazing!)", EditorStyles.boldLabel);
        mapBackgroundSprite = (Sprite)EditorGUILayout.ObjectField("Map Drawing (Sprite)", mapBackgroundSprite, typeof(Sprite), false);
        astrolabeBackgroundSprite = (Sprite)EditorGUILayout.ObjectField("Astrolabe UI (Sprite)", astrolabeBackgroundSprite, typeof(Sprite), false);

        GUILayout.Space(20);
        if (GUILayout.Button("Generate Puzzle System", GUILayout.Height(40)))
        {
            GeneratePuzzleSystem();
        }
    }

        private void GeneratePuzzleSystem()
    {
        // --- ROOT OBJECT (Holds the Main Script) ---
        GameObject rootObj = new GameObject($"AstrolabePuzzle_{puzzleID}");
        rootObj.transform.position = Vector3.zero;
        
        MapCoordinatesPuzzle mainScript = rootObj.AddComponent<MapCoordinatesPuzzle>();
        mainScript.puzzleID = puzzleID;

        // --- 1. PHYSICAL BRONZE GLOBE (The Interactable Target) ---
        GameObject globeRoot = new GameObject("Physical_Astrolabe_Globe");
        globeRoot.transform.SetParent(rootObj.transform);
        globeRoot.transform.localPosition = new Vector3(2, 0, 0);

        GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stand.transform.SetParent(globeRoot.transform);
        stand.transform.localPosition = new Vector3(0, 0.3f, 0);
        stand.transform.localScale = new Vector3(0.5f, 0.3f, 0.5f);
        
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(globeRoot.transform);
        sphere.transform.localPosition = new Vector3(0, 0.9f, 0);
        sphere.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        Material bronzeMat = new Material(urpLit);
        bronzeMat.color = bronzeColor;
        bronzeMat.SetFloat("_Metallic", 0.9f);
        bronzeMat.SetFloat("_Smoothness", 0.6f);
        stand.GetComponent<Renderer>().material = bronzeMat;
        sphere.GetComponent<Renderer>().material = bronzeMat;

        BoxCollider globeCol = globeRoot.AddComponent<BoxCollider>();
        globeCol.center = new Vector3(0, 0.6f, 0);
        globeCol.size = new Vector3(1f, 1.2f, 1f);
        
        GlobeInteractable globeInteractable = globeRoot.AddComponent<GlobeInteractable>();
        globeInteractable.parentPuzzle = mainScript; // Link to Root

        // --- 2. PHYSICAL PAPYRUS SCROLL (The Clue) ---
        GameObject scrollRoot = new GameObject("Physical_Map_Scroll");
        scrollRoot.transform.SetParent(rootObj.transform);
        scrollRoot.transform.localPosition = new Vector3(-2, 0, 0);

        GameObject scrollMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        scrollMesh.transform.SetParent(scrollRoot.transform);
        scrollMesh.transform.localPosition = new Vector3(0, 0.1f, 0);
        scrollMesh.transform.localRotation = Quaternion.Euler(0, 0, 90); // Laying flat
        scrollMesh.transform.localScale = new Vector3(0.1f, 0.4f, 0.1f);

        Material paperMat = new Material(urpLit);
        paperMat.color = parchmentColor;
        scrollMesh.GetComponent<Renderer>().material = paperMat;

        BoxCollider scrollCol = scrollRoot.AddComponent<BoxCollider>();
        scrollCol.center = new Vector3(0, 0.1f, 0);
        scrollCol.size = new Vector3(1f, 0.3f, 0.3f);

        MapScrollProp scrollScript = scrollRoot.AddComponent<MapScrollProp>();
        scrollScript.parentPuzzle = mainScript; // Link to Root

        // --- 3. THE UI CANVAS ---
        GameObject uiCanvas = new GameObject("Astrolabe_UI_Canvas");
        uiCanvas.transform.SetParent(rootObj.transform);
        Canvas canvas = uiCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50; 
        
        CanvasScaler scaler = uiCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        uiCanvas.AddComponent<GraphicRaycaster>();

        // A. FULL SCREEN MAP UI
        GameObject mapPanel = new GameObject("FullScreenMapPanel");
        mapPanel.transform.SetParent(uiCanvas.transform, false);
        RectTransform mapRect = mapPanel.AddComponent<RectTransform>();
        mapRect.anchorMin = Vector2.zero; mapRect.anchorMax = Vector2.one;
        mapRect.offsetMin = mapRect.offsetMax = Vector2.zero;
        
        Image mapImg = mapPanel.AddComponent<Image>();
        mapImg.color = parchmentColor;
        if (mapBackgroundSprite != null) mapImg.sprite = mapBackgroundSprite;

        GameObject stampObj = new GameObject("CoordinatesStampText");
        stampObj.transform.SetParent(mapPanel.transform, false);
        RectTransform stampRect = stampObj.AddComponent<RectTransform>();
        stampRect.anchorMin = new Vector2(0.7f, 0.1f); 
        stampRect.anchorMax = new Vector2(0.9f, 0.3f);
        stampRect.offsetMin = stampRect.offsetMax = Vector2.zero;
        stampRect.localRotation = Quaternion.Euler(0, 0, 15f);

        TextMeshProUGUI stampText = stampObj.AddComponent<TextMeshProUGUI>();
        stampText.text = "N29\nE31";
        stampText.fontSize = 60;
        stampText.color = darkInkColor;
        stampText.alignment = TextAlignmentOptions.Center;
        stampText.fontStyle = FontStyles.Bold;
        
        GameObject mapCloseObj = new GameObject("CloseMapButton");
        mapCloseObj.transform.SetParent(mapPanel.transform, false);
        RectTransform mapCloseRect = mapCloseObj.AddComponent<RectTransform>();
        mapCloseRect.anchorMin = new Vector2(0.5f, 0.05f);
        mapCloseRect.anchorMax = new Vector2(0.5f, 0.05f);
        mapCloseRect.sizeDelta = new Vector2(300, 80);
        mapCloseRect.anchoredPosition = Vector2.zero;
        Image mapCloseImg = mapCloseObj.AddComponent<Image>();
        mapCloseImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        Button mapCloseBtn = mapCloseObj.AddComponent<Button>();
        
        GameObject mcbTextObj = new GameObject("Text");
        mcbTextObj.transform.SetParent(mapCloseObj.transform, false);
        RectTransform mcbRect = mcbTextObj.AddComponent<RectTransform>();
        mcbRect.anchorMin = Vector2.zero; mcbRect.anchorMax = Vector2.one;
        mcbRect.offsetMin = mcbRect.offsetMax = Vector2.zero;
        TextMeshProUGUI mcbText = mcbTextObj.AddComponent<TextMeshProUGUI>();
        mcbText.text = "Put Away Map [E]";
        mcbText.alignment = TextAlignmentOptions.Center;
        mcbText.color = Color.white;
        mcbText.fontSize = 30;

        mapCloseBtn.onClick.AddListener(() => scrollScript.Interact());
        mapPanel.SetActive(false); 

        // B. ASTROLABE INPUT UI
        GameObject inputPanel = new GameObject("AstrolabeInputPanel");
        inputPanel.transform.SetParent(uiCanvas.transform, false);
        RectTransform inputRect = inputPanel.AddComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.5f, 0.5f);
        inputRect.anchorMax = new Vector2(0.5f, 0.5f);
        inputRect.sizeDelta = new Vector2(800, 600);
        inputRect.anchoredPosition = Vector2.zero;
        
        Image inputImg = inputPanel.AddComponent<Image>();
        inputImg.color = new Color(bronzeColor.r, bronzeColor.g, bronzeColor.b, 0.95f);
        if (astrolabeBackgroundSprite != null) inputImg.sprite = astrolabeBackgroundSprite;

        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(inputPanel.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.8f); titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "ALIGN ASTROLABE";
        titleText.fontSize = 50;
        titleText.color = parchmentColor;
        titleText.alignment = TextAlignmentOptions.Center;

        TMP_InputField latInput = CreateInputField(inputPanel.transform, "Latitude Input", new Vector2(-200, 50), "LAT (e.g. N29)", parchmentColor);
        TMP_InputField longInput = CreateInputField(inputPanel.transform, "Longitude Input", new Vector2(200, 50), "LONG (e.g. E31)", parchmentColor);

        GameObject submitObj = new GameObject("SubmitButton");
        submitObj.transform.SetParent(inputPanel.transform, false);
        RectTransform submitRect = submitObj.AddComponent<RectTransform>();
        submitRect.anchorMin = new Vector2(0.5f, 0.5f); submitRect.anchorMax = new Vector2(0.5f, 0.5f);
        submitRect.sizeDelta = new Vector2(300, 80);
        submitRect.anchoredPosition = new Vector2(0, -100);
        Image submitImg = submitObj.AddComponent<Image>();
        submitImg.color = darkInkColor;
        Button submitBtn = submitObj.AddComponent<Button>();
        
        GameObject subTextObj = new GameObject("Text");
        subTextObj.transform.SetParent(submitObj.transform, false);
        RectTransform subTRect = subTextObj.AddComponent<RectTransform>();
        subTRect.anchorMin = Vector2.zero; subTRect.anchorMax = Vector2.one;
        subTRect.offsetMin = subTRect.offsetMax = Vector2.zero;
        TextMeshProUGUI subText = subTextObj.AddComponent<TextMeshProUGUI>();
        subText.text = "ALIGN GEARS";
        subText.alignment = TextAlignmentOptions.Center;
        subText.color = parchmentColor;
        subText.fontSize = 36;

        GameObject closeObj = new GameObject("CloseButton");
        closeObj.transform.SetParent(inputPanel.transform, false);
        RectTransform closeRect = closeObj.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 1); closeRect.anchorMax = new Vector2(1, 1);
        closeRect.sizeDelta = new Vector2(60, 60);
        closeRect.anchoredPosition = new Vector2(-40, -40);
        Image closeImg = closeObj.AddComponent<Image>();
        closeImg.color = Color.red;
        Button closeBtn = closeObj.AddComponent<Button>();

        GameObject fbObj = new GameObject("FeedbackText");
        fbObj.transform.SetParent(inputPanel.transform, false);
        RectTransform fbRect = fbObj.AddComponent<RectTransform>();
        fbRect.anchorMin = new Vector2(0, 0); fbRect.anchorMax = new Vector2(0.5f, 0.2f);
        fbRect.offsetMin = fbRect.offsetMax = Vector2.zero;
        TextMeshProUGUI fbText = fbObj.AddComponent<TextMeshProUGUI>();
        fbText.text = "Awaiting Alignment...";
        fbText.fontSize = 30;
        fbText.color = parchmentColor;
        fbText.alignment = TextAlignmentOptions.Center;

        inputPanel.SetActive(false);

        // --- 4. LINK EVERYTHING TOGETHER (On the Root) ---
        SerializedObject so = new SerializedObject(mainScript);
        so.FindProperty("fullScreenMapPanel").objectReferenceValue = mapPanel;
        so.FindProperty("mapCoordinatesText").objectReferenceValue = stampText;
        so.FindProperty("astrolabeInputPanel").objectReferenceValue = inputPanel;
        so.FindProperty("latInputField").objectReferenceValue = latInput;
        so.FindProperty("longInputField").objectReferenceValue = longInput;
        so.FindProperty("submitButton").objectReferenceValue = submitBtn;
        so.FindProperty("closeButton").objectReferenceValue = closeBtn;
        so.FindProperty("feedbackText").objectReferenceValue = fbText;
        so.ApplyModifiedProperties();

        Selection.activeGameObject = rootObj;
        EditorUtility.DisplayDialog("Success", "Astrolabe Puzzle Updated!\n\nThe main script is now safely on the Root Object, preventing hierarchy bugs when locked/unlocked.", "Awesome!");
    }


    private TMP_InputField CreateInputField(Transform parent, string name, Vector2 position, string placeholderText, Color textColor)
    {
        GameObject inputObj = new GameObject(name);
        inputObj.transform.SetParent(parent, false);
        RectTransform rect = inputObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f); rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(300, 80);
        rect.anchoredPosition = position;
        
        Image img = inputObj.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.5f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(inputObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 0); textRect.offsetMax = new Vector2(-10, 0);
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 40;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.Center;

        GameObject phObj = new GameObject("Placeholder");
        phObj.transform.SetParent(inputObj.transform, false);
        RectTransform phRect = phObj.AddComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero; phRect.anchorMax = Vector2.one;
        phRect.offsetMin = new Vector2(10, 0); phRect.offsetMax = new Vector2(-10, 0);
        TextMeshProUGUI phText = phObj.AddComponent<TextMeshProUGUI>();
        phText.text = placeholderText;
        phText.fontSize = 40;
        phText.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
        phText.alignment = TextAlignmentOptions.Center;
        phText.fontStyle = FontStyles.Italic;

        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
        inputField.textComponent = text;
        inputField.placeholder = phText;

        return inputField;
    }
}
