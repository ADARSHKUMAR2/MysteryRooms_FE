using UnityEngine;
using System.Collections.Generic;
using MysteryRooms.Game.Data;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections;

public class CardDeckRiddlePuzzle : BasePuzzle
{
    [Header("Data References")]
    [SerializeField] private CardDatabase cardDatabase;

    [Header("Grid Layout Settings")]
    [Tooltip("Parent container that will hold the 4x4 grid of cards")]
    [SerializeField] private Transform gridContainer;
    [Tooltip("Prefab for individual card buttons")]
    [SerializeField] private GameObject cardButtonPrefab;
    
    [Header("Code Input UI")]
    [SerializeField] private GameObject codeInputPanel;
    [SerializeField] private TextMeshProUGUI codeDisplayText;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button clearButton;
    
    [Header("Grid Configuration")]
    private const int GRID_COLUMNS = 4;
    private const int GRID_ROWS = 4;
    private const int TOTAL_CARDS = 16;

    [Header("Visual Environmental Clues")]
    [Tooltip("Drag your 4 CardVisualClue objects here. They can be scattered around the room!")]
    [SerializeField] private List<CardVisualClue> visualClues;


    // Backend configuration
    private List<RiddleRule> riddleRules;
    private string correctCode = "";
    private List<CardData> gridCards;

    // Grid data
    private CardButton[,] cardButtons; // 2D array [row, col]
    private string currentInput = "";

    // Network sync
    private NetworkVariable<bool> isSolvedNet = new NetworkVariable<bool>(
        false, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        cardButtons = new CardButton[GRID_ROWS, GRID_COLUMNS];
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        isSolvedNet.OnValueChanged += OnSolvedStateChanged;
        
        if (isSolvedNet.Value) OnSolvedStateChanged(false, true);
        
        // Setup UI buttons
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(OnSubmitClicked);
        }
        
        if (clearButton != null)
        {
            clearButton.onClick.AddListener(OnClearClicked);
        }
        
        UpdateCodeDisplay();
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
            riddleRules = config.config.riddleRules;
            correctCode = config.config.correctCode;
            gridCards = config.config.gridCards;
            
            Debug.Log($"🃏 [{puzzleID}] Configured card deck puzzle. Code: {correctCode}");
            
            GenerateCardGrid();
            UpdateCodeDisplay(); // Update display NOW that we have the code

