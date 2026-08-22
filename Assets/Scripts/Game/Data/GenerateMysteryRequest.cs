using System;

namespace MysteryRooms.Game.Data
{
    [Serializable]
    public class GenerateMysteryRequest
    {
        public string room = "mummy_tomb";
        public int difficulty = 3;
        public int player_count = 1;
    }
}
