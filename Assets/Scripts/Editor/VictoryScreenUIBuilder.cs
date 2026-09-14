#if UNITY_EDITOR
// ============================================================
// VictoryScreenUIBuilder.cs
// Folder: Assets/Scripts/Editor/
// Assembly: MysteryRooms.Editor (MysteryRooms.Editor.asmdef)
//
// PURPOSE:
//   EditorWindow that procedurally generates the complete
//   Victory Screen UI hierarchy in the active scene.
//
//   Accessible via: Mystery Rooms → Create Victory Screen UI
//
//   What it creates:
//     VictoryScreenCanvas (Canvas + Scaler + Raycaster)
//       └── FadeOverlay          (full-screen black Image)
//       └── VictoryScreenPanel   (starts INACTIVE, has VictoryScreenUI script)
//           ├── Background        (dark Egyptian-themed overlay)
//           ├── ThemeBanner       (theme-specific image strip)
//           ├── LoadingSpinner    (shown during API fetch)
//           ├── ContentRoot       (hidden during load, shown after)
//           │   ├── HeaderSection
//           │   │   ├── RankTitleText
//           │   │   ├── VictoryMessageText
//           │   │   └── SecondaryMessageText
//           │   ├── StatsSection
//           │   │   ├── CompletionTimeText
//           │   │   ├── TimeRemainingText
//           │   │   ├── PuzzlesSolvedText
//           │   │   ├── HintsUsedText
//           │   │   ├── SuccessRateText
//           │   │   ├── NewRecordBadge
//           │   │   ├── PreviousBestText
//           │   │   └── DifficultyStars (5 star Images)
//           │   ├── ScoreSection
//           │   │   ├── BaseScoreText
//           │   │   ├── TimeBonusText
//           │   │   ├── DifficultyMultiplierText
//           │   │   ├── HintPenaltyText
//           │   │   ├── TeamBonusRow
//           │   │   └── FinalScoreText
//           │   ├── RewardsSection
//           │   │   ├── CoinsEarnedText
//           │   │   ├── XpEarnedText
//           │   │   └── AchievementsContainer (ScrollView content)
//           │   └── PlayersSection
//           │       └── PlayerListContainer   (ScrollView content)
//           └── ButtonsRow
//               ├── PlayAgainButton
//               └── MainMenuButton
//
// PATTERN:
//   Matches MysteryGeneratorUIBuilder.cs exactly:
//   - EditorWindow + [MenuItem]
//   - scrollPosition + OnGUI
//   - Undo.RegisterCreatedObjectUndo on every object
//   - SerializedObject + FindProperty + AssignField for script refs
//   - AssetDatabase.GetBuiltinExtraResource for sprites
//   - AnimatedButton on every button
//   - FindObjectOfType<Canvas> reuse
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using MysteryRooms.UI;

namespace MysteryRooms.Editor
{
    public class VictoryScreenUIBuilder : EditorWindow
    {
        // ── Window state ─────────────────────────────────────────────
        private Vector2 _scrollPosition;

        // ── Theme color constants (Egyptian gold palette) ─────────────
        private static readonly Color GoldPrimary    = new Color(0.80f, 0.60f, 0.30f, 1.00f);
        private static readonly Color GoldAccent     = new Color(1.00f, 0.84f, 0.40f, 1.00f);
        private static readonly Color DarkBackground = new Color(0.08f, 0.05f, 0.03f, 0.97f);
        private static readonly Color PanelDark      = new Color(0.12f, 0.08f, 0.04f, 0.95f);
        private static readonly Color SectionBg      = new Color(0.15f, 0.10f, 0.06f, 0.90f);
        private static readonly Color TextWhite      = new Color(0.95f, 0.92f, 0.88f, 1.00f);
        private static readonly Color TextGold       = new Color(0.80f, 0.60f, 0.30f, 1.00f);
        private static readonly Color GreenReward    = new Color(0.30f, 0.80f, 0.40f, 1.00f);
        private static readonly Color BlueBonus      = new Color(0.30f, 0.60f, 1.00f, 1.00f);
        private static readonly Color RedPenalty     = new Color(0.90f, 0.30f, 0.30f, 1.00f);
        private static readonly Color MvpGold        = new Color(1.00f, 0.84f, 0.00f, 0.30f);

        // ================================================================
        // Menu Item
        // ================================================================

        [MenuItem("Mystery Rooms/Create Victory Screen UI")]
        public static void ShowWindow()
        {
            VictoryScreenUIBuilder window = GetWindow<VictoryScreenUIBuilder>("Victory UI Builder");
            window.minSize = new Vector2(420, 640);
            window.Show();
        }

