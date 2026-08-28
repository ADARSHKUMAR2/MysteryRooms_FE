using UnityEngine;
using UnityEditor;
using MysteryRooms.Game.Data;
using System.Collections.Generic;
using System.IO;

public class CardDatabaseGenerator : EditorWindow
{
    private string outputPath = "Assets/Data/Puzzles";
    private DefaultAsset spriteFolder; // Drag a folder here to auto-assign sprites

    [MenuItem("MysteryRooms/Tools/Generate Card Database")]
    public static void ShowWindow()
    {
        GetWindow<CardDatabaseGenerator>("Card DB Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Card Database Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This will create a new CardDatabase ScriptableObject pre-filled with all 52 card IDs " +
            "(Spades_A, Spades_2, etc.).\n\n" +
            "If you assign the folder containing your sliced card sprites, it will automatically link them!", 
            MessageType.Info
        );
        GUILayout.Space(10);

        outputPath = EditorGUILayout.TextField("Save Path", outputPath);
        
        GUILayout.Space(5);
        GUILayout.Label("Optional: Auto-Assign Sprites", EditorStyles.boldLabel);
        spriteFolder = (DefaultAsset)EditorGUILayout.ObjectField("Sprite Folder", spriteFolder, typeof(DefaultAsset), false);

        GUILayout.Space(20);

        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
        if (GUILayout.Button("Generate Card Database", GUILayout.Height(40)))
        {
            GenerateDatabase();
        }
        GUI.backgroundColor = Color.white;
    }

    private void GenerateDatabase()
    {
        // 1. Create the directory if it doesn't exist
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        string assetPath = $"{outputPath}/CardDatabase.asset";

        // 2. Create the ScriptableObject instance
        CardDatabase db = ScriptableObject.CreateInstance<CardDatabase>();
        db.cards = new List<CardSpriteEntry>();

        string[] suits = { "Spades", "Hearts", "Diamonds", "Clubs" };
        string[] ranks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };

        string spriteFolderPath = spriteFolder != null ? AssetDatabase.GetAssetPath(spriteFolder) : "";
        int autoAssignedCount = 0;

        // 3. Generate the 52 entries
        foreach (string suit in suits)
        {
            foreach (string rank in ranks)
            {
                string id = $"{suit}_{rank}";
                Sprite foundSprite = null;

                // 4. Try to auto-assign the sprite if a folder was provided
                if (!string.IsNullOrEmpty(spriteFolderPath))
                {
                    // Search for a sprite with the exact name in the specified folder
                    string[] guids = AssetDatabase.FindAssets($"{id} t:Sprite", new[] { spriteFolderPath });
                    
                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path) as Sprite[];
                        
                        // Because sliced sprites share the same file path, we must check sub-assets
                        if (sprites != null)
                        {
                            foreach (Sprite s in sprites)
                            {
                                if (s.name == id)
                                {
                                    foundSprite = s;
                                    break;
                                }
                            }
                        }
                        
                        // If we didn't find it in sub-assets (meaning it's a standalone PNG), load it directly
                        if (foundSprite == null)
                        {
                            Sprite standalone = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                            if (standalone != null && standalone.name == id)
                            {
                                foundSprite = standalone;
                            }
                        }
                        
                        if (foundSprite != null) break;
                    }

                    if (foundSprite != null) autoAssignedCount++;
                }

                // Add to database
                db.cards.Add(new CardSpriteEntry
                {
                    cardId = id,
                    cardSprite = foundSprite
                });
            }
        }

        // 5. Save the Asset
        AssetDatabase.CreateAsset(db, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 6. Focus the asset in the Project window
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = db;

        // Show Success Message
        string message = $"Generated CardDatabase at '{assetPath}' with 52 entries.\n";
        if (spriteFolder != null)
        {
            message += $"\nAuto-assigned {autoAssignedCount}/52 sprites successfully!";
        }
        else
        {
            message += "\n(Sprites were left empty as no folder was provided).";
        }

        EditorUtility.DisplayDialog("Success", message, "OK");
    }
}
