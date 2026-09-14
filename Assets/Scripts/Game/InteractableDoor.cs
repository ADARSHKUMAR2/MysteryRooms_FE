using UnityEngine;
using MysteryRooms.Game.Services;
using MysteryRooms.UI;
using System.Collections;
using UnityEngine.UI;

public class InteractableDoor : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    public bool isLocked = true;
    public string doorUnlockKey = "victory";

    [Header("Door Animation")]
    [Tooltip("Animator on the door mesh — needs an 'Open' trigger")]
    public Animator doorAnimator;
    public string openAnimationTrigger = "Open";
    public float doorOpenWaitSeconds = 1.5f;

    [Header("Audio")]
    public AudioClip doorOpenSound;
    public AudioClip doorLockedSound;

    [Header("Screen Fade")]
    [Tooltip("A full-screen black Image on a World-Space or Screen-Space canvas")]
    public Image fadeOverlay;
    public float fadeDuration = 1.2f;

    private AudioSource _audioSource;
    private bool _sequenceStarted = false;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        if (fadeOverlay != null)
            fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
    }

    public string GetInteractionPrompt()
    {
        // Check with session manager if we use it, otherwise fall back to local flag
        bool unlocked = !isLocked;
        if (GameSessionManager.Instance != null)
        {
            unlocked = GameSessionManager.Instance.IsDoorUnlocked(doorUnlockKey);
        }

        if (!unlocked)
            return "Press E to Try Door (Locked)";
        else
            return "Press E to Exit the Tomb";
    }

    public void Interact()
    {
        if (_sequenceStarted) return;

        bool unlocked = !isLocked;
        if (GameSessionManager.Instance != null)
        {
            unlocked = GameSessionManager.Instance.IsDoorUnlocked(doorUnlockKey);
        }

        if (!unlocked)
        {
            Debug.Log("The door is locked! You need to solve the puzzles first.");
            if (doorLockedSound != null && _audioSource != null)
                _audioSource.PlayOneShot(doorLockedSound);
                
            GameUIController.Instance?.ShowRecentAction("Solve all puzzles to unlock the exit!");
            return;
        }

        Debug.Log("<color=yellow>[InteractableDoor] Door unlocked! Victory sequence starting!</color>");
        _sequenceStarted = true;
        StartCoroutine(VictorySequence());
    }

    // This is called by your puzzle system when all puzzles are solved
    public void UnlockDoor()
    {
        isLocked = false;
        Debug.Log("Door has been unlocked locally!");
    }

    private IEnumerator VictorySequence()
    {
        GameUIController.Instance?.HideInteractionPrompt();

        if (doorAnimator != null)
            doorAnimator.SetTrigger(openAnimationTrigger);

        if (doorOpenSound != null && _audioSource != null)
            _audioSource.PlayOneShot(doorOpenSound);

        yield return new WaitForSeconds(doorOpenWaitSeconds);

        if (fadeOverlay != null)
        {
            float elapsed = 0f;
            Color transparent = new Color(0f, 0f, 0f, 0f);
            Color black = new Color(0f, 0f, 0f, 1f);

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                fadeOverlay.color = Color.Lerp(transparent, black, t);
                yield return null;
            }
            fadeOverlay.color = black;
        }

        // Show the Victory Screen!
        if (VictoryScreenUI.Instance != null)
        {
            VictoryScreenUI.Instance.ShowVictoryScreen();
        }
        else
        {
            Debug.LogError("[InteractableDoor] VictoryScreenUI.Instance is null! Generate it using 'Mystery Rooms -> Create Victory Screen UI'");
        }
    }
}
