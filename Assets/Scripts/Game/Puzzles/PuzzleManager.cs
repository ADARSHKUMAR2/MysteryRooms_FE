using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MysteryRooms.Multiplayer.Network;
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
            puzzle.OnPuzzleSolvedWithPlayer += OnPuzzleSolved;
        }

        Debug.Log($"PuzzleManager initialized with {puzzles.Count} puzzles.");
    }

    private void OnPuzzleSolved(string puzzleID, ulong solverClientId, string solverFirebaseUid)
    {
        solvedPuzzleCount++;
        Debug.Log($"🎯 Progress: {solvedPuzzleCount}/{puzzles.Count} puzzles solved!");

        // Check if all puzzles are solved
        if (solvedPuzzleCount >= puzzles.Count)
        {
            UnlockExit();
        }

        NetworkedPlayerController player = GetComponent<NetworkedPlayerController>();
        if (player != null)
        {
            player.OnPuzzleSolved(puzzleID);
        }
        
        // Update networked puzzle manager
        NetworkedPuzzleManager npm = FindObjectOfType<NetworkedPuzzleManager>();
        if (npm != null)
        {
            npm.MarkPuzzleSolved(puzzleID);
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
            // if (puzzle != null)
                // if (puzzle != null) puzzle.OnPuzzleSolvedWithPlayer -= LocalPuzzleSolved;
        }
    }
}
