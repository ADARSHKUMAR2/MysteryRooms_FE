using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace MysteryRooms.Multiplayer.Network
{
    public class PlayerSpawner : NetworkBehaviour
    {
        [Header("Spawn Points")]
        public List<Transform> spawnPoints;

        public override void OnNetworkSpawn()
        {
            // Only the server dictates where players go
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                
                // Immediately place the Host (who is already connected)
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var client))
                {
                    MovePlayerToSpawnPoint(client.PlayerObject, NetworkManager.Singleton.LocalClientId);
                }
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                MovePlayerToSpawnPoint(client.PlayerObject, clientId);
            }
        }

        private void MovePlayerToSpawnPoint(NetworkObject playerObj, ulong clientId)
        {
            if (spawnPoints == null || spawnPoints.Count == 0) return;

            // Pick a spawn point based on the client ID (e.g., Player 0 gets spawn 0)
            int spawnIndex = (int)(clientId % (ulong)spawnPoints.Count);
            Transform spawnPoint = spawnPoints[spawnIndex];

            // CRITICAL: CharacterControllers block direct transform changes. 
            // We must temporarily disable it to teleport the player.
            CharacterController cc = playerObj.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            playerObj.transform.position = spawnPoint.position;
            playerObj.transform.rotation = spawnPoint.rotation;

            if (cc != null) cc.enabled = true;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            }
        }
    }
}
