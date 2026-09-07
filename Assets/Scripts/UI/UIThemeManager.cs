using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MysteryRooms.UI
{
    [CreateAssetMenu(fileName = "UITheme", menuName = "Mystery Rooms/UI Theme")]
    public class UIThemeManager : ScriptableObject
    {
        [Header("Color Palette")]
        public Color primaryColor = new Color(0.8f, 0.6f, 0.3f);
        public Color secondaryColor = new Color(0.4f, 0.3f, 0.2f);
        public Color accentColor = new Color(1f, 0.8f, 0.4f);
        public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        public Color textColor = new Color(0.9f, 0.9f, 0.9f);
        public Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        
        [Header("Fonts")]
        public TMP_FontAsset titleFont;
        public TMP_FontAsset bodyFont;
        public TMP_FontAsset buttonFont;
        
        [Header("Font Sizes")]
        public float titleFontSize = 48f;
        public float headerFontSize = 36f;
        public float bodyFontSize = 24f;
        public float buttonFontSize = 28f;
        
        [Header("Spacing")]
        public float elementSpacing = 20f;
        public float sectionSpacing = 40f;
        public float padding = 30f;
        
        [Header("Animation")]
        public float defaultAnimationSpeed = 0.3f;
        public AnimationCurve defaultCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        public void ApplyToButton(Button button, TextMeshProUGUI text)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = primaryColor;
            colors.highlightedColor = accentColor;
            colors.pressedColor = secondaryColor;
            colors.disabledColor = disabledColor;
            button.colors = colors;
            
            if (text != null)
            {
                text.font = buttonFont;
                text.fontSize = buttonFontSize;
                text.color = textColor;
            }
        }
        
        public void ApplyToText(TextMeshProUGUI text, TextType type)
        {
            switch (type)
            {
                case TextType.Title:
                    text.font = titleFont;
                    text.fontSize = titleFontSize;
                    break;
                case TextType.Header:
                    text.font = titleFont;
                    text.fontSize = headerFontSize;
                    break;
                case TextType.Body:
                    text.font = bodyFont;
                    text.fontSize = bodyFontSize;
                    break;
                case TextType.Button:
                    text.font = buttonFont;
                    text.fontSize = buttonFontSize;
                    break;
            }
            
            text.color = textColor;
        }
        
        public enum TextType
        {
            Title,
            Header,
            Body,
            Button
        }
    }
}
