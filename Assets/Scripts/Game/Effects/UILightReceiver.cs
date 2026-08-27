using UnityEngine;
using UnityEngine.UI;

namespace MysteryRooms.Game.Effects
{
    [RequireComponent(typeof(Image))]
    public class UILightReceiver : MonoBehaviour
    {
        private Image uiImage;
        private Color originalColor;

        [Tooltip("Maximum distance a light can be to affect this UI")]
        public float maxLightDistance = 10f;
        
        [Tooltip("Minimum brightness to always show (0 = pitch black)")]
        [Range(0f, 1f)]
        public float ambientBrightness = 0.0f;

        private Light[] allLights;
        private float checkTimer = 0f;
        private float checkInterval = 0.5f; // Update the list of lights every 0.5s to save performance

        private void Start()
        {
            uiImage = GetComponent<Image>();
            originalColor = uiImage.color;
            FindAllLights();
        }

        private void FindAllLights()
        {
            // Finds every Light component currently in the scene (including newly spawned torches)
            allLights = FindObjectsOfType<Light>();
        }

        private void Update()
        {
            // Periodically check for new lights just in case a player late-joined with a torch
            checkTimer += Time.deltaTime;
            if (checkTimer >= checkInterval)
            {
                FindAllLights();
                checkTimer = 0f;
            }

            // Start with total darkness (or whatever ambient brightness you set)
            float highestBrightness = ambientBrightness;

            if (allLights != null)
            {
                foreach (Light light in allLights)
                {
                    // Skip lights that are destroyed or turned off
                    if (light == null || !light.isActiveAndEnabled) continue;

                    // Calculate distance from this UI element to the light source
                    float distance = Vector3.Distance(transform.position, light.transform.position);

                    // We care about the light's actual range setting, capped by our max limit
                    float effectiveRange = Mathf.Min(maxLightDistance, light.range);

                    if (distance <= effectiveRange)
                    {
                        // Calculate brightness (1 when right next to it, 0 when at the edge of the light's range)
                        float lightContribution = 1f - (distance / effectiveRange);
                        
                        // Factor in the light's intensity (Assuming an intensity of ~5 is "full brightness")
                        lightContribution *= (light.intensity / 5f);
                        
                        // If this light illuminates the image brighter than the others, use this one!
                        if (lightContribution > highestBrightness)
                        {
                            highestBrightness = lightContribution;
                        }
                    }
                }
            }

            // Cap brightness so it doesn't wash out the image colors
            highestBrightness = Mathf.Clamp01(highestBrightness);

            // Apply the calculated brightness dynamically to the image
            uiImage.color = new Color(
                originalColor.r * highestBrightness, 
                originalColor.g * highestBrightness, 
                originalColor.b * highestBrightness, 
                originalColor.a // Preserve the original transparency
            );
        }
    }
}
