using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PressurePlateGenerator : EditorWindow
{
    [Header("Puzzle Settings")]
    private string puzzleID = "floor_puzzle";
    private int numberOfPlates = 5;

    [Header("Visual Theme (URP)")]
    private Color stoneColor = new Color(0.4f, 0.4f, 0.4f, 1f); // Gray stone
    private Color runeGlowColor = new Color(0.1f, 0.8f, 0.9f, 1f); // Cyan magic glow
    
    [Header("Layout Settings")]
    private Vector3 startPosition = Vector3.zero;
    private float plateSpacing = 1.5f;
    private float plateSize = 1.2f;

    [Header("Cinematic Heights")]
    private float hiddenHeight = -1.5f;
    private float activeHeight = 0f;
    private float pressedHeight = -0.15f;

    [MenuItem("MysteryRooms/Create Pressure Plate Puzzle")]
    public static void ShowWindow()
    {
        PressurePlateGenerator window = GetWindow<PressurePlateGenerator>("Pressure Plate Gen");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Pressure Plate Puzzle Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Generates physical floor tiles that rise from the ground cinematically when unlocked. " +
            "Fully configured for URP with emissive materials and trigger colliders.", 
            MessageType.Info
        );
        GUILayout.Space(10);

        GUILayout.Label("Configuration", EditorStyles.boldLabel);
        puzzleID = EditorGUILayout.TextField("Puzzle ID", puzzleID);
        numberOfPlates = EditorGUILayout.IntSlider("Number of Plates", numberOfPlates, 3, 9);

        GUILayout.Space(10);
        GUILayout.Label("Visual Theme", EditorStyles.boldLabel);
        stoneColor = EditorGUILayout.ColorField("Stone Color", stoneColor);
        runeGlowColor = EditorGUILayout.ColorField("Rune Glow Color", runeGlowColor);

        GUILayout.Space(10);
        GUILayout.Label("Layout & Dimensions", EditorStyles.boldLabel);
        startPosition = EditorGUILayout.Vector3Field("Start Position", startPosition);
        plateSize = EditorGUILayout.FloatField("Plate Size", plateSize);
        plateSpacing = EditorGUILayout.FloatField("Spacing", plateSpacing);

        GUILayout.Space(10);
        GUILayout.Label("Cinematic Heights (Y-Axis)", EditorStyles.boldLabel);
        hiddenHeight = EditorGUILayout.FloatField("Hidden (Locked) Height", hiddenHeight);
        activeHeight = EditorGUILayout.FloatField("Active (Unlocked) Height", activeHeight);
        pressedHeight = EditorGUILayout.FloatField("Pressed Height", pressedHeight);

        GUILayout.Space(20);

        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.8f);
        if (GUILayout.Button("Generate Pressure Plate Puzzle", GUILayout.Height(40)))
        {
            GeneratePuzzle();
        }
        GUI.backgroundColor = Color.white;
    }

    private void GeneratePuzzle()
    {
        // 1. Root Object
        GameObject rootObj = new GameObject($"PressurePlatePuzzle_{puzzleID}");
        rootObj.transform.position = startPosition;

        PressurePlatePuzzle puzzleScript = rootObj.AddComponent<PressurePlatePuzzle>();
        
        // Use SerializedObject to set private fields on the parent script
        SerializedObject so = new SerializedObject(puzzleScript);
        so.FindProperty("puzzleID").stringValue = puzzleID;
        
        SerializedProperty platesListProp = so.FindProperty("physicalPlates");
        platesListProp.ClearArray();

        // 2. Generate URP Materials
        Material inactiveMat = CreateURPMaterial("Plate_Inactive", stoneColor, false);
        Material activeMat = CreateURPMaterial("Plate_Active", stoneColor, true);

        // 3. Create the plates
        for (int i = 0; i < numberOfPlates; i++)
        {
            // Position them in a row (or grid depending on your room shape)
            Vector3 pos = startPosition + new Vector3(i * plateSpacing, 0, 0);
            
            PhysicalPressurePlate plateScript = CreateSinglePlate(rootObj.transform, pos, i + 1, inactiveMat, activeMat);
            
            // Add to the parent puzzle's list
            platesListProp.InsertArrayElementAtIndex(i);
            platesListProp.GetArrayElementAtIndex(i).objectReferenceValue = plateScript;
        }

        so.ApplyModifiedProperties();
        
        Selection.activeGameObject = rootObj;
        EditorUtility.DisplayDialog("Success", "Pressure Plate puzzle generated successfully!\n\nThe plates are currently at 'Hidden' height. They will rise automatically during gameplay when unlocked.", "OK");
    }

    private PhysicalPressurePlate CreateSinglePlate(Transform parent, Vector3 position, int id, Material inactiveMat, Material activeMat)
    {
        // --- A. The Root Object ---
        // This stays still. It acts as the anchor point.
        GameObject plateRoot = new GameObject($"Plate_{id}");
        plateRoot.transform.SetParent(parent);
        plateRoot.transform.position = position;

        // Add the Trigger Collider to the ROOT so it never moves up/down.
        // It stays fixed at floor level so the player can always walk into it.
        BoxCollider trigger = plateRoot.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        // Make the trigger tall so it catches the player even if they jump
        trigger.size = new Vector3(plateSize, 2f, plateSize);
        trigger.center = new Vector3(0, 1f, 0);

        // --- B. The Moving Mesh Object ---
        // This is the child that visually rises and sinks
        GameObject meshObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        meshObj.name = "MovingStoneMesh";
        meshObj.transform.SetParent(plateRoot.transform);
        
        // Start it at the hidden height!
        meshObj.transform.localPosition = new Vector3(0, hiddenHeight, 0);
        meshObj.transform.localScale = new Vector3(plateSize, 0.2f, plateSize);
        
        // Remove the default collider from the moving mesh so the player doesn't trip on it while it moves
        DestroyImmediate(meshObj.GetComponent<Collider>());

        Renderer rend = meshObj.GetComponent<Renderer>();
        rend.material = inactiveMat;

        // --- C. Configure the Script ---
        PhysicalPressurePlate script = plateRoot.AddComponent<PhysicalPressurePlate>();
        script.plateID = id;
        script.movingPlateMesh = meshObj.transform;
        script.plateRenderer = rend;
        
        script.inactiveMaterial = inactiveMat;
        script.activeMaterial = activeMat;
        
        script.hiddenYOffset = hiddenHeight;
        script.activeYOffset = activeHeight;
        script.pressedYOffset = pressedHeight;
        script.riseSpeed = 2f;

        // Setup colors (defaulting to Egyptian Gold for success, Red for failure)
        script.correctGlowColor = new Color(0.85f, 0.65f, 0.13f); 
        script.errorGlowColor = new Color(0.8f, 0.1f, 0.1f);

        // Add an audio source for the grinding stone sound
        AudioSource audio = plateRoot.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 1f; // 3D sound
        audio.maxDistance = 15f;
        script.grindSound = audio;

        return script;
    }

    private Material CreateURPMaterial(string name, Color baseColor, bool enableEmission)
    {
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null) urpShader = Shader.Find("Standard");

        Material mat = new Material(urpShader);
        mat.name = name;
        mat.color = baseColor;
        mat.SetFloat("_Smoothness", 0.1f);

        if (enableEmission)
        {
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            // Set the emission color to the Rune Glow Color
            mat.SetColor("_EmissionColor", runeGlowColor * 1.5f); // Multiply by 1.5 for bloom effect
        }

        return mat;
    }
}
