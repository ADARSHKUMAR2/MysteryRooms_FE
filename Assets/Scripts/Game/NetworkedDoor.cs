using UnityEngine;
using Unity.Netcode;

public class NetworkedDoor : NetworkBehaviour, IInteractable
{
    [Header("Door Settings")]
    [SerializeField] private bool requiresInteraction = false;
    [SerializeField] private float openSpeed = 0.5f;
    [Header("Door Positions")]
    [Tooltip("The exact position of the door when it is closed (locked).")]
    [SerializeField] private Vector3 closedPosition;
    
    [Tooltip("The exact position of the door when it is open.")]
    [SerializeField] private Vector3 openPosition;

    private NetworkVariable<bool> isOpen = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, //READ
        NetworkVariableWritePermission.Server //WRITE
    );

    public override void OnNetworkSpawn()
    {
        // Snap the door to the correct state for players who join late
        if (isOpen.Value)
        {
            transform.position = openPosition;
        }
        else
        {
            transform.position = closedPosition;
        }
    }

    void Update()
    {
        Vector3 targetPosition = isOpen.Value ? openPosition : closedPosition;
        
        if (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * openSpeed);
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
        closedPosition = transform.position;
    }

    [ContextMenu("Set Open Position from Current")]
    private void SetOpenPosition()
    {
        openPosition = transform.position;
    }
}
