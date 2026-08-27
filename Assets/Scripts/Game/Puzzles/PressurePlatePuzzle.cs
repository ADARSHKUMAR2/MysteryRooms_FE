using UnityEngine;
using MysteryRooms.Game.Data;
using System.Collections.Generic;
using Unity.Netcode;
using System.Linq;
using System.Collections;

public class PressurePlatePuzzle : BasePuzzle
{
    [Header("Physical Plates")]
    [Tooltip("Drag the individual plate objects from the scene here")]
    [SerializeField] private List<PhysicalPressurePlate> physicalPlates;
    
    private List<int> correctPattern = new List<int>();
    
    // Tracks the current sequence of plates stepped on
    private NetworkList<int> syncedPlayerPattern;
    
    private NetworkVariable<bool> isSolvedNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        syncedPlayerPattern = new NetworkList<int>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isSolvedNet.OnValueChanged += OnSolvedStateChanged;
        syncedPlayerPattern.OnListChanged += OnPatternChanged;

        if (isSolvedNet.Value) OnSolvedStateChanged(false, true);
        
        UpdatePlateVisuals();
    }

    public override void OnNetworkDespawn()
    {
        isSolvedNet.OnValueChanged -= OnSolvedStateChanged;
        syncedPlayerPattern.OnListChanged -= OnPatternChanged;
        base.OnNetworkDespawn();
    }

    public override void ConfigureFromBackend(PuzzleConfigData config)
    {
        base.ConfigureFromBackend(config);
        
        if (config.config != null && config.config.correctPattern != null)
        {
            correctPattern = config.config.correctPattern;
            AssignPlates();
        }
    }

    private void AssignPlates()
    {
        if (physicalPlates == null) return;
        foreach (var plate in physicalPlates)
        {
            plate.onPlateStepped = OnPlateStepped;
        }
    }

    public void OnPlateStepped(int plateID)
    {
        if (currentState == PuzzleState.Locked || currentState == PuzzleState.Solved) return;

        Debug.Log($"[PressurePlatePuzzle] 💡 CHEAT CODE: The correct pattern is: {string.Join(", ", correctPattern)}");
       
        ActivatePuzzle();
        if (IsSpawned) SubmitPlateStepServerRpc(plateID);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitPlateStepServerRpc(int plateID, ServerRpcParams rpcParams = default) // Add Params!
    {
        if (isSolvedNet.Value) return;

        // Prevent double triggering if they stand on it
        if (syncedPlayerPattern.Contains(plateID)) return;

        syncedPlayerPattern.Add(plateID);
        Debug.Log($"[PressurePlates] Step recorded: {plateID}. Sequence so far: {syncedPlayerPattern.Count}/{correctPattern.Count}");

        // Convert network list to standard list for comparison
        List<int> currentAttempt = new List<int>();
        foreach (int step in syncedPlayerPattern)
        {
            currentAttempt.Add(step);
        }

        if (currentAttempt.SequenceEqual(correctPattern))
        {
            isSolvedNet.Value = true; // Solved!

            // FIRE THE EVENT TO GIVE POINTS AND TELL DYNAMIC PUZZLE MANAGER!
            InvokeOnPuzzleSolved(rpcParams.Receive.SenderClientId, "unknown_firebase_id");
      
        }
        else if (currentAttempt.Count >= correctPattern.Count || !IsMatchingSoFar(currentAttempt))
        {
            // Wrong sequence - fail and reset immediately
            TriggerErrorVisualClientRpc();
            syncedPlayerPattern.Clear();
        }
    }

    // Helper to reset early if they make a mistake midway
    private bool IsMatchingSoFar(List<int> attempt)
    {
        for (int i = 0; i < attempt.Count; i++)
        {
            if (attempt[i] != correctPattern[i]) return false;
        }
        return true;
    }

    private void OnPatternChanged(NetworkListEvent<int> changeEvent)
    {
        UpdatePlateVisuals();
    }

    private void UpdatePlateVisuals()
    {
        if (physicalPlates == null) return;

        foreach (var plate in physicalPlates)
        {
            PhysicalPressurePlate.PlateState state;

            if (isLockedByDependencies)
            {
                // The puzzle hasn't been unlocked yet (show as Black/Inactive)
                state = PhysicalPressurePlate.PlateState.Inactive;
            }
            else if (isSolvedNet.Value || syncedPlayerPattern.Contains(plate.plateID))
            {
                // The puzzle is solved, OR the player has stepped on this specific plate
                state = PhysicalPressurePlate.PlateState.Pressed;
            }
            else
            {
                // Puzzle is active and waiting to be stepped on
                state = PhysicalPressurePlate.PlateState.Default;
            }

            plate.SetVisualState(state);
        }
    }

    // This overrides a method from BasePuzzle to ensure visuals update 
    // the exact moment a dependency puzzle is solved!
     public override void SetLocked(bool locked)
    {
        base.SetLocked(locked); // Calls the BasePuzzle logic to set isLockedByDependencies
        
        UpdatePlateVisuals(); // Instantly update the black/active materials
    }

    private void OnSolvedStateChanged(bool prev, bool isSolved)
    {
        if (isSolved)
        {
            UpdatePlateVisuals(); // Ensure they all light up at the end
        }
    }

    [ClientRpc]
    private void TriggerErrorVisualClientRpc()
    {
        Debug.Log("❌ Wrong plate sequence! Resetting...");
        // You can add an AudioSource.Play() here for an error buzz sound
    }

    protected override bool CheckSolution()
    {
        return isSolvedNet.Value;
    }

    public override void ResetPuzzle()
    {
        base.ResetPuzzle();
        if (IsServer)
        {
            syncedPlayerPattern.Clear();
            isSolvedNet.Value = false;
        }
        UpdatePlateVisuals();
    }
}