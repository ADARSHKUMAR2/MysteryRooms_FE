using UnityEngine;
using System.Collections.Generic;

namespace MysteryRooms.Game.Data
{
    [System.Serializable]
    public struct SymbolEntry
    {
        [Tooltip("The exact string name used in the backend (e.g., 'EyeOfHorus')")]
        public string symbolName;
        public Sprite symbolSprite;
    }

    [CreateAssetMenu(fileName = "NewSymbolDatabase", menuName = "MysteryRooms/Data/Symbol Database")]
    public class SymbolDatabase : ScriptableObject
    {
        [Header("Symbol Dictionary")]
        public List<SymbolEntry> symbols = new List<SymbolEntry>();

        // We use a dictionary at runtime for fast lookups instead of looping the list
        private Dictionary<string, Sprite> _symbolLookup;

        /// <summary>
        /// Retrieves the sprite associated with the given backend string name.
        /// </summary>
        public Sprite GetSprite(string symbolName)
        {
            // Initialize dictionary on first use
            if (_symbolLookup == null || _symbolLookup.Count != symbols.Count)
            {
                InitializeDictionary();
            }

            if (_symbolLookup.TryGetValue(symbolName, out Sprite foundSprite))
            {
                return foundSprite;
            }

            Debug.LogWarning($"[SymbolDatabase] Sprite not found for symbol name: {symbolName}");
            return null;
        }

        private void InitializeDictionary()
        {
            _symbolLookup = new Dictionary<string, Sprite>();
            foreach (var entry in symbols)
            {
                if (!string.IsNullOrEmpty(entry.symbolName) && !_symbolLookup.ContainsKey(entry.symbolName))
                {
                    _symbolLookup.Add(entry.symbolName, entry.symbolSprite);
                }
            }
        }
    }
}
