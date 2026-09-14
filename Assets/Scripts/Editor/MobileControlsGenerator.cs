using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

// We need to use reflection or string paths to add the InputSystem components
// because the InputSystem package might not be referenced in this assembly by default.
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.OnScreen;
#endif

public class MobileControlsGenerator : EditorWindow
{
    [MenuItem("MysteryRooms/Create Mobile Controls UI")]
    public static void ShowWindow()
    {
        GetWindow<MobileControlsGenerator>("Mobile Controls Gen").Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Mobile On-Screen Controls Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This will generate a Canvas with two Joysticks (Movement & Look), " +
            "an Interact button, a Hint button, and a Rotate Mirror button " +
            "mapped to the 'E' key.\\n\\n" +
            "It uses Unity's New Input System 'On-Screen Controls' to fake keyboard/gamepad inputs.", 
            MessageType.Info
        );

        GUILayout.Space(20);

        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
        if (GUILayout.Button("Generate Mobile Canvas", GUILayout.Height(40)))
        {
            GenerateMobileUI();
        }
        GUI.backgroundColor = Color.white;
    }

    private void GenerateMobileUI()
    {
#if !ENABLE_INPUT_SYSTEM
        EditorUtility.DisplayDialog("Error", "You must install and enable the New Input System package to use On-Screen Controls!", "OK");
        return;
#else
        // 1. Create the Canvas
        GameObject canvasObj = new GameObject("MobileControlsCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Always on top

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. Create Left Joystick (Movement)
        CreateJoystick(canvasObj.transform, "LeftJoystick_Move", new Vector2(250, 250), "<Gamepad>/leftStick");

        // 3. Create Right Joystick (Look)
        CreateJoystick(canvasObj.transform, "RightJoystick_Look", new Vector2(-250, 250), "<Gamepad>/rightStick");

        // 4. Create Interact Button ('E' Key)
        CreateButton(canvasObj.transform, "InteractButton", new Vector2(-250, 600), "<Keyboard>/e", "Interact [E]");

        // 5. Create Hint Button ('H' Key)
        CreateButton(canvasObj.transform, "HintButton", new Vector2(-450, 600), "<Keyboard>/h", "Hint [H]");

        // 6. Create Rotate Mirror Button (Custom - maps to a custom input action)
        CreateRotateMirrorButton(canvasObj.transform, "RotateMirrorButton", new Vector2(-650, 600), "Rotate\nMirror");

        // 7. Add the script that hides this on PC
        CreateMobileToggleScript(canvasObj);

        Selection.activeGameObject = canvasObj;
        EditorUtility.DisplayDialog("Success", "Mobile Controls Generated!\\n\\nIf you want to test them in the Editor, temporarily disable the 'Mobile Control Toggle' script on the Canvas.", "Awesome!");
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private void CreateJoystick(Transform parent, string name, Vector2 anchoredPos, string controlPath)
    {
        // Background Area
        GameObject bgObj = new GameObject(name);
        bgObj.transform.SetParent(parent, false);
        
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        // Anchor to bottom left
        bgRect.anchorMin = new Vector2(0, 0);
        bgRect.anchorMax = new Vector2(0, 0);
        bgRect.anchoredPosition = anchoredPos;
        bgRect.sizeDelta = new Vector2(300, 300);

        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.3f); // Semi-transparent black circle
        // Note: For a real game, you would assign a circle sprite here: bgImg.sprite = ...

        // The Handle (The part you drag)
        GameObject handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(bgObj.transform, false);
        
        RectTransform handleRect = handleObj.AddComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.sizeDelta = new Vector2(100, 100);

        Image handleImg = handleObj.AddComponent<Image>();
        handleImg.color = new Color(1, 1, 1, 0.8f);

        // Add the Input System On-Screen Stick component
        OnScreenStick stick = bgObj.AddComponent<OnScreenStick>();
        stick.movementRange = 100f;
        
        // Use SerializedObject to set the control path (since it's a private field in the Input System)
        SerializedObject so = new SerializedObject(stick);
        so.FindProperty("m_ControlPath").stringValue = controlPath;
        so.ApplyModifiedProperties();
    }

    private void CreateButton(Transform parent, string name, Vector2 anchoredPos, string controlPath, string label)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1, 0);
        btnRect.anchorMax = new Vector2(1, 0);
        btnRect.anchoredPosition = anchoredPos;
        btnRect.sizeDelta = new Vector2(150, 150);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.85f, 0.65f, 0.13f, 0.8f); // Golden color

        // Add Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRect = textObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = txtRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 24;

        // Add the Input System On-Screen Button component
        OnScreenButton osBtn = btnObj.AddComponent<OnScreenButton>();
        
        SerializedObject so = new SerializedObject(osBtn);
        so.FindProperty("m_ControlPath").stringValue = controlPath;
        so.ApplyModifiedProperties();
    }

    private void CreateRotateMirrorButton(Transform parent, string name, Vector2 anchoredPos, string label)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1, 0);
        btnRect.anchorMax = new Vector2(1, 0);
        btnRect.anchoredPosition = anchoredPos;
        btnRect.sizeDelta = new Vector2(150, 150);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.7f, 0.9f, 0.8f); // Cyan/blue color to differentiate

        // Add Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRect = textObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = txtRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 20;

        // Add the MobileRotateMirrorButton component (custom script)
        btnObj.AddComponent<MobileRotateMirrorButton>();
        
        // Start inactive - will be activated when mirror is picked up
        btnObj.SetActive(false);
    }
#endif

    private void CreateMobileToggleScript(GameObject canvasObj)
    {
        // Add the MobileControlToggle component to hide it on PC
        // We assume you have created this script!
        System.Type type = System.Type.GetType("MobileControlToggle, Assembly-CSharp");
        if (type == null) type = System.Type.GetType("MobileControlToggle, MysteryRooms.Runtime");
        
        if (type != null)
        {
            canvasObj.AddComponent(type);
        }
        else
        {
            Debug.LogWarning("MobileControlToggle.cs not found. Canvas will be visible on all platforms by default.");
        }
    }
}
