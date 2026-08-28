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

    [Header("Grid Layout Settings")]
    [Tooltip("Parent container that will hold the 8x5 grid of symbol buttons")]
    [SerializeField] private Transform gridContainer;
    [Tooltip("Prefab for individual symbol buttons")]
    [SerializeField] private GameObject symbolButtonPrefab;
    
    [Header("Grid Configuration")]
    private const int GRID_COLUMNS = 8;
    private const int GRID_ROWS = 5;
    private const int TOTAL_SYMBOLS = 40; // 8 x 5

    [Header("Sequence Display")]
    [Tooltip("The UI slots showing the current sequence attempt (e.g. at the top of the wall)")]
    [SerializeField] private List<Image> sequenceAttemptPlaceholders;

    // Backend configuration
    private List<string> correctSequence; // The 4 symbols in correct order
    private string patternType; // "horizontal_row" or "vertical_column"
    private PatternStartPosition patternStartPosition;

    // Grid data
    private SymbolButton[,] gridButtons; // 2D array [row, col]
    private List<GridPosition> correctPositions = new List<GridPosition>(); // The exact grid positions of the solution

    // Netcode: NetworkList automatically syncs the list to all clients (including late joiners)
    private NetworkList<FixedString32Bytes> syncedPlayerSequence;

    private NetworkVariable<bool> isSolvedNet = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    [System.Serializable]
    private struct GridPosition
    {
        public int row;
        public int col;
        
        public GridPosition(int r, int c)
        {
            row = r;
            col = c;
        }
    }

    private void Awake()
    {
        // NetworkLists must be initialized in Awake
        syncedPlayerSequence = new NetworkList<FixedString32Bytes>();
        gridButtons = new SymbolButton[GRID_ROWS, GRID_COLUMNS];
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
            patternType = config.config.patternType;
            patternStartPosition = config.config.patternStartPosition;
            
            Debug.Log($"🎯 [{puzzleID}] Configured grid puzzle: {correctSequence.Count} symbols, pattern: {patternType}, start: ({patternStartPosition.row}, {patternStartPosition.col})");
            
            GenerateGridPuzzle();
        }
    }

    /// <summary>
    /// Generates the 8x5 grid with 40 symbols, including the correct sequence at the specified positions
    /// </summary>
    private void GenerateGridPuzzle()
    {
        if (symbolDatabase == null || gridContainer == null || symbolButtonPrefab == null)
        {
            Debug.LogError($"[{puzzleID}] Missing references! Cannot generate grid.");
            return;
        }

        // Step 1: Calculate the exact grid positions where the correct symbols should appear
        CalculateCorrectPositions();

        // Step 2: Get all available symbols from the database
        List<string> allSymbols = GetAllAvailableSymbols();
        
        // Step 3: Create a pool of random symbols (excluding the ones in correctSequence to avoid duplicates)
        List<string> decoySymbols = allSymbols.Where(s => !correctSequence.Contains(s)).ToList();
        
        // Step 4: Generate the 8x5 grid
        for (int row = 0; row < GRID_ROWS; row++)
        {
            for (int col = 0; col < GRID_COLUMNS; col++)
            {
                // Check if this position is part of the solution
                GridPosition currentPos = new GridPosition(row, col);
                int correctIndex = GetCorrectSequenceIndex(currentPos);
                
                string symbolToPlace;
                
                if (correctIndex >= 0)
                {
                    // This position is part of the correct sequence
                    symbolToPlace = correctSequence[correctIndex];
                }
                else
                {
                    // This is a decoy position - pick a random symbol
                    symbolToPlace = decoySymbols[Random.Range(0, decoySymbols.Count)];
                }
                
                // Instantiate the button
                GameObject buttonObj = Instantiate(symbolButtonPrefab, gridContainer);
                SymbolButton button = buttonObj.GetComponent<SymbolButton>();
                
                if (button != null)
                {
                    button.symbolName = symbolToPlace;
                    button.SetSprite(symbolDatabase.GetSprite(symbolToPlace));
                    button.onSymbolClicked = OnSymbolClicked;
                    
                    // Store in 2D array for reference
                    gridButtons[row, col] = button;
                }
            }
        }

        // Step 5: Initialize sequence attempt placeholders
        InitializePlaceholders();
        
        Debug.Log($"✅ [{puzzleID}] Grid generated with {TOTAL_SYMBOLS} symbols. Correct positions: {string.Join(", ", correctPositions.Select(p => $"({p.row},{p.col})"))}");
    }

    /// <summary>
    /// Calculates the exact grid positions where the correct sequence should appear
    /// </summary>
    private void CalculateCorrectPositions()
    {
        correctPositions.Clear();
        
        if (patternStartPosition == null || correctSequence == null || correctSequence.Count != 4)
        {
            Debug.LogError($"[{puzzleID}] Invalid pattern configuration!");
            return;
        }

        int startRow = patternStartPosition.row;
        int startCol = patternStartPosition.col;

        if (patternType == "horizontal_row")
        {
            // Place 4 symbols horizontally in the same row
            for (int i = 0; i < 4; i++)
            {
                correctPositions.Add(new GridPosition(startRow, startCol + i));
            }
        }
        else if (patternType == "vertical_column")
        {
            // Place 4 symbols vertically in the same column
            for (int i = 0; i < 4; i++)
            {
                correctPositions.Add(new GridPosition(startRow + i, startCol));
            }
        }
    }

    /// <summary>
    /// Checks if a grid position is part of the correct sequence and returns its index
    /// </summary>
    private int GetCorrectSequenceIndex(GridPosition pos)
    {
        for (int i = 0; i < correctPositions.Count; i++)
        {
            if (correctPositions[i].row == pos.row && correctPositions[i].col == pos.col)
            {
                return i;
            }
        }
        return -1; // Not part of the solution
    }

    /// <summary>
    /// Gets all available symbol names from the database
    /// </summary>
    private List<string> GetAllAvailableSymbols()
    {
        // This assumes your SymbolDatabase has a way to get all symbol names
        // You may need to add a method to SymbolDatabase to expose this
        List<string> allSymbols = new List<string>();
        
        // Fallback: use the symbols from the database
        // If your SymbolDatabase doesn't expose all symbols, you'll need to add a public method
        // For now, let's assume we can access them via reflection or a new method
        foreach (var entry in symbolDatabase.symbols)
        {
            if (!string.IsNullOrEmpty(entry.symbolName))
            {
                allSymbols.Add(entry.symbolName);
            }
        }
        
        return allSymbols;
    }

    private void InitializePlaceholders()
    {
        if (sequenceAttemptPlaceholders != null)
        {
            // Initialize placeholders (hide them until a symbol is pressed)
            foreach (var placeholder in sequenceAttemptPlaceholders)
            {
                if (placeholder != null)
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
    private void SubmitSymbolServerRpc(string symbolName, ServerRpcParams rpcParams = default)
    {
        if (isSolvedNet.Value) return;

        // Add to synced list
        syncedPlayerSequence.Add(new FixedString32Bytes(symbolName));
        Debug.Log($"[{puzzleID}] Server recorded symbol: {symbolName} | Sequence: {syncedPlayerSequence.Count}/{correctSequence.Count}");

        // Convert FixedString back to normal string for comparison
        List<string> currentAttempt = new List<string>();
        foreach (var str in syncedPlayerSequence)
        {
            currentAttempt.Add(str.ToString());
        }

        if (currentAttempt.SequenceEqual(correctSequence))
        {
            isSolvedNet.Value = true; // Solved!
            Debug.Log($"✅ [{puzzleID}] SOLVED! Correct sequence matched.");
            
            // FIRE THE EVENT TO GIVE POINTS AND TELL DYNAMIC PUZZLE MANAGER!
            InvokeOnPuzzleSolved(rpcParams.Receive.SenderClientId, "unknown_firebase_id");
            
            // Clear the attempt display after a short delay
            Invoke(nameof(ClearSequenceDisplay), 2f);
        }
        else if (currentAttempt.Count >= correctSequence.Count)
        {
            // Wrong sequence - tell clients to play error animation, then reset
            Debug.Log($"❌ [{puzzleID}] Wrong sequence! Expected: {string.Join(", ", correctSequence)} | Got: {string.Join(", ", currentAttempt)}");
            TriggerErrorVisualClientRpc();
            syncedPlayerSequence.Clear();
        }
        else
        {
            // Still building the sequence - check if it matches so far
            bool matchingSoFar = true;
            for (int i = 0; i < currentAttempt.Count; i++)
            {
                if (currentAttempt[i] != correctSequence[i])
                {
                    matchingSoFar = false;
                    break;
                }
            }
            
            if (!matchingSoFar)
            {
                // Wrong symbol pressed - reset immediately
                Debug.Log($"❌ [{puzzleID}] Wrong symbol at position {currentAttempt.Count - 1}! Resetting.");
                TriggerErrorVisualClientRpc();
                syncedPlayerSequence.Clear();
            }
        }
    }

    private void ClearSequenceDisplay()
    {
        if (IsServer)
        {
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
            Debug.Log($"🎉 [{puzzleID}] Puzzle solved state synced to all clients!");
        }
    }

    [ClientRpc]
    private void TriggerErrorVisualClientRpc()
    {
        Debug.Log($"❌ [{puzzleID}] Wrong sequence! Resetting...");
        // TODO: Add visual/audio failure feedback here (e.g. flash placeholders red, play error sound)
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
            syncedPlayerSequence.Clear();
            isSolvedNet.Value = false;
        }
    }
}
