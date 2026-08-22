using UnityEngine;
using System.Collections;
using MysteryRooms.Game.Data;
using MysteryRooms.Game.Services;

namespace MysteryRooms.Game.Managers
{
    public class MysteryLoader : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MysteryAPIService apiService;
        [SerializeField] private DynamicPuzzleManager puzzleManager;

        [Header("Mystery Settings")]
        [SerializeField] private string room = "mummy_tomb";
        [SerializeField] private int difficulty = 3;
        [SerializeField] private int playerCount = 1;

        [Header("Current Mystery")]
        public MysteryConfigData currentMystery;

        [Header("UI Callbacks")]
        public System.Action<MysteryConfigData> OnMysteryLoaded;
        public System.Action<string> OnMysteryLoadFailed;

        private void Awake()
        {
            if (apiService == null)
            {
                apiService = GetComponent<MysteryAPIService>();
                if (apiService == null)
                {
                    Debug.LogError("MysteryAPIService not found!");
                }
            }

            if (puzzleManager == null)
            {
                puzzleManager = FindObjectOfType<DynamicPuzzleManager>();
                if (puzzleManager == null)
                {
                    Debug.LogError("DynamicPuzzleManager not found in scene!");
                }
            }
        }

        /// <summary>
        /// Generate and load a new mystery from backend
        /// </summary>
        public void GenerateNewMystery()
        {
            GenerateMysteryRequest request = new GenerateMysteryRequest
            {
                room = this.room,
                difficulty = this.difficulty,
                player_count = this.playerCount
            };

            StartCoroutine(apiService.GenerateMystery(
                request,
                OnMysteryGenerationSuccess,
                OnMysteryGenerationError
            ));
        }

        /// <summary>
        /// Load an existing mystery by ID
        /// </summary>
        public void LoadMysteryById(string mysteryId)
        {
            StartCoroutine(apiService.GetMystery(
                mysteryId,
                OnMysteryGenerationSuccess,
                OnMysteryGenerationError
            ));
        }

        private void OnMysteryGenerationSuccess(MysteryConfigData mystery)
        {
            currentMystery = mystery;
            Debug.Log($"🎯 Mystery loaded: {mystery.objective}");
            Debug.Log($"📊 Difficulty: {mystery.difficulty} | Puzzles: {mystery.puzzles.Count}");

            // Configure puzzles with the loaded mystery
            if (puzzleManager != null)
            {
                puzzleManager.ConfigurePuzzlesFromMystery(mystery);
            }

            // Notify listeners
            OnMysteryLoaded?.Invoke(mystery);
        }

        private void OnMysteryGenerationError(string error)
        {
            Debug.LogError($"❌ Mystery load failed: {error}");
            OnMysteryLoadFailed?.Invoke(error);
        }

        /// <summary>
        /// Public accessors for UI
        /// </summary>
        public void SetDifficulty(int diff) => difficulty = Mathf.Clamp(diff, 1, 5);
        public void SetRoom(string roomType) => room = roomType;
        public void SetPlayerCount(int count) => playerCount = Mathf.Clamp(count, 1, 4);
    }
}
