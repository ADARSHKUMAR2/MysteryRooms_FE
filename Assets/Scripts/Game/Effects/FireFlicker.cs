using UnityEngine;

namespace MysteryRooms.Game.Effects
{
    [RequireComponent(typeof(Light))]
    public class FireFlicker : MonoBehaviour
    {
        [Header("Flicker Settings")]
        [Tooltip("Minimum light intensity")]
        [SerializeField] private float minIntensity = 3.0f;
        
        [Tooltip("Maximum light intensity")]
        [SerializeField] private float maxIntensity = 6.0f;
        
        [Tooltip("How fast the light flickers (lower is faster)")]
        [SerializeField] private float flickerSpeed = 0.1f;
        
        [Header("Color Settings")]
        [SerializeField] private Color color1 = new Color(1.0f, 0.5f, 0.0f); // Orange
        [SerializeField] private Color color2 = new Color(1.0f, 0.3f, 0.0f); // Deep Orange/Red
        [SerializeField] private float colorShiftSpeed = 2.0f;

        private Light fireLight;
        private float randomOffset;

        private void Start()
        {
            fireLight = GetComponent<Light>();
            
            // This prevents all torches in the room from flickering at the exact same time
            randomOffset = Random.Range(0f, 100f); 
        }

        private void Update()
        {
            if (fireLight == null) return;

            // 1. Flicker Intensity using Perlin Noise for smooth randomness (like real fire)
            float noise = Mathf.PerlinNoise(Time.time / flickerSpeed + randomOffset, randomOffset);
            fireLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);

            // 2. Shift Color slightly back and forth for realism
            float colorNoise = Mathf.PingPong(Time.time * colorShiftSpeed + randomOffset, 1f);
            fireLight.color = Color.Lerp(color1, color2, colorNoise);
        }
    }
}
