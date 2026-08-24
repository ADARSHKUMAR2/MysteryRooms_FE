using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MysteryRooms.Game.Data;
using MysteryRooms.Authentication;  
using MysteryRooms.Game.Services;

namespace MysteryRooms.Game.Managers
{
    public class DynamicPuzzleManager : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Transform puzzleContainer;
        [SerializeField] private InteractableDoor exitDoor;

        [Header("Runtime Data")]
        public List<BasePuzzle> allPuzzles = new List<BasePuzzle>();
        public Dictionary<string, BasePuzzle> puzzleRegistry = new Dictionary<string, BasePuzzle>();

        [Header("Session Tracking")]
        private MysteryAPIService apiService;
        private string currentSessionId;
        private string currentPlayerId;         private float sessionStartTime;
        
        private MysteryConfigData currentMystery;
        private HashSet<string> solvedPuzzleIds = new HashSet<string>();

        private void Awake()
        {
            GetUserID();

            // Find all puzzles in scene
            if (puzzleContainer != null)
            {
                allPuzzles = puzzleContainer.GetComponentsInChildren<BasePuzzle>(true).ToList();
            }
            else
            {
                allPuzzles = FindObjectsOfType<BasePuzzle>(true).ToList();
            }

            apiService = FindObjectOfType<MysteryAPIService>();
            if (apiService == null)
            {
                Debug.LogWarning("MysteryAPIService not found! Session tracking disabled.");
            }

            Debug.Log($"🧩 Found {allPuzzles.Count} puzzles in scene");
        }

        private void GetUserID()
        {
            // Get real user ID from UserSession
            if (UserSession.Instance != null)
            {
                currentPlayerId = UserSession.Instance.UserId;
                Debug.Log($"🎮 Player identified: {UserSession.Instance.Username} ({currentPlayerId})");
            }
            else
            {
                currentPlayerId = "guest_" + System.Guid.NewGuid().ToString();
                Debug.LogWarning("⚠️ No UserSession found. Using guest ID.");
            }
        }

