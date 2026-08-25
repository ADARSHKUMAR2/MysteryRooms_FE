using UnityEngine;
using MysteryRooms.Game.Data;
using System.Collections.Generic;
using Unity.Netcode;

public class PressurePlatePuzzle : BasePuzzle, IInteractable
{
    private List<int> correctPattern;
    private NetworkVariable<bool> isSolvedNet = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isSolvedNet.OnValueChanged += (prev, current) => { if (current) CompletePuzzle(); };
        if (isSolvedNet.Value) CompletePuzzle();
    }

    public override void ConfigureFromBackend(PuzzleConfigData config)
    {
        base.ConfigureFromBackend(config);
        if (config.config != null) correctPattern = config.config.correctPattern;
    }

    public string GetInteractionPrompt()
    {
        if (currentState == PuzzleState.Solved) return "Plates activated ✓";
        if (isLockedByDependencies) return "Plates are inactive";
        return "Press E to step on plates";
    }

    public void Interact()
    {
        if (currentState == PuzzleState.Solved || isLockedByDependencies) return;
        ActivatePuzzle();
        if (IsSpawned) SolveServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SolveServerRpc() { isSolvedNet.Value = true; }

    protected override bool CheckSolution() { return isSolvedNet.Value; }
}
