using UnityEngine;
using UnityEngine.UI;
using MysteryRooms.Game.Data;
using TMPro;

public class CombinationLockPuzzle : BasePuzzle, IInteractable
{
    [Header("Lock Settings")]
    [SerializeField] private TMP_InputField combinationInput;
    [SerializeField] private Button submitButton;
    
    private string correctCombination;

    protected override void Start()
    {
        base.Start();
        
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(OnSubmitClicked);
        }
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
        else if (isLockedByDependencies)
            return "Lock is sealed (solve other puzzles first)";
        else
            return "Press E to enter combination";
    }

    public void Interact()
    {
        if (currentState == PuzzleState.Solved || isLockedByDependencies) return;
        
        ActivatePuzzle();
        // Show UI for combination input
        if (combinationInput != null)
        {
            combinationInput.gameObject.SetActive(true);
            combinationInput.Select();
        }
    }

    private void OnSubmitClicked()
    {
        if (CheckSolution())
        {
            CompletePuzzle();
            if (combinationInput != null)
            {
                combinationInput.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.Log("❌ Wrong combination!");
            // Visual feedback here
        }
    }

    protected override bool CheckSolution()
    {
        if (combinationInput == null) return false;
        return combinationInput.text == correctCombination;
    }

    public override void ResetPuzzle()
    {
        base.ResetPuzzle();
        if (combinationInput != null)
        {
            combinationInput.text = "";
            combinationInput.gameObject.SetActive(false);
        }
    }
}
