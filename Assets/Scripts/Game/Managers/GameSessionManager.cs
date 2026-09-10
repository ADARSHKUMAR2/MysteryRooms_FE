using UnityEngine;
using Unity.Netcode;
using MysteryRooms.Multiplayer.Core;
using MysteryRooms.Multiplayer.UI;

namespace MysteryRooms.Game.Managers
{
    /// <summary>
    /// Manages the game session and handles disconnections during gameplay
    /// This should exist in the Game scene, not the lobby
    /// </summary>
    public class GameSessionManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HostDisconnectedPopup hostDisconnectedPopup;
        
        private MultiplayerSessionManager multiplayerSessionManager;

        private void Start()
        {
            // Find the MultiplayerSessionManager (it's DontDestroyOnLoad)
            multiplayerSessionManager = FindObjectOfType<MultiplayerSessionManager>();
            
            if (multiplayerSessionManager != null)
            {
                // Subscribe to host disconnect event
                multiplayerSessionManager.OnHostDisconnected += OnHostDisconnected;
                Debug.Log("✅ GameSessionManager subscribed to host disconnect events");
            }
            else
            {
                Debug.LogError("❌ MultiplayerSessionManager not found! Make sure you started from the multiplayer lobby.");
            }

            // Also subscribe to NetworkManager events as backup
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedCallback;
            }
        }

        private void OnClientDisconnectedCallback(ulong clientId)
        {
            // Check if the server/host disconnected
            if (clientId == NetworkManager.ServerClientId)
            {
                Debug.LogWarning("🚨 Host disconnected detected in Game scene!");
                ShowHostDisconnectedPopup();
            }
        }

        private void OnHostDisconnected()
        {
            Debug.LogWarning("🚨 Host disconnected event received in Game scene!");
            ShowHostDisconnectedPopup();
        }

        private void ShowHostDisconnectedPopup()
        {
            if (hostDisconnectedPopup != null)
            {
                hostDisconnectedPopup.Show("The host has disconnected from the game.\n\nYou will be returned to the main menu.");
            }
            else
            {
                Debug.LogError("❌ HostDisconnectedPopup is not assigned to GameSessionManager!");
                
                // Fallback: Just load main menu directly
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (multiplayerSessionManager != null)
            {
                multiplayerSessionManager.OnHostDisconnected -= OnHostDisconnected;
            }

            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedCallback;
            }
        }
    }
}
