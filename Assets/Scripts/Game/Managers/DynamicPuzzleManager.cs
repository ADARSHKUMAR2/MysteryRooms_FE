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
    public enum DoorConnectionID
    {
        door_entrance_hall_to_burial_chamber,
        door_entrance_hall_to_west_chamber,
        door_entrance_hall_to_treasure_room,
        door_west_chamber_to_treasure_room,
        door_west_chamber_to_secret_passage,
        door_treasure_room_to_main_chamber
    }


    [System.Serializable]
    public struct DoorConnection
    {
        [Tooltip("Select the specific door connection from the map layout")]
        public DoorConnectionID connectionID; 
        public NetworkedDoor physicalDoor;
    }

    [System.Serializable]
    public class RoomSockets
    {
        public RoomType roomType;
        [Tooltip("Empty GameObjects placed in this room where puzzles can spawn")]
        public List<Transform> availableSockets;
        
        // Internal tracking
        [HideInInspector] public List<Transform> usedSockets = new List<Transform>();
    }


    public class DynamicPuzzleManager : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Transform puzzleContainer;
        [SerializeField] private InteractableDoor exitDoor;
        [SerializeField] private List<DoorConnection> mapDoors = new List<DoorConnection>();

        [Header("Runtime Data")]
        public List<BasePuzzle> allPuzzles = new List<BasePuzzle>();
        public Dictionary<string, BasePuzzle> puzzleRegistry = new Dictionary<string, BasePuzzle>();

        public List<WallInscription> allInscriptions = new List<WallInscription>();

        [Header("Spawn Sockets")]
        [Tooltip("Map each room to a list of empty spawn point Transforms in your scene")]
        [SerializeField] private List<RoomSockets> roomSpawnSockets = new List<RoomSockets>();

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

        // ADD THIS NEW TOGGLE
        [Tooltip("If true, puzzles will pick a random valid socket. If false, they will fill sockets in the exact order listed above.")]
        [SerializeField] private bool randomizeSocketPlacement = true; 

        private void Awake()
        {
            GetUserID();

            if (puzzleContainer != null) allPuzzles = puzzleContainer.GetComponentsInChildren<BasePuzzle>(true).ToList();
            else allPuzzles = FindObjectsOfType<BasePuzzle>(true).ToList();

            apiService = FindObjectOfType<MysteryAPIService>();
            netPuzzleManager = FindObjectOfType<NetworkedPuzzleManager>();
            allInscriptions = FindObjectsOfType<WallInscription>(true).ToList();
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

            // Hide all inscriptions initially
            foreach (var inscription in allInscriptions)
            {
                inscription.gameObject.SetActive(false);
            }

            foreach (var puzzleData in mystery.puzzles) ConfigurePuzzle(puzzleData);
            foreach (var puzzleData in mystery.puzzles) SetupPuzzleDependencies(puzzleData);

            // Distribute the AI-generated clues to the walls!
            if (mystery.clues != null)
            {
                DistributeClues(mystery.clues);
            }

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

                // ==========================================
        // CLUE DISTRIBUTION LOGIC
        // ==========================================
        private void DistributeClues(List<ClueConfigData> clues)
        {
            var wallClues = clues.Where(c => c.type == "inscription" || c.type == "visual").ToList();
            
            List<WallInscription> usedInscriptions = new List<WallInscription>();
            int assignedCount = 0;

            foreach (var clue in wallClues)
            {
                string targetRoomString = clue.location?.ToLower().Trim();
                WallInscription bestInscription = null;

                if (!string.IsNullOrEmpty(targetRoomString) && System.Enum.TryParse(targetRoomString, out RoomType targetRoomEnum))
                {
                    // Pass 1: Find an unused inscription with the EXACT matching RoomType enum
                    bestInscription = allInscriptions.FirstOrDefault(i => 
                        !usedInscriptions.Contains(i) && 
                        i.roomLocation == targetRoomEnum);
                }

                // Pass 2: Fallback! If no matching room was found, grab ANY unused inscription
                if (bestInscription == null)
                {
                    bestInscription = allInscriptions.FirstOrDefault(i => !usedInscriptions.Contains(i));
                    
                    if (bestInscription != null)
                    {
                        Debug.LogWarning($"⚠️ No empty Wall Inscription found matching room '{targetRoomString}' for clue '{clue.id}'. Spawning in fallback location ({bestInscription.roomLocation}).");
                    }
                }

                // If we STILL don't have one, we are completely out of inscriptions!
                if (bestInscription == null)
                {
                    Debug.LogError($"❌ Ran out of empty Wall Inscriptions in the scene! Clue '{clue.id}' was lost.");
                    continue;
                }

                // Activate it and inject the AI text
                bestInscription.gameObject.SetActive(true);
                bestInscription.SetClueText(clue.content);
                usedInscriptions.Add(bestInscription);
                assignedCount++;

                Debug.Log($"📜 Placed Clue '{clue.id}' onto a wall in '{bestInscription.roomLocation}'!");
            }
            
            Debug.Log($"[DynamicPuzzleManager] Assigned {assignedCount} clues to walls out of {wallClues.Count} available.");
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
            // 1. Find the puzzle by TYPE
            BasePuzzle puzzle = FindPuzzleByType(data.type);
            if (puzzle == null) return;

            puzzle.gameObject.name = $"[ACTIVE] {data.id}";
            puzzle.ConfigureFromBackend(data);
            puzzleRegistry[data.id] = puzzle;

            // 2. TELEPORT it to the correct room!
            if (System.Enum.TryParse(data.position, out RoomType targetRoom))
            {
                TeleportPuzzleToRoom(puzzle, targetRoom);
            }
            else
            {
                Debug.LogWarning($"⚠️ Could not parse room position '{data.position}' for puzzle {data.id}");
            }
        }

        private void TeleportPuzzleToRoom(BasePuzzle puzzle, RoomType room)
        {
            // Find the socket list for this room
            var roomConfig = roomSpawnSockets.FirstOrDefault(r => r.roomType == room);
            if (roomConfig == null || roomConfig.availableSockets == null || roomConfig.availableSockets.Count == 0)
            {
                Debug.LogWarning($"⚠️ No available sockets configured for room {room}! Puzzle {puzzle.puzzleID} will stay where it is.");
                return;
            }

            // DEFINE THE SEARCH LIST BASED ON THE RANDOMIZE TOGGLE
            List<Transform> searchList;
            if (randomizeSocketPlacement)
            {
                // Shuffle the available sockets randomly
                searchList = roomConfig.availableSockets.OrderBy(x => System.Guid.NewGuid()).ToList();
            }
            else
            {
                searchList = roomConfig.availableSockets;
            }

            // --- NEW TWO-PASS SOCKET SEARCH ---
            Transform targetSocket = null;
            string incomingType = puzzle.backendConfig.type.ToLower().Replace("_", "");

            // PASS 1: Look for an EXACT match (e.g., PressurePlate puzzle looking for a PressurePlate socket)
            foreach (var socket in searchList)
            {
                if (!roomConfig.usedSockets.Contains(socket))
                {
                    PuzzleSocketHelper helper = socket.GetComponent<PuzzleSocketHelper>();
                    
                    if (helper != null && helper.allowedPuzzleType != AllowedPuzzleType.Any)
                    {
                        string constraintType = helper.allowedPuzzleType.ToString().ToLower().Replace("_", "");
                        
                        if (incomingType.Contains(constraintType) || constraintType.Contains(incomingType))
                        {
                            targetSocket = socket;
                            roomConfig.usedSockets.Add(socket);
                            Debug.Log($"[Socket] Exact match found for {puzzle.puzzleID} on {socket.name}");
                            break;
                        }
                    }
                }
            }

            // PASS 2: If no exact match was found, look for ANY generic "Any" socket
            if (targetSocket == null)
            {
                foreach (var socket in searchList)
                {
                    if (!roomConfig.usedSockets.Contains(socket))
                    {
                        PuzzleSocketHelper helper = socket.GetComponent<PuzzleSocketHelper>();
                        
                        // We can only use this socket if it has NO constraints (AllowedPuzzleType.Any)
                        if (helper == null || helper.allowedPuzzleType == AllowedPuzzleType.Any)
                        {
                            targetSocket = socket;
                            roomConfig.usedSockets.Add(socket);
                            Debug.Log($"[Socket] Fallback 'Any' socket used for {puzzle.puzzleID} on {socket.name}");
                            break;
                        }
                    }
                }
            }
            // ----------------------------------

            if (targetSocket != null)
            {
                // Teleport the root of the puzzle to the socket!
                Transform puzzleRoot = puzzle.transform;
                
                // If the BasePuzzle script is on a child, find the highest parent
                while (puzzleRoot.parent != null && puzzleRoot.parent != puzzleContainer)
                {
                    puzzleRoot = puzzleRoot.parent;
                }

                // Snap rotation first
                puzzleRoot.rotation = targetSocket.rotation;
                
                // Snap position, taking the puzzle's specific offset into account!
                Vector3 finalPosition = targetSocket.position + targetSocket.TransformDirection(puzzle.spawnOffset);
                puzzleRoot.position = finalPosition;

                Debug.Log($"🚀 Teleported {puzzle.puzzleID} to {room} at {targetSocket.name}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Ran out of matching empty sockets in {room} for {puzzle.puzzleID} (Type: {puzzle.backendConfig.type})!");
            }
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
            if (solvedPuzzleData != null && solvedPuzzleData.unlocks != null)
            {
                foreach (string unlockString in solvedPuzzleData.unlocks)
                {
                    if (unlockString.StartsWith("door_"))
                    {
                        // Safely try to convert the string from the AI into our Enum
                        if (System.Enum.TryParse(unlockString, true, out DoorConnectionID parsedConnectionID))
                        {
                            var mapping = mapDoors.FirstOrDefault(m => m.connectionID == parsedConnectionID);
                            if (mapping.physicalDoor != null)
                            {
                                Debug.Log($"🚪 Opening door: {parsedConnectionID} (Unlocked by puzzle {solvedPuzzleId})");
                                mapping.physicalDoor.OpenDoor();
                            }
                            else
                            {
                                Debug.LogWarning($"⚠️ Puzzle {solvedPuzzleId} tried to open door '{parsedConnectionID}', but no physical door is assigned to it in DynamicPuzzleManager!");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"⚠️ Backend sent an invalid door ID: '{unlockString}'. This does not exist in the DoorConnectionID Enum!");
                        }
                    }
                }
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
