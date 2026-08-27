# Mystery Rooms - Multiplayer Architecture Document

## Overview

The multiplayer architecture in Mystery Rooms is built around Unity Gaming Services (UGS) and Netcode for GameObjects (NGO). It operates on a Host-Client topology where one player acts as both a local client and the authoritative server (Host), while other players connect as clients.

The system integrates directly with a Python backend that generates dynamic mystery data, ensuring all players in a session experience the same generated room configuration.

## Core Components

### 1. `MultiplayerMysteryCoordinator.cs`
This is the highest-level orchestrator that bridges the game's UI, the Python backend, and the Unity multiplayer services.

**Host Flow:**
1. Calls backend API to generate a new mystery.
2. Receives a `share_code` from the backend.
3. Passes the `share_code` to the `MultiplayerSessionManager` to create a UGS Session.
4. Waits for the session to be ready and loads the mystery data into the scene.

**Client Flow:**
1. Accepts a Unity join code from the user.
2. Connects to the UGS Session using the join code.
3. Retrieves the backend `share_code` from the Session Properties.
4. Calls the backend API with the `share_code` to fetch the exact same mystery data the Host generated.
5. Loads the data locally so the client's scene perfectly matches the host's scene.

### 2. `MultiplayerSessionManager.cs`
Handles the low-level connection to Unity Gaming Services (Authentication, Multiplayer Sessions, Relay, and Vivox Voice Chat).

- **Authentication:** Anonymously authenticates players with UGS on startup.
- **Session Creation:** Creates a lobby/session and attaches the backend `share_code` as a Session Property so clients can read it later.
- **Relay Setup:** Automatically configures Unity Relay so players don't need port forwarding.
- **Scene Management:** Provides methods for the Host to transition all connected clients from the Lobby scene to the Game scene (`NetworkManager.Singleton.SceneManager.LoadScene`).

### 3. `NetworkedPuzzleManager.cs`
The central authoritative manager for game state and puzzle progression during gameplay.

- **Puzzle Synchronization:** Uses `NetworkList<FixedString64Bytes> solvedPuzzleIds` to track which puzzles have been solved.
- **RPC Communication:** 
  - Clients send `ServerRpc` calls when they solve a puzzle.
  - The Server validates and updates the network list, which automatically triggers state changes on all clients.
- **Backend Integration:** The Server is responsible for making backend API calls to update the database when a puzzle is solved, passing the correct `clientId` or `firebaseUid` to ensure the correct player gets credit.
- **Victory Condition:** Tracks the total number of solved puzzles against the required amount and triggers a victory event when all are solved.

### 4. `NetworkedScoreboard.cs` (and GameUI)
Manages the multiplayer UI, specifically tracking and displaying player scores.

- Uses a `NetworkList<PlayerScoreData>` to sync player IDs, names, and score counts.
- Captures players connecting *before* the scene loads (by iterating `ConnectedClientsIds` on spawn) and players connecting *after* (via `OnClientConnectedCallback`).
- Pushes state updates to `GameUIController` to instantiate and update UI score cards.

### 5. `NetworkedPlayerController.cs`
Attached to the player prefab, this manages individual player presence and networking.

- **Ownership Logic:** Uses `IsOwner` to enable first-person controls, camera, and interaction systems *only* for the local player. Disables these for remote players so they act as "dummies" driven by NetworkTransform.
- **Interaction Syncing:** Uses ClientRPCs to broadcast when a player interacts with an object or attempts a puzzle, allowing other clients to play animations or audio for that specific player.

## Scene Flow & Data Lifecycle

1. **Menu Scene:**
   - Player selects Host or Join.
   - `MultiplayerMysteryCoordinator` handles API calls.
   - Players sit in a Lobby waiting for others.

2. **Scene Transition:**
   - Host clicks "Start Game".
   - `MultiplayerSessionManager` tells `NetworkManager` to load the Game scene across the network.

3. **Game Scene Loading:**
   - `MysteryLoader` applies the fetched backend JSON data to the physical room (setting combination locks, symbols, etc.). This happens independently but identically on all clients.
   - `NetworkManager` spawns the Player Prefabs.
   - `NetworkedPuzzleManager` and `NetworkedScoreboard` initialize and take stock of connected clients.

4. **Gameplay:**
   - A player solves a puzzle locally.
   - Their local logic tells `NetworkedPuzzleManager` to fire a `ServerRpc`.
   - The Server receives it, updates the synced `NetworkList`, and pings the Python backend.
   - The updated `NetworkList` triggers a callback on all clients, unlocking the door/prop visually for everyone at the same time.

## Common Pitfalls Avoided

- **The Scene Transition Trap:** `OnClientConnectedCallback` only fires for clients who join *after* you start listening. Because players transition from a lobby scene, clients are already connected. The architecture avoids this by checking `NetworkManager.Singleton.ConnectedClientsIds` on `Awake`/`Start` to catch existing players.
- **The Initial State Trap:** `NetworkList.OnListChanged` only fires for future updates. When a client joins and downloads the list, they must manually iterate over the list once in `OnNetworkSpawn` to build their initial UI, which is handled in the codebase.
- **Identity Spoofery:** Clients should not dictate to the backend who they are. Clients send an RPC, and the *Server* checks the `rpcParams.Receive.SenderClientId` to guarantee who is actually claiming the point, before passing that verified ID to the backend.