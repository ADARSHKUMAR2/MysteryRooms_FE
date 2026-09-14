#if UNITY_EDITOR
// ============================================================
// VictoryScreenUIEditor.cs
// Folder: Assets/Scripts/Editor/
// Assembly: MysteryRooms.Editor (MysteryRooms.Editor.asmdef)
//
// PURPOSE:
//   Custom Inspector for the VictoryScreenUI component.
//   Adds four sections above the default field list:
//
//   1. QUICK SETUP
//      - Auto-Wire References        → finds all child TMP/Image/Button
//                                      components by name and assigns them
//      - Add Victory Particles       → spawns a ParticleSystem child for
//                                      the confetti/sand burst on victory
//      - Add Victory Audio Source    → ensures an AudioSource exists on
//                                      the same GameObject
//
//   2. PREFAB HELPERS
//      - Validate Prefab Slots       → checks playerRowPrefab and
//                                      achievementBadgePrefab are assigned,
//                                      logs warnings if missing
//      - Open Victory UI Builder     → shortcut to open the EditorWindow
//
//   3. PREVIEW TOOLS (Play Mode only)
//      - Preview Victory Screen      → calls ShowVictoryScreen() with a
//                                      fake session ID so designers can
//                                      see the fallback layout instantly
//      - Preview Score Animation     → triggers just the score count-up
//                                      coroutine with a test value
//      - Toggle Panel Visibility     → activates/deactivates the panel
//                                      without entering play mode
//
//   4. TESTING TOOLS
//      - Test 1080p / 720p           → logs resolution hints
//      - Test Mobile Portrait / Landscape → logs resolution hints
//
//   Then DrawDefaultInspector() renders all [SerializeField] fields.
//
// PATTERN:
//   Matches MysteryGeneratorUIEditor.cs exactly:
//   - #if UNITY_EDITOR guard
//   - namespace MysteryRooms.Editor
//   - [CustomEditor(typeof(...))]
//   - EditorGUILayout sections with boldLabel + HelpBox
//   - GUILayout.Button with Height(30)
//   - SerializedObject + FindProperty + objectReferenceValue for wiring
//   - EditorUtility.SetDirty after mutations
//   - SetGameViewResolution via reflection (same implementation)
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using MysteryRooms.UI;

namespace MysteryRooms.Editor
{
    [CustomEditor(typeof(VictoryScreenUI))]
    public class VictoryScreenUIEditor : UnityEditor.Editor
    {
        // ================================================================
        // OnInspectorGUI — the custom Inspector layout
        // ================================================================

        public override void OnInspectorGUI()
        {
            VictoryScreenUI screen = (VictoryScreenUI)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Victory Screen UI", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Central controller for the winning screen.\n" +
                "Fetches victory data from the backend and populates all UI panels.\n\n" +
                "Use 'Auto-Wire References' after running the Victory UI Builder to " +
                "assign all child TMP/Image/Button components automatically.",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            // ── Section 1: Quick Setup ────────────────────────────────
            EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);

            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Auto-Wire References", GUILayout.Height(34)))
                AutoWireReferences(screen);
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("Add Victory Particles", GUILayout.Height(30)))
                AddVictoryParticles(screen);

            if (GUILayout.Button("Add Victory Audio Source", GUILayout.Height(30)))
                AddVictoryAudioSource(screen);

            EditorGUILayout.Space(10);

            // ── Section 2: Prefab Helpers ─────────────────────────────
            EditorGUILayout.LabelField("Prefab Helpers", EditorStyles.boldLabel);

            if (GUILayout.Button("Validate Prefab Slots", GUILayout.Height(30)))
                ValidatePrefabSlots(screen);

