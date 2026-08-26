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
        return clientId == other.clientId;
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
    }

    public override void OnNetworkSpawn()
    {
        playerScores.OnListChanged += OnScoreboardUpdated;
        
        // Add self to scoreboard when spawning
        if (IsServer)
        {
            AddPlayerServerRpc(NetworkManager.Singleton.LocalClientId, "Host Player");
        }
        else
        {
            AddPlayerServerRpc(NetworkManager.Singleton.LocalClientId, "Client Player");
        }
    }

    public override void OnNetworkDespawn()
    {
        playerScores.OnListChanged -= OnScoreboardUpdated;
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
        // Whenever ANY score changes, update the HUD for EVERYONE
        if (MysteryRooms.UI.GameUIController.Instance != null)
        {
            foreach (var score in playerScores)
            {
                MysteryRooms.UI.GameUIController.Instance.UpdatePlayerScore(
                    score.clientId, 
                    score.playerName.ToString(), 
                    score.puzzlesSolved
                );
            }
        }
    }
}
