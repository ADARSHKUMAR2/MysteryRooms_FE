using UnityEngine;
using UnityEditor;
using TMPro;

public class WallInscriptionGenerator : EditorWindow
{
    [Header("Fonts")]
    private TMP_FontAsset hieroglyphFont;
    private TMP_FontAsset englishFont;

    [Header("Visual Theme")]
    private Color carvedStoneColor = new Color(0.15f, 0.12f, 0.1f, 0.9f);
    private Color glowingGoldColor = new Color(1f, 0.85f, 0.3f, 1f);

    [Header("Placement")]
    private Vector3 position = new Vector3(0, 2f, 0);
    private Vector2 textSize = new Vector2(4f, 3f);
    private string clueID = "pressure_plate_clue";

    [MenuItem("MysteryRooms/Create Wall Inscription Clue")]
    public static void ShowWindow()
    {
        WallInscriptionGenerator window = GetWindow<WallInscriptionGenerator>("Wall Inscription Gen");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Wall Inscription Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Generates a beautiful, world-space 3D text carving that cross-fades " +
            "into a glowing English translation when the player interacts with it.", 
            MessageType.Info
        );
        GUILayout.Space(10);

        GUILayout.Label("Configuration", EditorStyles.boldLabel);
        clueID = EditorGUILayout.TextField("Clue ID (Optional)", clueID);

        GUILayout.Space(10);
        GUILayout.Label("Fonts (TMP Assets)", EditorStyles.boldLabel);
        hieroglyphFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Hieroglyph Font", hieroglyphFont, typeof(TMP_FontAsset), false);
        englishFont = (TMP_FontAsset)EditorGUILayout.ObjectField("English/Readable Font", englishFont, typeof(TMP_FontAsset), false);

        GUILayout.Space(10);
        GUILayout.Label("Visual Theme", EditorStyles.boldLabel);
        carvedStoneColor = EditorGUILayout.ColorField("Carved Stone Color", carvedStoneColor);
        glowingGoldColor = EditorGUILayout.ColorField("Glowing Gold Color", glowingGoldColor);

        GUILayout.Space(10);
        GUILayout.Label("Placement & Dimensions", EditorStyles.boldLabel);
        position = EditorGUILayout.Vector3Field("Position", position);
        textSize = EditorGUILayout.Vector2Field("Text Area Size", textSize);

        GUILayout.Space(20);

        bool canGenerate = englishFont != null;

        if (!canGenerate)
        {
            EditorGUILayout.HelpBox("⚠️ Please assign at least the English TMP Font Asset.", MessageType.Warning);
        }

        GUI.enabled = canGenerate;
        GUI.backgroundColor = new Color(0.85f, 0.65f, 0.13f); // Gold button
        if (GUILayout.Button("Generate Wall Inscription", GUILayout.Height(40)))
        {
            GenerateInscription();
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
    }

    private void GenerateInscription()
    {
        // 1. Root Object
        GameObject rootObj = new GameObject($"WallInscription_{clueID}");
        rootObj.transform.position = position;

        // 2. Add the BoxCollider for Interaction
        BoxCollider collider = rootObj.AddComponent<BoxCollider>();
        collider.size = new Vector3(textSize.x, textSize.y, 1f);
        collider.center = Vector3.zero;

        // 3. Add Audio Source
        AudioSource audio = rootObj.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 1f;
        audio.maxDistance = 10f;

        // 4. Create the Hieroglyph Text
        GameObject hieroObj = new GameObject("Hieroglyph_Text");
        hieroObj.transform.SetParent(rootObj.transform);
        hieroObj.transform.localPosition = new Vector3(0, 0, -0.01f);
        
        TextMeshPro hieroTMP = hieroObj.AddComponent<TextMeshPro>();
        hieroTMP.rectTransform.sizeDelta = textSize;
        hieroTMP.alignment = TextAlignmentOptions.Center;
        hieroTMP.enableWordWrapping = true;
        hieroTMP.text = "This is ancient carved text.\nThe backend will replace this with the generated poem.";
        hieroTMP.color = carvedStoneColor;
        hieroTMP.fontSize = 4;
        
        if (hieroglyphFont != null) hieroTMP.font = hieroglyphFont;
        else hieroTMP.font = englishFont; // Fallback

        // 5. Create the English Text
        GameObject englishObj = new GameObject("English_Text");
        englishObj.transform.SetParent(rootObj.transform);
        englishObj.transform.localPosition = new Vector3(0, 0, -0.02f); // Slightly in front of hieroglyphs
        
        TextMeshPro englishTMP = englishObj.AddComponent<TextMeshPro>();
        englishTMP.rectTransform.sizeDelta = textSize;
        englishTMP.alignment = TextAlignmentOptions.Center;
        englishTMP.enableWordWrapping = true;
        englishTMP.text = "This is ancient carved text.\nThe backend will replace this with the generated poem.";
        englishTMP.color = new Color(glowingGoldColor.r, glowingGoldColor.g, glowingGoldColor.b, 0f); // Start hidden
        englishTMP.fontSize = 4;
        englishTMP.font = englishFont;
        englishObj.SetActive(false);

        // 6. Setup the Particle System
        GameObject particleObj = new GameObject("MagicParticles");
        particleObj.transform.SetParent(rootObj.transform);
        particleObj.transform.localPosition = Vector3.zero;
        
        ParticleSystem particles = particleObj.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startLifetime = 2f;
        main.startSpeed = 0.5f;
        main.startSize = 0.05f;
        main.startColor = glowingGoldColor;
        main.maxParticles = 50;
        
        var emission = particles.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0, 30) }); // Burst on translate
        
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(textSize.x, textSize.y, 0.1f);
        
        particles.Stop();

        // 7. Add and configure the script
        WallInscription script = rootObj.AddComponent<WallInscription>();
        SerializedObject so = new SerializedObject(script);
        so.FindProperty("hieroglyphText").objectReferenceValue = hieroTMP;
        so.FindProperty("englishText").objectReferenceValue = englishTMP;
        so.FindProperty("magicDustParticles").objectReferenceValue = particles;
        so.FindProperty("glowingGold").colorValue = glowingGoldColor;
        so.ApplyModifiedProperties();

        // Finish
        Selection.activeGameObject = rootObj;
        EditorUtility.DisplayDialog("Success", "Wall Inscription generated!\n\nPlace it flat against a stone wall in your tomb.", "Excellent!");
    }
}
