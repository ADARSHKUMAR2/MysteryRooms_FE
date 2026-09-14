// ============================================================
// FinalDoorController.cs
// Folder: Assets/Scripts/Game/
// Assembly: MysteryRooms.Main (via MysteryRooms.Main.asmdef)
//
// PURPOSE:
//   Attached to the final exit door 3D object in the scene.
//   Implements IInteractable — the existing interface in this project.
//
//   When the player interacts with the door:
//     1. Checks GameSessionManager.IsDoorUnlocked("victory")
//     2. If locked: plays locked sound + shows hint via GameUIController
//     3. If unlocked: starts the victory sequence:
//         a. Plays door open animation
//         b. Plays door open audio
//         c. Waits for animation to finish
//         d. Fades to black
//         e. Calls VictoryScreenUI.Instance.ShowVictoryScreen()
//
// IMPLEMENTS:
//   IInteractable — matches the interface in Game/Interfaces/IInteractable.cs
//     GetInteractionPrompt() → returns the prompt text for the HUD
//     Interact()             → called by the player interaction system
//
// REFERENCES:
//   GameSessionManager.Instance.IsDoorUnlocked("victory")
//   VictoryScreenUI.Instance.ShowVictoryScreen()
//   GameUIController.Instance.ShowInteractionPrompt() / HideInteractionPrompt()
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using MysteryRooms.Game.Services;
using MysteryRooms.UI;

namespace MysteryRooms
{
    public class FinalDoorController : MonoBehaviour, IInteractable
    {
        // ================================================================
        // Inspector References
        // ================================================================

        [Header("== Door Identity ==")]
        [Tooltip("The unlock key to check — must match what the backend sends.\n'victory' is the standard key when the final puzzle unlocks [\"victory\"].")]
        [SerializeField] private string doorUnlockKey = "victory";

        [Header("== Door Animation ==")]
        [Tooltip("Animator on the door mesh — needs an 'Open' trigger")]
        [SerializeField] private Animator doorAnimator;

        [Tooltip("Name of the trigger in the Animator Controller")]
        [SerializeField] private string openAnimationTrigger = "Open";

        [Tooltip("Seconds to wait after triggering the animation before fading")]
        [SerializeField] private float doorOpenWaitSeconds = 1.5f;

        [Header("== Audio ==")]
        [SerializeField] private AudioClip doorOpenSound;
        [SerializeField] private AudioClip doorLockedSound;

        [Header("== Screen Fade ==")]
        [Tooltip("A full-screen black Image on a World-Space or Screen-Space canvas")]
        [SerializeField] private Image fadeOverlay;

        [Tooltip("Seconds to fade from clear to black before showing victory screen")]
        [SerializeField] private float fadeDuration = 1.2f;

        [Header("== Lock Indicator ==")]
        [Tooltip("Visual indicator (glow/lock icon) on the door — hidden when unlocked")]
        [SerializeField] private GameObject lockedIndicator;

        // ================================================================
        // Internal state
        // ================================================================

        private AudioSource _audioSource;
        private bool _sequenceStarted = false;

        // ================================================================
        // Unity Lifecycle
        // ================================================================

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            // Fade overlay starts fully transparent
            if (fadeOverlay != null)
                fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
        }

        private void Update()
        {
            // Keep the lock indicator in sync with unlock state every frame
            UpdateLockedIndicator();
        }

        // ================================================================
        // IInteractable Implementation
        // ================================================================

        /// <summary>
        /// Called by the player interaction system to get the HUD prompt text.
        /// e.g. "Press E to Exit" or "Locked — solve all puzzles first"
        /// </summary>
        public string GetInteractionPrompt()
        {
            bool unlocked = GameSessionManager.Instance != null
                            && GameSessionManager.Instance.IsDoorUnlocked(doorUnlockKey);

            return unlocked
                ? "Press E to Exit the Tomb"
                : "Locked — solve all puzzles first";
        }

        /// <summary>
        /// Called by the player interaction system when the player presses E
        /// (or taps on mobile) while looking at this door.
        /// </summary>
        public void Interact()
        {
            if (_sequenceStarted) return;

            bool unlocked = GameSessionManager.Instance != null
                            && GameSessionManager.Instance.IsDoorUnlocked(doorUnlockKey);

            if (!unlocked)
            {
                // Door is still locked — give audio feedback and a HUD hint
                if (doorLockedSound != null && _audioSource != null)
                    _audioSource.PlayOneShot(doorLockedSound);

                GameUIController.Instance?.ShowRecentAction(
                    "Solve all puzzles to unlock the exit!"
                );

                Debug.Log("[FinalDoorController] Door is locked — victory condition not yet met.");
                return;
            }

            // All puzzles solved — start the victory sequence
            _sequenceStarted = true;
            StartCoroutine(VictorySequence());
        }

        // ================================================================
        // Victory Sequence Coroutine
        // ================================================================

        private IEnumerator VictorySequence()
        {
            Debug.Log("<color=yellow>[FinalDoorController] Victory sequence started!</color>");

            // Step 1 — Hide the interaction prompt from the HUD
            GameUIController.Instance?.HideInteractionPrompt();

            // Step 2 — Trigger door open animation
            if (doorAnimator != null)
                doorAnimator.SetTrigger(openAnimationTrigger);

            // Step 3 — Play the door open sound
            if (doorOpenSound != null && _audioSource != null)
                _audioSource.PlayOneShot(doorOpenSound);

            // Step 4 — Wait for the door animation to play through
            yield return new WaitForSeconds(doorOpenWaitSeconds);

            // Step 5 — Fade to black
            yield return StartCoroutine(FadeToBlack());

            // Step 6 — Hand off to the victory screen
            // VictoryScreenUI reads CurrentSessionId from GameSessionManager internally
            if (VictoryScreenUI.Instance != null)
            {
                VictoryScreenUI.Instance.ShowVictoryScreen();
            }
            else
            {
                Debug.LogError("[FinalDoorController] VictoryScreenUI.Instance is null! Is it in the scene?");
            }
        }

        // ================================================================
        // Fade Coroutine
        // ================================================================

        private IEnumerator FadeToBlack()
        {
            if (fadeOverlay == null) yield break;

            float elapsed = 0f;
            Color transparent = new Color(0f, 0f, 0f, 0f);
            Color black       = new Color(0f, 0f, 0f, 1f);

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                fadeOverlay.color = Color.Lerp(transparent, black, t);
                yield return null;
            }

            fadeOverlay.color = black;
        }

        // ================================================================
        // Lock Indicator Sync
        // ================================================================

        private void UpdateLockedIndicator()
        {
            if (lockedIndicator == null) return;

            bool unlocked = GameSessionManager.Instance != null
                            && GameSessionManager.Instance.IsDoorUnlocked(doorUnlockKey);

            lockedIndicator.SetActive(!unlocked);
        }

        // ================================================================
        // Editor Gizmo — shows the interaction hint in Scene view
        // ================================================================
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawIcon(transform.position + Vector3.up * 1.5f, "d_winbtn_win_max", true);
        }
#endif
    }
}
