// ============================================================
// GameSessionManager.cs
// Folder: Assets/Scripts/Game/Services/
// Assembly: MysteryRooms.Game.Services
//
// PURPOSE:
//   Lightweight singleton that holds live game state in memory:
//     - CurrentSessionId   (set when session starts)
//     - CurrentMysteryId   (set when mystery is loaded)
//     - Which unlock keys are active (set as puzzles are solved)
//
//   Consumed by:
//     FinalDoorController   → IsDoorUnlocked("victory")
//     VictoryScreenUI       → CurrentSessionId
//     Puzzle scripts        → MarkPuzzleSolved(puzzleId, unlocks)
//
// HOW IT GETS POPULATED:
//   When GameSessionManager.InitSession() is called after
//   MysteryAPIService.StartSession() succeeds.
//
//   When a puzzle solve is confirmed by the backend, the puzzle
//   script calls:
//     GameSessionManager.Instance.MarkPuzzleSolved(puzzleId, unlocks);
//   where unlocks is the List<string> from PuzzleConfigData.unlocks.
//   If it contains "victory", IsDoorUnlocked("victory") becomes true.
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using MysteryRooms.Game.Data;

namespace MysteryRooms.Game.Services
{
    public class GameSessionManager : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Singleton — DontDestroyOnLoad so it persists across scenes
        // ----------------------------------------------------------------
        public static GameSessionManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // ================================================================
        // Public State
        // ================================================================

        /// <summary>The session_id from StartSession API response.</summary>
        public string CurrentSessionId { get; private set; }

        /// <summary>The mystery_id for the current game.</summary>
        public string CurrentMysteryId { get; private set; }

        /// <summary>The full mystery config — used to look up puzzle unlock lists.</summary>
        public MysteryConfigData CurrentMystery { get; private set; }

        /// <summary>How many puzzles are in the current mystery.</summary>
        public int TotalPuzzleCount => CurrentMystery?.puzzles?.Count ?? 0;

        /// <summary>How many puzzles have been solved so far.</summary>
        public int SolvedPuzzleCount => _solvedPuzzles.Count;

        // ================================================================
        // Private State
        // ================================================================

        // All unlock keys activated this session (e.g. "victory", "door_west")
        private readonly HashSet<string> _unlockedKeys = new HashSet<string>();

        // All puzzle IDs confirmed solved by the backend this session
        private readonly HashSet<string> _solvedPuzzles = new HashSet<string>();

        // ================================================================
        // Public API — called when a new session starts
        // ================================================================

        /// <summary>
        /// Call this right after StartSession API succeeds.
        /// Stores the session + mystery IDs and resets all puzzle state.
        /// </summary>
        public void InitSession(string sessionId, string mysteryId, MysteryConfigData mystery)
        {
            CurrentSessionId = sessionId;
            CurrentMysteryId = mysteryId;
            CurrentMystery   = mystery;

            _unlockedKeys.Clear();
            _solvedPuzzles.Clear();

            Debug.Log($"<color=cyan>[GameSessionManager] Session started. ID: {sessionId} | Mystery: {mysteryId}</color>");
        }

        // ================================================================
        // Public API — called by puzzle scripts after backend confirms solve
        // ================================================================

        /// <summary>
        /// Mark a puzzle as solved and activate all its unlock keys.
        /// Pass the puzzle's unlocks list exactly from PuzzleConfigData.unlocks.
        /// If unlocks contains "victory", IsDoorUnlocked("victory") returns true.
        /// </summary>
        public void MarkPuzzleSolved(string puzzleId, List<string> unlockKeys)
        {
            if (_solvedPuzzles.Contains(puzzleId)) return; // already counted

            _solvedPuzzles.Add(puzzleId);

            if (unlockKeys != null)
            {
                foreach (string key in unlockKeys)
                {
                    _unlockedKeys.Add(key);
                    Debug.Log($"<color=green>[GameSessionManager] Unlocked: '{key}' (from puzzle '{puzzleId}')</color>");
                }
            }

            Debug.Log($"[GameSessionManager] Puzzles solved: {_solvedPuzzles.Count} / {TotalPuzzleCount}");
        }

        // ================================================================
        // Public API — queried by door and puzzle scripts
        // ================================================================

        /// <summary>
        /// Returns true if the given unlock key has been activated.
        /// FinalDoorController checks: IsDoorUnlocked("victory")
        /// </summary>
        public bool IsDoorUnlocked(string unlockKey)
        {
            return _unlockedKeys.Contains(unlockKey);
        }

        /// <summary>Returns true if the given puzzle ID has been confirmed solved.</summary>
        public bool IsPuzzleSolved(string puzzleId)
        {
            return _solvedPuzzles.Contains(puzzleId);
        }
    }
}
