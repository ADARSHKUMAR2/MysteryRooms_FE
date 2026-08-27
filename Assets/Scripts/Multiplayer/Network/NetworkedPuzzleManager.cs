using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using MysteryRooms.Game.Managers;

namespace MysteryRooms.Multiplayer.Network
{
    /// <summary>
    /// Manages synchronized puzzle state across the network.
    /// Ensures all players see the same puzzle progress.
    /// </summary>
    public class NetworkedPuzzleManager : NetworkBehaviour
    {
        private static string pendingBackendShareCode;

        [Header("References")]
        [SerializeField] private DynamicPuzzleManager localPuzzleManager;

        // Network list of solved puzzle IDs (automatically synced)
        public NetworkList<FixedString64Bytes> solvedPuzzleIds;

        // Network variable to share the backend mystery code with clients
        public NetworkVariable<FixedString64Bytes> backendShareCode = new NetworkVariable<FixedString64Bytes>();

        // Add NetworkVariable to sync the session ID from Backend so clients know which one to join
        public NetworkVariable<FixedString64Bytes> backendSessionId = new NetworkVariable<FixedString64Bytes>();
        
        // Event that only runs on the Server side passing the Puzzle ID and the Solver's UID
        public System.Action<string, string> OnPuzzleSolvedByPlayer;

        

        public static void SetPendingBackendShareCode(string code)
        {
            pendingBackendShareCode = code;
        }


        // Network variable for victory state
        private NetworkVariable<bool> allPuzzlesSolved = new NetworkVariable<bool>(false);

        // Events
        // Now carries both puzzleId AND the solver's Firebase UID
        public System.Action<string, string> OnPuzzleSolvedNetwork;

        public System.Action OnAllPuzzlesSolved;

        private void Awake()
        {
            // Initialize the network list
            solvedPuzzleIds = new NetworkList<FixedString64Bytes>();

            if (localPuzzleManager == null)
            {
                localPuzzleManager = FindObjectOfType<DynamicPuzzleManager>();
            }
        }

        public void SetBackendShareCode(string code)
        {
            if (!IsServer)
            {
                Debug.LogWarning("Cannot set backend mystery code from a client.");
                return;
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                Debug.LogError("Cannot set an empty backend mystery code.");
                return;
            }

            backendShareCode.Value = new FixedString64Bytes(code);
            Debug.Log($"Backend mystery code synchronized: {code}");
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Subscribe to list changes
            solvedPuzzleIds.OnListChanged += OnSolvedPuzzleListChanged;
            allPuzzlesSolved.OnValueChanged += OnVictoryStateChanged;

            Debug.Log("NetworkedPuzzleManager spawned");

            if (IsServer && !string.IsNullOrWhiteSpace(pendingBackendShareCode))
            {
                SetBackendShareCode(pendingBackendShareCode);
                pendingBackendShareCode = null;
            }
        }

        #region Puzzle Solving

        /// <summary>
        /// Call this when a puzzle is solved locally
        /// </summary>
        public void MarkPuzzleSolved(string puzzleId, string solverFirebaseUid = null)
        {
            if (string.IsNullOrEmpty(puzzleId))
            {
                Debug.LogError("Cannot mark puzzle solved - invalid ID");
                return;
            }

            Debug.Log($"🧩 Marking puzzle as solved: {puzzleId} by {solverFirebaseUid ?? "unknown"}");

            // Send to server — include the solver's backend identity
            MarkPuzzleSolvedServerRpc(puzzleId, solverFirebaseUid ?? "");
        }

        /// <summary>
        /// [ServerRpc] Client notifies server that a puzzle was solved
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void MarkPuzzleSolvedServerRpc(string puzzleId, string solverFirebaseUid, ServerRpcParams rpcParams = default)
        {
            FixedString64Bytes fixedId = new FixedString64Bytes(puzzleId);

            // Check if already solved
            if (solvedPuzzleIds.Contains(fixedId))
            {
                Debug.LogWarning($"Puzzle {puzzleId} already solved!");
                return;
            }

            // Use the NGO client ID for the in-game scoreboard
            ulong solverId = rpcParams.Receive.SenderClientId;
            if (NetworkedScoreboard.Instance != null)
            {
                NetworkedScoreboard.Instance.IncrementPlayerScoreServerRpc(solverId);
            }

            // Add to solved list (this automatically syncs to all clients)
            solvedPuzzleIds.Add(fixedId);

            Debug.Log($"✅ Server confirmed puzzle solved: {puzzleId} by Client {solverId} (Firebase: {solverFirebaseUid})");

            // Broadcast to all clients — include the solver's Firebase UID for backend reporting
            NotifyPuzzleSolvedClientRpc(puzzleId, solverFirebaseUid);

            // Trigger server event to notify the DynamicPuzzleManager to update Backend
            OnPuzzleSolvedByPlayer?.Invoke(puzzleId, solverFirebaseUid);

            // Check if all puzzles are solved
            CheckVictoryCondition();
        }

