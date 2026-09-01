using UnityEngine;
using System.Collections;
using MysteryRooms.Game.Managers; 
using System.Linq;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class HintManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Central Monolith (Oracle) placed in the Entrance Hall")]
    public HintMonolith centralOracle;

    [Header("Settings")]
    public float hintDuration = 15f;
    public float fadeSpeed = 2f;
    
    [Header("Debug")]
    public bool showDebugLogs = true;

    private DynamicPuzzleManager puzzleManager;
    private float hintCooldown = 0f;
    private Coroutine activeHintCoroutine;
    private Coroutine activeLightPulseCoroutine;

    private void Start()
    {
        puzzleManager = FindObjectOfType<DynamicPuzzleManager>();
        
        if (centralOracle == null)
        {
            // Auto-find if not assigned
            centralOracle = FindObjectOfType<HintMonolith>();
        }
    }

    private void Update()
    {
        if (hintCooldown > 0)
        {
            hintCooldown -= Time.deltaTime;
        }

        // Press 'H' to activate the Oracle (You can also call ShowHint() directly from the Monolith interaction)
        if (GetHintInput() && hintCooldown <= 0)
        {
            ShowHint();
            hintCooldown = 2f; 
        }
    }

    private bool GetHintInput()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.H);
#endif
    }

    /// <summary>
    /// Finds the earliest active, unsolved puzzle in the logical chain and displays its hint on the Oracle,
    /// while turning on a bright spotlight over the puzzle itself!
    /// </summary>
    public void ShowHint()
    {
        if (puzzleManager == null || centralOracle == null) return;

        // 1. Get all puzzles that are active but NOT solved yet
        var activePuzzles = puzzleManager.puzzleRegistry.Values
            .Where(p => p.currentState == PuzzleState.Unlocked || p.currentState == PuzzleState.InProgress)
            .ToList();

        if (activePuzzles.Count == 0)
        {
            if (showDebugLogs) Debug.Log("<color=orange>[HintManager] 📭 No active, unsolved puzzles found!</color>");
            TriggerOracleHint(null, "The spirits are silent. Your path is already clear.");
            return;
        }

        // 2. We pick the first active puzzle in the list
        BasePuzzle targetPuzzle = activePuzzles[0];

        if (targetPuzzle.backendConfig != null)
        {
            string hintString = targetPuzzle.backendConfig.hint;
            if (string.IsNullOrEmpty(hintString)) hintString = "Trust your instincts on this path.";

            if (showDebugLogs) Debug.Log($"<color=green>[HintManager] 🎯 Oracle selected '{targetPuzzle.puzzleID}'. Hint: \"{hintString}\"</color>");

            TriggerOracleHint(targetPuzzle, hintString);
        }
    }

    private void TriggerOracleHint(BasePuzzle targetPuzzle, string text)
    {
        // Stop any existing animations
        if (activeHintCoroutine != null) StopCoroutine(activeHintCoroutine);
        if (activeLightPulseCoroutine != null) StopCoroutine(activeLightPulseCoroutine);
        
        activeHintCoroutine = StartCoroutine(OracleAnimationRoutine(targetPuzzle, text));
    }

    private IEnumerator OracleAnimationRoutine(BasePuzzle targetPuzzle, string text)
    {
        // 1. Turn the Oracle screen on with the text
        centralOracle.SetText(text);
        centralOracle.SetVisualsActive(true);

        // 2. Trigger the spotlight above the puzzle!
        if (targetPuzzle != null && targetPuzzle.puzzleHighlightLight != null)
        {
            Light pLight = targetPuzzle.puzzleHighlightLight.GetComponent<Light>();
            if (pLight != null)
            {
                activeLightPulseCoroutine = StartCoroutine(PulsePuzzleLight(pLight));
            }
        }

        float t = 0;
        
        // Fade in Screen 
        while (t < 1f)
        {
            t += Time.deltaTime * fadeSpeed;
            centralOracle.SetFadeLevel(t);
            yield return null;
        }

        // Wait for players to read the screen
        yield return new WaitForSeconds(hintDuration);

        // Fade out Screen
        t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime * fadeSpeed;
            centralOracle.SetFadeLevel(t);
            yield return null;
        }

        centralOracle.SetVisualsActive(false);
    }

    /// <summary>
    /// Makes the spotlight above the puzzle flash brightly to draw attention!
    /// </summary>
    private IEnumerator PulsePuzzleLight(Light pLight)
    {
        Color originalColor = pLight.color;
        float originalIntensity = pLight.intensity;
        bool wasActive = pLight.gameObject.activeSelf;

        // Force the light on and make it golden
        pLight.gameObject.SetActive(true);
        pLight.color = new Color(1f, 0.85f, 0.3f); // Glowing Gold

        // Pulse it 6 times (Bright -> Normal -> Bright)
        for (int i = 0; i < 6; i++)
        {
            pLight.intensity = originalIntensity * 4f; // Very Bright!
            yield return new WaitForSeconds(0.6f);
            
            pLight.intensity = originalIntensity; // Normal
            yield return new WaitForSeconds(0.6f);
        }

        // Return the light to its original state
        pLight.color = originalColor;
        pLight.intensity = originalIntensity;
        pLight.gameObject.SetActive(wasActive);
    }
}
