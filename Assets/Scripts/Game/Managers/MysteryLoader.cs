using UnityEngine;
using System.Collections;
using MysteryRooms.Game.Data;
using MysteryRooms.Game.Services;

namespace MysteryRooms.Game.Managers
{
    public class MysteryLoader : MonoBehaviour
    {
        private static MysteryLoader instance;

        [Header("References")]
        [SerializeField] private MysteryAPIService apiService;

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
            if (instance != null && instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(this.gameObject);

            if (apiService == null)
            {
                apiService = GetComponent<MysteryAPIService>();
            }
        }

        public void GenerateNewMystery()
        {
            GenerateMysteryRequest request = new GenerateMysteryRequest
            {
                room = this.room,
                difficulty = this.difficulty,
                player_count = this.playerCount
            };
            StartCoroutine(apiService.GenerateMystery(request, OnMysteryGenerationSuccess, OnMysteryGenerationError));
        }

        public void LoadMysteryById(string mysteryId)
        {
            StartCoroutine(apiService.GetMystery(mysteryId, OnMysteryGenerationSuccess, OnMysteryGenerationError));
        }

        public void OnJoinByCodeClicked(string inputCode)
        {
            StartCoroutine(apiService.GetMysteryByShareCode(inputCode, OnMysteryGenerationSuccess, OnMysteryGenerationError));
        }

        private void OnMysteryGenerationSuccess(MysteryConfigData mystery)
        {
            LoadMysteryData(mystery);
        }

        private void OnMysteryGenerationError(string error)
        {
            Debug.LogError($"❌ Mystery load failed: {error}");
            OnMysteryLoadFailed?.Invoke(error);
        }

        public void SetDifficulty(int diff) => difficulty = Mathf.Clamp(diff, 1, 5);
        public void SetRoom(string roomType) => room = roomType;
        public void SetPlayerCount(int count) => playerCount = Mathf.Clamp(count, 1, 4);

        public void LoadMysteryData(MysteryConfigData mystery)
        {
            if (mystery == null)
            {
                Debug.LogError("Cannot load null mystery data!");
                OnMysteryLoadFailed?.Invoke("Mystery data is null");
                return;
            }

            currentMystery = mystery;
            Debug.Log($"🎯 Mystery JSON loaded into memory: {mystery.objective}");

            // We just announce it to whoever is listening (if anyone)
            OnMysteryLoaded?.Invoke(mystery);
        }

        public MysteryConfigData GetCurrentMystery() => currentMystery;
        public bool HasMysteryLoaded() => currentMystery != null;
    }
}
