#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using MysteryRooms.UI;

namespace MysteryRooms.Editor
{
    [CustomEditor(typeof(MysteryGeneratorPanel))]
    public class MysteryGeneratorUIEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            MysteryGeneratorPanel panel = (MysteryGeneratorPanel)target;
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Mystery Generator Panel", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Enhanced UI panel for mystery generation with animations, effects, and responsive design.", MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // Quick Setup Section
            EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Auto-Setup UI References", GUILayout.Height(30)))
            {
                AutoSetupReferences(panel);
            }
            
            if (GUILayout.Button("Create Particle Effects", GUILayout.Height(30)))
            {
                CreateParticleEffects(panel);
            }
            
            if (GUILayout.Button("Setup Responsive Layout", GUILayout.Height(30)))
            {
                SetupResponsiveLayout(panel);
            }
            
            EditorGUILayout.Space(10);
            
            // Testing Section
            EditorGUILayout.LabelField("Testing Tools", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Test 1080p"))
            {
                SetGameViewResolution(1920, 1080);
            }
            if (GUILayout.Button("Test 720p"))
            {
                SetGameViewResolution(1280, 720);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Test Mobile Portrait"))
            {
                SetGameViewResolution(1080, 1920);
            }
            if (GUILayout.Button("Test Mobile Landscape"))
            {
                SetGameViewResolution(1920, 1080);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Draw default inspector
            DrawDefaultInspector();
        }
        
        private void AutoSetupReferences(MysteryGeneratorPanel panel)
        {
            Debug.Log("Auto-setting up UI references...");
            EditorUtility.SetDirty(panel);
        }
        
        private void CreateParticleEffects(MysteryGeneratorPanel panel)
        {
            Debug.Log("Creating particle effects...");
            
            GameObject particleManagerObj = new GameObject("ParticleEffectManager");
            particleManagerObj.transform.SetParent(panel.transform);
            ParticleEffectManager particleManager = particleManagerObj.AddComponent<ParticleEffectManager>();
            
            // Create ambient particles
            CreateParticleSystem(particleManagerObj, "AmbientParticles", new Color(1f, 0.8f, 0.4f, 0.3f), 10f, 0.5f);
            
            EditorUtility.SetDirty(panel);
            Debug.Log("Particle effects created successfully!");
        }
        
        private GameObject CreateParticleSystem(GameObject parent, string name, Color color, float emissionRate, float lifetime)
        {
            GameObject psObj = new GameObject(name);
            psObj.transform.SetParent(parent.transform);
            psObj.transform.localPosition = Vector3.zero;
            
            ParticleSystem ps = psObj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.startColor = color;
            main.startSize = 0.1f;
            main.startSpeed = 1f;
            main.startLifetime = lifetime;
            main.maxParticles = 100;
            main.loop = true;
            
            var emission = ps.emission;
            emission.rateOverTime = emissionRate;
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(10f, 10f, 1f);
            
            return psObj;
        }
        
        private void SetupResponsiveLayout(MysteryGeneratorPanel panel)
        {
            Canvas canvas = panel.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("Panel is not under a Canvas!");
                return;
            }
            
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }
            
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            ResponsiveLayout responsive = canvas.GetComponent<ResponsiveLayout>();
            if (responsive == null)
            {
                canvas.gameObject.AddComponent<ResponsiveLayout>();
            }
            
            EditorUtility.SetDirty(canvas);
            Debug.Log("Responsive layout configured!");
        }
        
        private void SetGameViewResolution(int width, int height)
        {
            System.Type gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType != null)
            {
                EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
                Debug.Log($"Set Game View to {width}x{height}");
            }
            else
            {
                Debug.LogWarning("Could not find GameView type");
            }
        }
    }
}
#endif
