using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Collections;
using MysteryRooms.Config;
using MysteryRooms.Game.Data;
using MysteryRooms.Authentication;
using System.Collections.Generic;

namespace MysteryRooms.Game.Services
{
    public class MysteryAPIService : MonoBehaviour
    {
        [SerializeField] private BackendConfig backendConfig;

        private string GameServiceURL => $"{backendConfig.CurrentURL}/game";

        private void Awake()
        {
            if (backendConfig == null)
            {
                Debug.LogError("BackendConfig is not assigned to MysteryAPIService!");
            }
        }

        /// <summary>
        /// Generate a new mystery from the backend
        /// </summary>
        public IEnumerator GenerateMystery(
            GenerateMysteryRequest request,
            Action<MysteryConfigData> onSuccess,
            Action<string> onError)
        {
            string url = $"{GameServiceURL}/generate";
            string jsonBody = JsonUtility.ToJson(request);

            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                webRequest.timeout = 60;

                string token = PlayerPrefs.GetString("FirebaseToken", "");
                if (!string.IsNullOrEmpty(token))
                {
                    webRequest.SetRequestHeader("Authorization", "Bearer " + token);
                }
                else
                {
                    Debug.LogWarning("[UNITY-API] No Firebase token found in PlayerPrefs! Request might fail 401.");
                }

                Debug.Log($"🌐 Requesting mystery generation: {url}");
                Debug.Log($"📤 Request body: {jsonBody}");

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = webRequest.downloadHandler.text;
                    Debug.Log("<color=green>[UNITY-API] ✅ Success! Received Response.</color>");
                    Debug.Log("<color=yellow>[UNITY-API] 📥 Full JSON Response: </color>\n" + jsonResponse);

                    try
                    {
                        MysteryConfigData mystery = JsonUtility.FromJson<MysteryConfigData>(jsonResponse);
                        onSuccess?.Invoke(mystery);
                        Debug.Log($"<color=white>[UNITY-API] 🧩 Parsed Mystery ID: {mystery.mystery_id} | Objective: {mystery.objective}</color>");
                        
                    }
                    catch (Exception e)
                    {
                        string error = $"Failed to parse mystery JSON: {e.Message}";
                        Debug.LogError(error);
                        onError?.Invoke(error);
                    }
                }
                else
                {
                    string error = $"Mystery generation failed: {webRequest.error}";
                    Debug.LogError(error);
                    onError?.Invoke(error);
                }
            }
        }

        /// <summary>
        /// Fetch an existing mystery by its 6-character share code
        /// </summary>
        public IEnumerator GetMysteryByShareCode(
            string shareCode,
            Action<MysteryConfigData> onSuccess,
            Action<string> onError)
        {
            string url = $"{GameServiceURL}/mysteries/shared/{shareCode}";
            yield return FetchMysteryData(url, onSuccess, onError);
        }

        /// <summary>
        /// Fetch an existing mystery by ID
        /// </summary>
        /// <summary>
        /// Fetch an existing mystery by ID
        /// </summary>
        public IEnumerator GetMystery(
            string mysteryId,
            Action<MysteryConfigData> onSuccess,
            Action<string> onError)
        {
            string url = $"{GameServiceURL}/mysteries/{mysteryId}";
            yield return FetchMysteryData(url, onSuccess, onError);
        }

        /// <summary>
        /// Common helper method to execute the UnityWebRequest for fetching mysteries
        /// </summary>
        private IEnumerator FetchMysteryData(
            string url,
            Action<MysteryConfigData> onSuccess,
            Action<string> onError)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                // Optional: set timeout here as well, though GET requests usually don't need 45s
                webRequest.timeout = 30; 

                string token = UserSession.Instance?.FirebaseToken ?? PlayerPrefs.GetString("FirebaseToken", "");
                if (!string.IsNullOrEmpty(token))
                {
                    webRequest.SetRequestHeader("Authorization", "Bearer " + token);
                }
                else
                {
                    Debug.LogWarning("[UNITY-API] No Firebase token found! Request will likely fail 401.");
                }
                
                Debug.Log($"🌐 Fetching mystery from: {url}");

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = webRequest.downloadHandler.text;
                    Debug.Log($"✅ Mystery fetched successfully!");

                    try
                    {
                        MysteryConfigData mystery = JsonUtility.FromJson<MysteryConfigData>(jsonResponse);
                        onSuccess?.Invoke(mystery);
                    }
                    catch (Exception e)
                    {
                        string error = $"Failed to parse mystery JSON: {e.Message}";
                        Debug.LogError(error);
                        onError?.Invoke(error);
                    }
                }
                else
                {
                    string error = $"Failed to fetch mystery: {webRequest.error}";
                    Debug.LogError(error);
                    onError?.Invoke(error);
                }
            }
        }

        /// <summary>
        /// Join an ongoing game session
        /// </summary>
        public IEnumerator JoinSession(
            string sessionId,
            string playerId,
            Action<GameSessionData> onSuccess,
            Action<string> onError)
        {
            string url = $"{GameServiceURL}/sessions/{sessionId}/join";
            
            JoinSessionRequestPayload request = new JoinSessionRequestPayload { player_id = playerId };
            string jsonBody = JsonUtility.ToJson(request);

            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                string token = UserSession.Instance?.FirebaseToken ?? PlayerPrefs.GetString("FirebaseToken", "");
                if (!string.IsNullOrEmpty(token))
                {
                    webRequest.SetRequestHeader("Authorization", "Bearer " + token);
                }

                Debug.Log($"<color=yellow>[UNITY-API] ➕ Joining Session: {sessionId}</color>");

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = webRequest.downloadHandler.text;
                    try
                    {
                        GameSessionData session = JsonUtility.FromJson<GameSessionData>(jsonResponse);
                        onSuccess?.Invoke(session);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to parse joined session: {e.Message}");
                        onError?.Invoke(e.Message);
                    }
                }
                else
                {
                    Debug.LogError($"Session join failed: {webRequest.error}");
                    onError?.Invoke(webRequest.error);
                }
            }
        }


        /// <summary>
        /// Wrapper to help JsonUtility parse a JSON array
        /// </summary>
        [Serializable]
        private class MysteryListWrapper
        {
            public List<MysteryConfigData> mysteries;
        }

        /// <summary>
        /// Fetch a list of recently generated mysteries from the database
        /// </summary>
        public IEnumerator GetRecentMysteries(
            int limit,
            Action<List<MysteryConfigData>> onSuccess,
            Action<string> onError)
        {
            string url = $"{GameServiceURL}/mysteries?limit={limit}";

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.timeout = 30; 

                string token = UserSession.Instance?.FirebaseToken ?? PlayerPrefs.GetString("FirebaseToken", "");
                if (!string.IsNullOrEmpty(token))
                {
                    webRequest.SetRequestHeader("Authorization", "Bearer " + token);
                }
                
                Debug.Log($"🌐 Fetching recent mysteries from: {url}");

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = webRequest.downloadHandler.text;
                    
                    try
                    {
                        // Wrap the array in an object so JsonUtility can parse it
                        string wrappedJson = "{\"mysteries\":" + jsonResponse + "}";
                        MysteryListWrapper wrapper = JsonUtility.FromJson<MysteryListWrapper>(wrappedJson);
                        
                        onSuccess?.Invoke(wrapper.mysteries);
                    }
                    catch (Exception e)
                    {
                        string error = $"Failed to parse recent mysteries JSON: {e.Message}";
                        Debug.LogError(error);
                        onError?.Invoke(error);
                    }
                }
                else
                {
                    string error = $"Failed to fetch recent mysteries: {webRequest.error}";
                    Debug.LogError(error);
                    onError?.Invoke(error);
                }
            }
        }


        /// <summary>
        /// Start a new game session
        /// </summary>
        public IEnumerator StartSession(
            StartSessionRequest request,
            Action<GameSessionData> onSuccess,
            Action<string> onError)
        {
            string url = $"{GameServiceURL}/sessions/start";
            string jsonBody = JsonUtility.ToJson(request);

            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                string token = UserSession.Instance?.FirebaseToken ?? PlayerPrefs.GetString("FirebaseToken", "");
                if (!string.IsNullOrEmpty(token))
                {
                    webRequest.SetRequestHeader("Authorization", "Bearer " + token);
                }

                Debug.Log("<color=cyan>[UNITY-API] 📤 Starting Session</color>");

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = webRequest.downloadHandler.text;
                    Debug.Log("<color=green>[UNITY-API] ✅ Session Started!</color>");

                    try
                    {
                        GameSessionData session = JsonUtility.FromJson<GameSessionData>(jsonResponse);
                        onSuccess?.Invoke(session);
                    }
                    catch (Exception e)
                    {
                        string error = $"Failed to parse session JSON: {e.Message}";
                        Debug.LogError(error);
                        onError?.Invoke(error);
                    }
                }
                else
                {
                    string error = $"Session start failed: {webRequest.error}";
                    Debug.LogError(error);
                    onError?.Invoke(error);
                }
            }
        }

        /// <summary>
        /// Update session progress (puzzle solved/attempted)
        /// </summary>
        public IEnumerator UpdateSession(
            UpdateSessionRequest request,
            Action<GameSessionData> onSuccess,
            Action<string> onError)
        {
            string url = $"{GameServiceURL}/sessions/update";
            string jsonBody = JsonUtility.ToJson(request);

            using (UnityWebRequest webRequest = new UnityWebRequest(url, "PUT"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");

                string token = UserSession.Instance?.FirebaseToken ?? PlayerPrefs.GetString("FirebaseToken", "");
                if (!string.IsNullOrEmpty(token))
                {
                    webRequest.SetRequestHeader("Authorization", "Bearer " + token);
                }

                Debug.Log($"<color=yellow>[UNITY-API] 🔄 Updating Session: {request.puzzle_solved ?? request.puzzle_attempted}</color>");

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = webRequest.downloadHandler.text;
                    
                    try
                    {
                        GameSessionData session = JsonUtility.FromJson<GameSessionData>(jsonResponse);
                        onSuccess?.Invoke(session);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to parse session update: {e.Message}");
                        onError?.Invoke(e.Message);
                    }
                }
                else
                {
                    Debug.LogError($"Session update failed: {webRequest.error}");
                    onError?.Invoke(webRequest.error);
                }
            }
        }

        /// <summary>
        /// Complete the game session
        /// </summary>
        public IEnumerator CompleteSession(
            CompleteSessionRequest request,
            Action<GameSessionData> onSuccess,
            Action<string> onError)
        {
            string url = $"{GameServiceURL}/sessions/complete";
            string jsonBody = JsonUtility.ToJson(request);

            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                
                string token = UserSession.Instance?.FirebaseToken ?? PlayerPrefs.GetString("FirebaseToken", "");
                if (!string.IsNullOrEmpty(token))
                {
                    webRequest.SetRequestHeader("Authorization", "Bearer " + token);
                }

                Debug.Log("<color=green>[UNITY-API] 🏁 Completing Session</color>");

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = webRequest.downloadHandler.text;
                    Debug.Log("<color=green>[UNITY-API] ✅ Session Completed!</color>");

                    try
                    {
                        GameSessionData session = JsonUtility.FromJson<GameSessionData>(jsonResponse);
                        onSuccess?.Invoke(session);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to parse completed session: {e.Message}");
                        onError?.Invoke(e.Message);
                    }
                }
                else
                {
                    Debug.LogError($"Session complete failed: {webRequest.error}");
                    onError?.Invoke(webRequest.error);
                }
            }
        }
    }
}
