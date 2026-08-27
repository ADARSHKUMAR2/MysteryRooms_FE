using UnityEngine;
using UnityEngine.UI;

namespace MysteryRooms.Game.Effects
{
    [RequireComponent(typeof(Image))]
    public class UILightReceiver : MonoBehaviour
    {
        private Image uiImage;
        private Color originalColor;

        [Tooltip("The light source that illuminates this UI (e.g., the Player's Torch or the Room Light)")]
        public Light targetLight;

        [Tooltip("How close the light needs to be to fully illuminate the image")]
        public float maxLightDistance = 10f;

        private void Start()
        {
            uiImage = GetComponent<Image>();
            originalColor = uiImage.color;
        }

        private void Update()
        {
            if (targetLight == null || !targetLight.isActiveAndEnabled)
            {
                // If there is no light, or the light is turned off, the UI goes completely black!
                uiImage.color = Color.black;
                return;
            }

            // Calculate how far the light is from the UI Image
            float distance = Vector3.Distance(transform.position, targetLight.transform.position);

            if (distance > maxLightDistance)
            {
                // Light is too far away, stay black
                uiImage.color = Color.black;
            }
            else
            {
                // Light is close! Calculate how bright the image should be (0 to 1)
                // If distance is 0, brightness is 1. If distance is maxLightDistance, brightness is 0.
                float brightness = 1f - (distance / maxLightDistance);
                
                // Multiply brightness by the light's intensity so brighter lights make it pop more
                brightness *= (targetLight.intensity / 5f); 
                
                // Clamp it so it doesn't get brighter than the original color
                brightness = Mathf.Clamp01(brightness);

                // Apply the calculated darkness to the image
                uiImage.color = new Color(
                    originalColor.r * brightness, 
                    originalColor.g * brightness, 
                    originalColor.b * brightness, 
                    originalColor.a
                );
            }
        }
    }
}
