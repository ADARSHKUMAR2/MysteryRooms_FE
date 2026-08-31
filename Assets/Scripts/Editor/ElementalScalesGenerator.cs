using UnityEngine;
using UnityEditor;
using TMPro;

public class ElementalScalesGenerator : EditorWindow
{
    [Header("Visual Theme")]
    private Color stoneColor = new Color(0.3f, 0.25f, 0.2f, 1f); 
    private Color goldTrimColor = new Color(0.85f, 0.65f, 0.13f, 1f);
    
    [Header("Element Colors (Glow)")]
    private Color fireColor = new Color(0.9f, 0.1f, 0.1f, 1f);
    private Color waterColor = new Color(0.1f, 0.4f, 0.9f, 1f);
    private Color leafColor = new Color(0.1f, 0.8f, 0.2f, 1f);
    private Color sunColor = new Color(0.9f, 0.8f, 0.1f, 1f);
    
    [Header("Placement")]
    private Vector3 position = new Vector3(0, 0, 0);

    [MenuItem("MysteryRooms/Create Elemental Scales Clue")]
    public static void ShowWindow()
    {
        ElementalScalesGenerator window = GetWindow<ElementalScalesGenerator>("Scales Clue Gen");
        window.minSize = new Vector2(400, 450);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Elemental Scales Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Generates 4 physical weighing scales side-by-side. " +
            "Each scale represents an element. The backend will automatically spawn " +
            "iron weights onto these scales to give the player the passcode sequence!", 
            MessageType.Info
        );
        GUILayout.Space(10);

        GUILayout.Label("Visual Theme (URP)", EditorStyles.boldLabel);
        stoneColor = EditorGUILayout.ColorField("Stone/Iron Color", stoneColor);
        goldTrimColor = EditorGUILayout.ColorField("Gold Trim Color", goldTrimColor);

        GUILayout.Space(10);
        GUILayout.Label("Element Emissive Colors", EditorStyles.boldLabel);
        fireColor = EditorGUILayout.ColorField("Fire Color", fireColor);
        waterColor = EditorGUILayout.ColorField("Water Color", waterColor);
        leafColor = EditorGUILayout.ColorField("Leaf Color", leafColor);
        sunColor = EditorGUILayout.ColorField("Sun Color", sunColor);

        GUILayout.Space(10);
        GUILayout.Label("Placement", EditorStyles.boldLabel);
        position = EditorGUILayout.Vector3Field("Start Position", position);

        GUILayout.Space(20);

