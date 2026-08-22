using UnityEngine;
using System;

public enum PuzzleState
{
    Locked,
    InProgress,
    Solved
}

public abstract class BasePuzzle : MonoBehaviour
{
    [Header("Puzzle Settings")]
    public string puzzleID;
    public PuzzleState currentState = PuzzleState.Locked;

    // Event triggered when puzzle is solved
    public event Action<string> OnPuzzleSolved;

    protected virtual void Start()
    {
        if (string.IsNullOrEmpty(puzzleID))
        {
            puzzleID = gameObject.name;
        }
    }

    // Check if the puzzle has been solved
    protected abstract bool CheckSolution();

    // Called when player interacts with the puzzle
    public virtual void ActivatePuzzle()
    {
        if (currentState == PuzzleState.Locked)
        {
            currentState = PuzzleState.InProgress;
            Debug.Log($"Puzzle {puzzleID} activated!");
        }
    }

    // Call this when puzzle conditions are met
    protected void CompletePuzzle()
    {
        if (currentState != PuzzleState.Solved)
        {
            currentState = PuzzleState.Solved;
            Debug.Log($"✅ Puzzle {puzzleID} SOLVED!");
            OnPuzzleSolved?.Invoke(puzzleID);
        }
    }

    // Reset puzzle to initial state
    public virtual void ResetPuzzle()
    {
        currentState = PuzzleState.Locked;
        Debug.Log($"Puzzle {puzzleID} reset.");
    }
}
