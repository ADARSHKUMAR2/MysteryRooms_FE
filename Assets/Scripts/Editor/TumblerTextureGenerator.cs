#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class TumblerTextureGenerator : EditorWindow
{
    private int textureWidth = 1024;
    private int textureHeight = 256;
    private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f); // Dark Grey
    private Color textColor = Color.white;
    private int fontSize = 120;
    private Font customFont;
    private string savePath = "Assets/Textures/TumblerTexture.png";

    [MenuItem("MysteryRooms/Tools/Generate Tumbler Texture")]
    public static void ShowWindow()
    {
        GetWindow<TumblerTextureGenerator>("Tumbler Texture Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Tumbler Texture Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        textureWidth = EditorGUILayout.IntField("Width (Pixels)", textureWidth);
        textureHeight = EditorGUILayout.IntField("Height (Pixels)", textureHeight);
        
        GUILayout.Space(5);
        backgroundColor = EditorGUILayout.ColorField("Background Color", backgroundColor);
        textColor = EditorGUILayout.ColorField("Text Color", textColor);
        
        GUILayout.Space(5);
        customFont = (Font)EditorGUILayout.ObjectField("Font (Optional)", customFont, typeof(Font), false);
        fontSize = EditorGUILayout.IntField("Font Size", fontSize);

        GUILayout.Space(10);
        savePath = EditorGUILayout.TextField("Save Path", savePath);

        GUILayout.Space(20);
        if (GUILayout.Button("Generate Texture", GUILayout.Height(40)))
        {
            GenerateTexture();
        }
    }

    private void GenerateTexture()
    {
        // 1. Create a temporary RenderTexture to draw on
        RenderTexture rt = RenderTexture.GetTemporary(textureWidth, textureHeight, 24, RenderTextureFormat.ARGB32);
        RenderTexture.active = rt;

        // 2. Clear background
        GL.Clear(true, true, backgroundColor);

        // 3. Setup drawing tools
        Material drawMat = new Material(Shader.Find("Hidden/Internal-Colored"));
        drawMat.SetPass(0);

        // 4. Create a Texture2D to hold the final result
        Texture2D finalTex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);

        // 5. Setup GUI for drawing text
        GUIStyle textStyle = new GUIStyle();
        textStyle.normal.textColor = textColor;
        textStyle.fontSize = fontSize;
        textStyle.alignment = TextAnchor.MiddleCenter;
        if (customFont != null) textStyle.font = customFont;

        // We use GUI.DrawTexture/Label inside a GUI layout block via RenderTexture
        // To do this properly in Editor without a camera, we temporarily override GUI matrix
        
        // 6. Read the pixels from the background color
        finalTex.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
        
        // Let's use a simpler approach: modify the Texture2D directly using Unity's GUI utility
        // Actually, drawing text directly to Texture2D in code is tricky without a camera.
        // A better, perfectly accurate way is to just write the pixels manually if we want basic blocky numbers,
        // BUT since we want nice fonts, we will use a hidden Camera approach or EditorGUI rendering.

        // Cleanest approach for Editor: Render GUI to texture
        var oldRT = RenderTexture.active;
        RenderTexture.active = rt;
        
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, textureWidth, textureHeight, 0);
        
        // Draw the numbers 0-9
        float segmentWidth = textureWidth / 10f;
        
        // Note: GUI commands don't work well outside OnGUI, so we draw them using a small trick.
        // We will just create a blank texture and ask the user to use a standard text component in scene,
        // OR we can generate a classic 8-segment display look procedurally via pixels.
        
        // Let's do the procedural pixel way for reliability!
        Color[] pixels = new Color[textureWidth * textureHeight];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = backgroundColor;
        finalTex.SetPixels(pixels);
        
        // Helper to draw rough pixel text (fallback if GUI fails)
        // Since we want this to look good, we'll construct a dynamic texture using Unity's text rendering.
        RenderTextToTexture(finalTex, segmentWidth);

        finalTex.Apply();

        // 7. Save to PNG
        byte[] bytes = finalTex.EncodeToPNG();
        
        // Ensure directory exists
        string directory = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(savePath, bytes);
        
        // Cleanup
        RenderTexture.active = oldRT;
        RenderTexture.ReleaseTemporary(rt);
        DestroyImmediate(finalTex);
        DestroyImmediate(drawMat);

        AssetDatabase.Refresh();
        Debug.Log($"[TumblerTexture] Successfully generated at: {savePath}");
    }

    private void RenderTextToTexture(Texture2D tex, float segmentWidth)
    {
        // For a perfect result without dealing with Editor GUI rendering quirks, 
        // the easiest way is to generate a temporary GameObject with a TextMesh, render it to a camera, and capture it.
        
        GameObject camObj = new GameObject("TempCam");
        Camera cam = camObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = textureHeight / 2f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = backgroundColor;
        cam.targetTexture = RenderTexture.GetTemporary(textureWidth, textureHeight, 24);
        
        GameObject textObj = new GameObject("TempText");
        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.color = textColor;
        tm.fontSize = fontSize;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        if (customFont != null) tm.font = customFont;

        // Position text
        string fullText = "";
        for (int i = 0; i < 10; i++)
        {
            fullText += i.ToString();
            // Add spaces to approximate segment width
            if (i < 9) fullText += "     "; 
        }
        
        tm.text = "0      1      2      3      4      5      6      7      8      9";
        
        // Align camera and text
        textObj.transform.position = new Vector3(0, 0, 0);
        cam.transform.position = new Vector3(0, 0, -10);

        // Render
        cam.Render();
        
        RenderTexture.active = cam.targetTexture;
        tex.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
        
        // Cleanup
        RenderTexture.ReleaseTemporary(cam.targetTexture);
        DestroyImmediate(camObj);
        DestroyImmediate(textObj);
    }
}
#endif
