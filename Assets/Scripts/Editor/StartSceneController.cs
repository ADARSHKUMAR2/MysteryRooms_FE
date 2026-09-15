using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class StartSceneController
{
    // WARNING: Change this to the exact path of your Login scene!
    private const string LoginScenePath = "Assets/Scenes/Login.unity"; 
    private const string MenuName = "Tools/Always Start From Login Scene";

    static StartSceneController()
    {
        // This ensures the menu checkmark matches the current state when Unity compiles/loads
        EditorApplication.delayCall += () =>
        {
            Menu.SetChecked(MenuName, EditorSceneManager.playModeStartScene != null);
        };
    }

    [MenuItem(MenuName)]
    public static void ToggleStartScene()
    {
        // Toggle the current state
        bool isCurrentlyEnabled = Menu.GetChecked(MenuName);
        SetPlayModeStartScene(!isCurrentlyEnabled);
    }

    private static void SetPlayModeStartScene(bool enable)
    {
        if (enable)
        {
            SceneAsset loginScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(LoginScenePath);
            if (loginScene != null)
            {
                EditorSceneManager.playModeStartScene = loginScene;
                Menu.SetChecked(MenuName, true);
                Debug.Log($"[StartSceneController] Play mode will now ALWAYS start from: {loginScene.name}");
            }
            else
            {
                Debug.LogError($"[StartSceneController] Could not find scene at path: {LoginScenePath}. Please check the path!");
            }
        }
        else
        {
            EditorSceneManager.playModeStartScene = null;
            Menu.SetChecked(MenuName, false);
            Debug.Log("[StartSceneController] Start scene cleared. Unity will start from the currently open scene.");
        }
    }
}