        // ================================================================
        // Editor Window GUI
        // ================================================================

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Victory Screen UI Builder", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Generates the complete Victory Screen UI hierarchy in your active scene.\n\n" +
                "What gets created:\n" +
                "• VictoryScreenCanvas  (Canvas + Scaler + Raycaster)\n" +
                "• FadeOverlay          (full-screen black fade Image)\n" +
                "• VictoryScreenPanel   (starts INACTIVE — shown on victory)\n" +
                "  ├─ Header            (rank title + victory messages)\n" +
                "  ├─ Statistics        (time, puzzles, hints, new record)\n" +
                "  ├─ Score Breakdown   (animated final score + line items)\n" +
                "  ├─ Rewards           (coins, XP, achievement badges)\n" +
                "  ├─ Player Leaderboard (one row per player)\n" +
                "  └─ Buttons           (Play Again + Main Menu)\n\n" +
                "All VictoryScreenUI script references are auto-wired.",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            // ── Main build button ─────────────────────────────────────
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Generate Complete Victory Screen UI", GUILayout.Height(50)))
                GenerateCompleteUI();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(20);

            // ── Individual component buttons ──────────────────────────
            EditorGUILayout.LabelField("Individual Components", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Canvas Only", GUILayout.Height(30)))
                CreateCanvas();

            if (GUILayout.Button("Create PlayerRow Prefab Setup", GUILayout.Height(30)))
                CreatePlayerRowPrefabSetup();

            if (GUILayout.Button("Create AchievementBadge Prefab Setup", GUILayout.Height(30)))
                CreateAchievementBadgePrefabSetup();

            EditorGUILayout.Space(20);

            // ── Utilities ─────────────────────────────────────────────
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);

            if (GUILayout.Button("Setup Canvas Scaler (1920×1080)", GUILayout.Height(30)))
                SetupCanvasSettings();

            if (GUILayout.Button("Import TextMeshPro Essentials", GUILayout.Height(30)))
                ImportTMPEssentials();

            EditorGUILayout.EndScrollView();
        }

        // ================================================================
        // Generate Complete UI
        // ================================================================

        private void GenerateCompleteUI()
        {
            if (!EditorUtility.DisplayDialog(
                    "Generate Victory Screen UI",
                    "This will create the complete Victory Screen hierarchy in your scene.\n\nContinue?",
                    "Yes", "Cancel"))
                return;

            // Build top-down
            GameObject canvas       = CreateCanvas();
            GameObject fadeOverlay  = CreateFadeOverlay(canvas);
            GameObject panel        = CreateVictoryPanel(canvas);
            GameObject background   = CreateBackground(panel);
            GameObject themeBanner  = CreateThemeBanner(panel);
            GameObject spinner      = CreateLoadingSpinner(panel);
            GameObject contentRoot  = CreateContentRoot(panel);

            // Content sections (children of contentRoot)
            GameObject headerSection  = CreateHeaderSection(contentRoot);
            GameObject statsSection   = CreateStatsSection(contentRoot);
            GameObject scoreSection   = CreateScoreSection(contentRoot);
            GameObject rewardsSection = CreateRewardsSection(contentRoot);
            GameObject playersSection = CreatePlayersSection(contentRoot);
            GameObject buttonsRow     = CreateButtonsRow(panel);

            // Wire all references into the VictoryScreenUI component
            ConfigureReferences(panel, fadeOverlay, spinner, contentRoot,
                headerSection, statsSection, scoreSection, rewardsSection,
                playersSection, buttonsRow);

            Selection.activeGameObject = panel;
            EditorGUIUtility.PingObject(panel);

            Debug.Log("✅ Victory Screen UI created successfully!");
            EditorUtility.DisplayDialog(
                "Victory Screen UI Created!",
                "Complete hierarchy generated and all script references auto-wired.\n\n" +
                "Next steps:\n" +
                "1. Create a PlayerRow prefab and drag it to 'Player Row Prefab'\n" +
                "2. Create an AchievementBadge prefab and drag it to 'Achievement Badge Prefab'\n" +
                "3. Assign theme sprites to the Theme Sprites list\n" +
                "4. Assign star Filled/Empty sprites to the Difficulty Stars section\n" +
                "5. Drag a VictoryAPIService component reference into the script\n" +
                "6. Assign Audio Clips for victory fanfare",
                "Got it!");
        }

        // ================================================================
        // Canvas
        // ================================================================

        private GameObject CreateCanvas()
        {
            // Reuse an existing Canvas if present (same pattern as MysteryGeneratorUIBuilder)
            Canvas existing = FindObjectOfType<Canvas>();
            if (existing != null)
            {
                Debug.Log("[VictoryScreenUIBuilder] Using existing Canvas.");
                return existing.gameObject;
            }

            GameObject canvasObj = new GameObject("VictoryScreenCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10; // Above game world, below critical overlays

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem — only create if absent
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject evtObj = new GameObject("EventSystem");
                evtObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                evtObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(evtObj, "Create EventSystem");
            }

            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Victory Canvas");
            Debug.Log("✅ Canvas created.");
            return canvasObj;
        }

