using UnityEngine;
using UnityEditor;
using System.IO;

public class MummyRoomBuilder : EditorWindow
{
    [MenuItem("Mystery Rooms/Build Mummy Room Blockout")]
    public static void BuildRoom()
    {
        // Root object
        GameObject root = new GameObject("MummyRoom_Environment");

        // Materials
        Material floorMat = GetOrCreateMaterial("FloorMat", new Color(0.15f, 0.15f, 0.15f));
        Material wallMat = GetOrCreateMaterial("WallMat", new Color(0.6f, 0.5f, 0.35f));
        Material doorMat = GetOrCreateMaterial("DoorMat", new Color(0.35f, 0.2f, 0.1f));
        Material sarcophagusMat = GetOrCreateMaterial("SarcophagusMat", new Color(0.8f, 0.65f, 0.1f));
        Material statueMat = GetOrCreateMaterial("StatueMat", new Color(0.2f, 0.6f, 0.5f));

        // Build environment
        CreateRoomBounds(root, floorMat, wallMat);
        CreateWalls(root, wallMat);

        // Exit door
        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.name = "Locked_Door_Exit";
        door.transform.position = new Vector3(0, 2.5f, 9.4f);
        door.transform.localScale = new Vector3(3, 5, 0.5f);
        door.transform.parent = root.transform;
        door.GetComponent<Renderer>().sharedMaterial = doorMat;
        door.AddComponent<InteractableDoor>();

        // Sarcophagus
        GameObject sarcophagus = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sarcophagus.name = "Sarcophagus";
        sarcophagus.transform.position = new Vector3(0, 1f, 0);
        sarcophagus.transform.localScale = new Vector3(2.5f, 1.5f, 5f);
        sarcophagus.transform.parent = root.transform;
        sarcophagus.GetComponent<Renderer>().sharedMaterial = sarcophagusMat;

        // Puzzle Statues with different solutions
        CreateStatuePuzzle(root, "Statue_NE", new Vector3(7, 1f, 7), statueMat, 2);
        CreateStatuePuzzle(root, "Statue_NW", new Vector3(-7, 1f, 7), statueMat, 1);
        CreateStatuePuzzle(root, "Statue_SE", new Vector3(7, 1f, -7), statueMat, 3);
        CreateStatuePuzzle(root, "Statue_SW", new Vector3(-7, 1f, -7), statueMat, 0);

        // Lights
        CreateTorchLight(root, "Torch_Light_North", new Vector3(0, 5, 9));
        CreateTorchLight(root, "Torch_Light_South", new Vector3(0, 5, -9));
        CreateTorchLight(root, "Torch_Light_East", new Vector3(9, 5, 0));
        CreateTorchLight(root, "Torch_Light_West", new Vector3(-9, 5, 0));
        CreateTorchLight(root, "Ambient_Center_Light", new Vector3(0, 7, 0), 1.5f, 25f);

        // Create Puzzle Manager
        GameObject puzzleManagerGO = new GameObject("PuzzleManager");
        PuzzleManager puzzleManager = puzzleManagerGO.AddComponent<PuzzleManager>();
        puzzleManager.exitDoor = door.GetComponent<InteractableDoor>();

        Debug.Log("✅ Mummy Room with Interactive Puzzles Generated!");
    }

    private static Material GetOrCreateMaterial(string matName, Color color)
    {
        string folderPath = "Assets/Materials/Blockout";
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder("Assets/Materials", "Blockout");

        string matPath = $"{folderPath}/{matName}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);

            AssetDatabase.CreateAsset(mat, matPath);
        }
        return mat;
    }

    private static void CreateRoomBounds(GameObject parent, Material floorMat, Material ceilingMat)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.position = new Vector3(0, 0, 0);
        floor.transform.localScale = new Vector3(20, 0.5f, 20);
        floor.transform.parent = parent.transform;
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;

        GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceiling.name = "Ceiling";
        ceiling.transform.position = new Vector3(0, 8, 0);
        ceiling.transform.localScale = new Vector3(20, 0.5f, 20);
        ceiling.transform.parent = parent.transform;
        ceiling.GetComponent<Renderer>().sharedMaterial = ceilingMat;
    }

    private static void CreateWalls(GameObject parent, Material wallMat)
    {
        GameObject wallN = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallN.name = "Wall_North";
        wallN.transform.position = new Vector3(0, 4, 10);
        wallN.transform.localScale = new Vector3(20, 8, 1);
        wallN.transform.parent = parent.transform;
        wallN.GetComponent<Renderer>().sharedMaterial = wallMat;

        GameObject wallS = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallS.name = "Wall_South";
        wallS.transform.position = new Vector3(0, 4, -10);
        wallS.transform.localScale = new Vector3(20, 8, 1);
        wallS.transform.parent = parent.transform;
        wallS.GetComponent<Renderer>().sharedMaterial = wallMat;

        GameObject wallE = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallE.name = "Wall_East";
        wallE.transform.position = new Vector3(10, 4, 0);
        wallE.transform.localScale = new Vector3(1, 8, 20);
        wallE.transform.parent = parent.transform;
        wallE.GetComponent<Renderer>().sharedMaterial = wallMat;

        GameObject wallW = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallW.name = "Wall_West";
        wallW.transform.position = new Vector3(-10, 4, 0);
        wallW.transform.localScale = new Vector3(1, 8, 20);
        wallW.transform.parent = parent.transform;
        wallW.GetComponent<Renderer>().sharedMaterial = wallMat;
    }

    private static void CreateStatuePuzzle(GameObject parent, string name, Vector3 position, Material mat, int solutionStep)
    {
        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObj.name = name;
        baseObj.transform.position = position;
        baseObj.transform.localScale = new Vector3(1.5f, 1f, 1.5f);
        baseObj.transform.parent = parent.transform;
        baseObj.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject topObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topObj.name = name + "_Bust";
        topObj.transform.position = position + new Vector3(0, 1.5f, 0);
        topObj.transform.localScale = new Vector3(1f, 2f, 1f);
        topObj.transform.parent = baseObj.transform;
        topObj.GetComponent<Renderer>().sharedMaterial = mat;

        // Add puzzle component
        RotatingStatuePuzzle puzzle = baseObj.AddComponent<RotatingStatuePuzzle>();
        puzzle.puzzleID = name;
        puzzle.correctRotationSteps = solutionStep;
    }

    private static void CreateTorchLight(GameObject parent, string name, Vector3 position, float intensity = 2f, float range = 15f)
    {
        GameObject lightObj = new GameObject(name);
        Light light = lightObj.AddComponent<Light>();
        
        light.type = LightType.Point;
        light.color = new Color(1f, 0.65f, 0.3f);
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.Soft;

        lightObj.transform.position = position;
        lightObj.transform.parent = parent.transform;
    }
}
