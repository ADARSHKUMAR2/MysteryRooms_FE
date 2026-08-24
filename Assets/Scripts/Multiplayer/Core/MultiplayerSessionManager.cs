using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using Unity.Netcode;
using Unity.Services.Vivox;

namespace MysteryRooms.Multiplayer.Core
{
    /// <summary>
    /// Manages Unity Gaming Services integration for multiplayer:
    /// - Authentication
    /// - Session creation (with Relay)
    /// - Session joining
    /// - Voice chat (Vivox)
    /// </summary>
    public class MultiplayerSessionManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int maxPlayers = 4;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        

        // Current session information
        public MultiplayerSessionInfo CurrentSession { get; private set; }
        public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;
        public bool IsHost => CurrentSession?.role == SessionRole.Host;
        public bool IsConnected => Status == ConnectionStatus.Connected;

        // Events for other systems to listen to
        public event Action<string> OnSessionCreated;        // Fired when host creates session (passes join code)
        public event Action OnSessionJoined;                 // Fired when client joins session
        public event Action<string> OnConnectionFailed;      // Fired on connection failure
        public event Action<string> OnPlayerJoined;          // Fired when a new player joins
        public event Action<string> OnPlayerLeft;            // Fired when a player leaves

        private ISession currentUnitySession;
        private object currentVoiceChannel; 

        #region Initialization

        private async void Start()
        {
            await InitializeUnityServices();
        }