        // ================================================================
        // Fade Overlay — full-screen black Image used for fade-to-black
        // ================================================================

        private GameObject CreateFadeOverlay(GameObject canvas)
        {
            GameObject obj = new GameObject("FadeOverlay");
            obj.transform.SetParent(canvas.transform, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            Image img = obj.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f); // Starts fully transparent
            img.raycastTarget = false;               // Doesn't block input

            // Put the overlay above the panel in sibling order
            obj.transform.SetAsLastSibling();

            Undo.RegisterCreatedObjectUndo(obj, "Create Fade Overlay");
            return obj;
        }

        // ================================================================
        // Victory Panel Root
        // ================================================================

        private GameObject CreateVictoryPanel(GameObject canvas)
        {
            GameObject panelObj = new GameObject("VictoryScreenPanel");
            panelObj.transform.SetParent(canvas.transform, false);

            // Full-screen stretch
            RectTransform rect = panelObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            CanvasGroup group = panelObj.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;

            // Animator — FinalDoorController triggers "Show" via this
            Animator animator = panelObj.AddComponent<Animator>();

            // Attach the runtime script
            panelObj.AddComponent<VictoryScreenUI>();

            // Starts INACTIVE — only activated on victory
            panelObj.SetActive(false);

            Undo.RegisterCreatedObjectUndo(panelObj, "Create Victory Panel");
            Debug.Log("✅ Victory Panel root created.");
            return panelObj;
        }

        // ================================================================
        // Background — dark Egyptian overlay behind all content
        // ================================================================

        private GameObject CreateBackground(GameObject panel)
        {
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(panel.transform, false);

            RectTransform rect = bgObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            Image img = bgObj.AddComponent<Image>();
            img.color = DarkBackground;
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            img.type = Image.Type.Sliced;

            bgObj.transform.SetAsFirstSibling(); // Always behind everything

            Undo.RegisterCreatedObjectUndo(bgObj, "Create Background");
            return bgObj;
        }

        // ================================================================
        // Theme Banner — horizontal gold strip at the top for the theme image
        // ================================================================

        private GameObject CreateThemeBanner(GameObject panel)
        {
            GameObject bannerObj = new GameObject("ThemeBanner");
            bannerObj.transform.SetParent(panel.transform, false);

            RectTransform rect = bannerObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.88f);
            rect.anchorMax = new Vector2(1f, 1.00f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            Image img = bannerObj.AddComponent<Image>();
            img.color = new Color(0.15f, 0.10f, 0.05f, 1f);
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;

            Undo.RegisterCreatedObjectUndo(bannerObj, "Create Theme Banner");
            return bannerObj;
        }

        // ================================================================
        // Loading Spinner — shown while fetching victory API response
        // ================================================================

        private GameObject CreateLoadingSpinner(GameObject panel)
        {
            GameObject spinnerObj = new GameObject("LoadingSpinner");
            spinnerObj.transform.SetParent(panel.transform, false);

            RectTransform rect = spinnerObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(100f, 100f);
            rect.anchoredPosition = Vector2.zero;

            Image img = spinnerObj.AddComponent<Image>();
            img.color = GoldPrimary;
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            spinnerObj.AddComponent<LoadingSpinnerRotation>(); // Reuses existing helper class

            spinnerObj.SetActive(false); // Hidden until load begins
            Undo.RegisterCreatedObjectUndo(spinnerObj, "Create Loading Spinner");
            return spinnerObj;
        }

        // ================================================================
        // Content Root — parent of all visible sections; hidden during load
        // ================================================================

