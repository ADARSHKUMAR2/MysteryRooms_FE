using UnityEngine;
using UnityEditor;

namespace MysteryRooms.EditorTools
{
    public class DesertSandGenerator : EditorWindow
    {
        [MenuItem("Mystery Rooms/Tools/Generate Desert Sand Effect")]
        public static void GenerateDesertSand()
        {
            // 1. Create the Sand Particle System Object
            GameObject sandObj = new GameObject("Desert_Sand_Storm");
            
            // Move it just slightly above the floor (e.g. 10cm up)
            sandObj.transform.position = new Vector3(0, 0.1f, 0); 
            
            ParticleSystem ps = sandObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.prewarm = true; // Sand is already blowing when the scene loads
            
            // Sand particles live long enough to drift across the room
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
            
            // Speed of the wind blowing the sand
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2.0f);
            
            // Tiny speck sizes
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            
            // Sand colors (Light tan to dark gold)
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1.0f, 0.9f, 0.7f, 0.6f), 
                new Color(0.8f, 0.6f, 0.3f, 0.4f)
            );
            
            // ZERO GRAVITY! This prevents the sand from falling through the floor.
            main.gravityModifier = 0.0f; 
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 2000; // Dense but incredibly cheap without collision

            // 2. Emission (Constant blowing wind)
            var emission = ps.emission;
            emission.rateOverTime = 150f;

            // 3. Shape (Spawn across the entire floor)
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            // Scale this box to match the size of your room floor (e.g. 20x20 meters)
            shape.scale = new Vector3(20f, 0f, 20f); 

            // 4. Velocity over Lifetime (Organic swirling wind)
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            
            // Drift generally across the room, but with random speed per particle
            velocity.x = new ParticleSystem.MinMaxCurve(-0.2f, 1.0f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f); // Tiny vertical drift so it's not perfectly flat
            velocity.z = new ParticleSystem.MinMaxCurve(-0.2f, 1.0f);
            
            // Orbital Velocity (Must all be the same curve mode!)
            // X and Z are 0 so it doesn't swirl up and down. Y makes it curve left/right.
            velocity.orbitalX = new ParticleSystem.MinMaxCurve(0.0f, 0.0f);
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(0.0f, 0.0f);

            // 5. NOISE! (This makes the sand dance and swirl randomly without physics)
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.2f;      // Gentle push
            noise.frequency = 0.5f;     // Size of the wind swirls
            noise.scrollSpeed = 0.5f;   // The wind currents move over time
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.Medium;

            // 6. Color over Lifetime (Fade in and fade out so they don't pop abruptly)
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.0f, 0.0f),  // Fade in
                    new GradientAlphaKey(1.0f, 0.2f),  // Fully visible
                    new GradientAlphaKey(1.0f, 0.8f), 
                    new GradientAlphaKey(0.0f, 1.0f)   // Fade out
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

            // 7. Material Setup (Unlit + Additive so it's visible in the dark room)
            ParticleSystemRenderer psRenderer = sandObj.GetComponent<ParticleSystemRenderer>();
            
            // Use UNLIT so we can actually see it in the dark tomb!
            Material sandMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            sandMat.SetFloat("_Surface", 1); // Transparent
            sandMat.SetFloat("_Blend", 2);   // Additive (Makes the sand look like glowing dust/magic!)
            
            // Try to use the default particle texture for soft fuzzy sand grains
            Texture2D defaultParticleTex = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
            if (defaultParticleTex != null)
            {
                sandMat.SetTexture("_BaseMap", defaultParticleTex);
            }

            // Save material
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            AssetDatabase.CreateAsset(sandMat, "Assets/Materials/SandParticleMat.mat");
            AssetDatabase.SaveAssets();

            psRenderer.sharedMaterial = sandMat;

            // Highlight the object so the user can adjust the box scale
            Selection.activeGameObject = sandObj;
            Debug.Log("🌪️ Optimized Organic Desert Sand Storm generated! (Noise & Orbital velocity added, Unlit Additive material applied).");
        }
    }
}
