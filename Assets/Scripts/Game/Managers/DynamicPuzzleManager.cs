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

            puzzle.ConfigureFromBackend(data);
            puzzleRegistry[data.id] = puzzle;
            Debug.Log($"✓ Configured {data.type} as {data.id}");
        }

        private BasePuzzle FindPuzzleByType(string puzzleType)
        {
            foreach (var puzzle in allPuzzles)
            {
                if (puzzle.gameObject.activeSelf) continue;

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
            if (data.dependencies == null || data.dependencies.Count == 0) return;
            if (!puzzleRegistry.ContainsKey(data.id)) return;

            BasePuzzle puzzle = puzzleRegistry[data.id];
            puzzle.SetLocked(true);
        }

        private void OnPuzzleSolved(string puzzleID)
        {
            if (solvedPuzzleIds.Contains(puzzleID)) return;

            solvedPuzzleIds.Add(puzzleID);
            ReportPuzzleSolved(puzzleID);
            UnlockDependentPuzzles(puzzleID);

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

            StartCoroutine(apiService.UpdateSession(request, s => {}, e => {}));
        }

        private void UnlockDependentPuzzles(string solvedPuzzleId)
        {
            foreach (var puzzleData in currentMystery.puzzles)
            {
                if (puzzleData.dependencies != null && puzzleData.dependencies.Contains(solvedPuzzleId))
                {
                    bool allDependenciesSolved = puzzleData.dependencies.All(dep => solvedPuzzleIds.Contains(dep));
                    if (allDependenciesSolved && puzzleRegistry.ContainsKey(puzzleData.id))
                    {
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
