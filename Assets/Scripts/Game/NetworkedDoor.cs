using UnityEngine;
using Unity.Netcode;

public class NetworkedDoor : NetworkBehaviour, IInteractable
{
    [Header("Door Settings")]
    [SerializeField] private bool requiresInteraction = false;
    [SerializeField] private float openSpeed = 2.0f; // Increased slightly so it doesn't take forever to open

    [Header("Door Positions (Local Space)")]
    [Tooltip("The exact LOCAL position of the door when it is closed (locked).")]
    [SerializeField] private Vector3 closedPosition;
    
    [Tooltip("The exact LOCAL position of the door when it is open.")]
    [SerializeField] private Vector3 openPosition;

    private NetworkVariable<bool> isOpen = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        // Snap the door to the correct state for players who join late
        if (isOpen.Value)
        {
            transform.localPosition = openPosition;
        }
        else
        {
            transform.localPosition = closedPosition;
        }
    }

    void Update()
    {
        Vector3 targetPosition = isOpen.Value ? openPosition : closedPosition;
        
        // Use localPosition instead of position!
        if (Vector3.Distance(transform.localPosition, targetPosition) > 0.001f)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * openSpeed);
        }
    }

    public void OpenDoor()
    {
        if (IsServer)
        {
            isOpen.Value = true;
        }
    }

    public string GetInteractionPrompt()
    {
        if (isOpen.Value) return "";
        return requiresInteraction ? "Press E to Open Door" : "The door is sealed shut.";
    }

    public void Interact()
    {
        if (isOpen.Value || !requiresInteraction) return;
        
        if (IsSpawned) RequestOpenServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestOpenServerRpc()
    {
        isOpen.Value = true;
    }

    // Optional Editor Helper: You can right-click the component and select these to easily grab the current transform position!
    [ContextMenu("Set Closed Position from Current")]
    private void SetClosedPosition()
    {
        closedPosition = transform.localPosition; // Changed to localPosition
        Debug.Log($"Set Closed Position: {closedPosition}");
    }

    [ContextMenu("Set Open Position from Current")]
    private void SetOpenPosition()
    {
        openPosition = transform.localPosition; // Changed to localPosition
        Debug.Log($"Set Open Position: {openPosition}");
    }
}
