#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using MysteryRooms.Game.Data;
using System;

[CustomEditor(typeof(SymbolDatabase))]
public class SymbolDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI(); // Draw the default list

        SymbolDatabase database = (SymbolDatabase)target;

        GUILayout.Space(20);
        if (GUILayout.Button("Auto-Populate from Enum Names", GUILayout.Height(30)))
        {
            AutoPopulateNames(database);
        }
    }

    private void AutoPopulateNames(SymbolDatabase db)
    {
        // Clear existing list
        db.symbols.Clear();

        // Get all names from your existing SpriteSequenceDisplay Enum
        string[] enumNames = Enum.GetNames(typeof(SpriteSequenceDisplay.EgyptianSymbol));

        foreach (string name in enumNames)
        {
            db.symbols.Add(new SymbolEntry { symbolName = name, symbolSprite = null });
        }

        // Mark asset as dirty so Unity saves the changes
        EditorUtility.SetDirty(db);
        Debug.Log($"Populated Symbol Database with {enumNames.Length} entries! Now just drag and drop the sprites.");
    }
}
#endif
