// ============================================================
// VictoryAPIService.cs
// Folder: Assets/Scripts/Game/Services/
// Assembly: MysteryRooms.Game.Services
//
// PURPOSE:
//   Handles the single HTTP call to the victory endpoint:
//     GET /game/sessions/{session_id}/victory
//
//   Returns WinningScreenData used to populate the victory screen.
//
// PATTERN:
//   Follows the exact same coroutine + UnityWebRequest + JsonUtility
//   pattern used in MysteryAPIService.cs in this same folder.
//   Auth token pulled from UserSession.Instance?.FirebaseToken
//   exactly as CompleteSession() does in MysteryAPIService.
//
// USAGE (from VictoryScreenUI or any MonoBehaviour):
//   StartCoroutine(
//       _victoryAPI.GetVictoryData(
//           sessionId,
//           onSuccess: data => Show(data),
//           onError:   err  => ShowFallback(err)
//       )
//   );
// ============================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using MysteryRooms.Config;
using MysteryRooms.Game.Data;
using MysteryRooms.Authentication;

namespace MysteryRooms.Game.Services
{
    public class VictoryAPIService : MonoBehaviour
    {
        // Injected in the Inspector — same pattern as MysteryAPIService
        [SerializeField] private BackendConfig backendConfig;

        // Builds the base game service URL from config
        // e.g. "http://localhost:8002/game" or production equivalent
        private string GameServiceURL => $"{backendConfig.CurrentGameURL}/game";

        private void Awake()
        {
            if (backendConfig == null)
                Debug.LogError("[VictoryAPIService] BackendConfig is not assigned!");
        }

        // ============================================================
        // GetVictoryData
        //   GET /game/sessions/{session_id}/victory
        //
        // Parameters:
        //   sessionId  — the session_id from StartSession response
        //   onSuccess  — called with parsed WinningScreenData
        //   onError    — called with a human-readable error string
        // ============================================================
        public IEnumerator GetVictoryData(
            string sessionId,
            Action<WinningScreenData> onSuccess,
            Action<string> onError)
        {
            // Build URL: /game/sessions/{session_id}/victory
            string url = $"{GameServiceURL}/sessions/{sessionId}/victory";

            Debug.Log($"<color=cyan>[VictoryAPIService] GET {url}</color>");

            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                // Attach auth token — same pattern as MysteryAPIService.CompleteSession
                string token = UserSession.Instance?.FirebaseToken
                               ?? PlayerPrefs.GetString("FirebaseToken", "");

                if (!string.IsNullOrEmpty(token))
                {
                    webRequest.SetRequestHeader("Authorization", "Bearer " + token);
                }
                else
                {
                    Debug.LogWarning("[VictoryAPIService] No Firebase token found! Request may return 401.");
                }

                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.timeout = 30;

                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = webRequest.downloadHandler.text;
                    Debug.Log("<color=green>[VictoryAPIService] ✅ Victory data received.</color>");
                    Debug.Log($"<color=yellow>[VictoryAPIService] Raw JSON:\n{jsonResponse}</color>");

                    try
                    {
                        WinningScreenData data = JsonUtility.FromJson<WinningScreenData>(jsonResponse);

                        if (data == null || !data.victory)
                        {
                            string err = "Victory data was null or victory flag was false.";
                            Debug.LogError($"[VictoryAPIService] {err}");
                            onError?.Invoke(err);
                        }
                        else
                        {
                            Debug.Log($"<color=white>[VictoryAPIService] Rank: {data.rank_title} | Score: {data.score?.final_score}</color>");
                            onSuccess?.Invoke(data);
                        }
                    }
                    catch (Exception e)
                    {
                        string err = $"Failed to parse victory JSON: {e.Message}";
                        Debug.LogError($"[VictoryAPIService] {err}");
                        onError?.Invoke(err);
                    }
                }
                else
                {
                    string err = $"Victory request failed [{webRequest.responseCode}]: {webRequest.error}";
                    Debug.LogError($"[VictoryAPIService] {err}");
                    onError?.Invoke(err);
                }
            }
        }
    }
}
