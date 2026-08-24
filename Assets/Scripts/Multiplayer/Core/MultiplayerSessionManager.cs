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

        private async void Start()
        {
            await InitializeUnityServices();
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
                OnPlayerLeft?.Invoke($"Player_{clientId}");
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
