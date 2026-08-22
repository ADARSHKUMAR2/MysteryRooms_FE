using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MysteryRooms.Game.Data;

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
        
        private MysteryConfigData currentMystery;
        private HashSet<string> solvedPuzzleIds = new HashSet<string>();

        private void Awake()
        {
            // Find all puzzles in scene
            if (puzzleContainer != null)
            {
                allPuzzles = puzzleContainer.GetComponentsInChildren<BasePuzzle>(true).ToList();
            }
            else
            {
                allPuzzles = FindObjectsOfType<BasePuzzle>(true).ToList();
            }

            Debug.Log($"🧩 Found {allPuzzles.Count} puzzles in scene");
        }

        /// <summary>
        /// Configure all puzzles from mystery data
        /// </summary>
        public void ConfigurePuzzlesFromMystery(MysteryConfigData mystery)
        {
            currentMystery = mystery;
            puzzleRegistry.Clear();
            solvedPuzzleIds.Clear();

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

            Debug.Log($"✅ All puzzles configured!");
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

            // Check and unlock dependent puzzles
            UnlockDependentPuzzles(puzzleID);

            // Check if all puzzles solved
            if (solvedPuzzleIds.Count >= currentMystery.puzzles.Count)
            {
                OnAllPuzzlesSolved();
            }
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

        private void OnAllPuzzlesSolved()
        {
            Debug.Log("🎉 ALL PUZZLES SOLVED!");
            
            if (exitDoor != null)
            {
                exitDoor.UnlockDoor();
            }
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
