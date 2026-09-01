using UnityEngine;
using UnityEditor;

public class PuzzleSocketGenerator : EditorWindow
{
    private string roomName = "entrance_hall";
    private int numberOfSockets = 3;
    
    [Header("Visual Editor Only")]
    private Color gizmoColor = new Color(0.2f, 0.8f, 0.2f, 0.5f); // Semi-transparent green
    private Vector3 gizmoSize = new Vector3(1f, 1.5f, 1f);

    [MenuItem("MysteryRooms/Create Room Puzzle Sockets")]
    public static void ShowWindow()
    {
        GetWindow<PuzzleSocketGenerator>("Puzzle Sockets").Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Room Socket Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Generates empty socket Transforms for a specific room. " +
            "These sockets have custom Editor-only Gizmos so you can clearly see " +
            "where puzzles will spawn and which direction they will face!", 
            MessageType.Info
        );
        GUILayout.Space(10);

        roomName = EditorGUILayout.TextField("Room Name (e.g. entrance_hall)", roomName);
        numberOfSockets = EditorGUILayout.IntSlider("Number of Sockets", numberOfSockets, 1, 10);

        GUILayout.Space(10);
        GUILayout.Label("Editor Gizmo Settings", EditorStyles.boldLabel);
        gizmoColor = EditorGUILayout.ColorField("Gizmo Color", gizmoColor);
        gizmoSize = EditorGUILayout.Vector3Field("Gizmo Box Size", gizmoSize);

        GUILayout.Space(20);

        if (GUILayout.Button("Generate Sockets", GUILayout.Height(40)))
        {
            GenerateSockets();
        }
    }

    private void GenerateSockets()
    {
        // Create a parent folder for neatness
        GameObject roomParent = GameObject.Find($"Sockets_{roomName}");
        if (roomParent == null)
        {
            roomParent = new GameObject($"Sockets_{roomName}");
            roomParent.transform.position = Vector3.zero;
        }

        // Try to find the helper script type
        System.Type helperType = System.Type.GetType("PuzzleSocketHelper, Assembly-CSharp");
        if (helperType == null) helperType = System.Type.GetType("PuzzleSocketHelper, MysteryRooms.Runtime");

        // Generate Sockets
        for (int i = 0; i < numberOfSockets; i++)
        {
            GameObject socket = new GameObject($"Socket_{roomName}_{i+1}");
            socket.transform.SetParent(roomParent.transform);
            
            // Space them out slightly so they don't spawn exactly on top of each other
            socket.transform.localPosition = new Vector3(i * 2f, 0, 0);

            if (helperType != null)
            {
                Component helper = socket.AddComponent(helperType);
                SerializedObject so = new SerializedObject(helper);
                so.FindProperty("gizmoColor").colorValue = gizmoColor;
                so.FindProperty("gizmoSize").vector3Value = gizmoSize;
                so.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning("⚠️ Could not find PuzzleSocketHelper script! Sockets were generated but won't have 3D Gizmos.");
            }
        }

        Selection.activeGameObject = roomParent;
        EditorUtility.DisplayDialog("Success", $"Generated {numberOfSockets} sockets for {roomName}!\n\nMove them to tables and walls, and rotate the BLUE Z-Axis arrow to point outward (puzzles will face this way).", "OK");
    }

}