        /// <summary>
        /// [ClientRpc] Server notifies all clients that a puzzle was solved
        /// </summary>
        [ClientRpc]
        private void NotifyPuzzleSolvedClientRpc(string puzzleId, string solverFirebaseUid)
        {
            Debug.Log($"📢 Received puzzle solved notification: {puzzleId} (solver: {solverFirebaseUid})");

            // Update local puzzle manager
            if (localPuzzleManager != null)
            {
                localPuzzleManager.MarkPuzzleAsSolved(puzzleId);
            }

            // Trigger event for other systems — now includes the solver's Firebase UID
            OnPuzzleSolvedNetwork?.Invoke(puzzleId, solverFirebaseUid);
        }

        #endregion

        #region Puzzle Queries

        /// <summary>
        /// Check if a specific puzzle is solved
        /// </summary>
        public bool IsPuzzleSolved(string puzzleId)
        {
            FixedString64Bytes fixedId = new FixedString64Bytes(puzzleId);
            return solvedPuzzleIds.Contains(fixedId);
        }

        /// <summary>
        /// Get list of all solved puzzle IDs
        /// </summary>
        public List<string> GetSolvedPuzzles()
        {
            List<string> solved = new List<string>();
            foreach (var id in solvedPuzzleIds)
            {
                solved.Add(id.ToString());
            }
            return solved;
        }

        /// <summary>
        /// Get count of solved puzzles
        /// </summary>
        public int GetSolvedPuzzleCount()
        {
            return solvedPuzzleIds.Count;
        }

        #endregion

        #region Victory Condition

        /// <summary>
        /// Check if all puzzles are solved (Server only)
        /// </summary>
        private void CheckVictoryCondition()
        {
            if (!IsServer) return;

            // You'll need to know the total puzzle count
            // This could be passed from MysteryLoader or stored in a NetworkVariable
            int totalPuzzles = GetTotalPuzzleCount();

            if (solvedPuzzleIds.Count >= totalPuzzles)
            {
                Debug.Log("🎉 All puzzles solved! Victory!");
                allPuzzlesSolved.Value = true;
            }
        }

        /// <summary>
        /// Get total puzzle count from the mystery configuration
        /// </summary>
        private int GetTotalPuzzleCount()
        {
            // Get from your MysteryLoader
            if (localPuzzleManager != null)
            {
                // You'll need to expose this in DynamicPuzzleManager
                return localPuzzleManager.GetTotalPuzzleCount();
            }
            return 0;
        }

        private void OnVictoryStateChanged(bool previous, bool current)
        {
            if (current)
            {
                Debug.Log("🏆 Victory state reached!");
                OnAllPuzzlesSolved?.Invoke();

                // Trigger victory sequence (unlock final door, show completion screen, etc.)
            }
        }

        #endregion

        #region Puzzle Attempts

        /// <summary>
        /// Track puzzle attempts (for analytics/hints)
        /// </summary>
        public void RecordPuzzleAttempt(string puzzleId, bool successful)
        {
            RecordPuzzleAttemptServerRpc(puzzleId, successful);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RecordPuzzleAttemptServerRpc(string puzzleId, bool successful)
        {
            Debug.Log($"Puzzle attempt recorded: {puzzleId} - Success: {successful}");
            
            // Could track this for analytics or hint systems
            // For now, just broadcast to clients
            NotifyPuzzleAttemptClientRpc(puzzleId, successful);
        }

        [ClientRpc]
        private void NotifyPuzzleAttemptClientRpc(string puzzleId, bool successful)
        {
            // Visual feedback for attempts
            if (!successful)
            {
                Debug.Log($"❌ Puzzle attempt failed: {puzzleId}");
                // Could show error effects, play sound, etc.
            }
        }

        #endregion

        #region Network List Callbacks

        private void OnSolvedPuzzleListChanged(NetworkListEvent<FixedString64Bytes> changeEvent)
        {
            Debug.Log($"Solved puzzle list changed - Count: {solvedPuzzleIds.Count}");
        }

        #endregion

        #region Cleanup

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (solvedPuzzleIds != null)
            {
                solvedPuzzleIds.OnListChanged -= OnSolvedPuzzleListChanged;
            }

            if (allPuzzlesSolved != null)
            {
                allPuzzlesSolved.OnValueChanged -= OnVictoryStateChanged;
            }
        }

        #endregion

        #region Debug

        [ContextMenu("Debug - Print Solved Puzzles")]
        private void DebugPrintSolvedPuzzles()
        {
            Debug.Log($"=== Solved Puzzles ({solvedPuzzleIds.Count}) ===");
            foreach (var id in solvedPuzzleIds)
            {
                Debug.Log($"  - {id}");
            }
        }

        #endregion
    }
}
