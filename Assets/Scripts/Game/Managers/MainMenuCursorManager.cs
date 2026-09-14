using UnityEngine;

namespace MysteryRooms.UI
{
    /// <summary>
    /// Ensures cursor is visible and unlocked in the Main Menu
    /// Place this on any GameObject in the MainMenu scene
    /// </summary>
    public class MainMenuCursorManager : MonoBehaviour
    {
        private void Start()
        {
            EnableCursor();
        }

        private void OnEnable()
        {
            EnableCursor();
        }

        private void EnableCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("✅ MainMenu cursor enabled");
        }
    }
}
