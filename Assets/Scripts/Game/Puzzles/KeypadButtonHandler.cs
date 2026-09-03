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
            Debug.LogError($"[KeypadButton] ❌ No CombinationLockPuzzle found in parents of {gameObject.name}!");
            return;
        }

        // Automatically wire the Unity UI Button to our local method
        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(OnButtonClicked);
        
        // Debug.Log($"[KeypadButton] ✅ Successfully wired up button '{gameObject.name}' (Type: {buttonType}, Value: {digitValue}) to {targetPuzzle.puzzleID}");
    }

    private void OnButtonClicked()
    {
        if (targetPuzzle == null)
        {
            Debug.LogWarning($"[KeypadButton] ⚠️ Clicked {gameObject.name}, but targetPuzzle is missing!");
            return;
        }

        // Debug.Log($"[KeypadButton] 🔘 Button Clicked! Type: {buttonType} | Value: '{digitValue}' | Object: {gameObject.name}");

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
                // Note: The CombinationLock script we updated earlier doesn't have OnClosePressed anymore 
                // because we moved to a physical 3D keypad. If you get errors here, you can remove this case!
                // Debug.LogWarning("[KeypadButton] Close button pressed, but physical keypads cannot be closed!");
                break;
        }
    }
}
