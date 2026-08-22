using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MysteryRooms.Game.Data;

public class SymbolSequencePuzzle : BasePuzzle
{
    [Header("Symbols")]
    [SerializeField] private List<SymbolButton> symbolButtons;
    
    private List<string> correctSequence;
    private List<string> playerSequence = new List<string>();

    public override void ConfigureFromBackend(PuzzleConfigData config)
    {
        base.ConfigureFromBackend(config);
        
        if (config.config != null && config.config.correctSequence != null)
        {
            correctSequence = config.config.correctSequence;
            Debug.Log($"🔣 Symbol puzzle {puzzleID} configured: {string.Join(" → ", correctSequence)}");
            
            // Assign symbols to buttons
            AssignSymbolsToButtons();
        }
    }

    private void AssignSymbolsToButtons()
    {
        if (symbolButtons == null || symbolButtons.Count == 0) return;

        for (int i = 0; i < Mathf.Min(symbolButtons.Count, correctSequence.Count); i++)
        {
            symbolButtons[i].symbolName = correctSequence[i];
            symbolButtons[i].onSymbolClicked = OnSymbolClicked;
        }
    }

    public void OnSymbolClicked(string symbolName)
    {
        if (currentState == PuzzleState.Solved || isLockedByDependencies) return;
        
        ActivatePuzzle();
        playerSequence.Add(symbolName);
        
        Debug.Log($"Symbol clicked: {symbolName} | Sequence: {string.Join(", ", playerSequence)}");

        if (CheckSolution())
        {
            CompletePuzzle();
        }
        else if (playerSequence.Count >= correctSequence.Count)
        {
            // Wrong sequence - reset
            Debug.Log("❌ Wrong sequence! Resetting...");
            playerSequence.Clear();
        }
    }

    protected override bool CheckSolution()
    {
        if (playerSequence.Count != correctSequence.Count) return false;
        return playerSequence.SequenceEqual(correctSequence);
    }

    public override void ResetPuzzle()
    {
        base.ResetPuzzle();
        playerSequence.Clear();
    }
}

// Helper class for symbol buttons
[System.Serializable]
public class SymbolButton : MonoBehaviour
{
    public string symbolName;
    public System.Action<string> onSymbolClicked;

    public void OnClick()
    {
        onSymbolClicked?.Invoke(symbolName);
    }
}
