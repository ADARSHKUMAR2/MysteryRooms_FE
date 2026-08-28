using UnityEngine;
using System.Collections.Generic;

namespace MysteryRooms.Game.Data
{
    [System.Serializable]
    public struct CardSpriteEntry
    {
        [Tooltip("Card identifier like 'Spades_A', 'Hearts_K', etc.")]
        public string cardId;
        public Sprite cardSprite;
    }

    [CreateAssetMenu(fileName = "CardDatabase", menuName = "MysteryRooms/Data/Card Database")]
    public class CardDatabase : ScriptableObject
    {
        [Header("Card Sprites")]
        public List<CardSpriteEntry> cards = new List<CardSpriteEntry>();
        
        [Header("Card Back")]
        public Sprite cardBackSprite;

        private Dictionary<string, Sprite> _cardLookup;

        /// <summary>
        /// Get sprite for a specific card
        /// </summary>
        public Sprite GetCardSprite(string suit, string rank)
        {
            if (_cardLookup == null || _cardLookup.Count != cards.Count)
            {
                InitializeDictionary();
            }

            string cardId = $"{suit}_{rank}";
            
            if (_cardLookup.TryGetValue(cardId, out Sprite sprite))
            {
                return sprite;
            }

            Debug.LogWarning($"[CardDatabase] Card sprite not found: {cardId}");
            return cardBackSprite; // Fallback
        }

        private void InitializeDictionary()
        {
            _cardLookup = new Dictionary<string, Sprite>();
            foreach (var entry in cards)
            {
                if (!string.IsNullOrEmpty(entry.cardId) && !_cardLookup.ContainsKey(entry.cardId))
                {
                    _cardLookup.Add(entry.cardId, entry.cardSprite);
                }
            }
        }

        /// <summary>
        /// Get all card names for the database
        /// </summary>
        public List<string> GetAllCardIds()
        {
            List<string> ids = new List<string>();
            foreach (var entry in cards)
            {
                if (!string.IsNullOrEmpty(entry.cardId))
                {
                    ids.Add(entry.cardId);
                }
            }
            return ids;
        }
    }
}
