using UnityEngine;
using Unity.Netcode;

public class NetworkedDoor : NetworkBehaviour, IInteractable
{
    [Header("Door Settings")]
    [SerializeField] private bool requiresInteraction = false;
    [SerializeField] private float openSpeed = 0.5f;
    [SerializeField] private Vector3 openOffset = new Vector3(0, 0f, -15f);

    private NetworkVariable<bool> isOpen = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, //READ
        NetworkVariableWritePermission.Server //WRITE
    );

    private Vector3 closedPosition;
    private Vector3 openPosition;

    void Awake()
    {
        closedPosition = transform.position;
        openPosition = closedPosition + openOffset;
    }

    public override void OnNetworkSpawn()
    {
        if (isOpen.Value)
        {
            transform.position = openPosition;
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
}
