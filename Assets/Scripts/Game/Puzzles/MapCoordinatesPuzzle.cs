using UnityEngine;
using MysteryRooms.Game.Data;

public class MapCoordinatesPuzzle : BasePuzzle, IInteractable
{
    private string correctCoordinates;

    public override void ConfigureFromBackend(PuzzleConfigData config)
    {
        base.ConfigureFromBackend(config);
        if (config.config != null)
            correctCoordinates = config.config.correctCoordinates;
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
        CompletePuzzle(); // Auto solve for testing
    }

    protected override bool CheckSolution() { return true; }
}
