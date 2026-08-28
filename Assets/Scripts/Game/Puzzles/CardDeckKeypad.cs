using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CardDeckKeypad : MonoBehaviour
{
    [SerializeField] private string digitValue; // "1", "2", "3", "4"
    
    private CardDeckRiddlePuzzle targetPuzzle;

    private void Start()
    {
        targetPuzzle = GetComponentInParent<CardDeckRiddlePuzzle>();
        
        if (targetPuzzle == null)
        {
            Debug.LogError($"[CardDeckKeypad] No CardDeckRiddlePuzzle found in parents!");
            return;
        }

        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        if (targetPuzzle != null)
        {
            targetPuzzle.OnNumberInput(digitValue);
        }
    }
}
