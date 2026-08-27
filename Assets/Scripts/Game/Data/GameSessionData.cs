using System;
using System.Collections.Generic;

namespace MysteryRooms.Game.Data
{
    [Serializable]
    public class StartSessionRequest
    {
        public string mystery_id;
        public List<string> player_ids;
        public int max_players = 1;
    }

    [Serializable]
    public class UpdateSessionRequest
    {
        public string session_id;
        public string puzzle_solved;
        public string puzzle_attempted;
        public bool hint_used;
        public string player_id;
        public int time_elapsed_seconds;
    }

    [Serializable]
    public class CompleteSessionRequest
    {
        public string session_id;
        public string status; // "completed", "failed", "abandoned"
        public int difficulty_rating;
    }

    [Serializable]
    public class GameSessionData
    {
        public string session_id;
        public string mystery_id;
        public string room;
        public string status;
        public int time_elapsed_seconds;
        public List<string> puzzles_solved;
    }

    [Serializable]
    public class JoinSessionRequestPayload
    {
        public string player_id;
    }
}
