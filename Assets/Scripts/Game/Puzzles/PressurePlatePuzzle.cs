using UnityEngine;
using MysteryRooms.Game.Data;
using System.Collections.Generic;

public class PressurePlatePuzzle : BasePuzzle, IInteractable
{
    private List<int> correctPattern;
    
    public override void ConfigureFromBackend(PuzzleConfigData config)
    {
        base.ConfigureFromBackend(config);
        if (config.config != null)
            correctPattern = config.config.correctPattern;
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
        CompletePuzzle(); // Auto solve for testing
    }

    protected override bool CheckSolution() { return true; }
}
