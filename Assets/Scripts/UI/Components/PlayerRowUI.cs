// ============================================================
// PlayerRowUI.cs
// Folder: Assets/Scripts/UI/Components/
// Assembly: MysteryRooms.Main (via MysteryRooms.Main.asmdef)
//
// PURPOSE:
//   Component on the PlayerRow prefab inside the leaderboard.
//   VictoryScreenUI instantiates one row per player and calls
//   Populate(PlayerPerformanceData) to fill in all fields.
//
// PATTERN:
//   Same namespace and style as AnimatedButton.cs in this folder.
//
// PREFAB LAYOUT (create this in Unity):
//   PlayerRow (this component lives here)
//   ├── MvpCrownObject (GameObject) ← SetActive(player.mvp)
//   ├── UsernameText   (TMP)        ← player.username
//   ├── PuzzlesText    (TMP)        ← "3 Puzzles"
//   ├── HintsText      (TMP)        ← "1 Hint" or "No Hints!"
//   ├── ContribText    (TMP)        ← "60.0%"
//   ├── ContribBar     (Slider)     ← value = contribution / 100
//   └── RowBackground  (Image)      ← gold if MVP, normal otherwise
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MysteryRooms.Game.Data;

namespace MysteryRooms.UI
{
    public class PlayerRowUI : MonoBehaviour
    {
        [Header("Row References")]
        [Tooltip("Crown icon — visible only when player.mvp is true")]
        [SerializeField] private GameObject mvpCrownObject;

        [SerializeField] private TextMeshProUGUI usernameText;

        [Tooltip("e.g. '3 Puzzles' or '1 Puzzle'")]
        [SerializeField] private TextMeshProUGUI puzzlesSolvedText;

        [Tooltip("e.g. 'No Hints!' or '2 Hints'")]
        [SerializeField] private TextMeshProUGUI hintsText;

        [Tooltip("e.g. '60.0%'")]
        [SerializeField] private TextMeshProUGUI contributionText;

        [Tooltip("Horizontal slider 0.0–1.0 showing contribution proportion")]
        [SerializeField] private Slider contributionBar;

        [Tooltip("Row background image — tinted gold for MVP")]
        [SerializeField] private Image rowBackground;

        [Header("MVP Styling")]
        [SerializeField] private Color mvpBackgroundColor  = new Color(1f, 0.84f, 0f, 0.3f);
        [SerializeField] private Color normalBackgroundColor = new Color(1f, 1f, 1f, 0.1f);

        // ================================================================
        // Called by VictoryScreenUI immediately after Instantiate()
        // ================================================================

        /// <summary>
        /// Fills all text fields and toggles MVP crown from PlayerPerformanceData.
        /// </summary>
        public void Populate(PlayerPerformanceData player)
        {
            if (usernameText != null)
                usernameText.text = player.username;

            if (puzzlesSolvedText != null)
                puzzlesSolvedText.text = player.puzzles_solved == 1
                    ? "1 Puzzle"
                    : $"{player.puzzles_solved} Puzzles";

            if (hintsText != null)
                hintsText.text = player.hints_used == 0
                    ? "No Hints!"
                    : player.hints_used == 1
                        ? "1 Hint"
                        : $"{player.hints_used} Hints";

            if (contributionText != null)
                contributionText.text = $"{player.contribution_percentage:0.0}%";

            if (contributionBar != null)
                contributionBar.value = player.contribution_percentage / 100f;

            if (mvpCrownObject != null)
                mvpCrownObject.SetActive(player.mvp);

            if (rowBackground != null)
                rowBackground.color = player.mvp ? mvpBackgroundColor : normalBackgroundColor;
        }
    }
}
