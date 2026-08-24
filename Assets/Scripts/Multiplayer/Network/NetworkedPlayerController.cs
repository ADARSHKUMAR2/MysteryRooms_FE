using UnityEngine;
using Unity.Netcode;
using MysteryRooms.Game;
using UnityEngine.InputSystem;

namespace MysteryRooms.Multiplayer.Network
{
    /// <summary>
    /// Handles networked player behavior - movement sync, interaction sync, etc.
    /// Attach this to your player GameObject along with NetworkObject component.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkedPlayerController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private FirstPersonController localController;
        [SerializeField] private InteractionSystem interactionSystem;
        [SerializeField] private Camera playerCamera;

        [Header("Player Info")]
        [SerializeField] private string playerName = "Player";

        // Network variables (automatically synced across clients)
        private NetworkVariable<bool> isInteracting = new NetworkVariable<bool>();

        // Local cache
        private bool isLocalPlayer = false;

        #region Initialization

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            isLocalPlayer = IsOwner;

            if (isLocalPlayer)
            {
                // This is the local player - enable controls
                EnableLocalPlayer();
            }
            else
            {
                // This is a remote player - disable local controls
                DisableRemotePlayerControls();
            }

            Debug.Log($"NetworkedPlayer spawned. IsOwner: {IsOwner}, IsHost: {IsHost}, IsClient: {IsClient}");
        }

        private void EnableLocalPlayer()
        {
            // Enable the local player's controller and camera
            if (localController != null)
            {
                localController.enabled = true;
            }

            if (playerCamera != null)
            {
                playerCamera.enabled = true;
                // Tag as MainCamera for other systems
                playerCamera.tag = "MainCamera";
            }

            if (interactionSystem != null)
            {
                interactionSystem.enabled = true;
            }

            Debug.Log("✅ Local player controls enabled");
        }

        private void DisableRemotePlayerControls()
        {
            // Disable controls for remote players (we only watch them)
            if (localController != null)
            {
                localController.enabled = false;
            }

            if (playerCamera != null)
            {
                playerCamera.enabled = false;
                AudioListener audioListener = playerCamera.GetComponent<AudioListener>();
                if (audioListener != null)
                {
                    audioListener.enabled = false;
                }
            }

            if (interactionSystem != null)
            {
                interactionSystem.enabled = false;
            }

            // Disable CharacterController so physics don't conflict
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }

            // Disable PlayerInput if you are using it
            PlayerInput pi = GetComponent<PlayerInput>();
            if (pi != null)
            {
                pi.enabled = false;
            }

            Debug.Log("Remote player - controls disabled");
        }

        #endregion

        #region Interaction Sync

        /// <summary>
        /// Call this when the local player interacts with something
        /// </summary>
        public void OnInteract(string objectId, string interactionType)
        {
            if (!isLocalPlayer) return;

            Debug.Log($"Player interacting with {objectId} ({interactionType})");
            
            // Tell the server about this interaction
            InteractServerRpc(objectId, interactionType);
        }

        /// <summary>
        /// [ServerRpc] Client tells server about an interaction
        /// </summary>
        [ServerRpc]
        private void InteractServerRpc(string objectId, string interactionType)
        {
            // Server broadcasts this interaction to all clients
            InteractClientRpc(objectId, interactionType, OwnerClientId);
        }

        /// <summary>
        /// [ClientRpc] Server tells all clients about an interaction
        /// </summary>
        [ClientRpc]
        private void InteractClientRpc(string objectId, string interactionType, ulong interactingClientId)
        {
            // Don't process our own interactions twice
            if (IsOwner && interactingClientId == OwnerClientId) return;

            Debug.Log($"Remote player {interactingClientId} interacted with {objectId}");
            
            // You can trigger visual feedback here (e.g., show another player examining a clue)
            // FindObjectById(objectId)?.ShowRemoteInteraction();
        }

        #endregion

        #region Puzzle Interaction

        /// <summary>
        /// Notify network when local player attempts a puzzle
        /// </summary>
        public void OnPuzzleAttempted(string puzzleId)
        {
            if (!isLocalPlayer) return;
            
            Debug.Log($"Player attempted puzzle: {puzzleId}");
            PuzzleAttemptedServerRpc(puzzleId);
        }

        [ServerRpc]
        private void PuzzleAttemptedServerRpc(string puzzleId)
        {
            // Notify all clients that someone attempted a puzzle
            PuzzleAttemptedClientRpc(puzzleId, OwnerClientId);
        }

        [ClientRpc]
        private void PuzzleAttemptedClientRpc(string puzzleId, ulong attemptingClientId)
        {
            Debug.Log($"Player {attemptingClientId} attempted puzzle {puzzleId}");
            // Can show visual feedback (e.g., "Player 2 is solving the statue puzzle")
        }

        /// <summary>
        /// Notify network when local player solves a puzzle
        /// </summary>
        public void OnPuzzleSolved(string puzzleId)
        {
            if (!isLocalPlayer) return;
            
            Debug.Log($"Player solved puzzle: {puzzleId}");
            PuzzleSolvedServerRpc(puzzleId);
        }

        [ServerRpc]
        private void PuzzleSolvedServerRpc(string puzzleId)
        {
            // Notify all clients that a puzzle was solved
            PuzzleSolvedClientRpc(puzzleId, OwnerClientId);
        }

        [ClientRpc]
        private void PuzzleSolvedClientRpc(string puzzleId, ulong solvingClientId)
        {
            Debug.Log($"✅ Player {solvingClientId} solved puzzle {puzzleId}!");
            // Trigger celebration effects, unlock doors, etc.
        }

        #endregion

        #region Player Info

        /// <summary>
        /// Set this player's display name
        /// </summary>
        public void SetPlayerName(string name)
        {
            if (!isLocalPlayer) return;
            
            playerName = name;
            SetPlayerNameServerRpc(name);
        }

        [ServerRpc]
        private void SetPlayerNameServerRpc(string name)
        {
            playerName = name;
            // Could sync this with a NetworkVariable<string> if you want names displayed
        }

        public string GetPlayerName() => playerName;

        #endregion

        #region Cleanup

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            Debug.Log($"NetworkedPlayer despawned: {OwnerClientId}");
        }

        #endregion
    }
}
