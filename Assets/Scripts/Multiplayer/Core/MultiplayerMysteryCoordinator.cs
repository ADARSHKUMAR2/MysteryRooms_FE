using System;
using System.Collections;
using UnityEngine;
using MysteryRooms.Game.Services;
using MysteryRooms.Game.Managers;
using MysteryRooms.Game.Data;
using MysteryRooms.Multiplayer.Network;

namespace MysteryRooms.Multiplayer.Core
{
    /// <summary>
    /// Coordinates multiplayer sessions with mystery generation/loading.
    /// This is the bridge between Unity Gaming Services and your Python backend.
    /// 
    /// Flow:
    /// HOST: Generate mystery → Get share_code → Create Unity session with that code
    /// CLIENT: Join Unity session → Fetch mystery using the join code
    /// </summary>
    public class MultiplayerMysteryCoordinator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MultiplayerSessionManager sessionManager;
        [SerializeField] private MysteryAPIService apiService;
        [SerializeField] private MysteryLoader mysteryLoader;

        [Header("Mystery Settings")]
        [SerializeField] private string room = "mummy_tomb";
        [SerializeField] private int difficulty = 3;
        [SerializeField] private int playerCount = 4;

        // Current state
        private bool isGeneratingMystery = false;
        private bool isLoadingMystery = false;

        // Events
        public event Action<MysteryConfigData> OnMysteryReady;
        public event Action<string> OnError;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            // Validate references
            if (sessionManager == null)
            {
                sessionManager = FindObjectOfType<MultiplayerSessionManager>();
            }
            
            if (apiService == null)
            {
                apiService = FindObjectOfType<MysteryAPIService>();
            }
            
            if (mysteryLoader == null)
            {
                mysteryLoader = FindObjectOfType<MysteryLoader>();
            }

