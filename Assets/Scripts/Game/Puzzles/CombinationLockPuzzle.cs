using UnityEngine;
using UnityEngine.UI;
using MysteryRooms.Game.Data;
using TMPro;
using Unity.Netcode;

public class CombinationLockPuzzle : BasePuzzle, IInteractable
{
    [Header("Lock Settings")]
    [SerializeField] private TMP_InputField combinationInput;
    [SerializeField] private Button submitButton;
    
    private string correctCombination;
    // Network variable for late-joiners
    private NetworkVariable<bool> isUnlocked = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    protected override void Start()
    {
        base.Start();
        
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(OnSubmitClicked);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isUnlocked.OnValueChanged += OnUnlockedStateChanged;
        
        // Late joiner support
        if (isUnlocked.Value) OnUnlockedStateChanged(false, true);
    }

    public override void OnNetworkDespawn()
    {
        isUnlocked.OnValueChanged -= OnUnlockedStateChanged;
        base.OnNetworkDespawn();
    }

    public override void ConfigureFromBackend(PuzzleConfigData config)
    {
        base.ConfigureFromBackend(config);
        
        if (config.config != null)
        {
            correctCombination = config.config.correctCombination;
            Debug.Log($"🔢 Lock {puzzleID} configured with combination");
        }
    }

    public string GetInteractionPrompt()
    {
        if (currentState == PuzzleState.Solved)
            return "Lock opened ✓";
            
        if (currentState == PuzzleState.Locked)
            return "Lock is sealed (solve other puzzles first)";
            
        return "Press E to enter combination";
    }

    public void Interact()
    {
        if (currentState == PuzzleState.Locked || currentState == PuzzleState.Solved) return;
        
        ActivatePuzzle();
        
        if (combinationInput != null)
        {
            combinationInput.gameObject.SetActive(true);
            combinationInput.Select();
        }
        if (submitButton != null)
        {
            submitButton.gameObject.SetActive(true);
        }
    }

    private void OnSubmitClicked()
    {
        if (combinationInput == null) return;
        
        // Ask server to check combination
        if (IsSpawned) 
        {
            SubmitCombinationServerRpc(combinationInput.text);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitCombinationServerRpc(string attemptedCode)
    {
        if (attemptedCode == correctCombination)
        {
            isUnlocked.Value = true; // Solved!
        }
        else
        {
            WrongCombinationClientRpc(); // Tell all clients it was wrong
        }
    }

    private void OnUnlockedStateChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            CompletePuzzle();
            if (combinationInput != null) combinationInput.gameObject.SetActive(false);
            if (submitButton != null) submitButton.gameObject.SetActive(false);
        }
    }

    [ClientRpc]
    private void WrongCombinationClientRpc()
    {
        Debug.Log("❌ Wrong combination!");
        if (combinationInput != null) combinationInput.text = "";
    }

    protected override bool CheckSolution()
    {
        return isUnlocked.Value;
    }

    public override void ResetPuzzle()
    {
        base.ResetPuzzle();
        if (IsServer) isUnlocked.Value = false;
        
        if (combinationInput != null)
        {
            combinationInput.text = "";
            combinationInput.gameObject.SetActive(false);
        }
        if (submitButton != null) submitButton.gameObject.SetActive(false);
    }
}
