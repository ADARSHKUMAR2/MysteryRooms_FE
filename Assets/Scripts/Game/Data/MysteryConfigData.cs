using System;
using System.Collections.Generic;

namespace MysteryRooms.Game.Data
{
    [Serializable]
    public class MysteryConfigData
    {
        public string mystery_id;
        public string room;
        public int difficulty;
        public string theme;
        public string objective;
        public int time_limit_seconds;
        public List<PuzzleConfigData> puzzles;
        public List<ClueConfigData> clues;
        public string twist;
        public string created_at;
    }
}
