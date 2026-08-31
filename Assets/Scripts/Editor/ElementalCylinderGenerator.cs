using UnityEngine;
using UnityEditor;
using TMPro;

public class ElementalCylinderGenerator : EditorWindow
{
    [Header("Visual Theme")]
    private Color stoneColor = new Color(0.3f, 0.25f, 0.2f, 1f); 
    private Color goldTrimColor = new Color(0.85f, 0.65f, 0.13f, 1f);
    
    [Header("Element Colors (Gems)")]
    private Color fireColor = new Color(1f, 0.05f, 0.1f, 0.8f);  // Ruby (Alpha < 1 for transparency)
    private Color waterColor = new Color(0.1f, 0.4f, 1f, 0.8f);  // Sapphire
    private Color leafColor = new Color(0.1f, 0.9f, 0.2f, 0.8f); // Emerald
    private Color sunColor = new Color(1f, 0.8f, 0.05f, 0.8f);   // Topaz
    
    [Header("Placement")]
    private Vector3 position = new Vector3(0, 0, 0);

    [MenuItem("MysteryRooms/Create Elemental Cylinder Clue")]
    public static void ShowWindow()
    {
        ElementalCylinderGenerator window = GetWindow<ElementalCylinderGenerator>("Cylinder Clue Gen");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Elemental Gems Cylinder Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Generates faceted, glass-like glowing gems on a stone cylinder.", 
            MessageType.Info
        );
        GUILayout.Space(10);

        GUILayout.Label("Visual Theme (URP)", EditorStyles.boldLabel);
        stoneColor = EditorGUILayout.ColorField("Stone Color", stoneColor);
        goldTrimColor = EditorGUILayout.ColorField("Gold Trim Color", goldTrimColor);

        GUILayout.Space(10);
        GUILayout.Label("Gem Emissive Colors", EditorStyles.boldLabel);
        fireColor = EditorGUILayout.ColorField("Fire (Ruby)", fireColor);
        waterColor = EditorGUILayout.ColorField("Water (Sapphire)", waterColor);
        leafColor = EditorGUILayout.ColorField("Leaf (Emerald)", leafColor);
        sunColor = EditorGUILayout.ColorField("Sun (Topaz)", sunColor);

        GUILayout.Space(10);
        GUILayout.Label("Placement", EditorStyles.boldLabel);
        position = EditorGUILayout.Vector3Field("Position", position);

        GUILayout.Space(20);

