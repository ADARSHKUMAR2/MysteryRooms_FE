using UnityEngine;
using MysteryRooms.Game.Data;
using Unity.Netcode;

public class MapCoordinatesPuzzle : BasePuzzle, IInteractable
{
    private string correctCoordinates;
    private NetworkVariable<bool> isSolvedNet = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    public override void ConfigureFromBackend(PuzzleConfigData config)
    {
        base.ConfigureFromBackend(config);
        if (config.config != null) correctCoordinates = config.config.correctCoordinates;
    }

    public string GetInteractionPrompt()
    {
        if (currentState == PuzzleState.Solved) return "Coordinates found ✓";
        if (isLockedByDependencies) return "The map makes no sense yet";
        return $"Press E to read map ({correctCoordinates})";
    }

    public void Interact()
    {
        if (currentState == PuzzleState.Solved || isLockedByDependencies) return;
        ActivatePuzzle();
        if (IsSpawned) SolveServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SolveServerRpc(ServerRpcParams rpcParams = default) // Add Params!
    { 
        if (!isSolvedNet.Value)
        {
            isSolvedNet.Value = true; 
            InvokeOnPuzzleSolved(rpcParams.Receive.SenderClientId, "unknown_firebase_id");
        }
    }

    protected override bool CheckSolution() { return isSolvedNet.Value; }
}
