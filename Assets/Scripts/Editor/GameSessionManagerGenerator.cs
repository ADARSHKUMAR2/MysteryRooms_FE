#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using MysteryRooms.Game.Managers;
using MysteryRooms.Multiplayer.UI;

namespace MysteryRooms.Editor
{
    /// <summary>
    /// Editor utility to create GameSessionManager and link it to the Host Disconnected Popup
    /// Usage: Right-click in Hierarchy -> Multiplayer -> Generate Game Session Manager
    /// </summary>
    public class GameSessionManagerGenerator : EditorWindow
    {
        [MenuItem("GameObject/Multiplayer/Generate Game Session Manager", false, 2)]
        public static void GenerateGameSessionManager()
        {
            // Check if GameSessionManager already exists
            GameSessionManager existingManager = FindObjectOfType<GameSessionManager>();
            
            if (existingManager != null)
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "GameSessionManager Already Exists",
                    "A GameSessionManager already exists in the scene. Do you want to reconfigure it?",
                    "Yes, Reconfigure",
                    "Cancel"
                );

                if (!overwrite)
                {
                    return;
                }

                Selection.activeGameObject = existingManager.gameObject;
                ConfigureGameSessionManager(existingManager);
                return;
            }

            // Create new GameSessionManager
            GameObject managerObj = new GameObject("GameSessionManager");
            GameSessionManager manager = managerObj.AddComponent<GameSessionManager>();

            ConfigureGameSessionManager(manager);

            // Select the created object
            Selection.activeGameObject = managerObj;

            Debug.Log("✅ GameSessionManager created successfully!");
        }

        private static void ConfigureGameSessionManager(GameSessionManager manager)
        {
            // Find the HostDisconnectedPopup in the scene
            HostDisconnectedPopup popup = FindObjectOfType<HostDisconnectedPopup>(true); // Include inactive objects

            if (popup != null)
            {
                // Link the popup to the manager
                SerializedObject serializedManager = new SerializedObject(manager);
                serializedManager.FindProperty("hostDisconnectedPopup").objectReferenceValue = popup;
                serializedManager.ApplyModifiedProperties();

                Debug.Log("✅ GameSessionManager linked to HostDisconnectedPopup");
                
                EditorUtility.DisplayDialog(
                    "GameSessionManager Created",
                    "GameSessionManager has been created and linked to the HostDisconnectedPopup!\n\n" +
                    "Setup Complete:\n" +
                    "✅ Host Disconnected Popup UI\n" +
                    "✅ GameSessionManager\n" +
                    "✅ Automatically linked\n\n" +
                    "When the host disconnects during gameplay, clients will see the popup.",
                    "OK"
                );
            }
            else
            {
                Debug.LogWarning("⚠️ HostDisconnectedPopup not found in scene. Please create it first using:\n" +
                                 "Right-click → Multiplayer → Generate Host Disconnected Popup");
                
                EditorUtility.DisplayDialog(
                    "Popup Not Found",
                    "GameSessionManager created, but HostDisconnectedPopup was not found in the scene.\n\n" +
                    "Please:\n" +
                    "1. Right-click in Hierarchy → Multiplayer → Generate Host Disconnected Popup\n" +
                    "2. Manually drag the popup to the GameSessionManager's 'Host Disconnected Popup' field",
                    "OK"
                );
            }
        }
    }
}
#endif