        GUI.backgroundColor = new Color(0.85f, 0.65f, 0.13f); 
        if (GUILayout.Button("Generate Elemental Gems Cylinder", GUILayout.Height(40)))
        {
            GenerateCylinder();
        }
        GUI.backgroundColor = Color.white;
    }

    private void GenerateCylinder()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        Material stoneMat = new Material(urpLit);
        stoneMat.color = stoneColor;
        stoneMat.SetFloat("_Smoothness", 0.1f);

        Material goldMat = new Material(urpLit);
        goldMat.color = goldTrimColor;
        goldMat.SetFloat("_Metallic", 0.95f);
        goldMat.SetFloat("_Smoothness", 0.8f);

        GameObject rootObj = new GameObject("ElementalCylinderClue");
        rootObj.transform.position = position;

        GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pedestal.name = "StonePedestal";
        pedestal.transform.SetParent(rootObj.transform);
        pedestal.transform.localPosition = new Vector3(0, 0.5f, 0);
        pedestal.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f);
        pedestal.GetComponent<Renderer>().material = stoneMat;

        GameObject rotator = new GameObject("RotatingCylinderGroup");
        rotator.transform.SetParent(rootObj.transform);
        rotator.transform.localPosition = new Vector3(0, 1.4f, 0);
        
        ContinuousRotation rotScript = rotator.AddComponent<ContinuousRotation>();
        rotScript.rotationSpeed = new Vector3(0, 15f, 0); 
        rotScript.enableFloating = true;
        rotScript.floatAmplitude = 0.05f;
        rotScript.floatFrequency = 0.5f;

        GameObject mainCylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mainCylinder.name = "CylinderMesh";
        mainCylinder.transform.SetParent(rotator.transform);
        mainCylinder.transform.localPosition = Vector3.zero;
        mainCylinder.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);
        mainCylinder.GetComponent<Renderer>().material = stoneMat;

        CreateGoldTrim(rotator.transform, new Vector3(0, 0.42f, 0), goldMat);
        CreateGoldTrim(rotator.transform, new Vector3(0, -0.42f, 0), goldMat);

        // CREATE GLASS GEM MATERIALS
        Material fireMat = CreateGlassGemMaterial("Ruby", fireColor, urpLit);
        Material waterMat = CreateGlassGemMaterial("Sapphire", waterColor, urpLit);
        Material leafMat = CreateGlassGemMaterial("Emerald", leafColor, urpLit);
        Material sunMat = CreateGlassGemMaterial("Topaz", sunColor, urpLit);

        TextMeshPro fireText = CreateFacetedGem(rotator.transform, "Fire", 0f, fireColor, fireMat, goldMat);
        TextMeshPro waterText = CreateFacetedGem(rotator.transform, "Water", 90f, waterColor, waterMat, goldMat);
        TextMeshPro leafText = CreateFacetedGem(rotator.transform, "Leaf", 180f, leafColor, leafMat, goldMat);
        TextMeshPro sunText = CreateFacetedGem(rotator.transform, "Sun", 270f, sunColor, sunMat, goldMat);

        ElementalCylinderData dataScript = rootObj.AddComponent<ElementalCylinderData>();
        SerializedObject so = new SerializedObject(dataScript);
        so.FindProperty("fireText").objectReferenceValue = fireText;
        so.FindProperty("waterText").objectReferenceValue = waterText;
        so.FindProperty("leafText").objectReferenceValue = leafText;
        so.FindProperty("sunText").objectReferenceValue = sunText;
        so.ApplyModifiedProperties();

        Selection.activeGameObject = rootObj;
    }

    private void CreateGoldTrim(Transform parent, Vector3 localPos, Material goldMat)
    {
        GameObject trim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trim.name = "GoldTrim";
        trim.transform.SetParent(parent);
        trim.transform.localPosition = localPos;
        trim.transform.localScale = new Vector3(0.55f, 0.02f, 0.55f);
        DestroyImmediate(trim.GetComponent<Collider>());
        trim.GetComponent<Renderer>().material = goldMat;
    }

    private Material CreateGlassGemMaterial(string name, Color color, Shader shader)
    {
        Material mat = new Material(shader);
        mat.name = $"GlassGem_{name}";
        
        // 1. Make it Transparent
        mat.SetFloat("_Surface", 1); // 1 = Transparent in URP
        mat.SetFloat("_Blend", 0); // Alpha
        
        // Ensure standard transparent render queue
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.SetOverrideTag("RenderType", "Transparent");

        // 2. Base Color with Alpha
        mat.SetColor("_BaseColor", color);

        // 3. Make it shiny glass!
        mat.SetFloat("_Metallic", 0.2f);
        mat.SetFloat("_Smoothness", 0.95f); // Very high gloss
        
        // 4. Inner Glow
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        mat.SetColor("_EmissionColor", color * 2.0f); // Bright glow from inside

        return mat;
    }

    private TextMeshPro CreateFacetedGem(Transform parent, string elementName, float rotationY, Color textColor, Material gemMat, Material goldMat)
    {
        GameObject gemGroup = new GameObject($"Gem_{elementName}");
        gemGroup.transform.SetParent(parent);
        
        float radius = 0.32f;
        float angleRad = rotationY * Mathf.Deg2Rad;
        Vector3 gemPos = new Vector3(Mathf.Sin(angleRad) * radius, 0.45f, Mathf.Cos(angleRad) * radius);
        
        gemGroup.transform.localPosition = gemPos;
        gemGroup.transform.localRotation = Quaternion.Euler(0, rotationY, 0);

        // --- 1. Golden Socket (Holds the gem) ---
        GameObject socket = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        socket.transform.SetParent(gemGroup.transform);
        socket.transform.localPosition = new Vector3(0, -0.05f, 0);
        socket.transform.localScale = new Vector3(0.22f, 0.05f, 0.22f);
        DestroyImmediate(socket.GetComponent<Collider>());
        socket.GetComponent<Renderer>().material = goldMat;

        // --- 2. The Faceted Gem (A rotated cube looks like a diamond) ---
        GameObject gemMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gemMesh.transform.SetParent(gemGroup.transform);
        gemMesh.transform.localPosition = new Vector3(0, 0.05f, 0);
        
        // Rotate 45 degrees on multiple axes to make the corners point up like a diamond!
        gemMesh.transform.localRotation = Quaternion.Euler(45f, 45f, 0f); 
        
        // Scale it to look like a crystal shard
        gemMesh.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f); 
        
        DestroyImmediate(gemMesh.GetComponent<Collider>());
        gemMesh.GetComponent<Renderer>().material = gemMat;

        // --- 3. The Hovering Number Text ---
        GameObject textObj = new GameObject("NumberText");
        textObj.transform.SetParent(gemGroup.transform);
        textObj.transform.localPosition = new Vector3(0, 0.25f, 0); // Hovering above the sharp gem
        textObj.transform.localRotation = Quaternion.Euler(30f, 0, 0); 

        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.rectTransform.sizeDelta = new Vector2(0.5f, 0.5f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = "?"; 
        tmp.fontSize = 2.5f; 
        tmp.color = Color.white; 
        
        tmp.fontSharedMaterial.EnableKeyword("OUTLINE_ON");
        tmp.outlineColor = new Color(0, 0, 0, 0.9f);
        tmp.outlineWidth = 0.25f;

        return tmp;
    }
}
