using UnityEngine;
using UnityEditor;

public class LightMirrorGenerator : EditorWindow
{
    private string puzzleID = "east_light";
    private Color beamColor = new Color(0, 1, 1, 1); // Cyan
    private int numberOfMirrors = 3;

    [MenuItem("MysteryRooms/Create Light Mirror Puzzle")]
    public static void ShowWindow()
    {
        GetWindow<LightMirrorGenerator>("Light Mirror Gen").Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Light Mirror Puzzle Generator (URP)", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Generates a laser reflection puzzle with URP-compatible materials. " +
            "The source crystal emits a beam that must bounce off rotating mirrors to hit a target crystal.", 
            MessageType.Info
        );
        GUILayout.Space(10);

        puzzleID = EditorGUILayout.TextField("Puzzle ID", puzzleID);
        beamColor = EditorGUILayout.ColorField("Beam Color", beamColor);
        numberOfMirrors = EditorGUILayout.IntSlider("Number of Mirrors", numberOfMirrors, 1, 10);

        GUILayout.Space(20);

        GUI.backgroundColor = new Color(0, 0.8f, 1f); // Cyan button
        if (GUILayout.Button("Generate URP Puzzle", GUILayout.Height(40)))
        {
            GeneratePuzzle();
        }
        GUI.backgroundColor = Color.white;
    }

    private void GeneratePuzzle()
    {
        // 1. Ensure Layers Exist
        CreateLayer("Mirror");
        CreateLayer("Obstacle");

        int mirrorLayer = LayerMask.NameToLayer("Mirror");
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");

        // 2. Safely Get URP Shaders
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard"); // Absolute fallback

        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (urpUnlit == null) urpUnlit = Shader.Find("Legacy Shaders/Particles/Additive");

        // 3. Create URP Materials
        Material laserMat = new Material(urpUnlit);
        if (urpUnlit.name == "Universal Render Pipeline/Particles/Unlit")
        {
            laserMat.SetInt("_Surface", 1); // Transparent
            laserMat.SetInt("_Blend", 0); // Additive
            laserMat.SetColor("_BaseColor", beamColor * 3f); // Glow multiplier
        }
        
        Material stoneMat = new Material(urpLit);
        stoneMat.color = new Color(0.3f, 0.25f, 0.2f); // Dark Sandstone
        stoneMat.SetFloat("_Smoothness", 0.1f); // Not shiny

        Material goldMat = new Material(urpLit);
        goldMat.color = new Color(0.85f, 0.65f, 0.13f);
        goldMat.SetFloat("_Metallic", 0.9f);
        goldMat.SetFloat("_Smoothness", 0.7f);

        Material glassMat = new Material(urpLit);
        glassMat.color = new Color(0.8f, 0.95f, 1f);
        glassMat.SetFloat("_Metallic", 0.8f);
        glassMat.SetFloat("_Smoothness", 0.95f); // Very shiny mirror

        Material targetMat = new Material(urpLit);
        targetMat.color = Color.yellow;
        targetMat.EnableKeyword("_EMISSION");
        targetMat.SetColor("_EmissionColor", new Color(0.2f, 0.2f, 0)); // Dim glow until hit
        targetMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        // Save materials (Optional but clean)
        string matPath = "Assets/Materials/Puzzles";
        System.IO.Directory.CreateDirectory(matPath);
        
        // 4. Create Root Object
        GameObject rootObj = new GameObject($"MirrorPuzzle_{puzzleID}");
        rootObj.transform.position = Vector3.zero;

        // 5. Create Source
        GameObject sourceRoot = new GameObject("LightBeamSource");
        sourceRoot.transform.SetParent(rootObj.transform);
        sourceRoot.transform.position = new Vector3(-5, 0, 0);

        GameObject sourcePedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        sourcePedestal.transform.SetParent(sourceRoot.transform);
        sourcePedestal.transform.localPosition = new Vector3(0, 0.5f, 0);
        sourcePedestal.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f);
        sourcePedestal.GetComponent<Renderer>().material = stoneMat;

        GameObject sourceCrystal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sourceCrystal.transform.SetParent(sourceRoot.transform);
        sourceCrystal.transform.localPosition = new Vector3(0, 1.2f, 0);
        sourceCrystal.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        sourceCrystal.GetComponent<Renderer>().material = goldMat;
        
        LightPuzzle sourceScript = sourceRoot.AddComponent<LightPuzzle>();
        sourceScript.puzzleID = puzzleID;
        sourceScript.reflectiveLayer = 1 << mirrorLayer;
        sourceScript.obstacleLayer = 1 << obstacleLayer;

        GameObject emitterObj = new GameObject("LaserEmitter");
        emitterObj.transform.SetParent(sourceCrystal.transform);
        emitterObj.transform.localPosition = Vector3.zero;
        emitterObj.transform.localRotation = Quaternion.Euler(0, 90, 0); // Pointing right

        LineRenderer lr = emitterObj.AddComponent<LineRenderer>();
        lr.material = laserMat;
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;
        lr.startColor = beamColor;
        lr.endColor = beamColor;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.numCapVertices = 5; 
        lr.numCornerVertices = 5; 
        
        SerializedObject so = new SerializedObject(sourceScript);
        so.FindProperty("lineRenderer").objectReferenceValue = lr;
        so.FindProperty("emissionPoint").objectReferenceValue = emitterObj.transform;
        so.ApplyModifiedProperties();

        // 6. Create Target
        GameObject targetObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        targetObj.name = "TargetCrystal";
        targetObj.transform.SetParent(rootObj.transform);
        targetObj.transform.position = new Vector3(5, 1.5f, 0);
        targetObj.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        targetObj.layer = obstacleLayer;
        targetObj.GetComponent<Renderer>().material = targetMat;
        
        sourceScript.targetCrystal = targetObj.transform;

        // 7. Create Mirrors
        for (int i = 0; i < numberOfMirrors; i++)
        {
            GameObject mirrorRoot = new GameObject($"MirrorPedestal_{i}");
            mirrorRoot.transform.SetParent(rootObj.transform);
            mirrorRoot.transform.position = new Vector3(-2 + (i * 2), 0, 2);

            // Pedestal base
            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.transform.SetParent(mirrorRoot.transform);
            pedestal.transform.localPosition = new Vector3(0, 0.5f, 0);
            pedestal.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            DestroyImmediate(pedestal.GetComponent<Collider>()); 
            pedestal.GetComponent<Renderer>().material = stoneMat;

            // Mirror Frame (Gold)
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.transform.SetParent(mirrorRoot.transform);
            frame.transform.localPosition = new Vector3(0, 1.3f, 0);
            frame.transform.localScale = new Vector3(0.85f, 1.05f, 0.15f);
            DestroyImmediate(frame.GetComponent<Collider>()); 
            frame.GetComponent<Renderer>().material = goldMat;

            // The actual reflective glass
            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.name = "GlassPane";
            glass.transform.SetParent(mirrorRoot.transform);
            glass.transform.localPosition = new Vector3(0, 1.3f, 0.08f); // Slightly protruding from frame
            glass.transform.localScale = new Vector3(0.75f, 0.95f, 0.05f); 
            glass.layer = mirrorLayer; // CRITICAL

            glass.GetComponent<Renderer>().material = glassMat;

            // Add Interaction Script to Root
            BoxCollider interactionCollider = mirrorRoot.AddComponent<BoxCollider>();
            interactionCollider.center = new Vector3(0, 1f, 0);
            interactionCollider.size = new Vector3(1.2f, 2.5f, 1.2f);
            
            ReflectingMirror mirrorScript = mirrorRoot.AddComponent<ReflectingMirror>();
            mirrorScript.rotationAngle = 45f;
            mirrorScript.rotationSpeed = 8f;
        }

        Selection.activeGameObject = rootObj;
        EditorUtility.DisplayDialog("Success", "URP Light Mirror Puzzle Generated!\n\nDelete the pink ones, drag these into place, and you're good to go!", "OK");
    }

    private void CreateLayer(string layerName)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        
        for (int i = 8; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue == layerName) return;
        }

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty sp = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return;
            }
        }
    }
}
