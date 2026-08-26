using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MysteryRooms.Game.Data;
using MysteryRooms.Authentication;
using MysteryRooms.Game.Services;
using Unity.Netcode; 
namespace MysteryRooms.Game.Managers
{
    public enum RoomType
    {
        entrance_hall,
        main_chamber,
        west_chamber,
        east_chamber,
        secret_passage,
        burial_chamber,
        treasure_room,
        antechamber
    }
    [System.Serializable]
    public struct RoomDoorMapping
    {
        [Tooltip("Exact room name from JSON (e.g., 'entrance_hall')")]
        public RoomType roomType;
        [Tooltip("The door that opens when a puzzle in this room is solved")]
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
        private HashSet<string> solvedPuzzleIds = new HashSet<string>();
        
        private MysteryLoader currentLoader;

        private void Awake()
        {
            GetUserID();

            if (puzzleContainer != null)
            {
                allPuzzles = puzzleContainer.GetComponentsInChildren<BasePuzzle>(true).ToList();
            }
            else
            {
                allPuzzles = FindObjectsOfType<BasePuzzle>(true).ToList();
            }

            apiService = FindObjectOfType<MysteryAPIService>();

            Debug.Log($"🧩 Game Scene: Found {allPuzzles.Count} puzzles.");
        }

        private void Start()
        {
            currentLoader = FindObjectOfType<MysteryLoader>();
            
            if (currentLoader != null)
            {
                // Subscribe to future mystery loads (Single-player flow)
                currentLoader.OnMysteryLoaded += ConfigurePuzzlesFromMystery;

                // If it ALREADY has a mystery, load it now (Multiplayer flow)
                if (currentLoader.HasMysteryLoaded())
                {
                    Debug.Log("🧩 Game Scene Started! Pulling existing mystery...");
                    ConfigurePuzzlesFromMystery(currentLoader.GetCurrentMystery());
                }
            }
        }

        private void GetUserID()
        {
            if (UserSession.Instance != null)
            {
                currentPlayerId = UserSession.Instance.UserId;
            }
            else
            {
                currentPlayerId = "guest_" + System.Guid.NewGuid().ToString();
            }
        }

        public void ConfigurePuzzlesFromMystery(MysteryConfigData mystery)
        {
            currentMystery = mystery;
            puzzleRegistry.Clear();
            solvedPuzzleIds.Clear();
            sessionStartTime = Time.time;

            Debug.Log($"⚙️ Configuring {mystery.puzzles.Count} puzzles...");

            foreach (var puzzle in allPuzzles)
            {
                puzzle.isConfiguredByBackend = false;
                puzzle.gameObject.SetActive(false); // Turn off everything first!
            }

            foreach (var puzzleData in mystery.puzzles)
            {
                ConfigurePuzzle(puzzleData);
            }

            foreach (var puzzleData in mystery.puzzles)
            {
                SetupPuzzleDependencies(puzzleData);
            }

            foreach (var puzzle in puzzleRegistry.Values)
            {
                puzzle.OnPuzzleSolved += OnPuzzleSolved;
            }

            StartSessionTracking();
            Debug.Log($"✅ All puzzles configured!");
        }

        private void StartSessionTracking()
        {
            if (apiService == null || currentMystery == null) return;

            StartSessionRequest request = new StartSessionRequest
            {
                mystery_id = currentMystery.mystery_id,
                player_ids = new List<string> { currentPlayerId },
                max_players = 1
            };

            StartCoroutine(apiService.StartSession(
                request,
                (session) => currentSessionId = session.session_id,
                (error) => Debug.LogError($"Failed to start session: {error}")
            ));
        }

        private void ConfigurePuzzle(PuzzleConfigData data)
        {
            BasePuzzle puzzle = FindPuzzleByType(data.type);

            if (puzzle == null)
            {
                Debug.LogWarning($"⚠️ No puzzle found in scene for type: {data.type}");
                return;
            }

            puzzle.gameObject.name = $"[ACTIVE] {data.id}";
            puzzle.ConfigureFromBackend(data);
            puzzleRegistry[data.id] = puzzle;
            Debug.Log($"✓ Configured {data.type} as {data.id}");
        }

