#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MysteryRooms.UI;

namespace MysteryRooms.EditorScripts
{
    public class LoginLoadingUISetup : EditorWindow
    {
        [MenuItem("MysteryRooms/Setup Login Loading UI")]
        public static void SetupUI()
        {
            // Find the controller in the active scene
            LoginUIController controller = FindObjectOfType<LoginUIController>(true);
            if (controller == null)
            {
                Debug.LogError("❌ Could not find LoginUIController. Please open the Login scene first.");
                return;
            }

            SerializedObject so = new SerializedObject(controller);
            
            // 1. Setup CanvasGroup to hide the Login Card by default (prevents 1-frame flash)
            SerializedProperty loginCanvasGroupProp = so.FindProperty("loginCanvasGroup");
            CanvasGroup cg = loginCanvasGroupProp.objectReferenceValue as CanvasGroup;
            
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
                EditorUtility.SetDirty(cg);
            }
            else
            {
                Debug.LogWarning("⚠️ No CanvasGroup assigned to loginCanvasGroup in LoginUIController.");
            }

            // 2. Move Login Card off-screen by default
            SerializedProperty loginCardProp = so.FindProperty("loginCard");
            RectTransform loginCard = loginCardProp.objectReferenceValue as RectTransform;
            if (loginCard != null)
            {
                Vector2 pos = loginCard.anchoredPosition;
                pos.y = -1200f;
                loginCard.anchoredPosition = pos;
                EditorUtility.SetDirty(loginCard);
            }

            // 3. Ensure Loading Panel is active by default
            SerializedProperty loadingPanelProp = so.FindProperty("loadingPanel");
            GameObject loadingPanel = loadingPanelProp.objectReferenceValue as GameObject;
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
                EditorUtility.SetDirty(loadingPanel);
            }

            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("✅ Login UI updated! The loading screen will now show by default.");
        }
    }
}
#endif
