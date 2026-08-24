using System;
using System.Collections.Generic;

namespace MysteryRooms.Multiplayer.Core
{
    /// <summary>
    /// Represents the current state of multiplayer connection
    /// </summary>
    public enum ConnectionStatus
    {
        Disconnected,      // Not connected to any session
        Connecting,        // Attempting to connect
        Connected,         // Successfully connected
        Failed             // Connection failed
    }

    /// <summary>
    /// Session role for the local player
    /// </summary>
    public enum SessionRole
    {
        None,             // Not in a session
        Host,             // Hosting the session
        Client            // Joined as client
    }

    /// <summary>
    /// Data class holding current multiplayer session information
    /// </summary>
    [Serializable]
    public class MultiplayerSessionInfo
    {
        public string sessionId;           // Unity session ID
        public string joinCode;            // 6-character join code (matches backend share_code)
        public string mysteryId;           // Backend mystery ID
        public SessionRole role;           // Host or Client
        public int maxPlayers;             // Maximum players allowed
        public int currentPlayerCount;     // Current number of players
        public List<string> playerIds;     // List of connected player IDs

        public MultiplayerSessionInfo()
        {
            playerIds = new List<string>();
            maxPlayers = 4; // Default to 4 players
        }
    }

    /// <summary>
    /// Event arguments for connection events
    /// </summary>
    public class ConnectionEventArgs : EventArgs
    {
        public ConnectionStatus Status { get; set; }
        public string Message { get; set; }
        public string ErrorDetails { get; set; }
    }

    /// <summary>
    /// Event arguments for session events
    /// </summary>
    public class SessionEventArgs : EventArgs
    {
        public MultiplayerSessionInfo SessionInfo { get; set; }
        public string JoinCode { get; set; }
    }
}
