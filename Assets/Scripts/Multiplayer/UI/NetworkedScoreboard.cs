using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System;

public struct PlayerScoreData : INetworkSerializable, IEquatable<PlayerScoreData>
{
    public ulong clientId;
    public FixedString32Bytes playerName;
    public int puzzlesSolved;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref puzzlesSolved);
    }

    public bool Equals(PlayerScoreData other)
    {
        // Now it will trigger an update if the score changes OR the name changes!
        return clientId == other.clientId && 
               puzzlesSolved == other.puzzlesSolved && 
               playerName == other.playerName;
    }
}

public class NetworkedScoreboard : NetworkBehaviour
{
    public static NetworkedScoreboard Instance { get; private set; }

    public NetworkList<PlayerScoreData> playerScores;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        playerScores = new NetworkList<PlayerScoreData>();
        Debug.Log($"[Scoreboard] Awake - Instance set: {Instance == this}, playerScores initialized: {playerScores != null}");

    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[Scoreboard] OnNetworkSpawn - IsServer: {IsServer}, IsClient: {IsClient}, IsHost: {IsHost}");
        
        playerScores.OnListChanged += OnScoreboardUpdated;
        
        if (IsServer)
        {
            // 1. Subscribe to catch any late-joiners connecting AFTER this scene loads
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

            // FIX 1: Add all clients that are ALREADY connected (crucial for scene transitions!)
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                // Only add if they aren't already in the list
                bool exists = false;
                foreach(var score in playerScores) 
                {
                    if (score.clientId == clientId) { exists = true; break; }
                }

                if (!exists) 
                {
                    string playerName = (clientId == NetworkManager.Singleton.LocalClientId) ? "Host Player" : $"Client {clientId}";
                    Debug.Log($"[Scoreboard] Server adding existing connected player with clientId: {clientId}");
                    AddPlayer(clientId, playerName);
                }
            }
        }

        if (IsClient)
        {
            Debug.Log($"[Scoreboard] Client joined - current playerScores count: {playerScores.Count}");
            
            // FIX 2: Actually push the existing initial state to the UI!
            foreach (var score in playerScores)
            {
                Debug.Log($"[Scoreboard] Client pushing initial state to UI for: {score.clientId} ({score.playerName})");
                
                if (MysteryRooms.UI.GameUIController.Instance != null)
                {
                    MysteryRooms.UI.GameUIController.Instance.UpdatePlayerScore(
                        score.clientId, 
                        score.playerName.ToString(), 
                        score.puzzlesSolved
                    );
                }
                else
                {
                    Debug.LogWarning("[Scoreboard] GameUIController.Instance is null during OnNetworkSpawn! If UI cards are missing, check Script Execution Order.");
                }
            }
        }
    }


    
    public override void OnNetworkDespawn()
    {
        playerScores.OnListChanged -= OnScoreboardUpdated;

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[Scoreboard] OnClientConnected fired for clientId: {clientId}, IsServer: {IsServer}");
        if (IsServer)
        {
            AddPlayer(clientId, $"Client {clientId}");
        }
    }

    // Since only the Server adds players now, we don't need a ServerRpc for this, just a regular method
    private void AddPlayer(ulong clientId, string name)
    {
        playerScores.Add(new PlayerScoreData 
        { 
            clientId = clientId, 
            playerName = new FixedString32Bytes(name), 
            puzzlesSolved = 0 
        });
        
        Debug.Log($"[Scoreboard] Added Client {clientId} to the game!");
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddPlayerServerRpc(ulong clientId, string name)
    {
        playerScores.Add(new PlayerScoreData 
        { 
            clientId = clientId, 
            playerName = new FixedString32Bytes(name), 
            puzzlesSolved = 0 
        });
    }

    [ServerRpc(RequireOwnership = false)]
    public void IncrementPlayerScoreServerRpc(ulong clientId)
    {
        Debug.Log($"[Scoreboard] Server received request to give point to Client {clientId}. Current players in list: {playerScores.Count}");

        for (int i = 0; i < playerScores.Count; i++)
        {
            if (playerScores[i].clientId == clientId)
            {
                var data = playerScores[i];
                data.puzzlesSolved++;
                playerScores[i] = data; // Update triggers the network list changed event
                break;
            }
        }
    }

    private void OnScoreboardUpdated(NetworkListEvent<PlayerScoreData> changeEvent)
    {
        Debug.Log($"[Scoreboard] OnScoreboardUpdated - Event type: {changeEvent.Type}, playerScores count: {playerScores.Count}");

        if (MysteryRooms.UI.GameUIController.Instance == null)
        {
            Debug.LogWarning("[Scoreboard] ❌ GameUIController.Instance is NULL! Cannot update HUD.");
            return;
        }

        foreach (var score in playerScores)
        {
            Debug.Log($"[Scoreboard] Pushing to HUD -> clientId: {score.clientId}, name: {score.playerName}, solved: {score.puzzlesSolved}");
            MysteryRooms.UI.GameUIController.Instance.UpdatePlayerScore(
                score.clientId, 
                score.playerName.ToString(), 
                score.puzzlesSolved
            );
        }
    }

}
