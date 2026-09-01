using UnityEngine;
using UnityEditor;
using TMPro;

public class HintMonolithGenerator : EditorWindow
{
    [Header("Fonts")]
    private TMP_FontAsset hintFont;

    [Header("Visual Theme")]
    private Color stoneColor = new Color(0.15f, 0.12f, 0.1f, 1f); // Dark Obsidian
    private Color screenColor = new Color(0.05f, 0.05f, 0.05f, 1f); // Black Glass
    private Color glowingGoldColor = new Color(1f, 0.85f, 0.3f, 1f);
    
    [MenuItem("MysteryRooms/Create Central Oracle (Hint Monolith)")]
    public static void ShowWindow()
    {
        GetWindow<HintMonolithGenerator>("Oracle Gen").Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Central Oracle Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Generates a sleek Obsidian Monolith for the Entrance Hall. " +
            "Players interact with this to receive AI-generated hints on its glowing screen! " +
            "(100% URP Compatible - No Pink Textures!)", 
            MessageType.Info
        );
        GUILayout.Space(10);

        hintFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Hint Font (TMP)", hintFont, typeof(TMP_FontAsset), false);
        glowingGoldColor = EditorGUILayout.ColorField("Golden Glow Color", glowingGoldColor);
        stoneColor = EditorGUILayout.ColorField("Obsidian Color", stoneColor);
        
        GUILayout.Space(20);

        if (hintFont == null)
        {
            EditorGUILayout.HelpBox("⚠️ Please assign a TMP Font Asset for the text.", MessageType.Warning);
        }

        GUI.enabled = hintFont != null;
        GUI.backgroundColor = new Color(0.85f, 0.65f, 0.13f); // Gold button
        if (GUILayout.Button("Generate Oracle Monolith", GUILayout.Height(40)))
        {
            GenerateMonolith();
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
    }

    private void GenerateMonolith()
    {
        // 1. Shaders and Materials (100% URP Compatible!)
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");
        
        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (urpUnlit == null) urpUnlit = Shader.Find("Legacy Shaders/Particles/Additive");

        Material stoneMat = new Material(urpLit);
        stoneMat.color = stoneColor;
        stoneMat.SetFloat("_Smoothness", 0.3f); // Slightly polished obsidian
        stoneMat.SetFloat("_Metallic", 0.2f);

        Material screenMat = new Material(urpLit);
        screenMat.color = screenColor;
        screenMat.SetFloat("_Smoothness", 0.95f); // Highly glassy
        screenMat.SetFloat("_Metallic", 0.8f);
        screenMat.EnableKeyword("_EMISSION");
        screenMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        screenMat.SetColor("_EmissionColor", new Color(0.1f, 0.1f, 0.05f)); // Faint glow when active

        Material particleMat = new Material(urpUnlit);
        if (urpUnlit.name == "Universal Render Pipeline/Particles/Unlit")
        {
            particleMat.SetInt("_Surface", 1); 
            particleMat.SetInt("_Blend", 0);   
            particleMat.SetColor("_BaseColor", glowingGoldColor);
        }

        // 2. The Pillar Structure
        GameObject rootObj = new GameObject("CentralOracle_HintMonolith");
        rootObj.transform.position = new Vector3(0, 1.5f, 0);

        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pillar.name = "ObsidianPillar";
        pillar.transform.SetParent(rootObj.transform);
        pillar.transform.localPosition = Vector3.zero;
        pillar.transform.localScale = new Vector3(1f, 3f, 0.4f);
        
        // Remove Default Collider, we will add a Trigger to the Root
        DestroyImmediate(pillar.GetComponent<Collider>());
        pillar.GetComponent<Renderer>().material = stoneMat;

        GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
        screen.name = "GlassScreen";
        screen.transform.SetParent(rootObj.transform);
        screen.transform.localPosition = new Vector3(0, 0.5f, -0.16f); // Inset slightly
        screen.transform.localScale = new Vector3(0.8f, 1.2f, 0.1f);
        MeshRenderer screenRend = screen.GetComponent<MeshRenderer>();
        screenRend.material = screenMat;
        DestroyImmediate(screen.GetComponent<Collider>());

        // 3. The Text
        GameObject textObj = new GameObject("ScreenText");
        textObj.transform.SetParent(screen.transform);
        textObj.transform.localPosition = new Vector3(0, 0, -0.51f);
        textObj.transform.localScale = new Vector3(0.02f, 0.02f, 1f); // Scale down to fit screen

        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.rectTransform.sizeDelta = new Vector2(40, 50);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        tmp.text = "AWAITING\nCLUE";
        tmp.color = glowingGoldColor;
        tmp.fontSize = 4;
        if (hintFont != null) tmp.font = hintFont;
        tmp.fontSharedMaterial.EnableKeyword("GLOW_ON");

        // 4. Particles (Floating dust from the screen)
        GameObject dustObj = new GameObject("GoldenDust");
        dustObj.transform.SetParent(screen.transform);
        dustObj.transform.localPosition = new Vector3(0, 0, -1f);
        dustObj.transform.localRotation = Quaternion.Euler(0, 180, 0);

        ParticleSystem ps = dustObj.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psRenderer = ps.GetComponent<ParticleSystemRenderer>();
        psRenderer.material = particleMat;

        var main = ps.main;
        main.startLifetime = 2f;
        main.startSpeed = 0.2f;
        main.startSize = 0.08f;
        main.startColor = new Color(glowingGoldColor.r, glowingGoldColor.g, glowingGoldColor.b, 0.8f);
        main.maxParticles = 50;
        
        var emission = ps.emission;
        emission.rateOverTime = 10;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.8f, 1.2f, 0.1f);
        ps.Stop();

        // 5. Audio
        AudioSource audio = rootObj.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 1f;

        // 6. Interaction Collider
        BoxCollider trigger = rootObj.AddComponent<BoxCollider>();
        trigger.size = new Vector3(1.5f, 3f, 1.5f);
        trigger.center = Vector3.zero;

        // 7. Add Script
        HintMonolith script = rootObj.AddComponent<HintMonolith>();
        SerializedObject so = new SerializedObject(script);
        so.FindProperty("screenText").objectReferenceValue = tmp;
        so.FindProperty("dustParticles").objectReferenceValue = ps;
        so.FindProperty("activationSound").objectReferenceValue = audio;
        so.FindProperty("screenRenderer").objectReferenceValue = screenRend;
        so.ApplyModifiedProperties();

        // 8. Link to HintManager automatically!
        HintManager manager = FindObjectOfType<HintManager>();
        if (manager == null)
        {
            GameObject managerObj = GameObject.Find("GameManagers");
            if (managerObj == null) managerObj = new GameObject("GameManagers");
            manager = managerObj.AddComponent<HintManager>();
        }
        
        SerializedObject soManager = new SerializedObject(manager);
        soManager.FindProperty("centralOracle").objectReferenceValue = script;
        soManager.ApplyModifiedProperties();

        Selection.activeGameObject = rootObj;
        EditorUtility.DisplayDialog("Success", "Central Oracle Generated!\n\nIt is completely URP compatible. Place it in your Entrance Hall. The HintManager has been automatically updated to use it!", "Awesome!");
    }
}
