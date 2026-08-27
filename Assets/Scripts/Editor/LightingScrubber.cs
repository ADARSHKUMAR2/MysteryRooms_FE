using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace MysteryRooms.EditorTools
{
    public class LightingScrubber : EditorWindow
    {
        [MenuItem("Mystery Rooms/Tools/Nuke Ghost Lighting")]
        public static void NukeLighting()
        {
            Debug.Log("☢️ Initiating Lighting Scrub...");

            // 1. Force clear all baked data (just in case the UI button failed)
            Lightmapping.Clear();
            Lightmapping.ClearLightingDataAsset();
            Lightmapping.ClearDiskCache();

            // 2. Kill all ambient Spherical Harmonics
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;
            RenderSettings.ambientSkyColor = Color.black;
            RenderSettings.ambientEquatorColor = Color.black;
            RenderSettings.ambientGroundColor = Color.black;
            RenderSettings.ambientIntensity = 0f;
            
            // 3. Kill all reflections
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.customReflection = null;
            RenderSettings.reflectionIntensity = 0f;

            // 4. Force all Renderers in the scene to ignore Light Probes and Reflection Probes
            Renderer[] allRenderers = FindObjectsOfType<Renderer>();
            int scrubbedCount = 0;
            foreach (Renderer rend in allRenderers)
            {
                // Disconnect from ghost probes
                rend.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                rend.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                
                // Ensure they aren't marked as static for lighting
                GameObjectUtility.SetStaticEditorFlags(rend.gameObject, 
                    GameObjectUtility.GetStaticEditorFlags(rend.gameObject) & ~StaticEditorFlags.ContributeGI);
                
                scrubbedCount++;
            }

            // 5. Force update the scene
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            
            Debug.Log($"✅ Nuke Complete! Scrubbed {scrubbedCount} renderers and cleared all ambient/probe data. The scene should now be pitch black.");
        }
    }
}
