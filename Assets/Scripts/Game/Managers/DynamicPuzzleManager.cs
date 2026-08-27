using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MysteryRooms.Game.Data;
using MysteryRooms.Authentication;
using MysteryRooms.Game.Services;
using Unity.Netcode;
using Unity.Collections;
using MysteryRooms.Multiplayer.Network; // Add this

namespace MysteryRooms.Game.Managers
{
    public enum RoomType
    {
        entrance_hall, main_chamber, west_chamber, east_chamber,
        secret_passage, burial_chamber, treasure_room, antechamber
    }

    [System.Serializable]
    public struct RoomDoorMapping
    {
        public RoomType roomType;
        public NetworkedDoor doorToOpen;
    }

    public class DynamicPuzzleManager : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Transform puzzleContainer;
        [SerializeField] private InteractableDoor exitDoor;
        [SerializeField] private List<RoomDoorMapping> roomDoors = new List<RoomDoorMapping>();

        [Header("Runtime Data")]
        public List<BasePuzzle> allPuzzles = new List<BasePuzzle>();
        public Dictionary<string, BasePuzzle> puzzleRegistry = new Dictionary<string, BasePuzzle>();

        [Header("Session Tracking")]
        private MysteryAPIService apiService;
        private string currentSessionId;
        private string currentPlayerId;
        private float sessionStartTime;
        private MysteryConfigData currentMystery;
        private MysteryLoader currentLoader;
        
        // Reference to the networked manager instead of a local HashSet
        private NetworkedPuzzleManager netPuzzleManager;
        // Add a flag to track if we've successfully joined the backend session
        private bool hasJoinedBackendSession = false;

        private void Awake()
        {
            GetUserID();

            if (puzzleContainer != null) allPuzzles = puzzleContainer.GetComponentsInChildren<BasePuzzle>(true).ToList();
            else allPuzzles = FindObjectsOfType<BasePuzzle>(true).ToList();

            apiService = FindObjectOfType<MysteryAPIService>();
            netPuzzleManager = FindObjectOfType<NetworkedPuzzleManager>();
        }

        private void Start()
        {
            currentLoader = FindObjectOfType<MysteryLoader>();
            
            if (currentLoader != null)
            {
                currentLoader.OnMysteryLoaded += ConfigurePuzzlesFromMystery;

                if (currentLoader.HasMysteryLoaded())
                {
                    ConfigurePuzzlesFromMystery(currentLoader.GetCurrentMystery());
                }
            }
        }

        private void GetUserID()
        {
            if (UserSession.Instance != null) currentPlayerId = UserSession.Instance.UserId;
            else currentPlayerId = "guest_" + System.Guid.NewGuid().ToString();
        }

        public void ConfigurePuzzlesFromMystery(MysteryConfigData mystery)
        {
            currentMystery = mystery;
            puzzleRegistry.Clear();
            sessionStartTime = Time.time;

            foreach (var puzzle in allPuzzles)
            {
                puzzle.isConfiguredByBackend = false;
                puzzle.gameObject.SetActive(false); 
            }

            foreach (var puzzleData in mystery.puzzles) ConfigurePuzzle(puzzleData);
            foreach (var puzzleData in mystery.puzzles) SetupPuzzleDependencies(puzzleData);

            // Hook up local puzzle solve events
            foreach (var puzzle in puzzleRegistry.Values)
            {
                puzzle.OnPuzzleSolvedWithPlayer -= LocalPuzzleSolved; // Safety clear
                puzzle.OnPuzzleSolvedWithPlayer += LocalPuzzleSolved;
         
            }

            // Listen to the NETWORKED list of solved puzzles
            if (netPuzzleManager != null)
            {
                netPuzzleManager.solvedPuzzleIds.OnListChanged += OnNetworkedPuzzlesChanged;
            }

            StartSessionTracking();
            
            if (MysteryRooms.UI.GameUIController.Instance != null)
            {
                // MysteryRooms.UI.GameUIController.Instance.SetObjectiveTitle(mystery.objective);
                MysteryRooms.UI.GameUIController.Instance.UpdatePuzzleProgress(0, mystery.puzzles.Count);
            }

            // Listen to the NETWORKED list of solved puzzles
            if (netPuzzleManager != null)
            {
                netPuzzleManager.solvedPuzzleIds.OnListChanged += OnNetworkedPuzzlesChanged;
                
                // Subscribe server to explicitly report back to backend
                if (NetworkManager.Singleton != null)
                {
                    netPuzzleManager.OnPuzzleSolvedByPlayer -= ReportPuzzleSolvedByPlayer; // Unsubscribe just in case
                    netPuzzleManager.OnPuzzleSolvedByPlayer += ReportPuzzleSolvedByPlayer;
                }
            }
        }