            GUI.backgroundColor = new Color(0.4f, 0.7f, 1.0f);
            if (GUILayout.Button("Open Victory UI Builder Window", GUILayout.Height(30)))
                VictoryScreenUIBuilder.ShowWindow();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            // ── Section 3: Preview Tools ──────────────────────────────
            EditorGUILayout.LabelField("Preview Tools", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Preview Victory Screen and Score Animation require Play Mode.",
                    MessageType.Warning
                );
            }

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);

            GUI.backgroundColor = new Color(1.0f, 0.75f, 0.2f);
            if (GUILayout.Button("Preview Victory Screen (Fallback)", GUILayout.Height(34)))
                PreviewVictoryScreen(screen);
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("Preview Score Count Animation", GUILayout.Height(30)))
                PreviewScoreAnimation(screen);

            EditorGUI.EndDisabledGroup();

            // Toggle panel visibility works in both Edit and Play mode
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Show Panel", GUILayout.Height(30)))
                TogglePanel(screen, true);

            if (GUILayout.Button("Hide Panel", GUILayout.Height(30)))
                TogglePanel(screen, false);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // ── Section 4: Testing Tools ──────────────────────────────
            EditorGUILayout.LabelField("Testing Tools", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Test 1080p"))
                SetGameViewResolution(1920, 1080);
            if (GUILayout.Button("Test 720p"))
                SetGameViewResolution(1280, 720);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Test Mobile Portrait"))
                SetGameViewResolution(1080, 1920);
            if (GUILayout.Button("Test Mobile Landscape"))
                SetGameViewResolution(1920, 1080);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // ── Default Inspector fields ──────────────────────────────
            // Renders all [SerializeField] and [Header] fields as normal
            DrawDefaultInspector();
        }

        // ================================================================
        // Quick Setup — Auto-Wire References
        //
        // Walks the VictoryScreenPanel hierarchy and assigns every
        // [SerializeField] field by matching child GameObject names.
        // Exactly mirrors how ConfigureReferences() works in the Builder,
        // but runs from the Inspector button after the fact.
        // ================================================================

        private void AutoWireReferences(VictoryScreenUI screen)
        {
            SerializedObject so = new SerializedObject(screen);
            Transform root = screen.transform;

            // Helper: find a child by path relative to the panel root
            // and assign its component to the named serialized field.
            void Assign<T>(string field, string path) where T : Component
            {
                var found = root.Find(path)?.GetComponent<T>();
                AssignField(so, field, found);
            }

            void AssignGO(string field, string path)
            {
                var found = root.Find(path)?.gameObject;
                AssignField(so, field, found);
            }

            void AssignTransform(string field, string path)
            {
                var found = root.Find(path);
                AssignField(so, field, found);
            }

            // ── Root panel itself ──
            AssignField(so, "victoryScreenPanel", screen.gameObject);

            // ── Root panels ──
            AssignGO("loadingSpinner",  "LoadingSpinner");
            AssignGO("contentRoot",     "ContentRoot");

            // ── Theme banner ──
            Assign<Image>("themeBannerImage", "ThemeBanner");

            // ── Header ──
            Assign<TextMeshProUGUI>("victoryMessageText",   "ContentRoot/HeaderSection/VictoryMessageText");
            Assign<TextMeshProUGUI>("secondaryMessageText", "ContentRoot/HeaderSection/SecondaryMessageText");
            Assign<TextMeshProUGUI>("rankTitleText",        "ContentRoot/HeaderSection/RankTitleText");

            // ── Statistics ──
            Assign<TextMeshProUGUI>("completionTimeText",   "ContentRoot/StatsSection/CompletionTimeText");
            Assign<TextMeshProUGUI>("timeRemainingText",    "ContentRoot/StatsSection/TimeRemainingText");
            Assign<TextMeshProUGUI>("puzzlesSolvedText",    "ContentRoot/StatsSection/PuzzlesSolvedText");
            Assign<TextMeshProUGUI>("hintsUsedText",        "ContentRoot/StatsSection/HintsUsedText");
            Assign<TextMeshProUGUI>("successRateText",      "ContentRoot/StatsSection/SuccessRateText");
            AssignGO("newRecordBadge",                      "ContentRoot/StatsSection/NewRecordBadge");
            Assign<TextMeshProUGUI>("previousBestText",     "ContentRoot/StatsSection/PreviousBestText");

            // ── Score breakdown ──
            Assign<TextMeshProUGUI>("baseScoreText",            "ContentRoot/ScoreSection/BaseScoreText/Value");
            Assign<TextMeshProUGUI>("timeBonusText",            "ContentRoot/ScoreSection/TimeBonusText/Value");
            Assign<TextMeshProUGUI>("difficultyMultiplierText", "ContentRoot/ScoreSection/DifficultyMultiplierText/Value");
            Assign<TextMeshProUGUI>("hintPenaltyText",          "ContentRoot/ScoreSection/HintPenaltyText/Value");
            AssignGO("teamBonusRow",                            "ContentRoot/ScoreSection/TeamBonusRow");
            Assign<TextMeshProUGUI>("teamBonusText",            "ContentRoot/ScoreSection/TeamBonusRow/TeamBonusText/Value");
            Assign<TextMeshProUGUI>("finalScoreText",           "ContentRoot/ScoreSection/FinalScoreText");

            // ── Rewards ──
            Assign<TextMeshProUGUI>("coinsEarnedText", "ContentRoot/RewardsSection/CoinsEarnedText");
            Assign<TextMeshProUGUI>("xpEarnedText",    "ContentRoot/RewardsSection/XpEarnedText");
            AssignTransform("achievementsContainer",   "ContentRoot/RewardsSection/AchievementsScrollView/Viewport/Content");

            // ── Players ──
            AssignTransform("playerListContainer", "ContentRoot/PlayersSection/PlayerListScrollView/Viewport/Content");

            // ── Buttons ──
            Assign<Button>("playAgainButton", "ButtonsRow/PlayAgainButton");
            Assign<Button>("mainMenuButton",  "ButtonsRow/MainMenuButton");

            // ── Animator ──
            AssignField(so, "victoryPanelAnimator", screen.GetComponent<Animator>());

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(screen);

            // Count how many fields were successfully wired
            int wired   = CountWiredFields(so);
            int total   = 22; // total [SerializeField] refs the builder creates
            bool allOk  = wired >= total;

            if (allOk)
            {
                Debug.Log($"<color=green>[VictoryScreenUIEditor] ✅ Auto-wire complete. All {wired} references assigned.</color>");
                EditorUtility.DisplayDialog(
                    "Auto-Wire Complete",
                    $"✅ All {wired} references assigned successfully!\n\n" +
                    "Remaining manual steps:\n" +
                    "• Drag PlayerRow prefab → 'Player Row Prefab'\n" +
                    "• Drag AchievementBadge prefab → 'Achievement Badge Prefab'\n" +
                    "• Drag VictoryAPIService component → 'Victory API Service'\n" +
                    "• Assign theme sprites to the Theme Sprites list\n" +
                    "• Assign star Filled/Empty sprites\n" +
                    "• Assign Audio Clip for victory fanfare",
                    "Got it!"
                );
            }
            else
            {
                Debug.LogWarning(
                    $"[VictoryScreenUIEditor] ⚠️ Auto-wire partial: {wired}/{total} references found. " +
                    "Run the Victory UI Builder first to generate the full hierarchy."
                );
                EditorUtility.DisplayDialog(
                    "Auto-Wire Partial",
                    $"⚠️ Only {wired}/{total} references found.\n\n" +
                    "Some child GameObjects are missing from the hierarchy.\n\n" +
                    "Run 'Mystery Rooms → Create Victory Screen UI' first to generate the full hierarchy, " +
                    "then click Auto-Wire References again.",
                    "OK"
                );
            }
        }

        // Counts how many serialized object-reference fields are non-null
        private int CountWiredFields(SerializedObject so)
        {
            string[] fieldNames = new[]
            {
                "victoryScreenPanel", "loadingSpinner", "contentRoot",
                "themeBannerImage",
                "victoryMessageText", "secondaryMessageText", "rankTitleText",
                "completionTimeText", "timeRemainingText", "puzzlesSolvedText",
                "hintsUsedText", "successRateText",
                "newRecordBadge", "previousBestText",
                "baseScoreText", "timeBonusText", "difficultyMultiplierText",
                "hintPenaltyText", "teamBonusRow", "finalScoreText",
                "coinsEarnedText", "xpEarnedText",
                "achievementsContainer", "playerListContainer",
                "playAgainButton", "mainMenuButton",
                "victoryPanelAnimator"
            };

            int count = 0;
            foreach (string name in fieldNames)
            {
                var prop = so.FindProperty(name);
                if (prop != null && prop.objectReferenceValue != null)
                    count++;
            }
            return count;
        }

        // ================================================================
        // Quick Setup — Add Victory Particles
        //
        // Spawns a ParticleSystem child on the VictoryScreenUI GameObject
        // configured for a golden confetti / sand-dust burst effect.
        // Mirrors the CreateParticleSystem helper in MysteryGeneratorUIEditor.
        // ================================================================

        private void AddVictoryParticles(VictoryScreenUI screen)
        {
            // Don't add twice
            if (screen.transform.Find("VictoryParticles") != null)
            {
                Debug.LogWarning("[VictoryScreenUIEditor] VictoryParticles already exists on this GameObject.");
                return;
            }

            GameObject psObj = new GameObject("VictoryParticles");
            psObj.transform.SetParent(screen.transform, false);
            psObj.transform.localPosition = Vector3.zero;

            ParticleSystem ps = psObj.AddComponent<ParticleSystem>();

            // ── Main module — golden sand burst ──────────────────────
            var main = ps.main;
            main.startColor      = new ParticleSystem.MinMaxGradient(
                new Color(1.0f, 0.84f, 0.30f, 1f),
                new Color(0.9f, 0.60f, 0.15f, 1f)
            );
            main.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startLifetime   = new ParticleSystem.MinMaxCurve(1.5f, 3.0f);
            main.maxParticles    = 200;
            main.loop            = false;   // One-shot burst on victory
            main.playOnAwake     = false;   // VictoryScreenUI.PlayEntryEffects() calls .Play()
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.3f;

            // ── Emission — one large burst ────────────────────────────
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 80, 120, 1, 0.01f)
            });

            // ── Shape — wide spread across the screen top ─────────────
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale     = new Vector3(18f, 0.5f, 0.1f);
            shape.position  = new Vector3(0f, 5f, 0f); // Spawn above the panel

            // ── Velocity over lifetime — scatter sideways and down ─────
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space   = ParticleSystemSimulationSpace.World;
            vel.x       = new ParticleSystem.MinMaxCurve(-1f, 1f);
            vel.y       = new ParticleSystem.MinMaxCurve(-3f, -1f);

            // ── Rotation over lifetime — tumbling pieces ───────────────
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z       = new ParticleSystem.MinMaxCurve(-180f, 180f);

            // ── Size over lifetime — shrink at the end ─────────────────
            var sizeOL = ps.sizeOverLifetime;
            sizeOL.enabled = true;
            AnimationCurve shrink = new AnimationCurve();
            shrink.AddKey(0f, 1f);
            shrink.AddKey(0.7f, 1f);
            shrink.AddKey(1f, 0f);
            sizeOL.size = new ParticleSystem.MinMaxCurve(1f, shrink);

            // ── Assign to the victoryParticles field in VictoryScreenUI ──
            SerializedObject so = new SerializedObject(screen);
            AssignField(so, "victoryParticles", ps);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(screen);

            Undo.RegisterCreatedObjectUndo(psObj, "Add Victory Particles");
            Selection.activeGameObject = psObj;

            Debug.Log("<color=green>[VictoryScreenUIEditor] ✅ VictoryParticles added and wired.</color>");
        }

        // ================================================================
        // Quick Setup — Add Victory Audio Source
        //
        // Ensures the VictoryScreenUI GameObject has an AudioSource.
        // VictoryScreenUI.PlayEntryEffects() calls audioSource.PlayOneShot().
        // ================================================================

        private void AddVictoryAudioSource(VictoryScreenUI screen)
        {
            AudioSource existing = screen.GetComponent<AudioSource>();
            if (existing != null)
            {
                Debug.Log("[VictoryScreenUIEditor] AudioSource already present on VictoryScreenUI.");
                EditorUtility.DisplayDialog(
                    "AudioSource Already Exists",
                    "An AudioSource is already attached to this GameObject.\n\n" +
                    "Assign your victory fanfare AudioClip to the 'Victory Sound Clip' field in the Inspector.",
                    "OK"
                );
                return;
            }

            AudioSource source = screen.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D (UI sound)
            source.volume = 1f;

            EditorUtility.SetDirty(screen);
            Undo.RegisterCreatedObjectUndo(screen.gameObject, "Add Victory AudioSource");

            Debug.Log("<color=green>[VictoryScreenUIEditor] ✅ AudioSource added. Assign 'Victory Sound Clip' in the Inspector.</color>");
        }

        // ================================================================
        // Prefab Helpers — Validate Prefab Slots
        //
        // Checks that the two prefab fields (playerRowPrefab and
        // achievementBadgePrefab) are assigned and have the required
        // components on them. Logs detailed warnings for each issue.
        // ================================================================

        private void ValidatePrefabSlots(VictoryScreenUI screen)
        {
            SerializedObject so = new SerializedObject(screen);
            int issues = 0;
            string report = "";

            // ── Player Row Prefab ──
            var playerRowProp = so.FindProperty("playerRowPrefab");
            if (playerRowProp == null || playerRowProp.objectReferenceValue == null)
            {
                report += "❌ 'Player Row Prefab' is not assigned.\n   → Create it with 'Create PlayerRow Prefab Setup' in the Builder window.\n\n";
                issues++;
            }
            else
            {
                GameObject rowPrefab = playerRowProp.objectReferenceValue as GameObject;
                if (rowPrefab != null && rowPrefab.GetComponent<PlayerRowUI>() == null)
                {
                    report += "⚠️ 'Player Row Prefab' is assigned but has no PlayerRowUI component.\n   → Add PlayerRowUI.cs to the prefab root.\n\n";
                    issues++;
                }
                else
                {
                    report += "✅ Player Row Prefab — OK\n\n";
                }
            }

            // ── Achievement Badge Prefab ──
            var badgeProp = so.FindProperty("achievementBadgePrefab");
            if (badgeProp == null || badgeProp.objectReferenceValue == null)
            {
                report += "❌ 'Achievement Badge Prefab' is not assigned.\n   → Create it with 'Create AchievementBadge Prefab Setup' in the Builder window.\n\n";
                issues++;
            }
            else
            {
                GameObject badgePrefab = badgeProp.objectReferenceValue as GameObject;
                if (badgePrefab != null && badgePrefab.GetComponentInChildren<TextMeshProUGUI>() == null)
                {
                    report += "⚠️ 'Achievement Badge Prefab' has no TextMeshProUGUI child named 'Label'.\n   → VictoryScreenUI.PopulateRewards() needs it to set the achievement text.\n\n";
                    issues++;
                }
                else
                {
                    report += "✅ Achievement Badge Prefab — OK\n\n";
                }
            }

            // ── VictoryAPIService ──
            var apiProp = so.FindProperty("victoryAPIService");
            if (apiProp == null || apiProp.objectReferenceValue == null)
            {
                report += "❌ 'Victory API Service' is not assigned.\n   → Drag the VictoryAPIService component reference into this field.\n\n";
                issues++;
            }
            else
            {
                report += "✅ Victory API Service — OK\n\n";
            }

            // ── Summary ──
            if (issues == 0)
            {
                Debug.Log("<color=green>[VictoryScreenUIEditor] ✅ All prefab slots validated — no issues.</color>");
                EditorUtility.DisplayDialog("Validation Passed", "✅ All prefab slots are correctly assigned.\n\n" + report, "Great!");
            }
            else
            {
                Debug.LogWarning($"[VictoryScreenUIEditor] ⚠️ {issues} issue(s) found:\n{report}");
                EditorUtility.DisplayDialog($"Validation — {issues} Issue(s) Found", report, "OK");
            }
        }

        // ================================================================
        // Preview Tools — Preview Victory Screen
        //
        // In Play Mode: calls ShowVictoryScreen() via reflection so the
        // full screen activates using the fallback path (no real session ID).
        // Lets designers see the layout and animations without a live server.
        // ================================================================

        private void PreviewVictoryScreen(VictoryScreenUI screen)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[VictoryScreenUIEditor] Enter Play Mode first to preview the Victory Screen.");
                return;
            }

            // Force-activate the panel directly (bypasses the API call)
            SerializedObject so = new SerializedObject(screen);

            var panelProp = so.FindProperty("victoryScreenPanel");
            if (panelProp?.objectReferenceValue is GameObject panel)
                panel.SetActive(true);

            // Show fallback content via reflection (ShowFallback is private)
            var method = typeof(VictoryScreenUI).GetMethod(
                "ShowFallback",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            method?.Invoke(screen, null);

            // Also trigger PlayEntryEffects so particles + animator fire
            var effectsMethod = typeof(VictoryScreenUI).GetMethod(
                "PlayEntryEffects",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            effectsMethod?.Invoke(screen, null);

            Debug.Log("<color=cyan>[VictoryScreenUIEditor] 🎉 Victory Screen preview activated (fallback mode).</color>");
        }

        // ================================================================
        // Preview Tools — Preview Score Count Animation
        //
        // In Play Mode: fires the AnimateScoreCount coroutine with a
        // sample value of 2155 so designers can tune the duration/easing.
        // ================================================================

        private void PreviewScoreAnimation(VictoryScreenUI screen)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[VictoryScreenUIEditor] Enter Play Mode first to preview score animation.");
                return;
            }

            // Trigger AnimateScoreCount(0, 2155, duration) via reflection
            var method = typeof(VictoryScreenUI).GetMethod(
                "AnimateScoreCount",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );

            if (method != null)
            {
                // AnimateScoreCount is a coroutine — start it via MonoBehaviour
                var coroutine = method.Invoke(screen, new object[] { 0, 2155, 1.8f });
                screen.StartCoroutine((System.Collections.IEnumerator)coroutine);
                Debug.Log("<color=cyan>[VictoryScreenUIEditor] 🎯 Score count animation started: 0 → 2,155</color>");
            }
            else
            {
                Debug.LogWarning("[VictoryScreenUIEditor] Could not find AnimateScoreCount via reflection. Ensure the method is in VictoryScreenUI.");
            }
        }

        // ================================================================
        // Preview Tools — Toggle Panel Visibility
        //
        // Works in both Edit Mode and Play Mode.
        // Directly sets victoryScreenPanel.SetActive() so designers can
        // visually inspect the layout without entering Play Mode.
        // ================================================================

        private void TogglePanel(VictoryScreenUI screen, bool visible)
        {
            SerializedObject so = new SerializedObject(screen);
            var panelProp = so.FindProperty("victoryScreenPanel");

            if (panelProp?.objectReferenceValue is GameObject panel)
            {
                panel.SetActive(visible);
                EditorUtility.SetDirty(panel);
                Debug.Log($"[VictoryScreenUIEditor] VictoryScreenPanel → {(visible ? "VISIBLE" : "HIDDEN")}");
            }
            else
            {
                Debug.LogWarning("[VictoryScreenUIEditor] 'victoryScreenPanel' field is not assigned. Run Auto-Wire first.");
            }
        }

        // ================================================================
        // Testing Tools — SetGameViewResolution
        // Same reflection-based implementation as MysteryGeneratorUIEditor.
        // ================================================================

        private void SetGameViewResolution(int width, int height)
        {
            System.Type gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType != null)
            {
                EditorWindow.GetWindow(gameViewType);
                Debug.Log($"[VictoryScreenUIEditor] Game View → {width}×{height}. Set resolution manually in the Game tab dropdown.");
            }
            else
            {
                Debug.LogWarning("[VictoryScreenUIEditor] Could not find UnityEditor.GameView type.");
            }
        }

        // ================================================================
        // AssignField overloads — matches MysteryGeneratorUIBuilder pattern
        // ================================================================

        private void AssignField(SerializedObject so, string fieldName, Object value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null && value != null)
                prop.objectReferenceValue = value;
        }

        private void AssignField(SerializedObject so, string fieldName, Transform value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null && value != null)
                prop.objectReferenceValue = value;
        }

        // Local helper overloads used by AutoWireReferences above
        private void AssignGO(SerializedObject so, string field, string path)
        {
            var found = ((VictoryScreenUI)target).transform.Find(path)?.gameObject;
            AssignField(so, field, found);
        }

        private void AssignTransform(SerializedObject so, string field, string path)
        {
            var found = ((VictoryScreenUI)target).transform.Find(path);
            AssignField(so, field, found);
        }
    }
}
#endif
