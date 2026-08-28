using UnityEngine;
using UnityEditor;
using System.IO;

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
            
            fireParticlesObj.transform.localPosition = new Vector3(0, 1.1f, 0); 
            fireParticlesObj.transform.localScale = Vector3.one;

            ParticleSystem ps = fireParticlesObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 1f;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            // Lifetime, speed, and size
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.0f);
            
            main.startColor = new Color(1f, 0.6f, 0.1f);
            main.gravityModifier = -0.1f; 
            main.simulationSpace = ParticleSystemSimulationSpace.World; 
            
            // 🚀 OPTIMIZATION 1: Cap the max particles! A single torch only needs ~50 particles to look full.
            main.maxParticles = 50; 

            // Emission 
            var emission = ps.emission;
            emission.rateOverTime = 40f;

            // Shape 
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

            // Size over Lifetime
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0.0f, 1.0f),
                new Keyframe(1.0f, 0.0f)
            );
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

            // 🚀 OPTIMIZATION 2: Reuse existing material so we don't spam the disk or break prefab links
            ParticleSystemRenderer psRenderer = fireParticlesObj.GetComponent<ParticleSystemRenderer>();
            Material particleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FireParticleMat.mat");
            
            if (particleMat == null)
            {
                particleMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                particleMat.SetFloat("_Surface", 1); // Transparent
                particleMat.SetFloat("_Blend", 2);   // Additive
                particleMat.SetFloat("_ColorMode", 1); // Multiply
                particleMat.SetColor("_BaseColor", Color.white);
                
                Texture2D defaultParticleTex = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
                if (defaultParticleTex != null)
                {
                    particleMat.SetTexture("_BaseMap", defaultParticleTex);
                }

                if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                {
                    AssetDatabase.CreateFolder("Assets", "Materials");
                }
                AssetDatabase.CreateAsset(particleMat, "Assets/Materials/FireParticleMat.mat");
                AssetDatabase.SaveAssets();
            }
            
            psRenderer.sharedMaterial = particleMat;

            // 3. Create the Point Light
            GameObject lightObj = new GameObject("Fire_Light");
            lightObj.transform.SetParent(torchStick.transform, false);
            lightObj.transform.localPosition = new Vector3(0, 1.3f, 0); 

            Light pointLight = lightObj.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = new Color(1f, 0.5f, 0f); 
            pointLight.range = 8f;
            pointLight.intensity = 5f;
            
            // 🚀 OPTIMIZATION 3: Disable Point Light Shadows! 
            // 6 torches with Soft Shadows = 36 simultaneous shadow maps rendered per frame. 
            // Turning this off guarantees perfect 60fps on almost any device.
            pointLight.shadows = LightShadows.None;

            // 4. Wrap up
            Selection.activeGameObject = torchStick;
            Debug.Log("🔥 Optimized Torch successfully generated!");
        }
    }
}
