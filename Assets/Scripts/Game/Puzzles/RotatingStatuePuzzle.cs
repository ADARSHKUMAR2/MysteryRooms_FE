using UnityEngine;
using MysteryRooms.Game.Data;
public class RotatingStatuePuzzle : BasePuzzle, IInteractable
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 90f; // Degrees per second
    public int correctRotationSteps = 2; // How many 90° rotations to solve

    private int currentRotationSteps = 0;
    private bool isRotating = false;
    private Quaternion targetRotation;

    public string GetInteractionPrompt()
    {
        if (currentState == PuzzleState.Solved)
            return "Statue aligned correctly ✓";
        else if (isLockedByDependencies)
            return "Statue is locked by a mysterious force";        
        else
            return "Press E to Rotate Statue";
    }

    public void Interact()
    {
        if (currentState == PuzzleState.Solved || isRotating) return;

        ActivatePuzzle();
        RotateStatue();
    }

    private void RotateStatue()
    {
        // Increment rotation step (wraps around at 4 = full 360°)
        currentRotationSteps = (currentRotationSteps + 1) % 4;

        // Calculate target rotation (90° increments around Y axis)
        targetRotation = Quaternion.Euler(0, currentRotationSteps * 90f, 0);
        isRotating = true;

        Debug.Log($"{gameObject.name} rotated to step {currentRotationSteps}");
    }

    /// <summary>
    /// Configure from backend data
    /// </summary>
    public override void ConfigureFromBackend(PuzzleConfigData config)
    {
        base.ConfigureFromBackend(config);
        
        // Extract rotation steps from config
        if (config.config != null)
        {
            correctRotationSteps = config.config.correctRotationSteps;
            Debug.Log($"🗿 Statue {puzzleID} configured: correct rotation = {correctRotationSteps} steps");
        }
    }

    void Update()
    {
        if (isRotating)
        {
            // Smoothly rotate to target
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            // Check if rotation is complete
            if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
            {
                transform.rotation = targetRotation;
                isRotating = false;

                // Check if this statue is now correctly aligned
                if (CheckSolution())
                {
                    CompletePuzzle();
                }
            }
        }
    }

    protected override bool CheckSolution()
    {
        // Check if current rotation matches the solution
        return currentRotationSteps == correctRotationSteps;
    }

    public override void ResetPuzzle()
    {
        base.ResetPuzzle();
        currentRotationSteps = 0;
        transform.rotation = Quaternion.identity;
    }
}