        private GameObject CreateContentRoot(GameObject panel)
        {
            GameObject rootObj = new GameObject("ContentRoot");
            rootObj.transform.SetParent(panel.transform, false);

            RectTransform rect = rootObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.0f);
            rect.anchorMax = new Vector2(1f, 0.88f); // Below theme banner
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            Undo.RegisterCreatedObjectUndo(rootObj, "Create Content Root");
            return rootObj;
        }

        // ================================================================
        // Header Section — rank title + both message lines
        // ================================================================

        private GameObject CreateHeaderSection(GameObject contentRoot)
        {
            GameObject sectionObj = MakeSection(contentRoot, "HeaderSection",
                new Vector2(0f, 0.78f), new Vector2(1f, 1.00f), SectionBg);

            // Rank title (e.g. "Master Detective") — gold, large
            CreateTMP(sectionObj, "RankTitleText",
                new Vector2(0.05f, 0.58f), new Vector2(0.95f, 0.95f),
                "MASTER DETECTIVE", 42f, FontStyles.Bold, GoldAccent,
                TextAlignmentOptions.Center);

            // Primary victory message — smaller, white
            CreateTMP(sectionObj, "VictoryMessageText",
                new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.60f),
                "The ancient curse has been lifted! Excellent work!", 22f,
                FontStyles.Normal, TextWhite, TextAlignmentOptions.Center);

            // Secondary message — smallest, muted
            CreateTMP(sectionObj, "SecondaryMessageText",
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.32f),
                "Solo victory! You conquered the mystery alone!", 18f,
                FontStyles.Italic, new Color(0.75f, 0.72f, 0.68f),
                TextAlignmentOptions.Center);

            Undo.RegisterCreatedObjectUndo(sectionObj, "Create Header Section");
            return sectionObj;
        }

        // ================================================================
        // Statistics Section — time, puzzles, hints, stars, new record
        // ================================================================

        private GameObject CreateStatsSection(GameObject contentRoot)
        {
            GameObject sectionObj = MakeSection(contentRoot, "StatsSection",
                new Vector2(0f, 0.50f), new Vector2(0.50f, 0.78f), SectionBg);

            // Section header
            CreateSectionHeader(sectionObj, "STATISTICS");

            // Stat rows — anchor rows stacked inside the section
            CreateTMP(sectionObj, "CompletionTimeText",
                new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.88f),
                "Time: 20:45", 24f, FontStyles.Bold, GoldAccent,
                TextAlignmentOptions.Left);

            CreateTMP(sectionObj, "TimeRemainingText",
                new Vector2(0.05f, 0.58f), new Vector2(0.95f, 0.73f),
                "09:15 remaining", 20f, FontStyles.Normal, TextWhite,
                TextAlignmentOptions.Left);

            CreateTMP(sectionObj, "PuzzlesSolvedText",
                new Vector2(0.05f, 0.44f), new Vector2(0.95f, 0.59f),
                "5 / 5 Puzzles", 20f, FontStyles.Normal, TextWhite,
                TextAlignmentOptions.Left);

            CreateTMP(sectionObj, "HintsUsedText",
                new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.45f),
                "No Hints Used!", 20f, FontStyles.Normal, TextWhite,
                TextAlignmentOptions.Left);

            CreateTMP(sectionObj, "SuccessRateText",
                new Vector2(0.05f, 0.16f), new Vector2(0.95f, 0.31f),
                "62.5% Success Rate", 20f, FontStyles.Normal, TextWhite,
                TextAlignmentOptions.Left);

            // New Record badge — small gold pill, starts hidden
            GameObject newRecordBadge = MakePanel(sectionObj, "NewRecordBadge",
                new Vector2(0.60f, 0.03f), new Vector2(0.98f, 0.16f),
                new Color(1f, 0.7f, 0f, 0.9f));
            CreateTMP(newRecordBadge, "Text",
                Vector2.zero, Vector2.one,
                "⭐ NEW RECORD!", 18f, FontStyles.Bold,
                new Color(0.1f, 0.05f, 0f), TextAlignmentOptions.Center);
            newRecordBadge.SetActive(false); // Hidden until is_new_record = true

            // Previous best text — shown only when NOT a new record
            CreateTMP(sectionObj, "PreviousBestText",
                new Vector2(0.05f, 0.03f), new Vector2(0.60f, 0.16f),
                "Previous best: 25:00", 16f, FontStyles.Italic,
                new Color(0.6f, 0.57f, 0.54f), TextAlignmentOptions.Left);

            // Difficulty stars — 5 Image GameObjects in a row
            CreateDifficultyStars(sectionObj);

            Undo.RegisterCreatedObjectUndo(sectionObj, "Create Stats Section");
            return sectionObj;
        }

        // ── 5 difficulty star Images ──────────────────────────────────

        private void CreateDifficultyStars(GameObject parent)
        {
            GameObject starsRoot = new GameObject("DifficultyStarsRow");
            starsRoot.transform.SetParent(parent.transform, false);

            RectTransform rowRect = starsRoot.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.05f, 0.88f);
            rowRect.anchorMax = new Vector2(0.55f, 0.98f);
            rowRect.sizeDelta = Vector2.zero;

            // 5 evenly spaced star Images
            for (int i = 0; i < 5; i++)
            {
                GameObject starObj = new GameObject($"Star_{i + 1}");
                starObj.transform.SetParent(starsRoot.transform, false);

                RectTransform starRect = starObj.AddComponent<RectTransform>();
                float xMin = i * 0.20f;
                starRect.anchorMin = new Vector2(xMin, 0f);
                starRect.anchorMax = new Vector2(xMin + 0.18f, 1f);
                starRect.sizeDelta = Vector2.zero;

                Image starImg = starObj.AddComponent<Image>();
                starImg.color = i == 0 ? GoldAccent : new Color(0.4f, 0.35f, 0.3f); // First star filled as default
                starImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

                Undo.RegisterCreatedObjectUndo(starObj, $"Create Star {i + 1}");
            }

            Undo.RegisterCreatedObjectUndo(starsRoot, "Create Difficulty Stars Row");
        }

        // ================================================================
        // Score Section — line items + animated final score
        // ================================================================

        private GameObject CreateScoreSection(GameObject contentRoot)
        {
            GameObject sectionObj = MakeSection(contentRoot, "ScoreSection",
                new Vector2(0.50f, 0.50f), new Vector2(1.00f, 0.78f), SectionBg);

            CreateSectionHeader(sectionObj, "SCORE");

            // Base score row
            CreateScoreRow(sectionObj, "BaseScoreText",   new Vector2(0f, 0.80f), new Vector2(1f, 0.93f), "Base Score",   "1,000",  TextWhite);
            CreateScoreRow(sectionObj, "TimeBonusText",    new Vector2(0f, 0.66f), new Vector2(1f, 0.80f), "Time Bonus",   "+555",   GreenReward);
            CreateScoreRow(sectionObj, "DifficultyMultiplierText", new Vector2(0f, 0.52f), new Vector2(1f, 0.66f), "Difficulty",   "x1.5",   BlueBonus);
            CreateScoreRow(sectionObj, "HintPenaltyText", new Vector2(0f, 0.38f), new Vector2(1f, 0.52f), "Hint Penalty", "No Penalty", RedPenalty);

            // Team bonus row — hidden if solo (team_bonus == 0)
            GameObject teamBonusRow = MakePanel(sectionObj, "TeamBonusRow",
                new Vector2(0f, 0.24f), new Vector2(1f, 0.38f), Color.clear);
            CreateScoreRow(teamBonusRow, "TeamBonusText",
                Vector2.zero, Vector2.one,
                "Team Bonus", "+200", GoldAccent);
            teamBonusRow.SetActive(false); // Hidden for solo games

            // Divider line
            GameObject divider = MakePanel(sectionObj, "Divider",
                new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.22f), GoldPrimary);

            // Final score — large, animated, gold
            CreateTMP(sectionObj, "FinalScoreText",
                new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.22f),
                "2,155", 52f, FontStyles.Bold, GoldAccent,
                TextAlignmentOptions.Center);

            Undo.RegisterCreatedObjectUndo(sectionObj, "Create Score Section");
            return sectionObj;
        }

        // ── Helper: creates a two-column score row (label left, value right) ──

        private void CreateScoreRow(GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            string label, string value, Color valueColor)
        {
            GameObject rowObj = new GameObject(name);
            rowObj.transform.SetParent(parent.transform, false);

            RectTransform rect = rowObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = Vector2.zero;

            // Label (left half)
            CreateTMP(rowObj, "Label",
                new Vector2(0.05f, 0f), new Vector2(0.65f, 1f),
                label, 18f, FontStyles.Normal,
                new Color(0.75f, 0.72f, 0.68f), TextAlignmentOptions.Left);

            // Value (right half) — this is the TMP that the script writes to
            CreateTMP(rowObj, "Value",
                new Vector2(0.65f, 0f), new Vector2(0.98f, 1f),
                value, 20f, FontStyles.Bold, valueColor,
                TextAlignmentOptions.Right);

            Undo.RegisterCreatedObjectUndo(rowObj, $"Create Score Row {name}");
        }

        // ================================================================
        // Rewards Section — coins, XP, achievement badges ScrollView
        // ================================================================

        private GameObject CreateRewardsSection(GameObject contentRoot)
        {
            GameObject sectionObj = MakeSection(contentRoot, "RewardsSection",
                new Vector2(0f, 0.25f), new Vector2(0.50f, 0.50f), SectionBg);

            CreateSectionHeader(sectionObj, "REWARDS");

            CreateTMP(sectionObj, "CoinsEarnedText",
                new Vector2(0.05f, 0.60f), new Vector2(0.95f, 0.80f),
                "🪙  +50 Coins", 26f, FontStyles.Bold, GoldAccent,
                TextAlignmentOptions.Center);

            CreateTMP(sectionObj, "XpEarnedText",
                new Vector2(0.05f, 0.38f), new Vector2(0.95f, 0.60f),
                "⭐  +150 XP", 26f, FontStyles.Bold,
                new Color(0.7f, 0.9f, 1f), TextAlignmentOptions.Center);

            // Achievements scroll view
            GameObject achievementsScrollView = CreateScrollView(
                sectionObj, "AchievementsScrollView",
                new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.38f));

            Undo.RegisterCreatedObjectUndo(sectionObj, "Create Rewards Section");
            return sectionObj;
        }

        // ================================================================
        // Players Section — leaderboard ScrollView
        // ================================================================

        private GameObject CreatePlayersSection(GameObject contentRoot)
        {
            GameObject sectionObj = MakeSection(contentRoot, "PlayersSection",
                new Vector2(0.50f, 0.25f), new Vector2(1.00f, 0.50f), SectionBg);

            CreateSectionHeader(sectionObj, "PLAYERS");

            // Player list scroll view
            GameObject playersScrollView = CreateScrollView(
                sectionObj, "PlayerListScrollView",
                new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.88f));

            Undo.RegisterCreatedObjectUndo(sectionObj, "Create Players Section");
            return sectionObj;
        }

        // ================================================================
        // Buttons Row — Play Again + Main Menu
        // ================================================================

        private GameObject CreateButtonsRow(GameObject panel)
        {
            GameObject rowObj = new GameObject("ButtonsRow");
            rowObj.transform.SetParent(panel.transform, false);

            RectTransform rect = rowObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.15f, 0.01f);
            rect.anchorMax = new Vector2(0.85f, 0.10f);
            rect.sizeDelta = Vector2.zero;

            // Play Again button (left)
            CreateActionButton(rowObj, "PlayAgainButton",
                new Vector2(0f, 0f), new Vector2(0.47f, 1f),
                "Play Again", GoldPrimary);

            // Main Menu button (right)
            CreateActionButton(rowObj, "MainMenuButton",
                new Vector2(0.53f, 0f), new Vector2(1f, 1f),
                "Main Menu", new Color(0.30f, 0.25f, 0.15f));

            Undo.RegisterCreatedObjectUndo(rowObj, "Create Buttons Row");
            return rowObj;
        }

        // ── Helper: creates one button with AnimatedButton + TMP label ──

        private GameObject CreateActionButton(GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, string label, Color color)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent.transform, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = Vector2.zero;

            Image img = btnObj.AddComponent<Image>();
            img.color = color;
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            btnObj.AddComponent<AnimatedButton>(); // Same as MysteryGeneratorUIBuilder

            // Button text
            CreateTMP(btnObj, "Text",
                Vector2.zero, Vector2.one,
                label, 28f, FontStyles.Bold,
                new Color(0.08f, 0.04f, 0.02f), TextAlignmentOptions.Center);

            Undo.RegisterCreatedObjectUndo(btnObj, $"Create {name}");
            return btnObj;
        }

        // ================================================================
        // ScrollView Helper — creates a vertical scroll view with a Content child
        // ================================================================

        private GameObject CreateScrollView(GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject svObj = new GameObject(name);
            svObj.transform.SetParent(parent.transform, false);

            RectTransform svRect = svObj.AddComponent<RectTransform>();
            svRect.anchorMin = anchorMin;
            svRect.anchorMax = anchorMax;
            svRect.sizeDelta = Vector2.zero;

            ScrollRect scrollRect = svObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // Viewport
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(svObj.transform, false);
            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportObj.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            Mask mask = viewportObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            contentObj.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            // Wire scroll rect
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            Undo.RegisterCreatedObjectUndo(svObj, $"Create ScrollView {name}");
            return contentObj; // Returns the Content so callers can assign it as container
        }

        // ================================================================
        // ConfigureReferences — wires all VictoryScreenUI [SerializeField] refs
        // ================================================================

        private void ConfigureReferences(
            GameObject panel,
            GameObject fadeOverlay,
            GameObject spinner,
            GameObject contentRoot,
            GameObject headerSection,
            GameObject statsSection,
            GameObject scoreSection,
            GameObject rewardsSection,
            GameObject playersSection,
            GameObject buttonsRow)
        {
            VictoryScreenUI script = panel.GetComponent<VictoryScreenUI>();
            if (script == null) return;

            SerializedObject so = new SerializedObject(script);
            Transform p = panel.transform;

            // ── Root panels ──
            AssignField(so, "victoryScreenPanel", panel);
            AssignField(so, "loadingSpinner",     spinner);
            AssignField(so, "contentRoot",        contentRoot);

            // ── Theme banner ──
            AssignField(so, "themeBannerImage",
                p.Find("ThemeBanner")?.GetComponent<Image>());

            // ── Header ──
            Transform header = p.Find("ContentRoot/HeaderSection");
            AssignField(so, "victoryMessageText",
                header?.Find("VictoryMessageText")?.GetComponent<TextMeshProUGUI>());
            AssignField(so, "secondaryMessageText",
                header?.Find("SecondaryMessageText")?.GetComponent<TextMeshProUGUI>());
            AssignField(so, "rankTitleText",
                header?.Find("RankTitleText")?.GetComponent<TextMeshProUGUI>());

            // ── Stats ──
            Transform stats = p.Find("ContentRoot/StatsSection");
            AssignField(so, "completionTimeText",
                stats?.Find("CompletionTimeText")?.GetComponent<TextMeshProUGUI>());
            AssignField(so, "timeRemainingText",
                stats?.Find("TimeRemainingText")?.GetComponent<TextMeshProUGUI>());
            AssignField(so, "puzzlesSolvedText",
                stats?.Find("PuzzlesSolvedText")?.GetComponent<TextMeshProUGUI>());
            AssignField(so, "hintsUsedText",
                stats?.Find("HintsUsedText")?.GetComponent<TextMeshProUGUI>());
            AssignField(so, "successRateText",
                stats?.Find("SuccessRateText")?.GetComponent<TextMeshProUGUI>());
            AssignField(so, "newRecordBadge",
                stats?.Find("NewRecordBadge")?.gameObject);
            AssignField(so, "previousBestText",
                stats?.Find("PreviousBestText")?.GetComponent<TextMeshProUGUI>());

            // ── Score ──
            Transform score = p.Find("ContentRoot/ScoreSection");
            AssignField(so, "baseScoreText",
                score?.Find("BaseScoreText/Value")?.GetComponent<TextMeshProUGUI>());
            AssignField(so, "timeBonusText",
                score?.Find("TimeBonusText/Value")?.GetComponent<TextMeshProUGUI>());
            AssignField(so, "difficultyMultiplierText",
                score?.Find("DifficultyMultiplierText/Value")?.GetComponent<TextMeshProUGUI>());
            AssignField(so, "hintPenaltyText",
                score?.Find("HintPenaltyText/Value")?.GetComponent<TextMeshProUGUI>());
            AssignField(so, "teamBonusRow",
                score?.Find("TeamBonusRow")?.gameObject);
            AssignField(so, "teamBonusText",
                score?.Find("TeamBonusRow/TeamBonusText/Value")?.GetComponent<TextMeshProUGUI>());
            AssignField(so, "finalScoreText",
                score?.Find("FinalScoreText")?.GetComponent<TextMeshProUGUI>());

            // ── Rewards ──
            Transform rewards = p.Find("ContentRoot/RewardsSection");
            AssignField(so, "coinsEarnedText",
                rewards?.Find("CoinsEarnedText")?.GetComponent<TextMeshProUGUI>());
            AssignField(so, "xpEarnedText",
                rewards?.Find("XpEarnedText")?.GetComponent<TextMeshProUGUI>());
            AssignField(so, "achievementsContainer",
                rewards?.Find("AchievementsScrollView/Viewport/Content"));

            // ── Players ──
            Transform players = p.Find("ContentRoot/PlayersSection");
            AssignField(so, "playerListContainer",
                players?.Find("PlayerListScrollView/Viewport/Content"));

            // ── Buttons ──
            Transform buttons = p.Find("ButtonsRow");
            AssignField(so, "playAgainButton",
                buttons?.Find("PlayAgainButton")?.GetComponent<Button>());
            AssignField(so, "mainMenuButton",
                buttons?.Find("MainMenuButton")?.GetComponent<Button>());

            // ── Animator ──
            AssignField(so, "victoryPanelAnimator",
                panel.GetComponent<Animator>());

            so.ApplyModifiedProperties();
            Debug.Log("✅ VictoryScreenUI references auto-wired.");
        }

        // ================================================================
        // Utilities — standalone menu actions
        // ================================================================

        private void SetupCanvasSettings()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Error", "No Canvas found in scene!", "OK");
                return;
            }
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>()
                                  ?? canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            EditorUtility.SetDirty(canvas);
            Debug.Log("✅ Canvas settings configured.");
        }

        private void ImportTMPEssentials()
        {
            EditorUtility.DisplayDialog("TextMeshPro",
                "Import TMP Essentials manually:\n\n" +
                "Window → TextMeshPro → Import TMP Essential Resources\n\n" +
                "Only needs to be done once per project.",
                "OK");
        }

        // Creates a temporary visible PlayerRow in the scene for prefab setup reference
        private void CreatePlayerRowPrefabSetup()
        {
            GameObject rowObj = new GameObject("PlayerRow_PrefabSetup");
            RectTransform rect = rowObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(600f, 70f);

            Image bg = rowObj.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.1f);
            bg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            bg.type = Image.Type.Sliced;

            rowObj.AddComponent<PlayerRowUI>();

            // MVP crown indicator
            GameObject crownObj = new GameObject("MvpCrownObject");
            crownObj.transform.SetParent(rowObj.transform, false);
            RectTransform crownRect = crownObj.AddComponent<RectTransform>();
            crownRect.anchorMin = new Vector2(0f, 0.1f);
            crownRect.anchorMax = new Vector2(0.08f, 0.9f);
            crownRect.sizeDelta = Vector2.zero;
            crownObj.AddComponent<Image>().color = GoldAccent;
            crownObj.SetActive(false);

            // Username
            CreateTMP(rowObj, "UsernameText",
                new Vector2(0.09f, 0.1f), new Vector2(0.40f, 0.9f),
                "Player1", 22f, FontStyles.Bold, TextWhite, TextAlignmentOptions.Left);

            // Puzzles solved
            CreateTMP(rowObj, "PuzzlesSolvedText",
                new Vector2(0.40f, 0.1f), new Vector2(0.58f, 0.9f),
                "3 Puzzles", 18f, FontStyles.Normal, TextWhite, TextAlignmentOptions.Center);

            // Hints
            CreateTMP(rowObj, "HintsText",
                new Vector2(0.58f, 0.1f), new Vector2(0.74f, 0.9f),
                "No Hints!", 18f, FontStyles.Normal, GreenReward, TextAlignmentOptions.Center);

            // Contribution %
            CreateTMP(rowObj, "ContribText",
                new Vector2(0.74f, 0.1f), new Vector2(0.88f, 0.9f),
                "60.0%", 20f, FontStyles.Bold, GoldAccent, TextAlignmentOptions.Right);

            // Contribution bar
            GameObject sliderObj = new GameObject("ContribBar");
            sliderObj.transform.SetParent(rowObj.transform, false);
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.09f, 0.0f);
            sliderRect.anchorMax = new Vector2(0.95f, 0.15f);
            sliderRect.sizeDelta = Vector2.zero;
            Slider slider = sliderObj.AddComponent<Slider>();
            slider.value = 0.6f;

            Selection.activeGameObject = rowObj;
            Debug.Log("✅ PlayerRow setup created. Convert to Prefab via right-click → Prefab → Create Prefab.");
        }

        private void CreateAchievementBadgePrefabSetup()
        {
            GameObject badgeObj = new GameObject("AchievementBadge_PrefabSetup");
            RectTransform rect = badgeObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(220f, 48f);

            Image bg = badgeObj.AddComponent<Image>();
            bg.color = new Color(0.8f, 0.6f, 0.3f, 0.85f);
            bg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            bg.type = Image.Type.Sliced;

            // "Label" child — VictoryScreenUI.PopulateRewards() looks for this by GetComponentInChildren<TMP>
            CreateTMP(badgeObj, "Label",
                Vector2.zero, Vector2.one,
                "No Hints Master", 18f, FontStyles.Bold,
                new Color(0.08f, 0.04f, 0.02f), TextAlignmentOptions.Center);

            Selection.activeGameObject = badgeObj;
            Debug.Log("✅ AchievementBadge setup created. Convert to Prefab via right-click → Prefab → Create Prefab.");
        }

        // ================================================================
        // Low-level helpers — shared by all section builders
        // ================================================================

        /// Makes a section panel with anchors and a background image
        private GameObject MakeSection(GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            Image img = obj.AddComponent<Image>();
            img.color = bgColor;
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;

            return obj;
        }

        /// Makes a plain transparent panel (used for rows and grouping)
        private GameObject MakePanel(GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = Vector2.zero;

            if (color != Color.clear)
            {
                Image img = obj.AddComponent<Image>();
                img.color = color;
            }

            return obj;
        }

        /// Adds a small bold section header label at the top of a section
        private void CreateSectionHeader(GameObject parent, string title)
        {
            CreateTMP(parent, "SectionHeader",
                new Vector2(0.02f, 0.90f), new Vector2(0.98f, 1.00f),
                title, 16f, FontStyles.Bold,
                new Color(0.6f, 0.57f, 0.50f), TextAlignmentOptions.Left);
        }

        /// Creates a TextMeshProUGUI with anchored rect and given properties
        private TextMeshProUGUI CreateTMP(GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            string text, float fontSize, FontStyles style,
            Color color, TextAlignmentOptions alignment)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            Undo.RegisterCreatedObjectUndo(obj, $"Create TMP {name}");
            return tmp;
        }

        /// Assigns a UnityEngine.Object to a SerializedProperty by field name
        private void AssignField(SerializedObject so, string fieldName, Object value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null && value != null)
                prop.objectReferenceValue = value;
        }

        /// Assigns a Transform to a SerializedProperty (used for containers)
        private void AssignField(SerializedObject so, string fieldName, Transform value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null && value != null)
                prop.objectReferenceValue = value;
        }
    }
}
#endif
