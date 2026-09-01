using UnityEngine;
using UnityEditor;
using TMPro;

public class HintProjectorGenerator : EditorWindow
{
    [Header("Fonts")]
    private TMP_FontAsset hintFont;

    [Header("Visual Theme")]
    private Color glowingGoldColor = new Color(1f, 0.85f, 0.3f, 1f);
    private float spotlightIntensity = 15f;
    private float spotlightAngle = 45f;

    [MenuItem("MysteryRooms/Create Hint Projector System")]
    public static void ShowWindow()
    {
        GetWindow<HintProjectorGenerator>("Hint Gen").Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Beam of Thoth - Hint Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Generates the HintManager and the 3D cinematic projector prefab. " +
            "When players press 'H', a beam of golden light strikes the puzzle " +
            "they are stuck on, and the AI's hint burns into the floor!", 
            MessageType.Info
        );
        GUILayout.Space(10);

        GUILayout.Label("Configuration", EditorStyles.boldLabel);
        hintFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Hint Font (TMP)", hintFont, typeof(TMP_FontAsset), false);
        glowingGoldColor = EditorGUILayout.ColorField("Golden Glow Color", glowingGoldColor);
        
        GUILayout.Space(20);

        if (hintFont == null)
        {
            EditorGUILayout.HelpBox("⚠️ Please assign a TMP Font Asset for the projected text.", MessageType.Warning);
        }

        GUI.enabled = hintFont != null;
        GUI.backgroundColor = new Color(0.85f, 0.65f, 0.13f); // Gold button
        if (GUILayout.Button("Generate Hint System", GUILayout.Height(40)))
        {
            GenerateHintSystem();
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
    }

    private void GenerateHintSystem()
    {
        // 1. Create or Find the GameManagers object
        GameObject managerObj = GameObject.Find("GameManagers");
        if (managerObj == null)
        {
            managerObj = new GameObject("GameManagers");
            managerObj.transform.position = Vector3.zero;
        }

        // Add HintManager if missing
        HintManager hintManager = managerObj.GetComponent<HintManager>();
        if (hintManager == null)
        {
            hintManager = managerObj.AddComponent<HintManager>();
        }

        // 2. Create the Projector Root
        GameObject projectorRoot = new GameObject("BeamOfThoth_HintProjector");
        projectorRoot.transform.position = new Vector3(0, 3f, 0);

        // 3. Create the Spotlight
        GameObject lightObj = new GameObject("DivineSpotlight");
        lightObj.transform.SetParent(projectorRoot.transform);
        lightObj.transform.localPosition = Vector3.zero;
        lightObj.transform.localRotation = Quaternion.Euler(90, 0, 0); // Point straight down

        Light spot = lightObj.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.color = glowingGoldColor;
        spot.intensity = spotlightIntensity;
        spot.spotAngle = spotlightAngle;
        spot.range = 10f;
        spot.shadows = LightShadows.Hard;

        // 4. Create the Projected Text on the floor
        GameObject textObj = new GameObject("ProjectedHintText");
        textObj.transform.SetParent(projectorRoot.transform);
        textObj.transform.localPosition = new Vector3(0, -2.9f, 0); // Put it near the floor
        textObj.transform.localRotation = Quaternion.Euler(90, 0, 0); // Lay flat on floor

        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.rectTransform.sizeDelta = new Vector2(3f, 3f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        tmp.text = "The Gods whisper their secrets here...";
        tmp.color = glowingGoldColor;
        tmp.fontSize = 3;
        if (hintFont != null) tmp.font = hintFont;
        
        // Make text glow if bloom is enabled
        tmp.fontSharedMaterial.EnableKeyword("GLOW_ON");

        // 5. Add Particle Dust Effect in the beam
        GameObject dustObj = new GameObject("GoldenDust");
        dustObj.transform.SetParent(projectorRoot.transform);
        dustObj.transform.localPosition = new Vector3(0, -1.5f, 0);
        dustObj.transform.localRotation = Quaternion.Euler(-90, 0, 0);

        ParticleSystem ps = dustObj.AddComponent<ParticleSystem>();
        
        ParticleSystemRenderer psRenderer = ps.GetComponent<ParticleSystemRenderer>();
        Shader urpParticle = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (urpParticle == null) urpParticle = Shader.Find("Legacy Shaders/Particles/Additive");
        
        Material particleMat = new Material(urpParticle);
        particleMat.SetInt("_Surface", 1); // Transparent
        particleMat.SetInt("_Blend", 0);   // Additive
        particleMat.SetColor("_BaseColor", glowingGoldColor);
        psRenderer.material = particleMat;

        var main = ps.main;
        main.startLifetime = 3f;
        main.startSpeed = 0.5f;
        main.startSize = 0.05f;
        main.startColor = new Color(glowingGoldColor.r, glowingGoldColor.g, glowingGoldColor.b, 0.5f);
        main.maxParticles = 100;
        
        var emission = ps.emission;
        emission.rateOverTime = 20;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = spotlightAngle / 2f;
        shape.radius = 0.1f;

        // 6. Audio
        AudioSource audio = projectorRoot.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 1f;

        // 7. Link to Manager
        SerializedObject so = new SerializedObject(hintManager);
        so.FindProperty("hintProjectorRoot").objectReferenceValue = projectorRoot;
        so.FindProperty("hintText").objectReferenceValue = tmp;
        so.FindProperty("hintSpotlight").objectReferenceValue = spot;
        so.FindProperty("hintAudio").objectReferenceValue = audio;
        so.ApplyModifiedProperties();

        // 8. Turn it off to start
        projectorRoot.SetActive(false);

        Selection.activeGameObject = managerObj;
        EditorUtility.DisplayDialog("Success", "Hint System Generated!\n\nPress 'H' in-game near an unlocked puzzle to test it!", "OK");
    }
}
