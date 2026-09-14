using System;
using System.Collections.Generic;
using UnityEngine;
using MysteryRooms.Game.Data;

namespace MysteryRooms.Game.Services
{
    [Serializable]
    public class MysteryCacheWrapper
    {
        public List<MysteryConfigData> mysteries;
    }

    public static class MysteryCacheService
    {
        private const string CACHE_KEY = "MysteryRooms_RecentMysteriesCache";

        public static List<MysteryConfigData> LoadFromCache()
        {
            // 1. Try to load from PlayerPrefs (Saved cache)
            if (PlayerPrefs.HasKey(CACHE_KEY))
            {
                try
                {
                    string json = PlayerPrefs.GetString(CACHE_KEY);
                    var wrapper = JsonUtility.FromJson<MysteryCacheWrapper>(json);
                    if (wrapper != null && wrapper.mysteries != null && wrapper.mysteries.Count > 0)
                    {
                        return wrapper.mysteries; // Return cached mysteries!
                    }
                }
                catch { /* Fallback to default below */ }
            }

            // 2. FIRST TIME PLAYER / NO CACHE? Load the starter pack!
            Debug.Log("[MysteryCacheService] No local cache found. Loading Starter Pack from Resources.");
            TextAsset defaultMysteriesAsset = Resources.Load<TextAsset>("DefaultMysteries");
            
            if (defaultMysteriesAsset != null)
            {
                var wrapper = JsonUtility.FromJson<MysteryCacheWrapper>(defaultMysteriesAsset.text);
                if (wrapper != null && wrapper.mysteries != null)
                {
                    // Optional: Save this starter pack into the cache so we have it next time
                    SaveToCache(wrapper.mysteries);
                    return wrapper.mysteries;
                }
            }

            return new List<MysteryConfigData>(); // Absolute fallback (empty)
        }

        public static void SaveToCache(List<MysteryConfigData> mysteries)
        {
            try
            {
                var wrapper = new MysteryCacheWrapper { mysteries = mysteries };
                PlayerPrefs.SetString(CACHE_KEY, JsonUtility.ToJson(wrapper));
                PlayerPrefs.Save();
            }
            catch (Exception e) { Debug.LogWarning("Failed to cache mysteries: " + e.Message); }
        }
    }
}
