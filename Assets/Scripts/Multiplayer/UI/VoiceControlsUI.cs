using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Vivox;
using TMPro;

public class VoiceControlsUI : MonoBehaviour
{
    // [Header("UI Elements")]
    // [SerializeField] private Toggle muteToggle;
    // [SerializeField] private Slider volumeSlider;
    // [SerializeField] private TextMeshProUGUI statusText;

    // private void Start()
    // {
    //     if (muteToggle != null)
    //     {
    //         muteToggle.onValueChanged.AddListener(OnMuteToggled);
    //     }

    //     if (volumeSlider != null)
    //     {
    //         volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    //         volumeSlider.value = 50; // Default volume
    //     }

    //     UpdateStatus();
    // }

    // private void OnMuteToggled(bool isMuted)
    // {
    //     try
    //     {
    //         // Mute/unmute local microphone
    //         VivoxService.Instance.AudioInputDevices.Muted = isMuted;
    //         UpdateStatus();
    //     }
    //     catch (System.Exception e)
    //     {
    //         Debug.LogError($"Failed to toggle mute: {e.Message}");
    //     }
    // }

    // private void OnVolumeChanged(float value)
    // {
    //     try
    //     {
    //         // Set master volume (0-100)
    //         int volume = Mathf.RoundToInt(value);
    //         VivoxService.Instance.AudioOutputDevices.VolumeAdjustment = volume;
    //         UpdateStatus();
    //     }
    //     catch (System.Exception e)
    //     {
    //         Debug.LogError($"Failed to change volume: {e.Message}");
    //     }
    // }

    // private void UpdateStatus()
    // {
    //     if (statusText == null) return;

    //     try
    //     {
    //         bool isMuted = VivoxService.Instance.AudioInputDevices.Muted;
    //         int volume = VivoxService.Instance.AudioOutputDevices.VolumeAdjustment;
            
    //         statusText.text = isMuted ? "🔇 Muted" : $"🎤 Volume: {volume}%";
    //     }
    //     catch
    //     {
    //         statusText.text = "Voice chat not initialized";
    //     }
    // }
}
