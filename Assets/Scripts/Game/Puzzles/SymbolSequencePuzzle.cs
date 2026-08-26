using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MysteryRooms.Game.Data;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.UI; 

public class SymbolSequencePuzzle : BasePuzzle
{
    [Header("Data References")]
    [SerializeField] private SymbolDatabase symbolDatabase;

    [Header("Symbols")]
    [SerializeField] private List<SymbolButton> symbolButtons;
    [Tooltip("The UI slots showing the current sequence attempt (e.g. at the top of the wall)")]
    [SerializeField] private List<Image> sequenceAttemptPlaceholders;

    private List<string> correctSequence;
    
    // Server maintains the current sequence attempts
    private List<string> serverPlayerSequence = new List<string>();

    // Netcode: NetworkList automatically syncs the list to all clients (including late joiners)
    private NetworkList<FixedString32Bytes> syncedPlayerSequence;

    private NetworkVariable<bool> isSolvedNet = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        // NetworkLists must be initialized in Awake
        syncedPlayerSequence = new NetworkList<FixedString32Bytes>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        isSolvedNet.OnValueChanged += OnSolvedStateChanged;
        syncedPlayerSequence.OnListChanged += OnSequenceChanged;
        
        // Handle Late Joiners
        if (isSolvedNet.Value) OnSolvedStateChanged(false, true);
        
        // Force a visual update for late joiners so they see the current attempt
        UpdateSequenceVisuals();
    }

    public override void OnNetworkDespawn()
    {
        isSolvedNet.OnValueChanged -= OnSolvedStateChanged;
        syncedPlayerSequence.OnListChanged -= OnSequenceChanged;
        base.OnNetworkDespawn();
    }

    public override void ConfigureFromBackend(PuzzleConfigData config)
    {
        base.ConfigureFromBackend(config);

        if (config.config != null && config.config.correctSequence != null)
        {
            correctSequence = config.config.correctSequence;
            AssignSymbolsToButtons();
        }
    }

    private void AssignSymbolsToButtons()
    {
        if (symbolButtons == null || symbolButtons.Count == 0 || symbolDatabase == null) return;

        // Note: For a real game, you would mix the correct sequence with decoy symbols
        // For now, we assign the sequence directly to the buttons based on your original logic
        for (int i = 0; i < Mathf.Min(symbolButtons.Count, correctSequence.Count); i++)
        {
            string symbolName = correctSequence[i];
            Sprite symbolSprite = symbolDatabase.GetSprite(symbolName);

            symbolButtons[i].gameObject.SetActive(true);
            symbolButtons[i].symbolName = symbolName;
            
            // Assuming your SymbolButton has a reference to its Image component (e.g. buttonImage.sprite = symbolSprite)
            symbolButtons[i].SetSprite(symbolSprite); 
            
            symbolButtons[i].onSymbolClicked = OnSymbolClicked;
        }

        if (sequenceAttemptPlaceholders != null)
        {
            // Initialize placeholders (hide them until a symbol is pressed)
            foreach (var placeholder in sequenceAttemptPlaceholders)
            {
                if(placeholder != null)
                {
                    placeholder.gameObject.SetActive(false);
                    placeholder.sprite = null;
                }
            }
        }
    }

    public void OnSymbolClicked(string symbolName)
    {
        if (currentState == PuzzleState.Locked || currentState == PuzzleState.Solved) return;
        
        ActivatePuzzle();
        if (IsSpawned) SubmitSymbolServerRpc(symbolName);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitSymbolServerRpc(string symbolName)
    {
        if (isSolvedNet.Value) return;

        // Add to synced list
        syncedPlayerSequence.Add(new FixedString32Bytes(symbolName));
        Debug.Log($"Server recorded symbol: {symbolName} | Sequence Length: {syncedPlayerSequence.Count}");

        // Convert FixedString back to normal string for comparison
        List<string> currentAttempt = new List<string>();
        foreach (var str in syncedPlayerSequence)
        {
            currentAttempt.Add(str.ToString());
        }

        if (currentAttempt.SequenceEqual(correctSequence))
        {
            isSolvedNet.Value = true; // Solved!
            syncedPlayerSequence.Clear(); // Clear the board visually
        }
        else if (currentAttempt.Count >= correctSequence.Count)
        {
            // Wrong sequence - tell clients to play error animation, then reset
            TriggerErrorVisualClientRpc();
            syncedPlayerSequence.Clear();
        }
    }

    // This fires automatically on ALL clients whenever an item is added/removed from the NetworkList
    private void OnSequenceChanged(NetworkListEvent<FixedString32Bytes> changeEvent)
    {
        UpdateSequenceVisuals();
    }

    private void UpdateSequenceVisuals()
    {
        if (symbolDatabase == null || sequenceAttemptPlaceholders == null) return;

        // Loop through placeholders and update them based on the current synced list
        for (int i = 0; i < sequenceAttemptPlaceholders.Count; i++)
        {
            if (sequenceAttemptPlaceholders[i] == null) continue;

            if (i < syncedPlayerSequence.Count)
            {
                // We have a symbol for this slot
                string symName = syncedPlayerSequence[i].ToString();
                sequenceAttemptPlaceholders[i].sprite = symbolDatabase.GetSprite(symName);
                sequenceAttemptPlaceholders[i].gameObject.SetActive(true);
            }
            else
            {
                // Slot is empty
                sequenceAttemptPlaceholders[i].sprite = null;
                sequenceAttemptPlaceholders[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnSolvedStateChanged(bool prev, bool isSolved)
    {
        if (isSolved)
        {
            CompletePuzzle();
        }
    }

    [ClientRpc]
    private void TriggerErrorVisualClientRpc()
    {
        Debug.Log("❌ Wrong sequence! Resetting...");
        // TODO: Add visual/audio failure feedback here (e.g. flash placeholders red before they clear)
    }

    [ClientRpc]
    private void ResetSequenceClientRpc()
    {
        Debug.Log("❌ Wrong sequence! Resetting...");
        // Add visual/audio failure feedback here
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
            serverPlayerSequence.Clear();
            isSolvedNet.Value = false;
        }
    }
}