        /// <summary>
        /// Initialize Unity Gaming Services (must be called before any multiplayer operations)
        /// </summary>
        private async Task InitializeUnityServices()
        {
            try
            {
                Log("Initializing Unity Gaming Services...");

                // Initialize Unity Services
                await UnityServices.InitializeAsync();

                // Authenticate anonymously (or use your Firebase token if you want)
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    Log($"Signed in as: {AuthenticationService.Instance.PlayerId}");
                }

                Log("✅ Unity Gaming Services initialized successfully!");
            }
            catch (Exception e)
            {
                LogError($"Failed to initialize Unity Gaming Services: {e.Message}");
            }
        }

        #endregion

        #region Host Session

        /// <summary>
        /// Create a new multiplayer session as host
        /// This will:
        /// 1. Create a Unity Relay allocation
        /// 2. Generate a join code
        /// 3. Start the NetworkManager as host
        /// </summary>
        public async Task<string> CreateSessionAsync(string mysteryShareCode)
        {
            try
            {
                Log($"Creating session with mystery code: {mysteryShareCode}...");
                Status = ConnectionStatus.Connecting;

                // Create session options
                var sessionOptions = new SessionOptions
                {
                    Name = $"Mystery_{mysteryShareCode}",
                    MaxPlayers = maxPlayers,
                    // Use the mystery share code as the join code for consistency
                    // Note: Unity may generate its own code, but we'll try to use the mystery code
                };

                // Create the session through Unity's Session Service
                currentUnitySession = await MultiplayerService.Instance.CreateSessionAsync(sessionOptions);

                // Get the join code (this is the code players will use to join)
                string joinCode = currentUnitySession.Code;

                // Start Netcode as Host
                NetworkManager.Singleton.StartHost();

                // Set up session info
                CurrentSession = new MultiplayerSessionInfo
                {
                    sessionId = currentUnitySession.Id,
                    joinCode = joinCode,
                    mysteryId = mysteryShareCode,
                    role = SessionRole.Host,
                    maxPlayers = maxPlayers,
                    currentPlayerCount = 1,
                    playerIds = new System.Collections.Generic.List<string> 
                    { 
                        AuthenticationService.Instance.PlayerId 
                    }
                };

                Status = ConnectionStatus.Connected;
                Log($"✅ Session created! Join Code: {joinCode}");

                // Setup voice chat
                await SetupVoiceChatAsync();

                // Register network callbacks
                RegisterNetworkCallbacks();

                // Notify listeners
                OnSessionCreated?.Invoke(joinCode);

                return joinCode;
            }
            catch (Exception e)
            {
                Status = ConnectionStatus.Failed;
                LogError($"Failed to create session: {e.Message}");
                OnConnectionFailed?.Invoke(e.Message);
                return null;
            }
        }

        #endregion

        #region Join Session

        /// <summary>
        /// Join an existing session using a join code
        /// </summary>
        public async Task<bool> JoinSessionAsync(string joinCode)
        {
            try
            {
                Log($"Joining session with code: {joinCode}...");
                Status = ConnectionStatus.Connecting;

                // Join the session using the code
                currentUnitySession = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode);

                // Start Netcode as Client
                NetworkManager.Singleton.StartClient();

                // Set up session info
                CurrentSession = new MultiplayerSessionInfo
                {
                    sessionId = currentUnitySession.Id,
                    joinCode = joinCode,
                    role = SessionRole.Client,
                    maxPlayers = maxPlayers,
                    playerIds = new System.Collections.Generic.List<string> 
                    { 
                        AuthenticationService.Instance.PlayerId 
                    }
                };

                Status = ConnectionStatus.Connected;
                Log($"✅ Joined session successfully!");

                // Setup voice chat
                await SetupVoiceChatAsync();

                // Register network callbacks
                RegisterNetworkCallbacks();

                // Notify listeners
                OnSessionJoined?.Invoke();

                return true;
            }
            catch (Exception e)
            {
                Status = ConnectionStatus.Failed;
                LogError($"Failed to join session: {e.Message}");
                OnConnectionFailed?.Invoke(e.Message);
                return false;
            }
        }

        #endregion

        #region Voice Chat (Vivox)

        /// <summary>
        /// Setup voice chat using Vivox (integrated with Unity Gaming Services)
        /// </summary>
        // private async Task SetupVoiceChatAsync()
        // {
        //     try
        //     {
        //         Log("Setting up Vivox voice chat...");

        //         // Initialize Vivox
        //         await VivoxService.Instance.InitializeAsync();
        //         Log("Vivox initialized");

        //         // Login to Vivox (required before joining channels)
        //         await VivoxService.Instance.LoginAsync();
        //         Log("Logged into Vivox");

        //         // Join voice channel (use session join code as channel name)
        //         // This ensures all players in same session are in same voice channel
        //         string channelName = CurrentSession.joinCode;
                
        //         currentVoiceChannel = await VivoxService.Instance.JoinPositionalChannelAsync(
        //             channelName,
        //             ChatCapability.AudioOnly,
        //             new Vector3(0, 0, 0), // Initial position
        //             new Vector3(0, 0, 1), // Forward direction
        //             new Vector3(0, 1, 0)  // Up direction
        //         );

        //         Log($"✅ Joined voice channel: {channelName}");

        //         // Optional: Set up participant events
        //         currentVoiceChannel.ParticipantAdded += OnParticipantJoinedVoice;
        //         currentVoiceChannel.ParticipantRemoved += OnParticipantLeftVoice;

        //         Log("✅ Voice chat ready!");
        //     }
        //     catch (Exception e)
        //     {
        //         LogError($"Voice chat setup failed: {e.Message}");
        //         // Don't fail the session if voice fails
        //     }
        // }

        /// <summary>
        /// Setup voice chat using Vivox (integrated with Unity Gaming Services)
        /// </summary>
        private async Task SetupVoiceChatAsync()
        {
            try
            {
                Log("Setting up Vivox voice chat...");

                await VivoxService.Instance.InitializeAsync();
                await VivoxService.Instance.LoginAsync();
                
                // Use 'var' to let the compiler infer whatever type your specific package version returns!
                await VivoxService.Instance.JoinGroupChannelAsync(
                    CurrentSession.joinCode, 
                    ChatCapability.AudioOnly
                );

                Log($"✅ Voice chat ready in channel: {CurrentSession.joinCode}");
            }
            catch (Exception e)
            {
                LogError($"Voice chat setup failed: {e.Message}");
            }
        }


        private void OnParticipantJoinedVoice(VivoxParticipant participant)
        {
            Log($"🎤 {participant.DisplayName} joined voice chat");
        }

        private void OnParticipantLeftVoice(VivoxParticipant participant)
        {
            Log($"🔇 {participant.DisplayName} left voice chat");
        }

        #endregion

        #region Network Callbacks

        /// <summary>
        /// Register Netcode callbacks to track players joining/leaving
        /// </summary>
        private void RegisterNetworkCallbacks()
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        private void OnClientConnected(ulong clientId)
        {
            Log($"Client {clientId} connected");
            
            if (IsHost)
            {
                CurrentSession.currentPlayerCount++;
                OnPlayerJoined?.Invoke(clientId.ToString());
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Log($"Client {clientId} disconnected");
            
            if (IsHost)
            {
                CurrentSession.currentPlayerCount--;
                OnPlayerLeft?.Invoke(clientId.ToString());
            }
        }

        #endregion

        #region Disconnect

        /// <summary>
        /// Leave the current session and cleanup
        /// </summary>
        public async Task DisconnectAsync()
        {
            try
            {
                Log("Disconnecting from session...");

                // Logout from Vivox (this automatically leaves all channels)
                if (VivoxService.Instance != null)
                {
                    await VivoxService.Instance.LogoutAsync();
                    Log("Logged out of Vivox");
                }

                // Shutdown Netcode
                if (NetworkManager.Singleton != null)
                {
                    NetworkManager.Singleton.Shutdown();
                }

                // Leave Unity Session
                if (currentUnitySession != null)
                {
                    await currentUnitySession.LeaveAsync();
                }

                // Cleanup
                CurrentSession = null;
                Status = ConnectionStatus.Disconnected;

                Log("✅ Disconnected successfully");
            }
            catch (Exception e)
            {
                LogError($"Error during disconnect: {e.Message}");
            }
        }

        /// <summary>
        /// (Host Only) Tells Netcode to load the Game scene for all connected players.
        /// Ensure "Game" is in your File -> Build Settings!
        /// </summary>
        public void StartNetworkedGame()
        {
            if (!IsHost)
            {
                LogError("Only the host can start the game!");
                return;
            }

            if (NetworkManager.Singleton == null)
            {
                LogError("NetworkManager is null. Cannot start game.");
                return;
            }

            Log("Host is starting the networked game scene for all clients...");
            
            // Use Unity Netcode's NetworkSceneManager to load the scene for everyone simultaneously
            NetworkManager.Singleton.SceneManager.LoadScene("Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }


        #endregion

        #region Utility

        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[MultiplayerSession] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[MultiplayerSession] {message}");
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        #endregion
    }
}