        private BasePuzzle FindPuzzleByType(string puzzleType)
        {
            foreach (var puzzle in allPuzzles)
            {
                // if (puzzle.gameObject.activeSelf) continue;

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

            // If there are dependencies, lock it.
            if (data.dependencies != null && data.dependencies.Count > 0)
            {
                puzzle.SetLocked(true);
            }
            else
            {
                // NO dependencies! This is a starting puzzle. Unlock it immediately!
                puzzle.SetLocked(false);
            }
        }

        private void OnPuzzleSolved(string puzzleID)
        {
            if (solvedPuzzleIds.Contains(puzzleID)) return;

            solvedPuzzleIds.Add(puzzleID);
            ReportPuzzleSolved(puzzleID);
            UnlockDependentPuzzles(puzzleID);

            // Check if this puzzle unlocks a door ---
            CheckDoorUnlocks(puzzleID);

            if (solvedPuzzleIds.Count >= currentMystery.puzzles.Count)
            {
                OnAllPuzzlesSolved();
            }
        }

        // --- Method to open doors based on JSON data ---
        private void CheckDoorUnlocks(string solvedPuzzleId)
        {
            // Only the server has authority to open network doors
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer) return;

            // Find the puzzle data from our mystery config
            var solvedPuzzleData = currentMystery.puzzles.FirstOrDefault(p => p.id == solvedPuzzleId);
            
            if (solvedPuzzleData != null)
            {
                string jsonRoomString = solvedPuzzleData.position;
                
                // Convert JSON string to our Enum safely
                if (System.Enum.TryParse(jsonRoomString, out RoomType parsedRoomType))
                {
                    // Find the door mapped to this room Enum in the Unity Inspector
                    var mapping = roomDoors.FirstOrDefault(m => m.roomType == parsedRoomType);
                    
                    if (mapping.doorToOpen != null)
                    {
                        Debug.Log($"🚪 Puzzle in '{parsedRoomType}' solved! Opening mapped door!");
                        mapping.doorToOpen.OpenDoor();
                    }
                    else
                    {
                        Debug.Log($"⚠️ Puzzle in '{parsedRoomType}' solved, but no door mapping was found in DynamicPuzzleManager.");
                    }
                }
                else
                {
                    Debug.LogError($"❌ Unknown room type from backend JSON: {jsonRoomString}");
                }
            }
            
            // Optional: You can expand this logic to open specific doors based on the 'unlocks' array in your JSON
        }

        private void ReportPuzzleSolved(string puzzleID)
        {
            if (apiService == null || string.IsNullOrEmpty(currentSessionId)) return;

            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer) return;

            UpdateSessionRequest request = new UpdateSessionRequest
            {
                session_id = currentSessionId,
                puzzle_solved = puzzleID,
                player_id = currentPlayerId,
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
                if (puzzleRegistry.ContainsKey(unlockId))
                {
                    Debug.Log($"🔓 {solvedPuzzleId} explicitly unlocks {unlockId}!");
                    puzzleRegistry[unlockId].SetLocked(false);
                }
            }
        }

        // 2. Second, check the standard "dependencies" array (Original Logic)
        foreach (var puzzleData in currentMystery.puzzles)
        {
            if (puzzleData.dependencies != null && puzzleData.dependencies.Contains(solvedPuzzleId))
            {
                // Make sure ALL dependencies for this puzzle are met
                bool allDependenciesSolved = puzzleData.dependencies.All(dep => solvedPuzzleIds.Contains(dep));
                
                if (allDependenciesSolved && puzzleRegistry.ContainsKey(puzzleData.id))
                {
                    Debug.Log($"🔓 All dependencies met for {puzzleData.id}. Unlocking!");
                    puzzleRegistry[puzzleData.id].SetLocked(false);
                }
            }
        }
        }

        public void MarkPuzzleAsSolved(string puzzleId) { }

        public int GetTotalPuzzleCount() => currentMystery?.puzzles?.Count ?? 0;

        private void OnAllPuzzlesSolved()
        {
            Debug.Log("🎉 ALL PUZZLES SOLVED!");
            if (exitDoor != null) exitDoor.UnlockDoor();
            CompleteSessionTracking("completed");
        }

        private void CompleteSessionTracking(string status)
        {
            if (apiService == null || string.IsNullOrEmpty(currentSessionId)) return;

            // NEW: Only let the Server/Host tell the backend the session is done
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer) return;

            CompleteSessionRequest request = new CompleteSessionRequest
            {
                session_id = currentSessionId,
                status = status,
                difficulty_rating = currentMystery.difficulty
            };

            StartCoroutine(apiService.CompleteSession(request, s => {}, e => {}));
        }

        private void OnDestroy()
        {
            if (currentLoader != null)
            {
                currentLoader.OnMysteryLoaded -= ConfigurePuzzlesFromMystery;
            }

            foreach (var puzzle in puzzleRegistry.Values)
            {
                if (puzzle != null) puzzle.OnPuzzleSolved -= OnPuzzleSolved;
            }
        }
    }
}
