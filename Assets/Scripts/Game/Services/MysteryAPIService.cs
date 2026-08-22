using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Collections;
using MysteryRooms.Config;
using MysteryRooms.Game.Data;

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
        /// Fetch an existing mystery by ID
        /// </summary>
        public IEnumerator GetMystery(
            string mysteryId,
            Action<MysteryConfigData> onSuccess,
            Action<string> onError)
        {
            string url = $"{GameServiceURL}/mysteries/{mysteryId}";

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                Debug.Log($"🌐 Fetching mystery: {url}");

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
    }
}
