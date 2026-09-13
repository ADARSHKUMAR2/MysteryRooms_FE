// ============================================================
// WinningScreenData.cs
// Folder: Assets/Scripts/Game/Data/
// Assembly: MysteryRooms.Game.Data
//
// PURPOSE:
//   C# mirror classes for the backend GET /game/sessions/{id}/victory
//   response. Every field name is snake_case to match the JSON keys
//   the backend sends. JsonUtility.FromJson<WinningScreenData>() maps
//   them automatically.
//
// PATTERN:
//   Follows the same [Serializable] + JsonUtility pattern used in
//   GameSessionData.cs and MysteryConfigData.cs in this same folder.
//
// BACKEND MODEL REFERENCE:
//   WinningScreenData      → backend WinningScreenData (game_session.py)
//   MysteryInfoData        → backend MysteryInfo
//   SessionStatisticsData  → backend SessionStatistics
//   ScoreBreakdownData     → backend ScoreBreakdown
//   RewardsData            → backend RewardsData
//   PlayerPerformanceData  → backend PlayerPerformance
// ============================================================

using System;
using System.Collections.Generic;

namespace MysteryRooms.Game.Data
{
    // ---------------------------------------------------------------
    // Root object — top-level response from the victory endpoint
    // ---------------------------------------------------------------
    [Serializable]
    public class WinningScreenData
    {
        // Always true — sanity check before showing the screen
        public bool victory;

        // e.g. "The ancient curse has been lifted! Excellent work!"
        public string victory_message;

        // e.g. "Solo victory! You conquered the mystery alone!"
        public string secondary_message;

        // e.g. "Master Detective", "Expert Investigator"
        public string rank_title;

        // Session reference — used for the rating call
        public string session_id;

        // Nested sections — each maps to its own backend model
        public MysteryInfoData mystery;
        public SessionStatisticsData statistics;
        public ScoreBreakdownData score;
        public RewardsData rewards;

        // One entry per player (always at least 1, even in solo)
        public List<PlayerPerformanceData> players;

        // ISO 8601 timestamp string e.g. "2026-09-11T06:47:08.881Z"
        public string completed_at;

        // True when this run beats the player's previous fastest time
        public bool is_new_record;

        // Previous best in seconds — 0 means no prior record
        public int previous_best_time;
    }

    // ---------------------------------------------------------------
    // Mystery context — which mystery was just beaten
    // ---------------------------------------------------------------
    [Serializable]
    public class MysteryInfoData
    {
        public string mystery_id;  // UUID
        public string theme;       // "ancient_curse", "stolen_artifact", etc.
        public int difficulty;     // 1–5
        public string room;        // "mummy_tomb"
        public string objective;   // Mission text shown during gameplay
    }

    // ---------------------------------------------------------------
    // Session statistics — time, puzzles, hints
    // ---------------------------------------------------------------
    [Serializable]
    public class SessionStatisticsData
    {
        public int completion_time_seconds;      // Raw total seconds
        public string completion_time_formatted; // "MM:SS" ready to display
        public int time_limit_seconds;           // e.g. 1800
        public int time_remaining_seconds;       // time_limit - completion_time
        public int puzzles_solved;               // How many puzzles done
        public int total_puzzles;                // Total in the mystery
        public int hints_used;                   // Across all players
        public int puzzle_attempts;              // Total attempts (pass + fail)
        public float success_rate;               // e.g. 62.5 (percentage)
    }

    // ---------------------------------------------------------------
    // Score breakdown — shown line-by-line on the results screen
    // ---------------------------------------------------------------
    [Serializable]
    public class ScoreBreakdownData
    {
        public int base_score;              // Always 1000
        public int time_bonus;             // Positive: faster = higher
        public float difficulty_multiplier;// 1.0 to 2.0
        public int hint_penalty;           // Negative: e.g. -100
        public int team_bonus;             // 0 if solo; 100+ in multiplayer
        public int final_score;            // The total displayed prominently
    }

    // ---------------------------------------------------------------
    // Rewards — coins, XP, achievements the player keeps
    // ---------------------------------------------------------------
    [Serializable]
    public class RewardsData
    {
        public int coins_earned;              // e.g. 75
        public int xp_earned;               // e.g. 150
        public List<string> achievements;   // e.g. ["No Hints Master", "Speed Demon"]
        public List<string> badges;         // e.g. ["First Victory"]
    }

    // ---------------------------------------------------------------
    // Per-player performance — one entry per player in the session
    // ---------------------------------------------------------------
    [Serializable]
    public class PlayerPerformanceData
    {
        public string user_id;
        public string username;
        public int puzzles_solved;
        public int puzzles_attempted;
        public int hints_used;
        public float contribution_percentage; // e.g. 60.0
        public bool mvp;                      // True for the top contributor
    }
}
