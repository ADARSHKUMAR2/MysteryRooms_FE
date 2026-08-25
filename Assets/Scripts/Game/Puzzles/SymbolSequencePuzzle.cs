using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MysteryRooms.Game.Data;
using Unity.Netcode;

public class SymbolSequencePuzzle : BasePuzzle
{
    [Header("Symbols")]
    [SerializeField] private List<SymbolButton> symbolButtons;

    private List<string> correctSequence;
    
    // Server maintains the current sequence attempts
    private List<string> serverPlayerSequence = new List<string>();

    private NetworkVariable<bool> isSolvedNet = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isSolvedNet.OnValueChanged += OnSolvedStateChanged;
        
        // Handle Late Joiners
        if (isSolvedNet.Value) OnSolvedStateChanged(false, true);
    }

    public override void OnNetworkDespawn()
    {
        isSolvedNet.OnValueChanged -= OnSolvedStateChanged;
        base.OnNetworkDespawn();
    }

    public override void ConfigureFromBackend(PuzzleConfigData config)
    {
        base.ConfigureFromBackend(config);

        if (config.config != null && config.config.correctSequence != null)
        {
            correctSequence = config.config.correctSequence;
            AssignSymbolsToButtons();
        }
    }

    private void AssignSymbolsToButtons()
    {
        if (symbolButtons == null || symbolButtons.Count == 0) return;

        for (int i = 0; i < Mathf.Min(symbolButtons.Count, correctSequence.Count); i++)
        {
            symbolButtons[i].gameObject.SetActive(true);
            symbolButtons[i].symbolName = correctSequence[i];
            symbolButtons[i].onSymbolClicked = OnSymbolClicked;
        }
    }

    public void OnSymbolClicked(string symbolName)
    {
        if (currentState == PuzzleState.Solved || isLockedByDependencies) return;
        
        ActivatePuzzle();
        if (IsSpawned) SubmitSymbolServerRpc(symbolName);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitSymbolServerRpc(string symbolName)
    {
        if (isSolvedNet.Value) return;

        serverPlayerSequence.Add(symbolName);
        Debug.Log($"Server recorded symbol: {symbolName} | Sequence Length: {serverPlayerSequence.Count}");

        if (serverPlayerSequence.SequenceEqual(correctSequence))
        {
            isSolvedNet.Value = true; // Solved!
        }
        else if (serverPlayerSequence.Count >= correctSequence.Count)
        {
            // Wrong sequence - reset
            serverPlayerSequence.Clear();
            ResetSequenceClientRpc();
        }
    }

    private void OnSolvedStateChanged(bool prev, bool isSolved)
    {
        if (isSolved)
        {
            CompletePuzzle();
        }
    }

    [ClientRpc]
    private void ResetSequenceClientRpc()
    {
        Debug.Log("❌ Wrong sequence! Resetting...");
        // Add visual/audio failure feedback here
    }

    protected override bool CheckSolution()
    {
        return isSolvedNet.Value;
    }

    public override void ResetPuzzle()
    {
        base.ResetPuzzle();
        if (IsServer)
        {
            serverPlayerSequence.Clear();
            isSolvedNet.Value = false;
        }
    }
}