            // Subscribe to session events
            if (sessionManager != null)
            {
                sessionManager.OnSessionCreated += OnSessionCreatedHandler;
                sessionManager.OnSessionJoined += OnSessionJoinedHandler;
                sessionManager.OnConnectionFailed += OnConnectionFailedHandler;
            }
        }

        #region Host Flow

        /// <summary>
        /// Host a new multiplayer mystery.
        /// Flow: Generate mystery → Get share_code → Create Unity session
        /// </summary>
        public void HostNewMystery(string roomType, int difficultyLevel, int players)
        {
            if (isGeneratingMystery)
            {
                Debug.LogWarning("Already generating a mystery!");
                return;
            }

            room = roomType;
            difficulty = difficultyLevel;
            playerCount = players;

            Debug.Log($"🎮 Starting Host Flow: Room={room}, Difficulty={difficulty}, Players={playerCount}");
            
            StartCoroutine(HostMysteryCoroutine());
        }

        private IEnumerator HostMysteryCoroutine()
        {
            isGeneratingMystery = true;

            // Step 1: Generate mystery from backend
            Debug.Log("📡 Step 1: Generating mystery from backend...");
            
            GenerateMysteryRequest request = new GenerateMysteryRequest
            {
                room = room,
                difficulty = difficulty,
                player_count = playerCount
            };

            MysteryConfigData generatedMystery = null;
            string error = null;

            yield return apiService.GenerateMystery(
                request,
                mystery => generatedMystery = mystery,
                err => error = err
            );

            if (error != null || generatedMystery == null)
            {
                Debug.LogError($"❌ Mystery generation failed: {error}");
                OnError?.Invoke($"Failed to generate mystery: {error}");
                isGeneratingMystery = false;
                yield break;
            }

            Debug.Log($"✅ Mystery generated! Share Code: {generatedMystery.share_code}");

            // Step 2: Create Unity session using the mystery's share_code
            Debug.Log($"🌐 Step 2: Creating Unity session with code: {generatedMystery.share_code}");
            
            var createTask = sessionManager.CreateSessionAsync(generatedMystery.share_code);
            yield return new WaitUntil(() => createTask.IsCompleted);

            string joinCode = createTask.Result;

            if (string.IsNullOrEmpty(joinCode))
            {
                Debug.LogError("❌ Failed to create Unity session");
                OnError?.Invoke("Failed to create multiplayer session");
                isGeneratingMystery = false;
                yield break;
            }

            Debug.Log($"✅ Unity session created with join code: {joinCode}");
            // yield return sessionManager.WaitForPlayers(minimumPlayers: 2, timeoutSeconds: 30f);


            // Step 3: Load the mystery into the scene
            Debug.Log("🎯 Step 3: Loading mystery into scene...");
            mysteryLoader.LoadMysteryData(generatedMystery);

            NetworkedPuzzleManager.SetPendingBackendShareCode(generatedMystery.share_code);

            isGeneratingMystery = false;
            OnMysteryReady?.Invoke(generatedMystery);

            // Debug.Log("🎮 Starting networked game...");
            // sessionManager.StartNetworkedGame();

            Debug.Log("🎉 Host setup complete!");
        }

                /// <summary>
        /// Host a multiplayer session using an EXISTING mystery from the database
        /// </summary>
        public void HostExistingMystery(MysteryConfigData oldMystery, int players)
        {
            if (isGeneratingMystery) return;
            
            playerCount = players;
            Debug.Log($"🎮 Starting Host Flow with EXISTING Mystery: {oldMystery.share_code}");
            
            StartCoroutine(HostExistingMysteryCoroutine(oldMystery));
        }

        private IEnumerator HostExistingMysteryCoroutine(MysteryConfigData oldMystery)
        {
            isGeneratingMystery = true;

            // Step 1: Create Unity session using the OLD mystery's share_code
            Debug.Log($"🌐 Creating Unity session with old code: {oldMystery.share_code}");
            
            var createTask = sessionManager.CreateSessionAsync(oldMystery.share_code);
            yield return new WaitUntil(() => createTask.IsCompleted);

            string joinCode = createTask.Result;

            if (string.IsNullOrEmpty(joinCode))
            {
                Debug.LogError("❌ Failed to create Unity session");
                OnError?.Invoke("Failed to create multiplayer session");
                isGeneratingMystery = false;
                yield break;
            }

            Debug.Log($"✅ Unity session created with code: {joinCode}");
            // yield return sessionManager.WaitForPlayers(minimumPlayers: 2, timeoutSeconds: 30f);


            // Step 2: Load the mystery into the scene
            Debug.Log("🎯 Loading existing mystery into scene...");
            mysteryLoader.LoadMysteryData(oldMystery);

            NetworkedPuzzleManager.SetPendingBackendShareCode(oldMystery.share_code);

            isGeneratingMystery = false;
            OnMysteryReady?.Invoke(oldMystery);

            // Debug.Log("🎮 Starting networked game...");
            // sessionManager.StartNetworkedGame();
            
            Debug.Log("🎉 Host setup complete for existing mystery!");
        }


        #endregion

        #region Client Flow

        /// <summary>
        /// Join an existing multiplayer mystery using a code.
        /// Flow: Join Unity session → Fetch mystery from backend → Load mystery
        /// </summary>
        public void JoinMysteryByCode(string joinCode)
        {
            if (isLoadingMystery)
            {
                Debug.LogWarning("Already loading a mystery!");
                return;
            }

            Debug.Log($"🎮 Starting Client Flow: Join Code={joinCode}");
            
            StartCoroutine(JoinMysteryCoroutine(joinCode));
        }

        private IEnumerator JoinMysteryCoroutine(string joinCode)
        {
            isLoadingMystery = true;

            // Step 1: Join Unity session
            Debug.Log($"🌐 Step 1: Joining Unity session with code: {joinCode}");
            
            var joinTask = sessionManager.JoinSessionAsync(joinCode);
            yield return new WaitUntil(() => joinTask.IsCompleted);

            bool joined = joinTask.Result;

            if (!joined)
            {
                Debug.LogError("❌ Failed to join Unity session");
                OnError?.Invoke("Failed to join multiplayer session");
                isLoadingMystery = false;
                yield break;
            }

            Debug.Log("✅ Joined Unity session successfully");

            // The backend share code was retrieved during JoinSessionAsync and saved to CurrentSession
            string actualShareCode = sessionManager.CurrentSession.mysteryId;
            
            if (string.IsNullOrEmpty(actualShareCode))
            {
                FailClientJoin("Could not retrieve backend mystery code from session properties.");
                yield break;
            }

            Debug.Log($"📡 Step 2: Fetching mystery from backend with REAL code: {actualShareCode}");
            
            MysteryConfigData fetchedMystery = null;
            string error = null;
            
            yield return apiService.GetMysteryByShareCode(
                actualShareCode,
                mystery => fetchedMystery = mystery,
                err => error = err
            );

            if (error != null || fetchedMystery == null)
            {
                Debug.LogError($"❌ Failed to fetch mystery: {error}");
                OnError?.Invoke($"Failed to load mystery: {error}");
                isLoadingMystery = false;
                yield break;
            }

            Debug.Log($"✅ Mystery fetched! ID: {fetchedMystery.mystery_id}");

            // Step 3: Load the mystery into the scene
            Debug.Log("🎯 Step 3: Loading mystery into scene...");
            mysteryLoader.LoadMysteryData(fetchedMystery);

            isLoadingMystery = false;
            OnMysteryReady?.Invoke(fetchedMystery);

            Debug.Log("🎉 Client setup complete! Waiting for host to start the game.");
        }


        private void FailClientJoin(string message)
        {
            Debug.LogError(message);
            OnError?.Invoke(message);
            isLoadingMystery = false;
        }

        #endregion

        #region Event Handlers

        private void OnSessionCreatedHandler(string joinCode)
        {
            Debug.Log($"📢 Session created event received: {joinCode}");
            // Session creation is handled in HostMysteryCoroutine
        }

        private void OnSessionJoinedHandler()
        {
            Debug.Log("📢 Session joined event received");
            // Session joining is handled in JoinMysteryCoroutine
        }

        private void OnConnectionFailedHandler(string errorMessage)
        {
            Debug.LogError($"📢 Connection failed: {errorMessage}");
            OnError?.Invoke(errorMessage);
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            if (sessionManager != null)
            {
                sessionManager.OnSessionCreated -= OnSessionCreatedHandler;
                sessionManager.OnSessionJoined -= OnSessionJoinedHandler;
                sessionManager.OnConnectionFailed -= OnConnectionFailedHandler;
            }
        }

        #endregion

        #region Public Accessors

        public void SetMysterySettings(string roomType, int difficultyLevel, int players)
        {
            room = roomType;
            difficulty = difficultyLevel;
            playerCount = players;
        }

        #endregion
    }
}
