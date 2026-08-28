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
    private const int TOTAL_SYMBOLS = 40;

    [Header("Sequence Display")]
    [Tooltip("The UI slots showing the current sequence attempt (e.g. at the top of the wall)")]
    [SerializeField] private List<Image> sequenceAttemptPlaceholders;

    // Backend configuration
    private List<string> correctSequence;
    private string patternType;
    private PatternStartPosition patternStartPosition;

    // Grid data
    private SymbolButton[,] gridButtons;
    private List<GridPosition> correctPositions = new List<GridPosition>();

    // Netcode
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
        syncedPlayerSequence = new NetworkList<FixedString32Bytes>();
        gridButtons = new SymbolButton[GRID_ROWS, GRID_COLUMNS];
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        isSolvedNet.OnValueChanged += OnSolvedStateChanged;
        syncedPlayerSequence.OnListChanged += OnSequenceChanged;
        
        if (isSolvedNet.Value) OnSolvedStateChanged(false, true);
        
        UpdateSequenceVisuals();
        
        // Initialize raycast state based on current puzzle state
        UpdateRaycastState();
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
    /// Override SetLocked to update raycast state when puzzle is locked/unlocked
    /// </summary>
    public override void SetLocked(bool locked)
    {
        base.SetLocked(locked);
        
        // Update raycast state immediately when lock state changes
        UpdateRaycastState();
        
        // Update visual feedback for locked/unlocked state
        UpdateGridVisualState();
    }

    /// <summary>
    /// Enable/disable raycasting on all symbol buttons based on puzzle state
    /// </summary>
    private void UpdateRaycastState()
    {
        bool shouldEnableRaycast = (currentState == PuzzleState.Unlocked || currentState == PuzzleState.InProgress);
        
        if (gridButtons == null) return;

        for (int row = 0; row < GRID_ROWS; row++)
        {
            for (int col = 0; col < GRID_COLUMNS; col++)
            {
                SymbolButton button = gridButtons[row, col];
                if (button != null)
                {
                    // Disable/enable BoxCollider for 3D raycasts
                    BoxCollider collider = button.GetComponent<BoxCollider>();
                    if (collider != null)
                    {
                        collider.enabled = shouldEnableRaycast;
                    }
                    
                    // Disable/enable UI raycast target
                    Image buttonImage = button.iconImage;
                    if (buttonImage != null)
                    {
                        buttonImage.raycastTarget = shouldEnableRaycast;
                    }
                    
                    // Disable/enable Unity UI Button component
                    Button uiButton = button.GetComponent<Button>();
                    if (uiButton != null)
                    {
                        uiButton.interactable = shouldEnableRaycast;
                    }
                }
            }
        }
        
        Debug.Log($"🔒 [{puzzleID}] Raycast state updated: {(shouldEnableRaycast ? "ENABLED" : "DISABLED")} (State: {currentState})");
    }

    /// <summary>
    /// Update visual appearance of grid based on locked/unlocked state
    /// </summary>
    private void UpdateGridVisualState()
    {
        if (gridButtons == null) return;

        Color lockedTint = new Color(0.5f, 0.5f, 0.5f, 0.6f); // Grayed out
        Color unlockedTint = Color.white; // Normal
        Color solvedTint = new Color(1f, 0.9f, 0.5f); // Golden

        Color targetColor = unlockedTint;
        
        if (currentState == PuzzleState.Locked)
        {
            targetColor = lockedTint;
        }
        else if (currentState == PuzzleState.Solved)
        {
            targetColor = solvedTint;
        }

        for (int row = 0; row < GRID_ROWS; row++)
        {
            for (int col = 0; col < GRID_COLUMNS; col++)
            {
                SymbolButton button = gridButtons[row, col];
                if (button != null && button.iconImage != null)
                {
                    button.iconImage.color = targetColor;
                }
            }
        }
    }

    /// <summary>
    /// Generates the 8x5 grid with 40 symbols
    /// </summary>
    private void GenerateGridPuzzle()
    {
        if (symbolDatabase == null || gridContainer == null || symbolButtonPrefab == null)
        {
            Debug.LogError($"[{puzzleID}] Missing references! Cannot generate grid.");
            return;
        }

        CalculateCorrectPositions();
        List<string> allSymbols = GetAllAvailableSymbols();
        List<string> decoySymbols = allSymbols.Where(s => !correctSequence.Contains(s)).ToList();
        
        for (int row = 0; row < GRID_ROWS; row++)
        {
            for (int col = 0; col < GRID_COLUMNS; col++)
            {
                GridPosition currentPos = new GridPosition(row, col);
                int correctIndex = GetCorrectSequenceIndex(currentPos);
                
                string symbolToPlace;
                
                if (correctIndex >= 0)
                {
                    symbolToPlace = correctSequence[correctIndex];
                }
                else
                {
                    symbolToPlace = decoySymbols[Random.Range(0, decoySymbols.Count)];
                }
                
                GameObject buttonObj = Instantiate(symbolButtonPrefab, gridContainer);
                SymbolButton button = buttonObj.GetComponent<SymbolButton>();
                
                if (button != null)
                {
                    button.symbolName = symbolToPlace;
                    button.SetSprite(symbolDatabase.GetSprite(symbolToPlace));
                    button.onSymbolClicked = OnSymbolClicked;
                    
                    gridButtons[row, col] = button;
                }
            }
        }

        InitializePlaceholders();
        
        // Set initial raycast state (should be disabled if puzzle starts locked)
        UpdateRaycastState();
        UpdateGridVisualState();
        
        Debug.Log($"✅ [{puzzleID}] Grid generated with {TOTAL_SYMBOLS} symbols. Correct positions: {string.Join(", ", correctPositions.Select(p => $"({p.row},{p.col})"))}");
    }

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
            for (int i = 0; i < 4; i++)
            {
                correctPositions.Add(new GridPosition(startRow, startCol + i));
            }
        }
        else if (patternType == "vertical_column")
        {
            for (int i = 0; i < 4; i++)
            {
                correctPositions.Add(new GridPosition(startRow + i, startCol));
            }
        }
    }

    private int GetCorrectSequenceIndex(GridPosition pos)
    {
        for (int i = 0; i < correctPositions.Count; i++)
        {
            if (correctPositions[i].row == pos.row && correctPositions[i].col == pos.col)
            {
                return i;
            }
        }
        return -1;
    }

    private List<string> GetAllAvailableSymbols()
    {
        List<string> allSymbols = new List<string>();
        
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
        // Double-check state before allowing interaction
        if (currentState == PuzzleState.Locked || currentState == PuzzleState.Solved)
        {
            Debug.Log($"⚠️ [{puzzleID}] Interaction blocked - puzzle is {currentState}");
            return;
        }
        
        ActivatePuzzle();
        if (IsSpawned) SubmitSymbolServerRpc(symbolName);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitSymbolServerRpc(string symbolName, ServerRpcParams rpcParams = default)
    {
        if (isSolvedNet.Value) return;

        syncedPlayerSequence.Add(new FixedString32Bytes(symbolName));
        Debug.Log($"[{puzzleID}] Server recorded symbol: {symbolName} | Sequence: {syncedPlayerSequence.Count}/{correctSequence.Count}");

        List<string> currentAttempt = new List<string>();
        foreach (var str in syncedPlayerSequence)
        {
            currentAttempt.Add(str.ToString());
        }

        if (currentAttempt.SequenceEqual(correctSequence))
        {
            isSolvedNet.Value = true;
            Debug.Log($"✅ [{puzzleID}] SOLVED! Correct sequence matched.");
            
            InvokeOnPuzzleSolved(rpcParams.Receive.SenderClientId, "unknown_firebase_id");
            
            Invoke(nameof(ClearSequenceDisplay), 2f);
        }
        else if (currentAttempt.Count >= correctSequence.Count)
        {
            Debug.Log($"❌ [{puzzleID}] Wrong sequence! Expected: {string.Join(", ", correctSequence)} | Got: {string.Join(", ", currentAttempt)}");
            TriggerErrorVisualClientRpc();
            syncedPlayerSequence.Clear();
        }
        else
        {
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

    private void OnSequenceChanged(NetworkListEvent<FixedString32Bytes> changeEvent)
    {
        UpdateSequenceVisuals();
    }

    private void UpdateSequenceVisuals()
    {
        if (symbolDatabase == null || sequenceAttemptPlaceholders == null) return;

        for (int i = 0; i < sequenceAttemptPlaceholders.Count; i++)
        {
            if (sequenceAttemptPlaceholders[i] == null) continue;

            if (i < syncedPlayerSequence.Count)
            {
                string symName = syncedPlayerSequence[i].ToString();
                sequenceAttemptPlaceholders[i].sprite = symbolDatabase.GetSprite(symName);
                sequenceAttemptPlaceholders[i].gameObject.SetActive(true);
            }
            else
            {
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
            
            // Disable raycasts when solved
            UpdateRaycastState();
            UpdateGridVisualState();
        }
    }

    [ClientRpc]
    private void TriggerErrorVisualClientRpc()
    {
        Debug.Log($"❌ [{puzzleID}] Wrong sequence! Resetting...");
        
        // Optional: Add visual feedback here
        StartCoroutine(FlashPlaceholdersRed());
    }

    private System.Collections.IEnumerator FlashPlaceholdersRed()
    {
        if (sequenceAttemptPlaceholders == null) yield break;
        
        Color[] originalColors = new Color[sequenceAttemptPlaceholders.Count];
        for (int i = 0; i < sequenceAttemptPlaceholders.Count; i++)
        {
            if (sequenceAttemptPlaceholders[i] != null)
            {
                originalColors[i] = sequenceAttemptPlaceholders[i].color;
                sequenceAttemptPlaceholders[i].color = Color.red;
            }
        }
        
        yield return new WaitForSeconds(0.3f);
        
        for (int i = 0; i < sequenceAttemptPlaceholders.Count; i++)
        {
            if (sequenceAttemptPlaceholders[i] != null)
            {
                sequenceAttemptPlaceholders[i].color = originalColors[i];
            }
        }
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
        
        // Re-enable raycasts when puzzle is reset
        UpdateRaycastState();
        UpdateGridVisualState();
    }
}
