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
    private void SubmitPlateStepServerRpc(int plateID, ServerRpcParams rpcParams = default)
    {
        if (isSolvedNet.Value) return;

        // Prevent double triggering if they stand on it
        if (syncedPlayerPattern.Contains(plateID)) return;

        syncedPlayerPattern.Add(plateID);
        Debug.Log($"[PressurePlates] Step recorded: {plateID}. Sequence so far: {syncedPlayerPattern.Count}/{correctPattern.Count}");

        List<int> currentAttempt = new List<int>();
        foreach (int step in syncedPlayerPattern)
        {
            currentAttempt.Add(step);
        }

        if (currentAttempt.SequenceEqual(correctPattern))
        {
            isSolvedNet.Value = true; // Solved!
            InvokeOnPuzzleSolved(rpcParams.Receive.SenderClientId, "unknown_firebase_id");
        }
        else if (currentAttempt.Count >= correctPattern.Count || !IsMatchingSoFar(currentAttempt))
        {
            // Wrong sequence - fail and reset
            TriggerErrorVisualClientRpc();
            
            // Clear the list after a delay so players see the red flash first
            StartCoroutine(ClearSequenceAfterDelayServerRoutine());
        }
    }

    private IEnumerator ClearSequenceAfterDelayServerRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        syncedPlayerPattern.Clear();
    }

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

        // Check if pattern is wrong right now
        List<int> currentAttempt = new List<int>();
        foreach (int step in syncedPlayerPattern) { currentAttempt.Add(step); }
        bool isWrong = currentAttempt.Count > 0 && !IsMatchingSoFar(currentAttempt);

        foreach (var plate in physicalPlates)
        {
            // Only update plates that have already risen
            if (isLockedByDependencies) continue;

            if (isSolvedNet.Value)
            {
                // Puzzle solved! All plates press down and glow gold
                plate.PressPlate(true);
            }
            else if (syncedPlayerPattern.Contains(plate.plateID))
            {
                // This specific plate was stepped on
                // If the sequence is wrong so far, make it glow red. If correct, glow gold.
                plate.PressPlate(!isWrong);
            }
            else
            {
                // Plate is waiting to be stepped on
                plate.ResetPlate();
            }
        }
    }

    // Overriding SetLocked is the secret to making them RISE UP dynamically!
    public override void SetLocked(bool locked)
    {
        bool wasLocked = isLockedByDependencies;
        base.SetLocked(locked); 
        
        // If it JUST unlocked, tell all plates to rise out of the ground!
        if (wasLocked && !locked)
        {
            StartCoroutine(StaggeredRiseRoutine());
        }
    }

    /// <summary>
    /// Makes the plates rise out of the floor one by one for a cool cinematic effect
    /// </summary>
    private IEnumerator StaggeredRiseRoutine()
    {
        if (physicalPlates == null) yield break;

        // Shake the camera here if you have a camera shake script!
        
        foreach (var plate in physicalPlates)
        {
            plate.RevealPlate();
            yield return new WaitForSeconds(0.4f); // Stagger the rising effect
        }
    }

    private void OnSolvedStateChanged(bool prev, bool isSolved)
    {
        if (isSolved)
        {
            UpdatePlateVisuals(); 
        }
    }

    [ClientRpc]
    private void TriggerErrorVisualClientRpc()
    {
        Debug.Log("❌ Wrong plate sequence! Resetting...");
        // Sound effect plays locally via the plate's press method
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
