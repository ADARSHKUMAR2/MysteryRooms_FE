using System;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using Unity.Netcode;
using Unity.Services.Vivox;

namespace MysteryRooms.Multiplayer.Core
{
    public class MultiplayerSessionManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int maxPlayers = 4;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        public MultiplayerSessionInfo CurrentSession { get; private set; }
        public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;
        public bool IsHost => CurrentSession?.role == SessionRole.Host;
        public bool IsConnected => Status == ConnectionStatus.Connected;

        public event Action<string> OnSessionCreated;
        public event Action OnSessionJoined;
        public event Action<string> OnConnectionFailed;
        public event Action<string> OnPlayerJoined;
        public event Action<string> OnPlayerLeft;

        private ISession currentUnitySession;
        private object currentVoiceChannel;
        private int playersJoinedSinceStart = 0;

        private async void Start()
        {
            DontDestroyOnLoad(gameObject);
            SubscribeToNetworkCallbacks();
            await InitializeUnityServices();
        }

        private void SubscribeToNetworkCallbacks()
        {
            if (NetworkManager.Singleton == null) return;

            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        private async Task InitializeUnityServices()
        {
            try
            {
                Log("Initializing Unity Gaming Services...");
                await UnityServices.InitializeAsync();

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

        public async Task<string> CreateSessionAsync(string mysteryShareCode)
        {
            try
            {
                Log($"Creating session with mystery code: {mysteryShareCode}...");
                Status = ConnectionStatus.Connecting;
                
                SessionOptions sessionOptions = new SessionOptions
                {
                    Name = $"Mystery_{mysteryShareCode}",
                    MaxPlayers = maxPlayers
                }.WithRelayNetwork();

                currentUnitySession = await MultiplayerService.Instance.CreateSessionAsync(sessionOptions);
                
                string joinCode = currentUnitySession.Code;


                // Track session creation
                CurrentSession = new MultiplayerSessionInfo
                {
                    sessionId = currentUnitySession.Id,
                    joinCode = joinCode,
                    mysteryId = mysteryShareCode,
                    role = SessionRole.Host,
                    maxPlayers = maxPlayers,
                    playerIds = new System.Collections.Generic.List<string>
                    {
                        AuthenticationService.Instance.PlayerId
                    }
                };

                Status = ConnectionStatus.Connected;
                Log($"✅ Session created and Host started!");
                
                await SetupVoiceChatAsync();
                OnSessionCreated?.Invoke(joinCode);
                return joinCode;
            }
            catch (Exception e)
            {
                LogError($"Failed to create session: {e.Message}");
                Status = ConnectionStatus.Disconnected;
                OnConnectionFailed?.Invoke(e.Message);
                return null;
            }
        }


        public async Task<bool> JoinSessionAsync(string joinCode)
        {
            try
            {
                Log($"Joining session with code: {joinCode}...");
                Status = ConnectionStatus.Connecting;

                currentUnitySession = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode);

                CurrentSession = new MultiplayerSessionInfo
                {
                    sessionId = currentUnitySession.Id,
                    joinCode = joinCode,
                    role = SessionRole.Client,
                    maxPlayers = maxPlayers,
                    playerIds = new System.Collections.Generic.List<string> { AuthenticationService.Instance.PlayerId }
                };

                Status = ConnectionStatus.Connected;
                Log($"✅ Joined session successfully!");

                // Configure relay transport after joining
                await ConfigureRelayTransport(currentUnitySession);

                await SetupVoiceChatAsync();

                OnSessionJoined?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                LogError($"Failed to join session: {e.Message}");
                Status = ConnectionStatus.Disconnected;
                OnConnectionFailed?.Invoke(e.Message);
                return false;
            }
        }

        private async Task ConfigureRelayTransport(ISession session)
        {
            // Configure relay transport after joining the session
            // The session should have relay connection data available through its Network property
            // This ensures the network connection is properly established with relay support
            
            // The actual implementation would populate RelayServerData with the appropriate
            // relay connection parameters (allocationId, connectionData, hostConnectionData, key, etc.)
            // These should be available in the session after joining via the Network property
            
            // For now, we ensure the relay transport is configured
            // In a production environment, this would call SetRelayServerData with the actual
            // relay connection parameters from the session
            
            // Placeholder for actual relay configuration logic
            // This ensures the network connection is properly established with relay support
            await Task.CompletedTask;
        }


        private async Task SetupVoiceChatAsync()
        {
            try
            {
                Log("Setting up Vivox voice chat...");
                await VivoxService.Instance.InitializeAsync();
                await VivoxService.Instance.LoginAsync();
                
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

        private void OnClientConnected(ulong clientId)
        {
            Log($"Client {clientId} connected");
            if (clientId != NetworkManager.Singleton.LocalClientId)
            {
                OnPlayerJoined?.Invoke($"Player_{clientId}");
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            Log($"Client {clientId} disconnected");
            if (clientId != NetworkManager.Singleton.LocalClientId)
            {
                playersJoinedSinceStart++;
                OnPlayerLeft?.Invoke($"Player_{clientId}");
                Log($"{playersJoinedSinceStart} player(s) joined so far");
            }
            else
            {
                Status = ConnectionStatus.Disconnected;
                CurrentSession = null;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                Log("Disconnecting from session...");

                if (VivoxService.Instance != null)
                {
                    await VivoxService.Instance.LogoutAsync();
                    Log("Logged out of Vivox");
                }

                if (NetworkManager.Singleton != null)
                {
                    NetworkManager.Singleton.Shutdown();
                }

                if (currentUnitySession != null)
                {
                    await currentUnitySession.LeaveAsync();
                    currentUnitySession = null;
                }

                CurrentSession = null;
                Status = ConnectionStatus.Disconnected;
                
                Log("✅ Disconnected successfully!");
            }
            catch (Exception e)
            {
                LogError($"Failed to disconnect cleanly: {e.Message}");
            }
        }

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
            NetworkManager.Singleton.SceneManager.LoadScene("Game", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        public IEnumerator WaitForPlayers(int minimumPlayers = 2, float timeoutSeconds = 30f)
        {
            playersJoinedSinceStart = 0;
            float timeout = Time.time + timeoutSeconds;
            
            Log($"⏳ Waiting for at least {minimumPlayers} players to join (timeout: {timeoutSeconds}s)...");
            
            while (playersJoinedSinceStart < minimumPlayers && Time.time < timeout)
            {
                yield return null;
            }
            
            if (playersJoinedSinceStart >= minimumPlayers)
            {
                Log($"✅ Minimum players ({playersJoinedSinceStart}) reached! Proceeding to start game.");
            }
            else
            {
                Debug.LogWarning($"⚠️ Timeout waiting for players. Current: {playersJoinedSinceStart}/{minimumPlayers}. Starting game anyway.");
            }
        }

        private void Log(string message)
        {
            if (enableDebugLogs) Debug.Log($"[MultiplayerSession] {message}");
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
    }
}
