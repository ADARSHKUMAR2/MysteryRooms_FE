using UnityEngine;
using MysteryRooms.Game.Data;
using System.Collections.Generic;
using Unity.Netcode;

public class LightPuzzle : BasePuzzle, IInteractable
{
    private List<int> correctTorchOrder;

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
        if (config.config != null) correctTorchOrder = config.config.correctTorchOrder;
    }

    public string GetInteractionPrompt()
    {
        if (currentState == PuzzleState.Solved) return "Lights aligned ✓";
        if (isLockedByDependencies) return "The lights won't ignite";
        return "Press E to arrange lights";
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
