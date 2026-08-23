using UnityEngine;
using MysteryRooms.Game.Data;
using System.Collections.Generic;

public class LightPuzzle : BasePuzzle, IInteractable
{
    private List<int> correctTorchOrder;

    public override void ConfigureFromBackend(PuzzleConfigData config)
    {
        base.ConfigureFromBackend(config);
        if (config.config != null)
            correctTorchOrder = config.config.correctTorchOrder;
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
        CompletePuzzle(); // Auto solve for testing
    }

    protected override bool CheckSolution() { return true; }
}