        private void StartSessionTracking()
        {
            if (apiService == null || currentMystery == null) return;

            // Only the server starts a NEW session
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                StartSessionRequest request = new StartSessionRequest
                {
                    mystery_id = currentMystery.mystery_id,
                    player_ids = new List<string> { currentPlayerId },
                    max_players = 4 
                };

                StartCoroutine(apiService.StartSession(
                    request,
                    session => 
                    {
                        currentSessionId = session.session_id;
                        Debug.Log($"<color=green>Session Started: {currentSessionId}</color>");
                        
                        // Sync the session ID to clients
                        if (netPuzzleManager != null)
                        {
                            netPuzzleManager.backendSessionId.Value = new FixedString64Bytes(currentSessionId);
                        }
                    },
                    error => Debug.LogError($"Failed to start session: {error}")
                ));
            }
        }

        private void ConfigurePuzzle(PuzzleConfigData data)
        {
            BasePuzzle puzzle = FindPuzzleByType(data.type);
            if (puzzle == null) return;

            puzzle.gameObject.name = $"[ACTIVE] {data.id}";
            puzzle.ConfigureFromBackend(data);
            puzzleRegistry[data.id] = puzzle;
        }

        private BasePuzzle FindPuzzleByType(string puzzleType)
        {
            foreach (var puzzle in allPuzzles)
            {
                string unityType = puzzle.GetType().Name.ToLower();
                string backendType = puzzleType.ToLower().Replace("_", "");

                if (unityType.Contains(backendType))
                {
                    if (puzzle.isConfiguredByBackend) continue;
                    puzzle.gameObject.SetActive(true);
                    return puzzle;
                }
            }
            return null;
        }

        private void SetupPuzzleDependencies(PuzzleConfigData data)
        {
            if (!puzzleRegistry.ContainsKey(data.id)) return;
            BasePuzzle puzzle = puzzleRegistry[data.id];

            if (data.dependencies != null && data.dependencies.Count > 0) puzzle.SetLocked(true);
            else puzzle.SetLocked(false);
        }

        // When a puzzle is solved on THIS specific computer
        private void LocalPuzzleSolved(string puzzleID, ulong solverClientId, string solverFirebaseUid)
        {
            if (netPuzzleManager == null) return;

            // Only let the SERVER sync the solved state to prevent duplicate RPCs
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                // MarkPuzzleSolved handles sending the RPC, which triggers the server event,
                // which then calls ReportPuzzleSolvedByPlayer on the server.
                // WE NOW PASS THE CORRECT FIREBASE UID OF THE SOLVER!
                netPuzzleManager.MarkPuzzleSolved(puzzleID, solverFirebaseUid);
            }
        }

        // This fires automatically whenever ANY player solves a puzzle
        private void OnNetworkedPuzzlesChanged(NetworkListEvent<FixedString64Bytes> changeEvent)
        {
            string puzzleID = changeEvent.Value.ToString();
            
            UnlockDependentPuzzles(puzzleID);
            CheckDoorUnlocks(puzzleID);

            // Update UI
            if (MysteryRooms.UI.GameUIController.Instance != null)
            {
                int totalPuzzles = currentMystery.puzzles.Count;
                MysteryRooms.UI.GameUIController.Instance.UpdatePuzzleProgress(netPuzzleManager.solvedPuzzleIds.Count, totalPuzzles);
                MysteryRooms.UI.GameUIController.Instance.ShowRecentAction($"Solved: {puzzleID}");
                
            }

            if (netPuzzleManager.solvedPuzzleIds.Count >= currentMystery.puzzles.Count)
            {
                OnAllPuzzlesSolved();
            }
        }

        private void CheckDoorUnlocks(string solvedPuzzleId)
        {
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer) return;

            var solvedPuzzleData = currentMystery.puzzles.FirstOrDefault(p => p.id == solvedPuzzleId);
            if (solvedPuzzleData != null && System.Enum.TryParse(solvedPuzzleData.position, out RoomType parsedRoomType))
            {
                var mapping = roomDoors.FirstOrDefault(m => m.roomType == parsedRoomType);
                if (mapping.doorToOpen != null) mapping.doorToOpen.OpenDoor();
            }
        }

        // This is called by the Server when a client (or host) solves a puzzle
        private void ReportPuzzleSolvedByPlayer(string puzzleID, string solverFirebaseUid)
        {
            Debug.Log($"📢 Received puzzle solved notification: {puzzleID} (solver: {solverFirebaseUid})");
            if (apiService == null || string.IsNullOrEmpty(currentSessionId)) return;
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer) return;

            UpdateSessionRequest request = new UpdateSessionRequest
            {
                session_id = currentSessionId,
                puzzle_solved = puzzleID,
                player_id = solverFirebaseUid, // Uses the actual solver's UID!
                time_elapsed_seconds = (int)(Time.time - sessionStartTime)
            };
            
            StartCoroutine(apiService.UpdateSession(request, s => {}, e => {}));
        }


        private void UnlockDependentPuzzles(string solvedPuzzleId)
        {
            var solvedPuzzleData = currentMystery.puzzles.FirstOrDefault(p => p.id == solvedPuzzleId);
            if (solvedPuzzleData != null && solvedPuzzleData.unlocks != null)
            {
                foreach (string unlockId in solvedPuzzleData.unlocks)
                {
                    if (puzzleRegistry.ContainsKey(unlockId)) puzzleRegistry[unlockId].SetLocked(false);
                }
            }

            foreach (var puzzleData in currentMystery.puzzles)
            {
                if (puzzleData.dependencies != null && puzzleData.dependencies.Contains(solvedPuzzleId))
                {
                    // Check the NETWORKED list
                    bool allDependenciesSolved = puzzleData.dependencies.All(dep => 
                        netPuzzleManager.solvedPuzzleIds.Contains(new FixedString64Bytes(dep)));
                    
                    if (allDependenciesSolved && puzzleRegistry.ContainsKey(puzzleData.id))
                    {
                        puzzleRegistry[puzzleData.id].SetLocked(false);
                    }
                }
            }
        }

        private void Update()
        {
            // If we are a client, wait until the host syncs the backendSessionId, then join it once
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer && !hasJoinedBackendSession)
            {
                if (netPuzzleManager != null && !string.IsNullOrEmpty(netPuzzleManager.backendSessionId.Value.ToString()))
                {
                    currentSessionId = netPuzzleManager.backendSessionId.Value.ToString();
                    hasJoinedBackendSession = true; // Prevent multiple joins
                    
                    StartCoroutine(apiService.JoinSession(
                        currentSessionId,
                        currentPlayerId,
                        session => Debug.Log($"<color=green>Client joined session successfully: {currentSessionId}</color>"),
                        error => Debug.LogError($"Client failed to join session: {error}")
                    ));
                }
            }
        }


        public void MarkPuzzleAsSolved(string puzzleId) { }
        public int GetTotalPuzzleCount() => currentMystery?.puzzles?.Count ?? 0;

        private void OnAllPuzzlesSolved()
        {
            if (exitDoor != null) exitDoor.UnlockDoor();
            CompleteSessionTracking("completed");
        }

        private void CompleteSessionTracking(string status)
        {
            if (apiService == null || string.IsNullOrEmpty(currentSessionId)) return;
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer) return;

            CompleteSessionRequest request = new CompleteSessionRequest
            {
                session_id = currentSessionId, 
                status = status, 
                difficulty_rating = currentMystery.difficulty
            };
            
            StartCoroutine(apiService.CompleteSession(
                request, 
                s => Debug.Log("<color=green>Server successfully closed the session</color>"), 
                e => Debug.LogError("Failed to close session: " + e)
            ));
        }


        private void OnDestroy()
        {
            if (currentLoader != null) currentLoader.OnMysteryLoaded -= ConfigurePuzzlesFromMystery;
            if (netPuzzleManager != null) netPuzzleManager.solvedPuzzleIds.OnListChanged -= OnNetworkedPuzzlesChanged;

            foreach (var puzzle in puzzleRegistry.Values)
            {
                if (puzzle != null) puzzle.OnPuzzleSolvedWithPlayer -= LocalPuzzleSolved;
            }
        }
    }
}
