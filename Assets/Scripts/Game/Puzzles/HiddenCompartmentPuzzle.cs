using UnityEngine;
using MysteryRooms.Game.Data;
using Unity.Netcode;

public class HiddenCompartmentPuzzle : BasePuzzle, IInteractable
{
    private bool requiresKey;
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
        if (config.config != null) requiresKey = config.config.requiresKey;
    }

    public string GetInteractionPrompt()
    {
        if (currentState == PuzzleState.Solved) return "Compartment opened ✓";
        if (isLockedByDependencies) return "It's sealed tight";
        return "Press E to search compartment";
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
