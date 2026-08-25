using UnityEngine;
using System;
using MysteryRooms.Game.Data;
using Unity.Netcode;
public enum PuzzleState
{
    Locked,
    InProgress,
    Solved
}

public abstract class BasePuzzle : NetworkBehaviour
{
    [Header("Puzzle Settings")]
    public string puzzleID;
    public PuzzleState currentState = PuzzleState.Locked;

    [Header("Backend Configuration")]
    protected PuzzleConfigData backendConfig;
    protected bool isLockedByDependencies = false;

    // Event triggered when puzzle is solved
    public event Action<string> OnPuzzleSolved;

    protected virtual void Start()
    {
        if (string.IsNullOrEmpty(puzzleID))
        {
            puzzleID = gameObject.name;
        }
    }

    /// <summary>
    /// Configure puzzle from backend data - OVERRIDE THIS
    /// </summary>
    public virtual void ConfigureFromBackend(PuzzleConfigData config)
    {
        backendConfig = config;
        puzzleID = config.id;
        
        Debug.Log($"🔧 Configuring {puzzleID} with backend data");
    }

    /// <summary>
    /// Lock/unlock puzzle based on dependencies
    /// </summary>
    public virtual void SetLocked(bool locked)
    {
        isLockedByDependencies = locked;
        
        if (locked)
        {
            currentState = PuzzleState.Locked;
        }
        
        Debug.Log($"{puzzleID} is now {(locked ? "LOCKED" : "UNLOCKED")}");
    }

    // Check if the puzzle has been solved
    protected abstract bool CheckSolution();

    // Called when player interacts with the puzzle
    public virtual void ActivatePuzzle()
    {
        if (isLockedByDependencies)
        {
            Debug.Log($"❌ {puzzleID} is locked. Solve other puzzles first!");
            return;
        }

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
        isLockedByDependencies = false;
        Debug.Log($"Puzzle {puzzleID} reset.");
    }
}
