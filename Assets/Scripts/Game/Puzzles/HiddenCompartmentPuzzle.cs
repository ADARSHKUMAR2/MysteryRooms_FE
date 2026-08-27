using UnityEngine;
using MysteryRooms.Game.Data;
using Unity.Netcode;

public class HiddenCompartmentPuzzle : BasePuzzle, IInteractable
{
    [Header("Compartment Settings")]
    [Tooltip("The physical object (door/lid/panel) that will move when opened")]
    [SerializeField] private Transform movingPanel;
    
    [Tooltip("Where the panel moves TO when opened (relative to its starting position)")]
    [SerializeField] private Vector3 openOffset = new Vector3(0, 1f, 0); // E.g., slide up 1 unit
    [SerializeField] private float animationSpeed = 2f;

    public bool requiresKey = false;
    
    // Animation State
    private Vector3 closedPosition;
    private Vector3 targetPosition;
    private bool isAnimating = false;

    private NetworkVariable<bool> isSolvedNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    protected override void Start()
    {
        base.Start();
        if (movingPanel != null)
        {
            closedPosition = movingPanel.localPosition;
            targetPosition = closedPosition;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isSolvedNet.OnValueChanged += OnSolvedStateChanged;
        
        // Late joiner support - snap open immediately if already solved
        if (isSolvedNet.Value) 
        {
            if (movingPanel != null)
            {
                movingPanel.localPosition = closedPosition + openOffset;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        isSolvedNet.OnValueChanged -= OnSolvedStateChanged;
        base.OnNetworkDespawn();
    }

    public override void ConfigureFromBackend(PuzzleConfigData config)
    {
        base.ConfigureFromBackend(config);
        
        if (config.config != null)
        {
            // For older backend configs, this might be null. Default to false if not found.
            requiresKey = config.config.requiresKey;
            Debug.Log($"[HiddenCompartment] {puzzleID} configured. Requires Key: {requiresKey}");
        }
    }

    public string GetInteractionPrompt()
    {
        if (currentState == PuzzleState.Solved) 
            return "Compartment is open";
            
        if (currentState == PuzzleState.Locked) 
            return "It seems sealed shut tightly.";

        // If it's Unlocked, tell them what they need
        if (requiresKey)
            return "Press E to unlock compartment (Requires Key)";
            
        return "Press E to open compartment";
    }

    public void Interact()
    {
        if (currentState == PuzzleState.Locked || currentState == PuzzleState.Solved) return;
        
        ActivatePuzzle();

        if (requiresKey)
        {
            bool hasKey = false;
            
            if (MysteryRooms.Game.Managers.InventoryManager.Instance != null)
            {
                hasKey = MysteryRooms.Game.Managers.InventoryManager.Instance.HasItem(ItemType.TombKey);
            }

            if (!hasKey)
            {
                Debug.Log($"[HiddenCompartment] You need the '{ItemType.TombKey}' to open {puzzleID}!");
                // Optionally show a UI message here
                
                // Reset state back to Unlocked so they can try again later
                currentState = PuzzleState.Unlocked; 
                return; 
            }
        }

        if (IsSpawned) SolveServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SolveServerRpc(ServerRpcParams rpcParams = default) // Add Params!
    { 
        if (!isSolvedNet.Value)
        {
            isSolvedNet.Value = true; 
            // FIRE THE EVENT TO GIVE POINTS AND TELL DYNAMIC PUZZLE MANAGER!
            InvokeOnPuzzleSolved(rpcParams.Receive.SenderClientId, "unknown_firebase_id");
       
        }
    }

    private void OnSolvedStateChanged(bool prev, bool current)
    {
        if (current)
        {
            // Start the physical animation!
            if (movingPanel != null)
            {
                targetPosition = closedPosition + openOffset;
                isAnimating = true;
            }
        }
    }

    private void Update()
    {
        // Handle the physical sliding animation locally on all clients
        if (isAnimating && movingPanel != null)
        {
            movingPanel.localPosition = Vector3.Lerp(
                movingPanel.localPosition, 
                targetPosition, 
                Time.deltaTime * animationSpeed
            );

            // Stop animating once it's close enough
            if (Vector3.Distance(movingPanel.localPosition, targetPosition) < 0.01f)
            {
                movingPanel.localPosition = targetPosition;
                isAnimating = false;
            }
        }
    }

    protected override bool CheckSolution() { return isSolvedNet.Value; }

    public override void ResetPuzzle()
    {
        base.ResetPuzzle();
        if (IsServer) isSolvedNet.Value = false;
        
        if (movingPanel != null)
        {
            targetPosition = closedPosition;
            isAnimating = true; // Animate it closing!
        }
    }
}
