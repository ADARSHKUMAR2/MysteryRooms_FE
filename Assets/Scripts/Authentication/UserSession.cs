using UnityEngine;

namespace MysteryRooms.Authentication
{
    /// <summary>
    /// Singleton to persist user data across scenes
    /// </summary>
    public class UserSession : MonoBehaviour
    {
        public static UserSession Instance { get; private set; }

        public string UserId { get; private set; }
        public string Username { get; private set; }
        public string Email { get; private set; }
        public string FirebaseToken { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Persist across scene loads
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void SetUserData(string userId, string username, string email, string token)
        {
            UserId = userId;
            Username = username;
            Email = email;
            FirebaseToken = token;
            
            Debug.Log($"✅ UserSession initialized: {username} ({userId})");
        }

        public void ClearUserData()
        {
            UserId = null;
            Username = null;
            Email = null;
            FirebaseToken = null;
        }
    }
}
