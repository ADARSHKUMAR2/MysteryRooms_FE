// ============================================================
// VictoryScreenUI.cs
// Folder: Assets/Scripts/UI/
// Assembly: MysteryRooms.Main (via MysteryRooms.Main.asmdef)
//
// PURPOSE:
//   The master UI controller for the victory/winning screen.
//   Receives WinningScreenData from VictoryAPIService and
//   populates every panel: header, stats, score, rewards,
//   player leaderboard, achievements, and buttons.
//
// PATTERN:
//   Follows GameUIController.cs singleton pattern (same folder).
//   Uses MysteryRooms.UI namespace. References Game.Data and
//   Game.Services namespaces exactly as GameUIController does.
//
// SCENE SETUP:
//   1. Attach to a VictoryScreenUI empty GameObject in the scene.
//   2. Assign VictoryAPIService component reference in Inspector.
//   3. Assign all UI Text/Image/Button references in Inspector.
//   4. VictoryScreenPanel starts INACTIVE — shown only on victory.
//
// HOW IT GETS CALLED:
//   FinalDoorController calls:
//     VictoryScreenUI.Instance.ShowVictoryScreen();
//   It reads GameSessionManager.Instance.CurrentSessionId internally.
// ============================================================
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MysteryRooms.Game.Data;
using MysteryRooms.Game.Services;

