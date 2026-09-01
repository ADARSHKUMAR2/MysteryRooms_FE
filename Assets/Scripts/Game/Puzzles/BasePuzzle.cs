using UnityEngine;
using System;
using MysteryRooms.Game.Data;
using Unity.Netcode;
public enum PuzzleState
{
    Locked,      // Waiting on previous puzzles to be solved
    Unlocked,    // Ready to be interacted with
    InProgress,  // Player has started interacting with it
    Solved       // Puzzle is complete
}

public abstract class BasePuzzle : NetworkBehaviour
{
    [Header("Puzzle Settings")]
    public string puzzleID;
    // Default to Locked! The Manager will Unlock the starting puzzles later.
    public PuzzleState currentState = PuzzleState.Locked;
    [Header("Spawn Settings")]
    [Tooltip("Adjust this if the prefab spawns too high, low, or deep into the wall at a socket.")]
    public Vector3 spawnOffset = Vector3.zero;

    [Header("Backend Configuration")]
    public PuzzleConfigData backendConfig;
    protected bool isLockedByDependencies = false;

    [Header("Backend Configuration Status")]
    // Shows up in the Inspector so you can see if the backend claimed it!
    [SerializeField] public bool isConfiguredByBackend = false;
    [Header("Feedback Visuals")]
    [Tooltip("Optional Light or GameObject that turns ON when puzzle is Unlocked, and OFF when Locked/Solved")]
    public GameObject puzzleHighlightLight;

    // Change this event to pass (puzzleID, clientId, firebaseUid)
    public event System.Action<string, ulong, string> OnPuzzleSolvedWithPlayer;

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
        isConfiguredByBackend = true;
        
        Debug.Log($"🔧 Configuring {puzzleID} with backend data");
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        UpdateHighlightLight(); // Ensure late-joiners see correct light state
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
        else
        {
            // When dependencies are met, it becomes Unlocked!
            if (currentState == PuzzleState.Locked) 
            {
                currentState = PuzzleState.Unlocked;
            }
        }

        UpdateHighlightLight(); // Update light when dependencies unlock it!
        Debug.Log($"{puzzleID} is now {(locked ? "LOCKED" : "UNLOCKED")}");
    }

    // Check if the puzzle has been solved
    protected abstract bool CheckSolution();

    // Called when player interacts with the puzzle
    public virtual void ActivatePuzzle()
    {
        if (isLockedByDependencies || currentState == PuzzleState.Locked)
        {
            Debug.Log($"❌ {puzzleID} is locked. Solve other puzzles first!");
            return;
        }

        // Move from Unlocked to InProgress when the player touches it
        if (currentState == PuzzleState.Unlocked)
        {
            currentState = PuzzleState.InProgress;
            Debug.Log($"Puzzle {puzzleID} activated (In Progress)!");
            UpdateHighlightLight();
        }
    }

    // Call this when puzzle conditions are met
    protected void CompletePuzzle()
    {
        if (currentState != PuzzleState.Solved)
        {
            currentState = PuzzleState.Solved;
            Debug.Log($"✅ Puzzle {puzzleID} SOLVED ");
            UpdateHighlightLight();
            
            // Pass BOTH the puzzle ID and the solver ID to the Manager!
            // OnPuzzleSolved?.Invoke(puzzleID);
        }
    }

    protected void InvokeOnPuzzleSolved(ulong solverClientId, string solverFirebaseUid)
    {
        // 1. Update the state on ALL machines (so the Inspector shows "Solved" for clients too)
        SyncPuzzleStateClientRpc(PuzzleState.Solved);
        
        // 1. Give the point to the specific client who solved it!
        if (NetworkedScoreboard.Instance != null && NetworkManager.Singleton.IsServer)
        {
            NetworkedScoreboard.Instance.IncrementPlayerScoreServerRpc(solverClientId);
        }

        // 2. Pass the data up the chain to DynamicPuzzleManager
        OnPuzzleSolvedWithPlayer?.Invoke(puzzleID, solverClientId, solverFirebaseUid);
    }

    [ClientRpc]
    protected void SyncPuzzleStateClientRpc(PuzzleState newState)
    {
        currentState = newState;
        UpdateHighlightLight(); // Turn off light for all clients when solved!
    }


    // Reset puzzle to initial state
    public virtual void ResetPuzzle()
    {
        // If it resets, it goes back to Unlocked (unless dependencies force it back to locked later)
        currentState = PuzzleState.Unlocked;
        isLockedByDependencies = false;
        Debug.Log($"Puzzle {puzzleID} reset.");
        UpdateHighlightLight();
    }

    /// <summary>
    /// Turns the assigned highlight light ON when unlocked/in-progress, and OFF when locked/solved.
    /// </summary>
    protected virtual void UpdateHighlightLight()
    {
        if (puzzleHighlightLight != null)
        {
            bool shouldBeOn = (currentState == PuzzleState.Unlocked || currentState == PuzzleState.InProgress);
            puzzleHighlightLight.SetActive(shouldBeOn);
        }
    }
}
