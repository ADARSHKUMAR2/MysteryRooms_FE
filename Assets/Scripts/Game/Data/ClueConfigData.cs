using System;

namespace MysteryRooms.Game.Data
{
    [Serializable]
    public class ClueConfigData
    {
        public string id;
        public string type;
        public string location;
        public string content;
        public string related_puzzle;
        public string requires_puzzle_solved;
    }
}
