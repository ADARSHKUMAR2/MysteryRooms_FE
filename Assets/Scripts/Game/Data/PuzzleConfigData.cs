using System;
using System.Collections.Generic;

/// <summary>
/// Individual puzzle configuration from backend.
/// </summary>

namespace MysteryRooms.Game.Data
{
    [Serializable]
    public class PuzzleConfigData
    {
        public string id;
        public string type;
        public string position;
        public PuzzleConfigParams config;
        public List<string> dependencies;
        public List<string> unlocks;
        public string hint;
    }

    [Serializable]
    public class PuzzleConfigParams
    {
        // Rotating Statue
        public int correctRotationSteps;
        
        // Combination Lock
        public string correctCombination;
        public ElementMapping elementalMapping;
        public string clueStyle;
        
        // Symbol/Hieroglyph Sequence
        public List<string> correctSequence;
        public string patternType; // "horizontal_row" or "vertical_column"
        public PatternStartPosition patternStartPosition;
        
        // Map Coordinates
        public string correctCoordinates;
        
        // Pressure Plate
        public List<int> correctPattern;
        
        // Hidden Compartment
        public bool requiresKey;
        
        // Light Puzzle
        public List<int> correctTorchOrder;
        public bool requiresAlignment;
        
        // Card Deck Riddle 
        public List<RiddleRule> riddleRules;
        public string correctCode;
        public List<CardData> gridCards;

        public string latitude;
        public string longitude;
    }
    [Serializable]
    public class PatternStartPosition
    {
        public int row; // 0-4
        public int col; // 0-7
    }

    [Serializable]
    public class ElementMapping
    {
        public int Fire;
        public int Leaf;
        public int Water;
        public int Sun;
        
        public Dictionary<string, int> ToDictionary()
        {
            return new Dictionary<string, int>
            {
                { "Fire", Fire },
                { "Leaf", Leaf },
                { "Water", Water },
                { "Sun", Sun }
            };
        }
    }
}