        GUI.backgroundColor = new Color(0.85f, 0.65f, 0.13f); 
        if (GUILayout.Button("Generate 4 Elemental Scales", GUILayout.Height(40)))
        {
            GenerateScales();
        }
        GUI.backgroundColor = Color.white;
    }

    private void GenerateScales()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        Material stoneMat = new Material(urpLit);
        stoneMat.color = stoneColor;
        stoneMat.SetFloat("_Smoothness", 0.1f);

        Material goldMat = new Material(urpLit);
        goldMat.color = goldTrimColor;
        goldMat.SetFloat("_Metallic", 0.9f);
        goldMat.SetFloat("_Smoothness", 0.7f);

        // 1. Create Root
        GameObject rootObj = new GameObject("ElementalScalesClueGroup");
        rootObj.transform.position = position;

        // 2. Create Iron Weight Prefab (so the script has something to spawn)
        GameObject ironWeightPrefab = CreateIronWeightPrefab(stoneMat);

        // 3. Create the 4 Scales
        Transform firePan = CreateSingleScale(rootObj.transform, "Fire", new Vector3(0, 0, 0), fireColor, urpLit, stoneMat, goldMat);
        Transform waterPan = CreateSingleScale(rootObj.transform, "Water", new Vector3(1.5f, 0, 0), waterColor, urpLit, stoneMat, goldMat);
        Transform leafPan = CreateSingleScale(rootObj.transform, "Leaf", new Vector3(3.0f, 0, 0), leafColor, urpLit, stoneMat, goldMat);
        Transform sunPan = CreateSingleScale(rootObj.transform, "Sun", new Vector3(4.5f, 0, 0), sunColor, urpLit, stoneMat, goldMat);

        // 4. Attach Data Script
        ElementalScalesData dataScript = rootObj.AddComponent<ElementalScalesData>();
        
        SerializedObject so = new SerializedObject(dataScript);
        so.FindProperty("fireScalePan").objectReferenceValue = firePan;
        so.FindProperty("waterScalePan").objectReferenceValue = waterPan;
        so.FindProperty("leafScalePan").objectReferenceValue = leafPan;
        so.FindProperty("sunScalePan").objectReferenceValue = sunPan;
        so.FindProperty("ironWeightPrefab").objectReferenceValue = ironWeightPrefab;
        so.ApplyModifiedProperties();

        Selection.activeGameObject = rootObj;
        EditorUtility.DisplayDialog("Success", "4 Elemental Scales Generated!\n\nMove the group to a table or wall in your room. The CombinationLock script will automatically link to this!", "OK");
    }

    private GameObject CreateIronWeightPrefab(Material stoneMat)
    {
        GameObject weight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        weight.name = "IronWeight";
        weight.transform.localScale = new Vector3(0.2f, 0.15f, 0.2f); // Small puck shape
        weight.GetComponent<Renderer>().material = stoneMat;
        
        // Remove collider so they don't bounce around physically (they are just visual clues)
        DestroyImmediate(weight.GetComponent<Collider>());
        
        // Save as prefab
        string path = "Assets/Prefabs/Puzzles/IronWeight.prefab";
        System.IO.Directory.CreateDirectory("Assets/Prefabs/Puzzles");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(weight, path);
        DestroyImmediate(weight);
        
        return prefab;
    }

    private Transform CreateSingleScale(Transform parent, string elementName, Vector3 localPos, Color glowColor, Shader shader, Material stoneMat, Material goldMat)
    {
        GameObject scaleRoot = new GameObject($"Scale_{elementName}");
        scaleRoot.transform.SetParent(parent);
        scaleRoot.transform.localPosition = localPos;

        // Base
        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseObj.transform.SetParent(scaleRoot.transform);
        baseObj.transform.localPosition = new Vector3(0, 0.05f, 0);
        baseObj.transform.localScale = new Vector3(0.8f, 0.1f, 0.6f);
        baseObj.GetComponent<Renderer>().material = stoneMat;

        // Pillar
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.transform.SetParent(scaleRoot.transform);
        pillar.transform.localPosition = new Vector3(0, 0.5f, 0);
        pillar.transform.localScale = new Vector3(0.1f, 0.4f, 0.1f);
        pillar.GetComponent<Renderer>().material = goldMat;

        // The Bowl/Pan (Where the weights go)
        GameObject pan = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pan.name = "WeightPan";
        pan.transform.SetParent(scaleRoot.transform);
        pan.transform.localPosition = new Vector3(0, 0.9f, 0);
        pan.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
        pan.GetComponent<Renderer>().material = stoneMat;

        // Emissive Rune on the front of the Base
        GameObject runeObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        runeObj.transform.SetParent(scaleRoot.transform);
        runeObj.transform.localPosition = new Vector3(0, 0.05f, -0.31f); // On the front face
        runeObj.transform.localRotation = Quaternion.Euler(0, 0, 0);
        runeObj.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        DestroyImmediate(runeObj.GetComponent<Collider>());

        Material glowMat = new Material(shader);
        glowMat.color = glowColor;
        glowMat.EnableKeyword("_EMISSION");
        glowMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        glowMat.SetColor("_EmissionColor", glowColor * 1.5f);
        runeObj.GetComponent<Renderer>().material = glowMat;

        // Cleanup colliders
        DestroyImmediate(baseObj.GetComponent<Collider>());
        DestroyImmediate(pillar.GetComponent<Collider>());
        DestroyImmediate(pan.GetComponent<Collider>());

        return pan.transform;
    }
}