            // Set up the visual clues in the room!
            SetupVisualClues(); 
        }
    }

    /// <summary>
    /// Generates the 4x4 grid of cards
    /// </summary>
    private void GenerateCardGrid()
    {
        if (cardDatabase == null || gridContainer == null || cardButtonPrefab == null)
        {
            Debug.LogError($"[{puzzleID}] Missing references! Cannot generate card grid.");
            return;
        }

        // Check for 16 total cards instead of 4 rows
        if (gridCards == null || gridCards.Count != TOTAL_CARDS)
        {
            Debug.LogError($"[{puzzleID}] Invalid gridCards data! Expected {TOTAL_CARDS}, got {(gridCards != null ? gridCards.Count : 0)}");
            return;
        }

        // Generate the 4x4 grid using a 1D list
        for (int row = 0; row < GRID_ROWS; row++)
        {
            for (int col = 0; col < GRID_COLUMNS; col++)
            {
                int index = row * GRID_COLUMNS + col;
                CardData cardData = gridCards[index];
                
                // Instantiate card button
                GameObject buttonObj = Instantiate(cardButtonPrefab, gridContainer);
                CardButton button = buttonObj.GetComponent<CardButton>();
                
                if (button != null)
                {
                    Sprite cardSprite = cardDatabase.GetCardSprite(cardData.suit, cardData.rank);
                    button.SetCardData(cardData, cardSprite);
                    
                    cardButtons[row, col] = button;
                }
            }
        }

        Debug.Log($"✅ [{puzzleID}] Card grid generated with {TOTAL_CARDS} cards.");
    }

    private void SetupVisualClues()
    {
        if (visualClues == null || visualClues.Count == 0) return;
        if (riddleRules == null || riddleRules.Count == 0) return;

        // Sort the rules by column just to guarantee column 0 is first, column 1 is second, etc.
        var sortedRules = riddleRules.OrderBy(r => r.column).ToList();

        for (int i = 0; i < sortedRules.Count; i++)
        {
            if (i < visualClues.Count)
            {
                // i + 1 because players read 1, 2, 3, 4 (not 0, 1, 2, 3)
                int sequenceNumber = sortedRules[i].column + 1; 
                string suitToCount = sortedRules[i].suit;

                visualClues[i].SetClue(suitToCount, sequenceNumber);
                
                Debug.Log($"🔍 Set Visual Clue {sequenceNumber}: Count {suitToCount}");
            }
        }
    }



    /// <summary>
    /// Called when player presses a number key or UI button
    /// </summary>
    public void OnNumberInput(string digit)
    {
        if (currentState == PuzzleState.Locked || currentState == PuzzleState.Solved) return;
        
        ActivatePuzzle();
        
        if (currentInput.Length < correctCode.Length)
        {
            currentInput += digit;
            UpdateCodeDisplay();
        }
    }

    private void OnSubmitClicked()
    {
        if (string.IsNullOrEmpty(currentInput)) return;
        
        if (IsSpawned)
        {
            SubmitCodeServerRpc(currentInput);
        }
    }

    private void OnClearClicked()
    {
        currentInput = "";
        UpdateCodeDisplay();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitCodeServerRpc(string code, ServerRpcParams rpcParams = default)
    {
        if (isSolvedNet.Value) return;

        Debug.Log($"[{puzzleID}] Server checking code: '{code}' vs correct: '{correctCode}'");

        if (code == correctCode)
        {
            isSolvedNet.Value = true;
            Debug.Log($"✅ [{puzzleID}] SOLVED! Correct code entered.");
            
            InvokeOnPuzzleSolved(rpcParams.Receive.SenderClientId, "unknown_firebase_id");
        }
        else
        {
            Debug.Log($"❌ [{puzzleID}] Wrong code!");
            WrongCodeClientRpc();
        }
    }

    [ClientRpc]
    private void WrongCodeClientRpc()
    {
        Debug.Log($"❌ [{puzzleID}] Wrong code! Try again.");
        
        // Flash red
        if (codeDisplayText != null)
        {
            StartCoroutine(FlashCodeRed());
        }
        
        // Auto-clear after delay
        Invoke(nameof(OnClearClicked), 1.5f);
    }

    private System.Collections.IEnumerator FlashCodeRed()
    {
        Color originalColor = codeDisplayText.color;
        codeDisplayText.color = Color.red;
        
        yield return new WaitForSeconds(0.5f);
        
        codeDisplayText.color = originalColor;
    }

        private void UpdateCodeDisplay()
    {
        if (codeDisplayText != null)
        {
            // Ensure correctCode is not null before accessing Length
            if (string.IsNullOrEmpty(correctCode))
            {
                codeDisplayText.text = "_ _ _ _";
                return;
            }

            // Show current input with underscores for missing digits
            string display = currentInput;
            while (display.Length < correctCode.Length)
            {
                display += "_";
            }
            
            codeDisplayText.text = string.Join(" ", display.ToCharArray());
        }
    }


    private void OnSolvedStateChanged(bool prev, bool isSolved)
    {
        if (isSolved)
        {
            Debug.Log($"🎉 [{puzzleID}] Puzzle solved!");
            
            // Hide input panel
            if (codeInputPanel != null)
            {
                codeInputPanel.SetActive(false);
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
            isSolvedNet.Value = false;
        }
        
        currentInput = "";
        UpdateCodeDisplay();
        
        if (codeInputPanel != null)
        {
            codeInputPanel.SetActive(true);
        }
    }
}
