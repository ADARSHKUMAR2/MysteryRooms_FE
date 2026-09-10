using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace MysteryRooms.Multiplayer.UI
{
    /// <summary>
    /// Popup that appears when the host disconnects from a multiplayer session
    /// </summary>
    public class HostDisconnectedPopup : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button returnToMenuButton;
        
        [Header("Settings")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string defaultMessage = "The host has disconnected from the game.";

        private void Awake()
        {
            // Hide popup by default
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }

            // Setup button listener
            if (returnToMenuButton != null)
            {
                returnToMenuButton.onClick.AddListener(OnReturnToMenuClicked);
            }
        }

        /// <summary>
        /// Show the host disconnected popup
        /// </summary>
        public void Show(string customMessage = null)
        {
            if (popupPanel != null)
            {
                popupPanel.SetActive(true);
            }

            if (messageText != null)
            {
                messageText.text = customMessage ?? defaultMessage;
            }

            Debug.Log("🚨 Host Disconnected Popup Shown");
        }

        /// <summary>
        /// Hide the popup
        /// </summary>
        public void Hide()
        {
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }
        }

        private void OnReturnToMenuClicked()
        {
            Debug.Log("Returning to main menu...");
    
            // Unlock and show cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // Clean up network manager if it exists
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Unity.Netcode.NetworkManager.Singleton.Shutdown();
            }
            
            // Clean up DontDestroyOnLoad objects
            CleanupGameObjects();
            
            // Load main menu scene
            SceneManager.LoadScene(mainMenuSceneName);
        }

        /// <summary>Clean up multiplayer-related DontDestroyOnLoad objects</summary>
        private void CleanupGameObjects()
        {
            // Destroy MultiplayerSessionManager
            var sessionManager = FindObjectOfType<MysteryRooms.Multiplayer.Core.MultiplayerSessionManager>();
            if (sessionManager != null)
            {
                Destroy(sessionManager.gameObject);
            }
            
            // Destroy MultiplayerMysteryCoordinator
            var coordinator = FindObjectOfType<MysteryRooms.Multiplayer.Core.MultiplayerMysteryCoordinator>();
            if (coordinator != null)
            {
                Destroy(coordinator.gameObject);
            }
            
            Debug.Log("✅ Cleaned up game session objects");
        }

        private void OnDestroy()
        {
            if (returnToMenuButton != null)
            {
                returnToMenuButton.onClick.RemoveListener(OnReturnToMenuClicked);
            }
        }
    }
}
