using System;
using UnityEngine;
using UnityEngine.UI;

namespace OSK
{
    internal static class DebugUIFactory
    {
        // Creates a standalone button under a parent.
        public static Button CreateButton(RectTransform parent, string label, Action action, Color rowColor, Color textColor)
        {
            RectTransform rect = CreateRect("Action_" + label, parent);
            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 22f;
            layout.preferredHeight = 22f;
            layout.preferredWidth = 304f;
            rect.sizeDelta = new Vector2(304f, 22f);
            AddImage(rect, rowColor);

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.colors = ButtonColors(rowColor);
            if (action != null)
            {
                button.onClick.AddListener(() => action());
            }

            Text text = CreateText("Label", rect, label, 12, FontStyle.Bold, textColor);
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 9;
            text.resizeTextMaxSize = 12;
            FullAnchor(text.rectTransform);
            return button;
        }

        // Creates a RectTransform child.
        public static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        // Creates a Text child with framework defaults.
        public static Text CreateText(string name, Transform parent, string value, int size, FontStyle style, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = GetBuiltinFont();
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.text = value;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(8, size - 4);
            text.resizeTextMaxSize = size;
            return text;
        }

        // Adds an Image to a RectTransform.
        public static Image AddImage(RectTransform rect, Color color)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        // Anchors a RectTransform to fill its parent.
        public static void FullAnchor(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // Creates the default debug button color block.
        public static ColorBlock ButtonColors(Color rowColor)
        {
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = rowColor;
            colors.highlightedColor = new Color(0.14f, 0.18f, 0.25f, 1f);
            colors.pressedColor = DebugConsoleView.DebugInspectorBuilder.AccentColor;
            colors.selectedColor = rowColor;
            colors.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.6f);
            colors.colorMultiplier = 1f;
            return colors;
        } 

        public static Font GetBuiltinFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
