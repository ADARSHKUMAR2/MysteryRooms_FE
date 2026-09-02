using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using MysteryRooms.Game.Data;

public class CombinationLockPuzzle : BasePuzzle, IInteractable
{
    [Header("Keypad UI References")]
    [Tooltip("The main Canvas or Panel that holds the Keypad")]
    [SerializeField] private GameObject keypadUIPanel;
    
    [Tooltip("The text component showing what the player is typing")]
    [SerializeField] private TextMeshProUGUI displayText;
    
    private string correctCombination = "";
    private string currentInput = "";

    // Network variable for late-joiners
    private NetworkVariable<bool> isUnlocked = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    protected override void Start()
    {
        base.Start();
        if (keypadUIPanel != null)
        {
            keypadUIPanel.SetActive(false); // Hide keypad on start
        }
        UpdateDisplay();
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
            Debug.Log($"🔢 Lock {puzzleID} configured. Answer: {correctCombination}");

            if (config.config.elementalMapping != null)
            {
                var mappings = config.config.elementalMapping.ToDictionary();
                string style = config.config.clueStyle; // "cylinder" or "scales"

                // Find both objects in the scene (even if they are disabled)
                ElementalCylinderData cylinder = FindObjectOfType<ElementalCylinderData>(true);
                ElementalScalesData scales = FindObjectOfType<ElementalScalesData>(true);

                if (style == "scales" && scales != null)
                {
                    if (cylinder != null) cylinder.gameObject.SetActive(false); // Hide cylinder
                    scales.gameObject.SetActive(true);
                    scales.SetMappings(mappings);
                    Debug.Log("⚖️ Activated Scales Clue!");
                }
                else if (cylinder != null)
                {
                    if (scales != null) scales.gameObject.SetActive(false); // Hide scales
                    cylinder.gameObject.SetActive(true);
                    cylinder.SetMappings(mappings);
                    Debug.Log("🌀 Activated Cylinder Clue!");
                }
            }
        }
    }


    public string GetInteractionPrompt()
    {
        if (currentState == PuzzleState.Solved)
            return "Lock opened ✓";
            
        if (currentState == PuzzleState.Locked)
            return "Lock is sealed (solve other puzzles first)";
            
        return "Press E to use Keypad";
    }

    public void Interact()
    {
        if (currentState == PuzzleState.Locked || currentState == PuzzleState.Solved) return;
        
        ActivatePuzzle();
        
        // Show the UI Keypad
        if (keypadUIPanel != null)
        {
            keypadUIPanel.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // Optional: If this is a Screen Space overlay, you might want to unlock the player's mouse cursor here!
            // Cursor.lockState = CursorLockMode.None;
            // Cursor.visible = true;
        }
    }

    // Call this from the OnClick events of your UI Buttons (0-9)
    public void OnKeypadButtonPressed(string digit)
    {
        // Don't allow typing more digits than the correct answer length
        if (currentInput.Length < correctCombination.Length)
        {
            currentInput += digit;
            UpdateDisplay();
        }
    }

    // Call this from a "Clear" button on the UI
    public void OnClearPressed()
    {
        currentInput = "";
        UpdateDisplay();
    }

    // Call this from an "Enter/Submit" button on the UI
    public void OnSubmitPressed()
    {
        if (string.IsNullOrEmpty(currentInput)) return;

        // Ask server to check combination
        if (IsSpawned) 
        {
            SubmitCombinationServerRpc(currentInput);
        }
    }

    // Call this from an "X" or "Close" button on the UI
    public void OnClosePressed()
    {
        if (keypadUIPanel != null)
        {
            keypadUIPanel.SetActive(false);
            
            // Re-lock mouse cursor if needed
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        // Reset state so they can interact again
        if (currentState == PuzzleState.InProgress)
        {
            currentState = PuzzleState.Unlocked;
        }
    }

    private void UpdateDisplay()
    {
        if (displayText != null)
        {
            // Show underscores for missing digits (e.g., "1 2 _ _")
            string displayStr = currentInput;
            while (displayStr.Length < correctCombination.Length)
            {
                displayStr += "_";
            }
            // Add spaces between characters for readability
            displayText.text = string.Join(" ", displayStr.ToCharArray());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitCombinationServerRpc(string attemptedCode, ServerRpcParams rpcParams = default) // Added ServerRpcParams!
    {
        if (attemptedCode == correctCombination)
        {
            isUnlocked.Value = true; // Solved!
            // FIRE THE EVENT TO GIVE POINTS AND TELL DYNAMIC PUZZLE MANAGER!
            InvokeOnPuzzleSolved(rpcParams.Receive.SenderClientId, "unknown_firebase_id");
    
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
            // CompletePuzzle();
            OnClosePressed(); // Auto-close the UI when solved
        }
    }

    [ClientRpc]
    private void WrongCombinationClientRpc()
    {
        Debug.Log("❌ Wrong combination!");
        if (displayText != null)
        {
            displayText.text = "<color=red>ERROR</color>";
        }
        
        // Auto-clear input after a delay
        Invoke(nameof(OnClearPressed), 1.5f);
    }

    protected override bool CheckSolution()
    {
        return isUnlocked.Value;
    }

    public override void ResetPuzzle()
    {
        base.ResetPuzzle();
        if (IsServer) isUnlocked.Value = false;
        
        OnClearPressed();
        OnClosePressed();
    }
}