        /// <summary>
        /// Configure all puzzles from mystery data
        /// </summary>
        public void ConfigurePuzzlesFromMystery(MysteryConfigData mystery)
        {
            currentMystery = mystery;
            puzzleRegistry.Clear();
            solvedPuzzleIds.Clear();
            sessionStartTime = Time.time;

            Debug.Log($"⚙️ Configuring {mystery.puzzles.Count} puzzles...");

            // First pass: Configure individual puzzles
            foreach (var puzzleData in mystery.puzzles)
            {
                ConfigurePuzzle(puzzleData);
            }

            // Second pass: Set up dependencies
            foreach (var puzzleData in mystery.puzzles)
            {
                SetupPuzzleDependencies(puzzleData);
            }

            // Subscribe to puzzle solved events
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
                OnSessionStarted,
                (error) => Debug.LogError($"Failed to start session: {error}")
            ));
        }

        private void OnSessionStarted(GameSessionData session)
        {
            currentSessionId = session.session_id;
            Debug.Log($"📊 Session tracking started: {currentSessionId}");
        }

        private void ConfigurePuzzle(PuzzleConfigData data)
        {
            // Find a puzzle of matching type in scene
            BasePuzzle puzzle = FindPuzzleByType(data.type);
            
            if (puzzle == null)
            {
                Debug.LogWarning($"⚠️ No puzzle found for type: {data.type}");
                return;
            }

            // Configure the puzzle
            puzzle.ConfigureFromBackend(data);
            puzzleRegistry[data.id] = puzzle;
            
            Debug.Log($"✓ Configured {data.type} as {data.id}");
        }

        private BasePuzzle FindPuzzleByType(string puzzleType)
        {
            // Map backend puzzle types to Unity puzzle classes
            foreach (var puzzle in allPuzzles)
            {
                if (puzzle.gameObject.activeSelf) continue; // Skip already configured
                
                string unityType = puzzle.GetType().Name.ToLower();
                string backendType = puzzleType.ToLower().Replace("_", "");

                if (unityType.Contains(backendType))
                {
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

            // Lock puzzle if it has unsolved dependencies
            if (data.dependencies != null && data.dependencies.Count > 0)
            {
                bool allDependenciesSolved = data.dependencies.All(dep => solvedPuzzleIds.Contains(dep));
                
                if (!allDependenciesSolved)
                {
                    puzzle.SetLocked(true);
                    Debug.Log($"🔒 Locked {data.id} (waiting on dependencies)");
                }
            }
        }

        private void OnPuzzleSolved(string puzzleID)
        {
            solvedPuzzleIds.Add(puzzleID);
            Debug.Log($"🎯 Puzzle solved: {puzzleID} ({solvedPuzzleIds.Count}/{currentMystery.puzzles.Count})");

            // Report to backend
            ReportPuzzleSolved(puzzleID);

            // Check and unlock dependent puzzles
            UnlockDependentPuzzles(puzzleID);

            // Check if all puzzles solved
            if (solvedPuzzleIds.Count >= currentMystery.puzzles.Count)
            {
                OnAllPuzzlesSolved();
            }
        }

        private void ReportPuzzleSolved(string puzzleID)
        {
            if (apiService == null || string.IsNullOrEmpty(currentSessionId)) return;

            UpdateSessionRequest request = new UpdateSessionRequest
            {
                session_id = currentSessionId,
                puzzle_solved = puzzleID,
                player_id = currentPlayerId,
                time_elapsed_seconds = (int)(Time.time - sessionStartTime)
            };

            StartCoroutine(apiService.UpdateSession(
                request,
                (session) => Debug.Log($"✅ Progress saved: {puzzleID}"),
                (error) => Debug.LogWarning($"Failed to update session: {error}")
            ));
        }

        private void UnlockDependentPuzzles(string solvedPuzzleId)
        {
            foreach (var puzzleData in currentMystery.puzzles)
            {
                if (puzzleData.dependencies != null && puzzleData.dependencies.Contains(solvedPuzzleId))
                {
                    // Check if ALL dependencies are now solved
                    bool allDependenciesSolved = puzzleData.dependencies.All(dep => solvedPuzzleIds.Contains(dep));
                    
                    if (allDependenciesSolved && puzzleRegistry.ContainsKey(puzzleData.id))
                    {
                        BasePuzzle puzzle = puzzleRegistry[puzzleData.id];
                        puzzle.SetLocked(false);
                        Debug.Log($"🔓 Unlocked {puzzleData.id}");
                    }
                }
            }
        }

        /// <summary>
        /// Mark a puzzle as solved (called by NetworkedPuzzleManager)
        /// </summary>
        public void MarkPuzzleAsSolved(string puzzleId)
        {
            // Find the puzzle in your internal list and mark it as solved
            // This ensures the local game state matches the network state
            
            Debug.Log($"Marking puzzle {puzzleId} as solved locally");
            
            // Your implementation here - update puzzle state, unlock doors, etc.
            // Example:
            // var puzzle = puzzles.Find(p => p.id == puzzleId);
            // if (puzzle != null) puzzle.isSolved = true;
        }

        /// <summary>
        /// Get total puzzle count
        /// </summary>
        public int GetTotalPuzzleCount()
        {
            // Return the number of puzzles in the current mystery
            return currentMystery?.puzzles?.Count ?? 0;
        }

        private void OnAllPuzzlesSolved()
        {
            Debug.Log("🎉 ALL PUZZLES SOLVED!");
            
            if (exitDoor != null)
            {
                exitDoor.UnlockDoor();
            }

            CompleteSessionTracking("completed");
        }

        private void CompleteSessionTracking(string status)
        {
            if (apiService == null || string.IsNullOrEmpty(currentSessionId)) return;

            CompleteSessionRequest request = new CompleteSessionRequest
            {
                session_id = currentSessionId,
                status = status,
                difficulty_rating = currentMystery.difficulty
            };

            StartCoroutine(apiService.CompleteSession(
                request,
                (session) => Debug.Log($"🏁 Session completed and saved to database!"),
                (error) => Debug.LogWarning($"Failed to complete session: {error}")
            ));
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            foreach (var puzzle in puzzleRegistry.Values)
            {
                if (puzzle != null)
                {
                    puzzle.OnPuzzleSolved -= OnPuzzleSolved;
                }
            }
        }
    }
}
