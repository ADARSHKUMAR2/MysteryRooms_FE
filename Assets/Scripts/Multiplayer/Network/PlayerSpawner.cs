using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace MysteryRooms.Multiplayer.Network
{
    public class PlayerSpawner : NetworkBehaviour
    {
        [Header("Spawn Points")]
        public List<Transform> spawnPoints;
        [Header("Player Prefab")]
        public GameObject playerPrefab;

        public override void OnNetworkSpawn()
        {
            // Only the server dictates where players go
            if (IsServer)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
                
                // Also listen for clients that join late (after the scene is already loaded)
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                
            }
        }

        private void OnSceneLoaded(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (!IsServer) return;

            // Only spawn if the scene that just finished loading is the Game scene
            if (sceneName == "Game")
            {
                Debug.Log($"Game scene fully loaded for {clientsCompleted.Count} clients! Spawning players...");
                
                // Spawn a player for every client that successfully loaded the scene
                foreach (ulong clientId in clientsCompleted)
                {
                    SpawnPlayerForClient(clientId);
                }
            }
        }

        private void SpawnPlayerForClient(ulong clientId)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("Player Prefab is missing in PlayerSpawner!");
                return;
            }

            // Prevent spawning twice for the same client
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
            {
                Debug.LogWarning($"Client {clientId} already has a player object!");
                return;
            }

            int spawnIndex = 0;
            if (spawnPoints != null && spawnPoints.Count > 0)
            {
                spawnIndex = (int)(clientId % (ulong)spawnPoints.Count);
            }
            
            Transform spawnPoint = spawnPoints != null && spawnPoints.Count > 0 ? spawnPoints[spawnIndex] : transform;

            GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
            
            NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();
            netObj.SpawnAsPlayerObject(clientId, true);
            
            Debug.Log($"✅ Successfully spawned player for Client {clientId}");
        }


        private void OnClientConnected(ulong clientId)
        {
            // Just spawn the player! The SpawnPlayerForClient method handles picking the spawn point and instantiating.
            if (IsServer)
            {
                SpawnPlayerForClient(clientId);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                
                if (NetworkManager.Singleton.SceneManager != null)
                {
                    NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
                }
            }
        }
    }
}
