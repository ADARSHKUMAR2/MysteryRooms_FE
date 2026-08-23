using UnityEngine;
using MysteryRooms.Game.Data;

public class HiddenCompartmentPuzzle : BasePuzzle, IInteractable
{
    private bool requiresKey;

    public override void ConfigureFromBackend(PuzzleConfigData config)
    {
        base.ConfigureFromBackend(config);
        if (config.config != null)
            requiresKey = config.config.requiresKey;
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
        CompletePuzzle(); // Auto-solve for testing
    }

    protected override bool CheckSolution() { return true; }
}
