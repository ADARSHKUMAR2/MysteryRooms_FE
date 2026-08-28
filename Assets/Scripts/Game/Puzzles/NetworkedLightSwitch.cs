using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class NetworkedLightSwitch : NetworkBehaviour, IInteractable
{
    [Header("Target Lights")]
    [Tooltip("The GameObjects to turn on/off (e.g. Torches, Fire_Light, or Room Lights)")]
    [SerializeField] private List<GameObject> targetLightObjects;

    [Header("Spawn Points")]
    [Tooltip("Empty GameObjects representing where the switch can randomly spawn. If empty, it stays where you placed it.")]
    [SerializeField] private List<Transform> randomSpawnPoints;

    [Header("Switch Visuals (Optional)")]
    [Tooltip("The physical switch or lever part to rotate when toggled")]
    [SerializeField] private Transform switchLever;
    [SerializeField] private Vector3 onRotation = new Vector3(45f, 0, 0);
    [SerializeField] private Vector3 offRotation = new Vector3(-45f, 0, 0);

    // Network variables to sync the state to all players (including late joiners!)
    private NetworkVariable<bool> isLightOn = new NetworkVariable<bool>(
        true, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> currentSpawnIndex = new NetworkVariable<int>(
        -1, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Subscribe to changes
        isLightOn.OnValueChanged += OnLightStateChanged;
        currentSpawnIndex.OnValueChanged += OnSpawnIndexChanged;

        // The SERVER decides where this switch spawns when the game starts
        if (IsServer && randomSpawnPoints != null && randomSpawnPoints.Count > 0)
        {
            currentSpawnIndex.Value = Random.Range(0, randomSpawnPoints.Count);
        }

        // Apply state instantly for the host and any late-joining clients
        ApplyLightState(isLightOn.Value);
        ApplySpawnPosition(currentSpawnIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        isLightOn.OnValueChanged -= OnLightStateChanged;
        currentSpawnIndex.OnValueChanged -= OnSpawnIndexChanged;
        base.OnNetworkDespawn();
    }

    // --- INTERACTION LOGIC ---

    public string GetInteractionPrompt()
    {
        return isLightOn.Value ? "Press E to Turn OFF" : "Press E to Turn ON";
    }

    public void Interact()
    {
        if (!IsSpawned) return;

        // Client asks the server to flip the switch
        ToggleLightServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleLightServerRpc()
    {
        // Server flips the boolean. This automatically triggers OnLightStateChanged on ALL clients!
        isLightOn.Value = !isLightOn.Value;
    }

    // --- VISUAL & POSITION UPDATES ---

    private void OnLightStateChanged(bool previous, bool current)
    {
        ApplyLightState(current);
    }

    private void ApplyLightState(bool state)
    {
        // Toggle all assigned lights/torches
        foreach (var lightObj in targetLightObjects)
        {
            if (lightObj != null)
            {
                lightObj.SetActive(state);
            }
        }

        // Animate the lever if assigned
        if (switchLever != null)
        {
            switchLever.localRotation = Quaternion.Euler(state ? onRotation : offRotation);
        }
    }

    private void OnSpawnIndexChanged(int previous, int current)
    {
        ApplySpawnPosition(current);
    }

    private void ApplySpawnPosition(int index)
    {
        // Move the switch to the synced random location
        if (index >= 0 && randomSpawnPoints != null && index < randomSpawnPoints.Count)
        {
            Transform targetSpot = randomSpawnPoints[index];
            transform.position = targetSpot.position;
            transform.rotation = targetSpot.rotation;
        }
    }
}
