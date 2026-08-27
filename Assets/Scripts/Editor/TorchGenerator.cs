using UnityEngine;
using UnityEditor;

namespace MysteryRooms.EditorTools
{
    public class TorchGenerator : EditorWindow
    {
        [MenuItem("Mystery Rooms/Tools/Generate Torch")]
        public static void GenerateTorch()
        {
            // 1. Create the base wooden stick
            GameObject torchStick = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            torchStick.name = "Torch";
            torchStick.transform.localScale = new Vector3(0.1f, 0.5f, 0.1f);

            // Create a simple dark brown material for the stick
            Material woodMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            woodMat.color = new Color(0.25f, 0.15f, 0.05f); // Dark Brown
            torchStick.GetComponent<Renderer>().sharedMaterial = woodMat;

            // 2. Create the Fire Particles
            GameObject fireParticlesObj = new GameObject("Fire_Particles");
            fireParticlesObj.transform.SetParent(torchStick.transform, false);
            
            // Move it to the top of the stick. Local scale stays 1,1,1 to avoid squashing!
            fireParticlesObj.transform.localPosition = new Vector3(0, 1.1f, 0); 
            fireParticlesObj.transform.localScale = Vector3.one;

            ParticleSystem ps = fireParticlesObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 1f;
            
            // Use Hierarchy scaling so the local scale of 1,1,1 doesn't get squashed by the stick's 0.1 scale
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            // Randomize lifetime, speed, and size
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
            
            // Scaled up because the hierarchy scale makes them tiny otherwise
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.0f);
            
            main.startColor = new Color(1f, 0.6f, 0.1f); // Bright Orange/Yellow base
            main.gravityModifier = -0.1f; // Negative gravity makes fire float upwards
            main.simulationSpace = ParticleSystemSimulationSpace.World; // Leaves a trail when moving

            // Emission (How much fire spawns)
            var emission = ps.emission;
            emission.rateOverTime = 40f;

            // Shape (Spawn in a tight cone)
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.05f;

            // Color over Lifetime
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(Color.yellow, 0.0f), 
                    new GradientColorKey(new Color(1f, 0.4f, 0f), 0.5f), 
                    new GradientColorKey(Color.red, 1.0f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1.0f, 0.0f), 
                    new GradientAlphaKey(1.0f, 0.7f), 
                    new GradientAlphaKey(0.0f, 1.0f) 
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

            // Size over Lifetime (Fire shrinks as it goes up)
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0.0f, 1.0f),
                new Keyframe(1.0f, 0.0f)
            );
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

            // Create the glowing Additive Material
            ParticleSystemRenderer psRenderer = fireParticlesObj.GetComponent<ParticleSystemRenderer>();
            Material particleMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            
            // URP Transparent + Additive Blend
            particleMat.SetFloat("_Surface", 1); // 1 = Transparent
            particleMat.SetFloat("_Blend", 2);   // 2 = Additive (Glow effect)
            particleMat.SetFloat("_ColorMode", 1); // 1 = Multiply
            particleMat.SetColor("_BaseColor", Color.white);
            
            // Try to find the default Unity fuzzy particle texture
            Texture2D defaultParticleTex = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
            if (defaultParticleTex != null)
            {
                particleMat.SetTexture("_BaseMap", defaultParticleTex);
            }

            // Save the material to the Assets folder so it doesn't get lost
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            AssetDatabase.CreateAsset(particleMat, "Assets/Materials/FireParticleMat.mat");
            AssetDatabase.SaveAssets();

            psRenderer.sharedMaterial = particleMat;

            // 3. Create the glowing Point Light
            GameObject lightObj = new GameObject("Fire_Light");
            lightObj.transform.SetParent(torchStick.transform, false);
            lightObj.transform.localPosition = new Vector3(0, 1.3f, 0); // Slightly above stick

            Light pointLight = lightObj.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = new Color(1f, 0.5f, 0f); // Warm Orange Glow
            pointLight.range = 8f;
            pointLight.intensity = 5f;
            pointLight.shadows = LightShadows.Soft;

            // 4. Wrap up
            Selection.activeGameObject = torchStick;
            Debug.Log("🔥 Torch successfully generated! (Fire material saved to Assets/Materials/FireParticleMat.mat)");
        }
    }
}
