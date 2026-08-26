
using UnityEngine;
using UnityEngine.UI;

public enum KeypadButtonType
{
    Digit,
    Clear,
    Submit,
    Close
}

[RequireComponent(typeof(Button))]
public class KeypadButtonHandler : MonoBehaviour
{
    public KeypadButtonType buttonType;
    
    [Tooltip("If this is a digit button, which number is it? (0-9)")]
    public string digitValue;

    private CombinationLockPuzzle targetPuzzle;

    private void Start()
    {
        // Automatically find the puzzle script in the parent hierarchy
        targetPuzzle = GetComponentInParent<CombinationLockPuzzle>();
        
        if (targetPuzzle == null)
        {
            Debug.LogError($"[KeypadButton] No CombinationLockPuzzle found in parents of {gameObject.name}!");
            return;
        }

        // Automatically wire the Unity UI Button to our local method
        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        if (targetPuzzle == null) return;

        switch (buttonType)
        {
            case KeypadButtonType.Digit:
                targetPuzzle.OnKeypadButtonPressed(digitValue);
                break;
            case KeypadButtonType.Clear:
                targetPuzzle.OnClearPressed();
                break;
            case KeypadButtonType.Submit:
                targetPuzzle.OnSubmitPressed();
                break;
            case KeypadButtonType.Close:
                targetPuzzle.OnClosePressed();
                break;
        }
    }
}
