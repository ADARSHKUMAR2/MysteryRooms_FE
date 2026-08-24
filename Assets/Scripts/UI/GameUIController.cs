using UnityEngine;
using TMPro;

namespace MysteryRooms.UI
{
    public class GameUIController : MonoBehaviour
    {
        public static GameUIController Instance { get; private set; }

        [Header("UI References")]
        public TextMeshProUGUI interactionPromptText;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ShowInteractionPrompt(string text)
        {
            if (interactionPromptText != null)
            {
                interactionPromptText.text = text;
                interactionPromptText.gameObject.SetActive(true);
            }
        }

        public void HideInteractionPrompt()
        {
            if (interactionPromptText != null)
            {
                interactionPromptText.gameObject.SetActive(false);
            }
        }
    }
}
