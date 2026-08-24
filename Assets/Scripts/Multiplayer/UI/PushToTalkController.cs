using UnityEngine;
using Unity.Services.Vivox;

public class PushToTalkController : MonoBehaviour
{
    // [SerializeField] private KeyCode pushToTalkKey = KeyCode.V;
    
    // private void Update()
    // {
    //     if (VivoxService.Instance == null) return;

    //     if (Input.GetKeyDown(pushToTalkKey))
    //     {
    //         // Enable microphone
    //         VivoxService.Instance.AudioInputDevices.Muted = false;
    //         Debug.Log("🎤 Push-to-talk: ON");
    //     }
        
    //     if (Input.GetKeyUp(pushToTalkKey))
    //     {
    //         // Disable microphone
    //         VivoxService.Instance.AudioInputDevices.Muted = true;
    //         Debug.Log("🔇 Push-to-talk: OFF");
    //     }
    // }
}
