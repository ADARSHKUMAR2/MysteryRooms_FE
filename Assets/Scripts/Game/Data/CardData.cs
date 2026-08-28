using System;
using System.Collections.Generic;

namespace MysteryRooms.Game.Data
{
    [Serializable]
    public class CardData
    {
        public string suit;  // "Spades", "Hearts", "Diamonds", "Clubs"
        public string rank;  // "A", "2", "3", ... "K"
        
        public CardData(string suit, string rank)
        {
            this.suit = suit;
            this.rank = rank;
        }
    }

    [Serializable]
    public class RiddleRule
    {
        public int column;   // 0-3
        public string suit;  // Which suit to count
        public int count;    // Expected count
        
        public RiddleRule(int column, string suit, int count)
        {
            this.column = column;
            this.suit = suit;
            this.count = count;
        }
    }
}
