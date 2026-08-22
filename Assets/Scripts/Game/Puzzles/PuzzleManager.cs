using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PuzzleManager : MonoBehaviour
{
    [Header("References")]
    public InteractableDoor exitDoor;
    public List<BasePuzzle> puzzles = new List<BasePuzzle>();

    private int solvedPuzzleCount = 0;

    void Start()
    {
        // Auto-find all puzzles in the scene if list is empty
        if (puzzles.Count == 0)
        {
            puzzles = FindObjectsOfType<BasePuzzle>().ToList();
        }

        // Subscribe to each puzzle's completion event
        foreach (var puzzle in puzzles)
        {
            puzzle.OnPuzzleSolved += OnPuzzleSolved;
        }

        Debug.Log($"PuzzleManager initialized with {puzzles.Count} puzzles.");
    }

    private void OnPuzzleSolved(string puzzleID)
    {
        solvedPuzzleCount++;
        Debug.Log($"🎯 Progress: {solvedPuzzleCount}/{puzzles.Count} puzzles solved!");

        // Check if all puzzles are solved
        if (solvedPuzzleCount >= puzzles.Count)
        {
            UnlockExit();
        }
    }

    private void UnlockExit()
    {
        if (exitDoor != null)
        {
            exitDoor.UnlockDoor();
            Debug.Log("🎉 ALL PUZZLES SOLVED! Door unlocked!");
        }
        else
        {
            Debug.LogWarning("Exit door reference is missing in PuzzleManager!");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        foreach (var puzzle in puzzles)
        {
            if (puzzle != null)
                puzzle.OnPuzzleSolved -= OnPuzzleSolved;
        }
    }
}
