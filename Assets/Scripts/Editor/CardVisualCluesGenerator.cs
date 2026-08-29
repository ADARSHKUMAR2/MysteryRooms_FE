using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class CardVisualCluesGenerator : EditorWindow
{
    [Header("Visual Theme")]
    private Color stoneColor = new Color(0.6f, 0.55f, 0.45f, 1f); // Sandstone
    private Color goldColor = new Color(0.85f, 0.65f, 0.13f, 1f);  // Gold Border
    private Color darkEngravingColor = new Color(0.1f, 0.08f, 0.05f, 0.9f);

    [Header("Number Sprites (1 to 4)")]
    private Sprite num1Sprite;
    private Sprite num2Sprite;
    private Sprite num3Sprite;
    private Sprite num4Sprite;

    [Header("Suit Sprites")]
    private Sprite spadesSprite;
    private Sprite heartsSprite;
    private Sprite diamondsSprite;
    private Sprite clubsSprite;

    [Header("Placement")]
    private Vector3 startPosition = new Vector3(0, 1.5f, 0);
    private float spacing = 1.5f;

    [MenuItem("MysteryRooms/Create Card Visual Clues (Plaques)")]
    public static void ShowWindow()
    {
        CardVisualCluesGenerator window = GetWindow<CardVisualCluesGenerator>("Card Clues Generator");
        window.minSize = new Vector2(400, 650);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Card Visual Clues Generator (URP)", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Generates 4 beautiful URP-compatible stone plaques for the visual hints.", 
            MessageType.Info
        );
        GUILayout.Space(10);

        GUILayout.Label("Custom Number Sprites (1-4)", EditorStyles.boldLabel);
        num1Sprite = (Sprite)EditorGUILayout.ObjectField("Number 1 Sprite", num1Sprite, typeof(Sprite), false);
        num2Sprite = (Sprite)EditorGUILayout.ObjectField("Number 2 Sprite", num2Sprite, typeof(Sprite), false);
        num3Sprite = (Sprite)EditorGUILayout.ObjectField("Number 3 Sprite", num3Sprite, typeof(Sprite), false);
        num4Sprite = (Sprite)EditorGUILayout.ObjectField("Number 4 Sprite", num4Sprite, typeof(Sprite), false);

        GUILayout.Space(10);
        GUILayout.Label("Suit Icons", EditorStyles.boldLabel);
        spadesSprite = (Sprite)EditorGUILayout.ObjectField("Spades Sprite", spadesSprite, typeof(Sprite), false);
        heartsSprite = (Sprite)EditorGUILayout.ObjectField("Hearts Sprite", heartsSprite, typeof(Sprite), false);
        diamondsSprite = (Sprite)EditorGUILayout.ObjectField("Diamonds Sprite", diamondsSprite, typeof(Sprite), false);
        clubsSprite = (Sprite)EditorGUILayout.ObjectField("Clubs Sprite", clubsSprite, typeof(Sprite), false);

        GUILayout.Space(10);
        GUILayout.Label("Placement", EditorStyles.boldLabel);
        startPosition = EditorGUILayout.Vector3Field("Start Position", startPosition);
        spacing = EditorGUILayout.FloatField("Spacing Between Plaques", spacing);

        GUILayout.Space(20);

        bool canGenerate = spadesSprite != null && heartsSprite != null && diamondsSprite != null && clubsSprite != null 
                           && num1Sprite != null && num2Sprite != null && num3Sprite != null && num4Sprite != null;

        if (!canGenerate)
        {
            EditorGUILayout.HelpBox("⚠️ Please assign ALL 4 number sprites AND all 4 suit sprites.", MessageType.Warning);
        }

        GUI.enabled = canGenerate;
        GUI.backgroundColor = goldColor;
        if (GUILayout.Button("Generate 4 Visual Clue Plaques", GUILayout.Height(40)))
        {
            GeneratePlaques();
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
    }

    private void GeneratePlaques()
    {
        // Delete previous group if it exists to avoid clutter
        GameObject oldGroup = GameObject.Find("CardVisualClues_Group");
        if (oldGroup != null) DestroyImmediate(oldGroup);

        GameObject rootObj = new GameObject("CardVisualClues_Group");
        rootObj.transform.position = startPosition;

        CardVisualClue[] generatedClues = new CardVisualClue[4];

        for (int i = 0; i < 4; i++)
        {
            Vector3 pos = startPosition + new Vector3(i * spacing, 0, 0);
            generatedClues[i] = CreateSinglePlaque(rootObj.transform, pos, i + 1);
        }

        CardDeckRiddlePuzzle puzzle = FindObjectOfType<CardDeckRiddlePuzzle>();
        if (puzzle != null)
        {
            SerializedObject serializedPuzzle = new SerializedObject(puzzle);
            SerializedProperty visualCluesProp = serializedPuzzle.FindProperty("visualClues");
            
            visualCluesProp.ClearArray();
            for (int i = 0; i < 4; i++)
            {
                visualCluesProp.InsertArrayElementAtIndex(i);
                visualCluesProp.GetArrayElementAtIndex(i).objectReferenceValue = generatedClues[i];
            }
            serializedPuzzle.ApplyModifiedProperties();
        }

        Selection.activeGameObject = rootObj;
        EditorUtility.DisplayDialog("Success", "Generated 4 beautiful URP Plaques!", "Awesome!");
    }

    private CardVisualClue CreateSinglePlaque(Transform parent, Vector3 position, int number)
    {
        // --- 1. Base 3D Structure (The Stone Plaque) ---
        GameObject plaqueRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plaqueRoot.name = $"VisualClue_Plaque_{number}";
        plaqueRoot.transform.SetParent(parent);
        plaqueRoot.transform.position = position;
        
        // Give it a slightly tilted, elegant shape
        plaqueRoot.transform.localScale = new Vector3(0.8f, 1.2f, 0.15f);
        plaqueRoot.transform.localRotation = Quaternion.Euler(-10f, 0, 0); 
        
        // URP MATERAL FIX: Use URP Lit Shader
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null) urpShader = Shader.Find("Standard"); // Fallback just in case

        Material stoneMat = new Material(urpShader);
        stoneMat.color = stoneColor;
        stoneMat.SetFloat("_Smoothness", 0.1f); // URP uses Smoothness, not Glossiness
        stoneMat.SetFloat("_Metallic", 0.0f);
        plaqueRoot.GetComponent<Renderer>().material = stoneMat;

        // --- 1.5. Golden Border Frame ---
        GameObject borderFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        borderFrame.name = "GoldFrame";
        borderFrame.transform.SetParent(plaqueRoot.transform);
        borderFrame.transform.localPosition = new Vector3(0, 0, -0.1f);
        borderFrame.transform.localScale = new Vector3(1.05f, 1.05f, 0.8f); // Slightly larger than stone
        DestroyImmediate(borderFrame.GetComponent<Collider>());

        Material goldMat = new Material(urpShader);
        goldMat.color = goldColor;
        goldMat.SetFloat("_Metallic", 0.9f);
        goldMat.SetFloat("_Smoothness", 0.8f);
        borderFrame.GetComponent<Renderer>().material = goldMat;

        // --- 2. Canvas for the UI Elements ---
        GameObject canvasObj = new GameObject("ClueCanvas");
        canvasObj.transform.SetParent(plaqueRoot.transform);
        canvasObj.transform.localPosition = new Vector3(0, 0, -0.52f); // Pop out past the frame
        canvasObj.transform.localRotation = Quaternion.identity;

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(700, 1100); 
        canvasRect.localScale = new Vector3(0.001f, 0.001f, 1f); 

        // --- 3. Dark Engraving Background (Inset look) ---
        GameObject insetObj = new GameObject("InsetBackground");
        insetObj.transform.SetParent(canvasObj.transform);
        RectTransform insetRect = insetObj.AddComponent<RectTransform>();
        insetRect.anchorMin = new Vector2(0.05f, 0.05f);
        insetRect.anchorMax = new Vector2(0.95f, 0.95f);
        insetRect.offsetMin = Vector2.zero;
        insetRect.offsetMax = Vector2.zero;
        
        Image insetImg = insetObj.AddComponent<Image>();
        insetImg.color = darkEngravingColor;

        // Subtle Glow behind the images
        GameObject glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(insetObj.transform);
        RectTransform glowRect = glowObj.AddComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero; glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = glowRect.offsetMax = Vector2.zero;
        Image glowImg = glowObj.AddComponent<Image>();
        glowImg.color = new Color(0.85f, 0.65f, 0.13f, 0.15f); // 15% opacity gold

        // --- 4. The Suit Image (Bottom Half) ---
        GameObject suitObj = new GameObject("SuitIcon");
        suitObj.transform.SetParent(insetObj.transform);
        RectTransform suitRect = suitObj.AddComponent<RectTransform>();
        suitRect.anchorMin = new Vector2(0.5f, 0.25f);
        suitRect.anchorMax = new Vector2(0.5f, 0.25f);
        suitRect.pivot = new Vector2(0.5f, 0.5f);
        suitRect.anchoredPosition = new Vector2(0, 50);
        suitRect.sizeDelta = new Vector2(350, 350);

        Image suitImg = suitObj.AddComponent<Image>();
        suitImg.sprite = spadesSprite; 
        suitImg.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);

        // --- 5. The Number Image (Top Half) ---
        GameObject numberObj = new GameObject("NumberIcon");
        numberObj.transform.SetParent(insetObj.transform);
        RectTransform numRect = numberObj.AddComponent<RectTransform>();
        numRect.anchorMin = new Vector2(0.5f, 0.75f);
        numRect.anchorMax = new Vector2(0.5f, 0.75f);
        numRect.pivot = new Vector2(0.5f, 0.5f);
        numRect.anchoredPosition = new Vector2(0, -50);
        numRect.sizeDelta = new Vector2(300, 300);

        Image numImg = numberObj.AddComponent<Image>();
        numImg.sprite = number == 1 ? num1Sprite : number == 2 ? num2Sprite : number == 3 ? num3Sprite : num4Sprite;
        numImg.color = Color.white;

        // --- 6. Add and Configure the Script ---
        CardVisualClue script = plaqueRoot.AddComponent<CardVisualClue>();
        
        SerializedObject so = new SerializedObject(script);
        
        so.FindProperty("suitImage").objectReferenceValue = suitImg;
        so.FindProperty("numberImage").objectReferenceValue = numImg;
        
        SerializedProperty numSpritesProp = so.FindProperty("numberSprites");
        numSpritesProp.ClearArray();
        numSpritesProp.InsertArrayElementAtIndex(0);
        numSpritesProp.GetArrayElementAtIndex(0).objectReferenceValue = num1Sprite;
        numSpritesProp.InsertArrayElementAtIndex(1);
        numSpritesProp.GetArrayElementAtIndex(1).objectReferenceValue = num2Sprite;
        numSpritesProp.InsertArrayElementAtIndex(2);
        numSpritesProp.GetArrayElementAtIndex(2).objectReferenceValue = num3Sprite;
        numSpritesProp.InsertArrayElementAtIndex(3);
        numSpritesProp.GetArrayElementAtIndex(3).objectReferenceValue = num4Sprite;

        so.FindProperty("spadesSprite").objectReferenceValue = spadesSprite;
        so.FindProperty("heartsSprite").objectReferenceValue = heartsSprite;
        so.FindProperty("diamondsSprite").objectReferenceValue = diamondsSprite;
        so.FindProperty("clubsSprite").objectReferenceValue = clubsSprite;
        
        so.ApplyModifiedProperties();

        return script;
    }
}
