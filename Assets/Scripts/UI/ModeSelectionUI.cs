using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MysteryRooms.Authentication;

namespace MysteryRooms.UI
{
    public class ModeSelectionUI : MonoBehaviour
    {
        [Header("Mode Panels")]
        [SerializeField] private GameObject modeSelectionPanel; // The panel containing the Solo/Multi buttons
        [SerializeField] private GameObject soloPanel;          // The panel with MysteryDebugPanel
        [SerializeField] private GameObject multiplayerPanel;   // The panel with MultiplayerUI

        [Header("Buttons")]
        [SerializeField] private Button soloModeButton;
        [SerializeField] private Button multiplayerModeButton;
        [SerializeField] private Button backFromSoloButton;
        [SerializeField] private Button backFromMultiButton;

        [Header("Authentication")]
        [SerializeField] private Button logoutButton; 

        private void Start()
        {
            // Set up button listeners
            if (soloModeButton != null)
                soloModeButton.onClick.AddListener(ShowSoloMode);
                
            if (multiplayerModeButton != null)
                multiplayerModeButton.onClick.AddListener(ShowMultiplayerMode);

            if (backFromSoloButton != null)
                backFromSoloButton.onClick.AddListener(ShowModeSelection);

            if (backFromMultiButton != null)
                backFromMultiButton.onClick.AddListener(ShowModeSelection);

            if (logoutButton != null)
            logoutButton.onClick.AddListener(LogoutUser);

            // Initialize default state (Show Selection, hide others)
            ShowModeSelection();
        }

        private void ShowModeSelection()
        {
            modeSelectionPanel.SetActive(true);
            soloPanel.SetActive(false);
            multiplayerPanel.SetActive(false);
        }

        private void ShowSoloMode()
        {
            modeSelectionPanel.SetActive(false);
            soloPanel.SetActive(true);
            multiplayerPanel.SetActive(false);
        }

        private void ShowMultiplayerMode()
        {
            modeSelectionPanel.SetActive(false);
            soloPanel.SetActive(false);
            multiplayerPanel.SetActive(true);
        }

        private void LogoutUser()
        {
            Debug.Log("Logging out...");
            
            // Call the existing Logout method on your Auth Manager
            if (FirebaseAuthManager.Instance != null)
            {
                FirebaseAuthManager.Instance.Logout();
            }

            // Make sure your login scene is exactly named "Login" in Build Settings
            SceneManager.LoadScene("Login"); 
        }
    }
}