namespace MysteryRooms.UI
{
    public class VictoryScreenUI : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Singleton — finds inactive objects too
        // ----------------------------------------------------------------
        public static VictoryScreenUI Instance 
        { 
            get 
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<VictoryScreenUI>(true);
                }
                return _instance;
            }
        }
        private static VictoryScreenUI _instance;

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else if (_instance != this)
                Destroy(gameObject);
        }

        // ================================================================
        // Inspector References
        // ================================================================

        [Header("== API Service ==")]
        [Tooltip("Drag the GameObject that has VictoryAPIService attached")]
        [SerializeField] private VictoryAPIService victoryAPIService;

        // ── Root panels ──────────────────────────────────────────────
        [Header("== Root Panels ==")]
        [Tooltip("Full-screen victory overlay — starts INACTIVE in the scene")]
        [SerializeField] private GameObject victoryScreenPanel;

        [Tooltip("Spinner shown while waiting for the API response")]
        [SerializeField] private GameObject loadingSpinner;

        [Tooltip("All content inside the panel — hidden during load")]
        [SerializeField] private GameObject contentRoot;

        // ── Header ───────────────────────────────────────────────────
        [Header("== Header ==")]
        [Tooltip("Main headline: the victory_message from the backend")]
        [SerializeField] private TextMeshProUGUI victoryMessageText;

        [Tooltip("Secondary line: secondary_message (solo vs team message)")]
        [SerializeField] private TextMeshProUGUI secondaryMessageText;

        [Tooltip("Rank badge: e.g. 'Master Detective'")]
        [SerializeField] private TextMeshProUGUI rankTitleText;

        [Tooltip("Banner image — one per theme, swapped at runtime")]
        [SerializeField] private Image themeBannerImage;

        [Tooltip("Theme name → sprite pairs assigned in the Inspector")]
        [SerializeField] private List<ThemeSpritePair> themeSprites;

        // ── Statistics ───────────────────────────────────────────────
        [Header("== Statistics ==")]
        [SerializeField] private TextMeshProUGUI completionTimeText;
        [SerializeField] private TextMeshProUGUI timeRemainingText;
        [SerializeField] private TextMeshProUGUI puzzlesSolvedText;
        [SerializeField] private TextMeshProUGUI hintsUsedText;
        [SerializeField] private TextMeshProUGUI successRateText;

        [Tooltip("'New Record' badge — only shown when is_new_record = true")]
        [SerializeField] private GameObject newRecordBadge;
        [SerializeField] private TextMeshProUGUI previousBestText;

        // ── Difficulty stars ─────────────────────────────────────────
        [Header("== Difficulty Stars ==")]
        [Tooltip("5 star Image objects in order — filled/empty by difficulty")]
        [SerializeField] private Image[] difficultyStars;
        [SerializeField] private Sprite starFilledSprite;
        [SerializeField] private Sprite starEmptySprite;

        // ── Score breakdown ──────────────────────────────────────────
        [Header("== Score Breakdown ==")]
        [SerializeField] private TextMeshProUGUI baseScoreText;
        [SerializeField] private TextMeshProUGUI timeBonusText;
        [SerializeField] private TextMeshProUGUI difficultyMultiplierText;
        [SerializeField] private TextMeshProUGUI hintPenaltyText;

        [Tooltip("Entire team bonus row — hidden when solo (team_bonus == 0)")]
        [SerializeField] private GameObject teamBonusRow;
        [SerializeField] private TextMeshProUGUI teamBonusText;

        [Tooltip("The big animated final score number")]
        [SerializeField] private TextMeshProUGUI finalScoreText;

        // ── Rewards ──────────────────────────────────────────────────
        [Header("== Rewards ==")]
        [SerializeField] private TextMeshProUGUI coinsEarnedText;
        [SerializeField] private TextMeshProUGUI xpEarnedText;

        [Tooltip("Scroll view content parent — achievement badge prefabs spawned here")]
        [SerializeField] private Transform achievementsContainer;

        [Tooltip("Prefab: Image + TextMeshProUGUI child named 'Label'")]
        [SerializeField] private GameObject achievementBadgePrefab;

        // ── Player leaderboard ───────────────────────────────────────
        [Header("== Player Leaderboard ==")]
        [Tooltip("Scroll view content parent — PlayerRowUI prefabs spawned here")]
        [SerializeField] private Transform playerListContainer;

        [Tooltip("Prefab: has PlayerRowUI component attached")]
        [SerializeField] private GameObject playerRowPrefab;

        // ── Buttons ──────────────────────────────────────────────────
        [Header("== Buttons ==")]
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button mainMenuButton;

        // ── Effects ──────────────────────────────────────────────────
        [Header("== Effects ==")]
        [Tooltip("Particle system for gold confetti / sand dust burst")]
        [SerializeField] private ParticleSystem victoryParticles;

        [Tooltip("Victory fanfare audio clip")]
        [SerializeField] private AudioClip victorySoundClip;

        // ── Animation ────────────────────────────────────────────────
        [Header("== Animation ==")]
        [Tooltip("Animator on victoryScreenPanel — must have a 'Show' trigger")]
        [SerializeField] private Animator victoryPanelAnimator;
        [SerializeField] private string showAnimationTrigger = "Show";

        // ── Score animation settings ─────────────────────────────────
        [Header("== Score Animation ==")]
        [Tooltip("How long the score counts up in seconds")]
        [SerializeField] private float scoreCountDuration = 1.8f;

        // ================================================================
        // Internal state
        // ================================================================
        private AudioSource _audioSource;

        // ================================================================
        // Unity Lifecycle
        // ================================================================

        private void Start()
        {
            // Hide panel and content on start
            if (victoryScreenPanel != null) victoryScreenPanel.SetActive(false);

            // Setup audio source
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            // Wire buttons
            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        // ================================================================
        // Public Entry Point — called by FinalDoorController
        // ================================================================

        /// <summary>
        /// Called when the player successfully interacts with the final exit door.
        /// Reads the session ID from GameSessionManager, shows the panel with a
        /// loading spinner, fetches victory data, then populates the UI.
        /// </summary>
        public void ShowVictoryScreen()
        {
            string sessionId = GameSessionManager.Instance?.CurrentSessionId ?? "";

            if (string.IsNullOrEmpty(sessionId))
            {
                Debug.LogError("[VictoryScreenUI] No CurrentSessionId in GameSessionManager. Cannot fetch victory data.");
                return;
            }

            // Show panel immediately with loading state so player gets instant feedback
            victoryScreenPanel.SetActive(true);
            SetLoadingState(true);

            Debug.Log($"<color=cyan>[VictoryScreenUI] Fetching victory data for session: {sessionId}</color>");

            StartCoroutine(victoryAPIService.GetVictoryData(
                sessionId,
                onSuccess: data =>
                {
                    SetLoadingState(false);
                    PopulateUI(data);
                    PlayEntryEffects();
                    Debug.Log($"<color=green>[VictoryScreenUI] ✅ Victory screen populated. Rank: {data.rank_title}</color>");
                },
                onError: err =>
                {
                    SetLoadingState(false);
                    ShowFallback();
                    Debug.LogError($"[VictoryScreenUI] Failed to load victory data: {err}");
                }
            ));
        }

        // ================================================================
        // Loading State
        // ================================================================

        private void SetLoadingState(bool isLoading)
        {
            if (loadingSpinner != null)  loadingSpinner.SetActive(isLoading);
            if (contentRoot != null)     contentRoot.SetActive(!isLoading);
        }

        // ================================================================
        // UI Population
        // ================================================================

        private void PopulateUI(WinningScreenData data)
        {
            PopulateHeader(data);
            PopulateStatistics(data.statistics, data.is_new_record, data.previous_best_time);
            PopulateDifficultyStars(data.mystery != null ? data.mystery.difficulty : 1);
            PopulateScoreBreakdown(data.score);
            PopulateRewards(data.rewards);
            PopulatePlayerLeaderboard(data.players);
            SetThemeBanner(data.mystery != null ? data.mystery.theme : "");
        }

        // ── Header ───────────────────────────────────────────────────

        private void PopulateHeader(WinningScreenData data)
        {
            if (victoryMessageText != null)
                victoryMessageText.text = data.victory_message;

            if (secondaryMessageText != null)
                secondaryMessageText.text = data.secondary_message;

            if (rankTitleText != null)
                rankTitleText.text = data.rank_title;
        }

        // ── Statistics ───────────────────────────────────────────────

        private void PopulateStatistics(SessionStatisticsData stats, bool isNewRecord, int previousBest)
        {
            if (stats == null) return;

            if (completionTimeText != null)
                completionTimeText.text = stats.completion_time_formatted;

            if (timeRemainingText != null)
            {
                int remMin = stats.time_remaining_seconds / 60;
                int remSec = stats.time_remaining_seconds % 60;
                timeRemainingText.text = $"{remMin:00}:{remSec:00} remaining";
            }

            if (puzzlesSolvedText != null)
                puzzlesSolvedText.text = $"{stats.puzzles_solved} / {stats.total_puzzles} Puzzles";

            if (hintsUsedText != null)
                hintsUsedText.text = stats.hints_used == 0
                    ? "No Hints Used!"
                    : $"{stats.hints_used} Hint{(stats.hints_used > 1 ? "s" : "")} Used";

            if (successRateText != null)
                successRateText.text = $"{stats.success_rate:0.0}% Success Rate";

            // New record badge
            if (newRecordBadge != null)
                newRecordBadge.SetActive(isNewRecord);

            // Previous best time line
            if (previousBestText != null)
            {
                if (!isNewRecord && previousBest > 0)
                {
                    int prevMin = previousBest / 60;
                    int prevSec = previousBest % 60;
                    previousBestText.text = $"Previous best: {prevMin:00}:{prevSec:00}";
                    previousBestText.gameObject.SetActive(true);
                }
                else
                {
                    previousBestText.gameObject.SetActive(false);
                }
            }
        }

        // ── Difficulty stars ─────────────────────────────────────────

        private void PopulateDifficultyStars(int difficulty)
        {
            if (difficultyStars == null) return;
            for (int i = 0; i < difficultyStars.Length; i++)
            {
                if (difficultyStars[i] != null)
                {
                    difficultyStars[i].sprite = i < difficulty
                        ? starFilledSprite
                        : starEmptySprite;
                }
            }
        }

        // ── Score breakdown ──────────────────────────────────────────

        private void PopulateScoreBreakdown(ScoreBreakdownData score)
        {
            if (score == null) return;

            if (baseScoreText != null)
                baseScoreText.text = $"{score.base_score:N0}";

            if (timeBonusText != null)
                timeBonusText.text = $"+{score.time_bonus:N0}";

            if (difficultyMultiplierText != null)
                difficultyMultiplierText.text = $"x{score.difficulty_multiplier:0.0}";

            if (hintPenaltyText != null)
                hintPenaltyText.text = score.hint_penalty < 0
                    ? $"{score.hint_penalty:N0}"
                    : "No Penalty";

            // Team bonus row — only visible in multiplayer
            if (teamBonusRow != null)
                teamBonusRow.SetActive(score.team_bonus > 0);

            if (teamBonusText != null && score.team_bonus > 0)
                teamBonusText.text = $"+{score.team_bonus:N0}";

            // Animate final score counting up from 0
            StartCoroutine(AnimateScoreCount(0, score.final_score, scoreCountDuration));
        }

        // Counting animation — eases out so it slows down near the target
        private IEnumerator AnimateScoreCount(int from, int to, float duration)
        {
            if (finalScoreText == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // EaseOut cubic — decelerates near the end
                t = 1f - Mathf.Pow(1f - t, 3f);
                int current = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
                finalScoreText.text = $"{current:N0}";
                yield return null;
            }

            finalScoreText.text = $"{to:N0}";
        }

        // ── Rewards ──────────────────────────────────────────────────

        private void PopulateRewards(RewardsData rewards)
        {
            if (rewards == null) return;

            if (coinsEarnedText != null)
                coinsEarnedText.text = $"+{rewards.coins_earned} Coins";

            if (xpEarnedText != null)
                xpEarnedText.text = $"+{rewards.xp_earned} XP";

            // Clear any previously spawned badges
            if (achievementsContainer != null)
            {
                foreach (Transform child in achievementsContainer)
                    Destroy(child.gameObject);

                // Spawn one badge per achievement string
                if (rewards.achievements != null && achievementBadgePrefab != null)
                {
                    foreach (string achievement in rewards.achievements)
                    {
                        GameObject badge = Instantiate(achievementBadgePrefab, achievementsContainer);
                        // The prefab must have a TMP child named "Label"
                        var label = badge.GetComponentInChildren<TextMeshProUGUI>();
                        if (label != null) label.text = achievement;
                    }
                }
            }
        }

        // ── Player leaderboard ───────────────────────────────────────

        private void PopulatePlayerLeaderboard(List<PlayerPerformanceData> players)
        {
            if (playerListContainer == null || playerRowPrefab == null || players == null) return;

            // Clear old rows
            foreach (Transform child in playerListContainer)
                Destroy(child.gameObject);

            // Sort: highest contribution first
            players.Sort((a, b) => b.contribution_percentage.CompareTo(a.contribution_percentage));

            foreach (PlayerPerformanceData player in players)
            {
                GameObject row = Instantiate(playerRowPrefab, playerListContainer);
                PlayerRowUI rowUI = row.GetComponent<PlayerRowUI>();
                if (rowUI != null)
                    rowUI.Populate(player);
            }
        }

        // ── Theme banner ─────────────────────────────────────────────

        private void SetThemeBanner(string theme)
        {
            if (themeBannerImage == null || themeSprites == null) return;

            foreach (ThemeSpritePair pair in themeSprites)
            {
                if (pair.themeName == theme && pair.sprite != null)
                {
                    themeBannerImage.sprite = pair.sprite;
                    return;
                }
            }
            // No match — leave the default sprite assigned in Inspector
        }

        // ================================================================
        // Effects
        // ================================================================

        private void PlayEntryEffects()
        {
            // Panel slide/fade animation
            if (victoryPanelAnimator != null)
                victoryPanelAnimator.SetTrigger(showAnimationTrigger);

            // Confetti/sand particle burst
            if (victoryParticles != null)
                victoryParticles.Play();

            // Victory fanfare
            if (victorySoundClip != null && _audioSource != null)
                _audioSource.PlayOneShot(victorySoundClip);
        }

        // ================================================================
        // Fallback — shown if the API call fails
        // ================================================================

        private void ShowFallback()
        {
            // Show minimal content so the player isn't left on a blank screen
            contentRoot?.SetActive(true);

            if (victoryMessageText != null)
                victoryMessageText.text = "You've Escaped the Tomb!";
            if (secondaryMessageText != null)
                secondaryMessageText.text = "Congratulations!";
            if (rankTitleText != null)
                rankTitleText.text = "Mystery Solver";
        }

        // ================================================================
        // Button Handlers
        // ================================================================

        private void OnPlayAgainClicked()
        {
            victoryScreenPanel.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
        }

        private void OnMainMenuClicked()
        {
            victoryScreenPanel.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    // ================================================================
    // Helper — serializable theme-to-sprite mapping
    // Appears in the Inspector as a list of name + sprite pairs
    // ================================================================
    [Serializable]
    public class ThemeSpritePair
    {
        [Tooltip("Exact theme string from backend: 'ancient_curse', 'stolen_artifact', etc.")]
        public string themeName;
        public Sprite sprite;
    }
}
