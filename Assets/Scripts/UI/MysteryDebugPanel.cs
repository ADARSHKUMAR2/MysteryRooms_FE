using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MysteryRooms.Game.Managers;
using MysteryRooms.Game.Data;

namespace MysteryRooms.UI
{
    public class MysteryDebugPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MysteryLoader mysteryLoader;

        [Header("UI Elements")]
        [SerializeField] private Button generateButton;
        [SerializeField] private Slider difficultySlider;
        [SerializeField] private TextMeshProUGUI difficultyText;
        [SerializeField] private TextMeshProUGUI mysteryInfoText;
        [SerializeField] private TextMeshProUGUI statusText;

        private void Start()
        {
            if (generateButton != null)
            {
                generateButton.onClick.AddListener(OnGenerateClicked);
            }

            if (difficultySlider != null)
            {
                difficultySlider.onValueChanged.AddListener(OnDifficultyChanged);
                OnDifficultyChanged(difficultySlider.value);
            }

            if (mysteryLoader != null)
            {
                mysteryLoader.OnMysteryLoaded += OnMysteryLoaded;
                mysteryLoader.OnMysteryLoadFailed += OnMysteryLoadFailed;
            }
        }

        private void OnGenerateClicked()
        {
            if (mysteryLoader == null) return;

            int difficulty = (int)difficultySlider.value;
            mysteryLoader.SetDifficulty(difficulty);

            SetStatus("⏳ Generating mystery...");
            mysteryLoader.GenerateNewMystery();
        }

        private void OnDifficultyChanged(float value)
        {
            int diff = (int)value;
            if (difficultyText != null)
            {
                difficultyText.text = $"Difficulty: {diff}";
            }
        }

        private void OnMysteryLoaded(MysteryConfigData mystery)
        {
            SetStatus("✅ Mystery loaded successfully!");
            
            if (mysteryInfoText != null)
            {
                string info = $"<b>Mystery ID:</b> {mystery.mystery_id}\n" +
                             $"<b>Room:</b> {mystery.room}\n" +
                             $"<b>Difficulty:</b> {mystery.difficulty}/5\n" +
                             $"<b>Theme:</b> {mystery.theme}\n" +
                             $"<b>Objective:</b> {mystery.objective}\n" +
                             $"<b>Time Limit:</b> {mystery.time_limit_seconds}s\n" +
                             $"<b>Puzzles:</b> {mystery.puzzles.Count}\n" +
                             $"<b>Clues:</b> {mystery.clues.Count}";
                
                mysteryInfoText.text = info;
            }
        }

        private void OnMysteryLoadFailed(string error)
        {
            SetStatus($"❌ Failed: {error}");
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log(message);
        }

        private void OnDestroy()
        {
            if (mysteryLoader != null)
            {
                mysteryLoader.OnMysteryLoaded -= OnMysteryLoaded;
                mysteryLoader.OnMysteryLoadFailed -= OnMysteryLoadFailed;
            }
        }
    }
}
