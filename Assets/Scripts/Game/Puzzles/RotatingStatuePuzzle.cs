using UnityEngine;
using MysteryRooms.Game.Data;
using Unity.Netcode; 
using MysteryRooms.Multiplayer.Network;

public class RotatingStatuePuzzle : BasePuzzle, IInteractable
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 90f; // Degrees per second
    public int correctRotationSteps = 2; 

     // 1. Replace local int with a NetworkVariable
    private NetworkVariable<int> currentRotationSteps = new NetworkVariable<int>(
        0, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    ); // How many 90° rotations to solve

    private bool isRotating = false;
    private Quaternion targetRotation;

    public string GetInteractionPrompt()
    {
        if (currentState == PuzzleState.Solved)
            return "Statue aligned correctly ✓";
            
        if (currentState == PuzzleState.Locked)
            return "Statue is locked by a mysterious force";        
            
        return "Press E to Rotate Statue";
    }

    // 2. Hook into Network spawn to set up listeners and handle late-joiners
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        currentRotationSteps.OnValueChanged += OnRotationStepsChanged;
        
        // Snap to correct rotation immediately for late joining players
        targetRotation = Quaternion.Euler(0, currentRotationSteps.Value * 90f, 0);
        transform.rotation = targetRotation;
    }
    public override void OnNetworkDespawn()
    {
        currentRotationSteps.OnValueChanged -= OnRotationStepsChanged;
        base.OnNetworkDespawn();
    }

    public void Interact()
    {
        if (currentState == PuzzleState.Locked || currentState == PuzzleState.Solved || isRotating) return;

        ActivatePuzzle();
        RequestRotateServerRpc(); 
    }

    // 4. Server updates the authoritative state
    [ServerRpc(RequireOwnership = false)]
    private void RequestRotateServerRpc(ServerRpcParams rpcParams = default)
    {
        int nextStep = (currentRotationSteps.Value + 1) % 4;
        currentRotationSteps.Value = nextStep;

        if (nextStep == correctRotationSteps)
        {
            // The Server knows this is correct!
            // Tell the NetworkManager to mark it solved, passing the exact ID of the sender!        
            // Give the point to the specific client who rotated it!
            // if (NetworkedScoreboard.Instance != null)
            // {
            //     Debug.Log($"✅  puzzle solved: )");
            //     NetworkedScoreboard.Instance.IncrementPlayerScoreServerRpc(rpcParams.Receive.SenderClientId);
            // }    

            // Set the base state to Solved (This triggers the base OnPuzzleSolved action)
            currentState = PuzzleState.Solved;
            
            // Fire the base event so DynamicPuzzleManager catches it
            // and correctly routes it through NetworkedPuzzleManager!
             InvokeOnPuzzleSolved(rpcParams.Receive.SenderClientId, "solver_firebase_id_here");
        }
    }

    // 5. This fires on all clients whenever the server updates the variable
    private void OnRotationStepsChanged(int previousValue, int newValue)
    {
        targetRotation = Quaternion.Euler(0, newValue * 90f, 0);
        isRotating = true; // This tells the local Update() loop to start animating
        // if (CheckSolution())
        // {
        //     CompletePuzzle();
        // }
        Debug.Log($"{gameObject.name} rotated to step {newValue}");
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
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
            {
                transform.rotation = targetRotation;
                isRotating = false;
            }
        }
    }

    protected override bool CheckSolution()
    {
        return currentRotationSteps.Value == correctRotationSteps;
    }

    public override void ResetPuzzle()
    {
        base.ResetPuzzle();
        // Only the server has permission to change the NetworkVariable
        if (IsServer)
        {
            currentRotationSteps.Value = 0;
        }
        transform.rotation = Quaternion.identity;
    }
}
