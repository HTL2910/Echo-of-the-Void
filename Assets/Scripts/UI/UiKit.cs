using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace EchoOfTheVoid.UI
{
    /// <summary>
    /// Small helpers to build the menu screens in code with a consistent Aether-punk look. Screens take a
    /// <see cref="Font"/> (Space Mono for text, per spec 9.7) and fall back to the built-in one.
    /// </summary>
    public static class UiKit
    {
        // Palette: Dark aether-punk theme with cyan/purple accents
        public static readonly Color Panel = new Color(0.03f, 0.05f, 0.09f, 0.94f);
        public static readonly Color PanelDark = new Color(0.01f, 0.02f, 0.04f, 0.98f);
        public static readonly Color Accent = new Color(0f, 0.85f, 1f, 1f);
        public static readonly Color AccentBright = new Color(0.2f, 1f, 1f, 1f);
        public static readonly Color Echo = new Color(0.85f, 0.25f, 1f, 1f);
        public static readonly Color EchoBright = new Color(1f, 0.4f, 1f, 1f);
        public static readonly Color TextColor = new Color(0.88f, 0.93f, 0.97f, 1f);
        public static readonly Color TextSecondary = new Color(0.7f, 0.8f, 0.88f, 1f);
        public static readonly Color Muted = new Color(0.55f, 0.62f, 0.7f, 1f);
        public static readonly Color ButtonNormal = new Color(0.1f, 0.15f, 0.22f, 1f);
        public static readonly Color ButtonHover = new Color(0.55f, 0.95f, 1f, 1f);
        public static readonly Color ButtonActive = new Color(0.35f, 0.7f, 0.85f, 1f);

        // Typography: Heading / Body / Small
        public const int SizeHeading = 44;
        public const int SizeTitle = 32;
        public const int SizeBody = 26;
        public const int SizeSmall = 20;

        private static Font _fallback;

        public static Font FontOrDefault(Font font)
        {
            if (font != null) return font;
            if (_fallback == null) _fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _fallback;
        }

        /// <summary>One EventSystem (mouse, keyboard and gamepad navigation through the Input System) for all menus.</summary>
        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform CreatePanel(Transform parent, string name, Color color, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>Vertical stack with improved spacing (heading→body sections), top-to-bottom layout.</summary>
        public static VerticalLayoutGroup MakeVertical(RectTransform rect, float spacing = 20f, int padding = 40)
        {
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static Text CreateLabel(Transform parent, string text, int size, Font font, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter, float height = 0f)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.font = FontOrDefault(font);
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            go.GetComponent<LayoutElement>().preferredHeight = height > 0f ? height : size * 1.8f;
            return t;
        }

        public static Text CreateHeading(Transform parent, string text, Font font)
        {
            return CreateLabel(parent, text, SizeHeading, font, Accent, TextAnchor.MiddleCenter, SizeHeading * 1.8f);
        }

        public static Text CreateTitle(Transform parent, string text, Font font)
        {
            return CreateLabel(parent, text, SizeTitle, font, AccentBright, TextAnchor.MiddleCenter, SizeTitle * 1.8f);
        }

        public static Text CreateBody(Transform parent, string text, Font font, Color? color = null)
        {
            return CreateLabel(parent, text, SizeBody, font, color ?? TextColor, TextAnchor.MiddleCenter, SizeBody * 1.8f);
        }

        public static Button CreateButton(Transform parent, string text, Font font, Action onClick, float height = 68f)
        {
            var go = new GameObject("Button_" + text, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = height;

            var image = go.GetComponent<Image>();
            image.color = ButtonNormal;

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = ButtonHover;
            colors.selectedColor = ButtonHover;
            colors.pressedColor = ButtonActive;
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
            button.colors = colors;
            button.targetGraphic = image;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            var label = CreateLabel(go.transform, text, SizeBody, font, TextColor);
            var rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            DestroyLayoutElement(label.gameObject);

            // Add transition animation via CanvasGroup
            var canvasGroup = go.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;

            return button;
        }

        public static void SetButtonText(Button button, string text)
        {
            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.text = text;
        }

        public static Slider CreateSlider(Transform parent, string label, float value, Font font, Action<float> onChange)
        {
            var row = CreateRow(parent, 64f);
            var text = CreateLabel(row, label, SizeBody, font, TextSecondary, TextAnchor.MiddleLeft);
            text.GetComponent<LayoutElement>().preferredWidth = 420f;

            var slider = DefaultControls.CreateSlider(new DefaultControls.Resources()).GetComponent<Slider>();
            slider.transform.SetParent(row, false);
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(v => onChange?.Invoke(v));
            var element = slider.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 520f;
            element.preferredHeight = 40f;

            // Color slider thumb and fill
            var fillImage = slider.transform.Find("Fill Area/Fill");
            if (fillImage != null) fillImage.GetComponent<Image>().color = Accent;
            var thumb = slider.transform.Find("Handle Slide Area/Handle");
            if (thumb != null) thumb.GetComponent<Image>().color = AccentBright;

            return slider;
        }

        public static Toggle CreateToggle(Transform parent, string label, bool value, Font font, Action<bool> onChange)
        {
            var row = CreateRow(parent, 60f);
            var text = CreateLabel(row, label, SizeBody, font, TextSecondary, TextAnchor.MiddleLeft);
            text.GetComponent<LayoutElement>().preferredWidth = 700f;

            var toggle = DefaultControls.CreateToggle(new DefaultControls.Resources()).GetComponent<Toggle>();
            toggle.transform.SetParent(row, false);
            foreach (var t in toggle.GetComponentsInChildren<Text>()) UnityEngine.Object.Destroy(t.gameObject);
            toggle.SetIsOnWithoutNotify(value);
            toggle.onValueChanged.AddListener(v => onChange?.Invoke(v));

            var element = toggle.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 48f;
            element.preferredHeight = 48f;

            // Color checkbox
            var bg = toggle.transform.Find("Background");
            if (bg != null) bg.GetComponent<Image>().color = ButtonNormal;
            var checkmark = toggle.transform.Find("Background/Checkmark");
            if (checkmark != null) checkmark.GetComponent<Image>().color = Accent;

            return toggle;
        }

        public static RectTransform CreateRow(Transform parent, float height)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.padding = new RectOffset(0, 0, 8, 8);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            go.GetComponent<LayoutElement>().preferredHeight = height;
            return go.GetComponent<RectTransform>();
        }

        private static void DestroyLayoutElement(GameObject go)
        {
            var element = go.GetComponent<LayoutElement>();
            if (element != null) UnityEngine.Object.Destroy(element);
        }
    }
}
