using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragUI.UI;

// The palette. The accent colours (buttons that are on, title bars, outlines, highlights) all come
// from one base colour, the player's choice in the config ([Appearance] AccentColor / CustomAccentColor),
// by the same hue with the brightness and saturation offsets the original crimson theme used.
// Read when the UI is built; picking a colour (Settings -> Theme, or a config manager) rebuilds the UI.
internal static class Theme
{
    public static readonly Color PanelBg = new(0.075f, 0.065f, 0.08f, 0.95f);
    public static readonly Color HudBg = new(0.075f, 0.065f, 0.08f, 0.86f);
    public static readonly Color Shadow = new(0f, 0f, 0f, 0.55f);
    public static readonly Color Divider = new(1f, 1f, 1f, 0.06f);
    public static readonly Color RowAlt = new(1f, 1f, 1f, 0.03f);
    public static readonly Color Button = new(0.17f, 0.15f, 0.17f, 1f);
    public static readonly Color ButtonHover = new(0.27f, 0.23f, 0.26f, 1f);
    public static readonly Color ButtonPressed = new(0.12f, 0.10f, 0.12f, 1f);
    public static readonly Color ButtonOutline = new(1f, 1f, 1f, 0.08f);
    public static readonly Color Text = new(0.92f, 0.90f, 0.88f, 1f);
    public static readonly Color TextDim = new(0.62f, 0.60f, 0.60f, 1f);
    public static readonly Color Alive = new(0.47f, 0.83f, 0.47f, 1f);
    public static readonly Color Respawning = new(0.92f, 0.73f, 0.32f, 1f);
    public static readonly Color Due = new(1f, 0.50f, 0.33f, 1f);
    public static readonly Color Unknown = new(0.55f, 0.55f, 0.58f, 1f);

    /// <summary>The palette's base colours (all dark enough for the light button text to read on).</summary>
    public static readonly Dictionary<AccentColor, Color> Palette = new()
    {
        [AccentColor.Crimson] = new(0.66f, 0.12f, 0.15f),
        [AccentColor.Ember] = new(0.72f, 0.30f, 0.08f),
        [AccentColor.Gold] = new(0.62f, 0.46f, 0.08f),
        [AccentColor.Emerald] = new(0.12f, 0.52f, 0.28f),
        [AccentColor.Teal] = new(0.07f, 0.50f, 0.52f),
        [AccentColor.Azure] = new(0.12f, 0.40f, 0.72f),
        [AccentColor.Royal] = new(0.27f, 0.25f, 0.70f),
        [AccentColor.Violet] = new(0.48f, 0.18f, 0.66f),
        [AccentColor.Rose] = new(0.70f, 0.18f, 0.45f),
        [AccentColor.Silver] = new(0.48f, 0.48f, 0.52f),
    };

    /// <summary>On buttons, tabs and filters that are switched on; progress fills.</summary>
    public static Color Accent { get; private set; }
    /// <summary>Hover on accent buttons; highlighted text (tracked boss, next spawn, live fight).</summary>
    public static Color AccentHover { get; private set; }
    /// <summary>Window and card borders; a focused text box.</summary>
    public static Color CardOutline { get; private set; }
    /// <summary>Window title bars and the tracker header.</summary>
    public static Color TitleBg { get; private set; }
    /// <summary>A highlighted list row (hunted V Blood, picked fight).</summary>
    public static Color RowHunted { get; private set; }
    public static Color CheckboxOutline { get; private set; }
    public static Color ScrollHandle { get; private set; }
    public static Color TextSelection { get; private set; }
    /// <summary>Class-synergy stat outline: green, or gold when the accent itself is green.</summary>
    public static Color Synergy { get; private set; }

    static Theme() => Apply(Palette[AccentColor.Crimson]);

    /// <summary>Use the colour from the config. Called before the UI is built.</summary>
    public static void Load()
    {
        string custom = Settings.CustomAccentColor;
        if (custom.Length > 0)
        {
            if (TryParseHex(custom, out var color))
            {
                Apply(color);
                Core.Log.LogInfo($"Theme: custom accent {custom}.");
                return;
            }
            Core.Log.LogWarning($"Theme: CustomAccentColor '{custom}' isn't a colour like #3A7BD5; using {Settings.AccentColor}.");
        }
        Apply(Palette.TryGetValue(Settings.AccentColor, out var chosen) ? chosen : Palette[AccentColor.Crimson]);
    }

    static void Apply(Color baseColor)
    {
        // Offsets measured from the original crimson theme (base S 0.82, V 0.66).
        Accent = Shade(baseColor, 1f, 1f, 1f);
        AccentHover = HoverOf(baseColor);
        CardOutline = Shade(baseColor, 0.87f, 0.94f, 0.65f);
        TitleBg = Shade(baseColor, 0.97f, 0.36f, 0.95f);
        RowHunted = Shade(baseColor, 1f, 1.06f, 0.17f);
        CheckboxOutline = Shade(baseColor, 0.87f, 0.94f, 0.8f);
        ScrollHandle = Shade(baseColor, 0.83f, 0.94f, 0.9f);
        TextSelection = Shade(baseColor, 1f, 1f, 0.5f);
        ToHsv(baseColor, out float h, out float s, out _);
        bool greenAccent = s > 0.25f && h >= 75f && h <= 170f;
        Synergy = greenAccent ? new Color(0.95f, 0.80f, 0.35f, 0.9f) : new Color(0.47f, 0.83f, 0.47f, 0.9f);
    }

    /// <summary>The hover shade of an accent colour (lighter, a little less saturated).</summary>
    public static Color HoverOf(Color baseColor) => Shade(baseColor, 0.95f, 1.21f, 1f);

    /// <summary>The same hue with its saturation and brightness scaled.</summary>
    static Color Shade(Color baseColor, float sScale, float vScale, float alpha)
    {
        ToHsv(baseColor, out float h, out float s, out float v);
        return FromHsv(h, s * sScale, v * vScale, alpha);
    }

    /// <summary>"#RRGGBB" or "RRGGBB".</summary>
    public static bool TryParseHex(string text, out Color color)
    {
        color = default;
        string hex = text.Trim().TrimStart('#');
        if (hex.Length != 6 || !int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int rgb)) return false;
        color = new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
        return true;
    }

    // Our own HSV maths (hue in degrees), so it doesn't lean on Unity statics with out parameters through interop.
    static void ToHsv(Color c, out float h, out float s, out float v)
    {
        float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), min = Mathf.Min(c.r, Mathf.Min(c.g, c.b)), d = max - min;
        v = max;
        s = max <= 0f ? 0f : d / max;
        if (d <= 0f) h = 0f;
        else if (max == c.r) h = 60f * ((c.g - c.b) / d % 6f);
        else if (max == c.g) h = 60f * ((c.b - c.r) / d + 2f);
        else h = 60f * ((c.r - c.g) / d + 4f);
        if (h < 0f) h += 360f;
    }

    static Color FromHsv(float h, float s, float v, float alpha)
    {
        s = Mathf.Clamp01(s);
        v = Mathf.Clamp01(v);
        float c = v * s, x = c * (1f - Mathf.Abs(h / 60f % 2f - 1f)), m = v - c;
        (float r, float g, float b) = (h % 360f) switch
        {
            < 60f => (c, x, 0f),
            < 120f => (x, c, 0f),
            < 180f => (0f, c, x),
            < 240f => (0f, x, c),
            < 300f => (x, 0f, c),
            _ => (c, 0f, x),
        };
        return new Color(r + m, g + m, b + m, alpha);
    }
}

/// <summary>The palette offered in the config.</summary>
public enum AccentColor { Crimson, Ember, Gold, Emerald, Teal, Azure, Royal, Violet, Rose, Silver }

// Minimal uGUI builder. Layout is done with explicit anchors/positions rather than layout groups so
// it behaves predictably under IL2CPP.
internal static class UIFactory
{
    static TMP_FontAsset _font;
    static readonly Queue<Action> _submits = new();

    static TMP_FontAsset Font
    {
        get
        {
            if (_font != null) return _font;
            _font = TMP_Settings.defaultFontAsset;
            if (_font == null)
            {
                var fonts = Resources.FindObjectsOfTypeAll(Il2CppType.Of<TMP_FontAsset>());
                if (fonts.Length > 0) _font = fonts[0].Cast<TMP_FontAsset>();
            }
            return _font;
        }
    }

    public static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.layer = 5; // UI
        var rect = go.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    /// <summary>Anchor to the parent's top-left; position is the offset of the rect's top-left corner.</summary>
    public static RectTransform Place(this RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    /// <summary>True on the frame any mouse button goes down anywhere but over <paramref name="rect"/>
    /// (e.g. to give up on a "press a key" capture when the player clicks elsewhere).</summary>
    public static bool ClickedOutside(RectTransform rect) =>
        (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        && (rect == null || !RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, null));

    /// <summary>Stretch to fill the parent, inset by the given margins.</summary>
    public static RectTransform Fill(this RectTransform rect, float left = 0, float top = 0, float right = 0, float bottom = 0)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        return rect;
    }

    public static Image CreatePanel(string name, Transform parent, Color color, bool raycast = true)
    {
        var rect = CreateRect(name, parent);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    /// <summary>Give an image rounded corners of the given radius.</summary>
    public static Image Rounded(this Image image, float radius)
    {
        image.sprite = RoundedSprites.Fill;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = RoundedSprites.MultiplierFor(radius);
        return image;
    }

    /// <summary>A thin rounded outline drawn over <paramref name="target"/> (doesn't take clicks).</summary>
    public static Image AddOutline(RectTransform target, float radius, Color color)
    {
        var outline = CreatePanel("Outline", target, color, raycast: false);
        outline.sprite = RoundedSprites.Outline;
        outline.type = Image.Type.Sliced;
        outline.pixelsPerUnitMultiplier = RoundedSprites.MultiplierFor(radius);
        outline.rectTransform.Fill();
        return outline;
    }

    /// <summary>A soft drop shadow behind <paramref name="target"/>'s content (add it before anything else).</summary>
    public static Image AddShadow(RectTransform target, float offsetY = 4f)
    {
        var shadow = CreatePanel("Shadow", target, Theme.Shadow, raycast: false);
        shadow.sprite = RoundedSprites.Shadow;
        shadow.type = Image.Type.Sliced;
        shadow.pixelsPerUnitMultiplier = 1f;
        float spread = RoundedSprites.ShadowSpread;
        shadow.rectTransform.Fill(-spread, -spread + offsetY, -spread, -spread - offsetY);
        return shadow;
    }

    public sealed class CardRef
    {
        public RectTransform Root;
        public Image Background;
    }

    /// <summary>A rounded panel with a drop shadow and an outline. Add content as children of Root.</summary>
    public static CardRef CreateCard(string name, Transform parent, Color background, float radius)
    {
        var root = CreateRect(name, parent);
        AddShadow(root);
        var bg = CreatePanel("Background", root, background).Rounded(radius);
        bg.rectTransform.Fill();
        AddOutline(root, radius, Theme.CardOutline);
        return new CardRef { Root = root, Background = bg };
    }

    /// <summary>A thin horizontal divider line.</summary>
    public static Image CreateDivider(Transform parent, float x, float y, float width)
    {
        var line = CreatePanel("Divider", parent, Theme.Divider, raycast: false);
        line.rectTransform.Place(x, y, width, 1f);
        return line;
    }

    /// <summary>A small filled circle (status dot).</summary>
    public static Image CreateDot(Transform parent, float x, float y, float size, Color color)
    {
        var dot = CreatePanel("Dot", parent, color, raycast: false).Rounded(size / 2f);
        dot.rectTransform.Place(x, y, size, size);
        return dot;
    }

    public static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size,
        Color color, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        var rect = CreateRect(name, parent);
        var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (Font != null) tmp.font = Font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }

    public sealed class ButtonRef
    {
        public Button Button;
        public Image Background;
        public TextMeshProUGUI Label;
        /// <summary>Last style applied by a caller, so per-frame refreshes only restyle on change.</summary>
        public string Style;
        public RectTransform Rect => Button.GetComponent<RectTransform>();

        public void SetColors(Color normal, Color hover)
        {
            var colors = Button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = hover;
            colors.selectedColor = normal;
            colors.pressedColor = Theme.ButtonPressed;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            Button.colors = colors;
        }
    }

    public static ButtonRef CreateButton(string name, Transform parent, string label, float fontSize, Action onClick,
        float radius = 6f, bool outline = true)
    {
        var rect = CreateRect(name, parent);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = Color.white; // tinted by the button's ColorBlock
        if (radius > 0) image.Rounded(radius);
        if (outline && radius > 0) AddOutline(rect, radius, Theme.ButtonOutline);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var navigation = button.navigation;
        navigation.mode = Navigation.Mode.None; // don't let keyboard/gamepad navigation grab our buttons
        button.navigation = navigation;

        var text = CreateText("Label", rect, label, fontSize, Theme.Text, TextAlignmentOptions.Center);
        text.rectTransform.Fill(4, 0, 4, 0);

        var buttonRef = new ButtonRef { Button = button, Background = image, Label = text };
        buttonRef.SetColors(Theme.Button, Theme.ButtonHover);
        button.onClick.AddListener((UnityEngine.Events.UnityAction)(() =>
        {
            try { onClick(); }
            catch (Exception ex) { Core.Log.LogError($"Button '{name}' failed: {ex}"); }
        }));
        return buttonRef;
    }

    public sealed class CheckboxRef
    {
        public ButtonRef Box;
        public Image Mark;
        public void Set(bool on) => Mark.gameObject.SetActive(on);
    }

    public static CheckboxRef CreateCheckbox(string name, Transform parent, Action onClick)
    {
        var box = CreateButton(name, parent, string.Empty, 12, onClick, radius: 5f, outline: false);
        box.SetColors(new Color(0.11f, 0.10f, 0.11f, 1f), new Color(0.22f, 0.19f, 0.21f, 1f));
        AddOutline(box.Rect, 5f, Theme.CheckboxOutline);
        var mark = CreatePanel("Mark", box.Rect, Theme.AccentHover, raycast: false).Rounded(3f);
        mark.rectTransform.Fill(5, 5, 5, 5);
        return new CheckboxRef { Box = box, Mark = mark };
    }

    /// <summary>A single-line text box. <paramref name="onSubmit"/> runs on Enter with the current text.</summary>
    public static TMP_InputField CreateInputField(string name, Transform parent, string placeholder, float fontSize,
        Action<string> onSubmit, int characterLimit = 0)
    {
        var rect = CreateRect(name, parent);
        var background = rect.gameObject.AddComponent<Image>();
        background.color = new Color(0.11f, 0.10f, 0.11f, 1f);
        background.Rounded(6f);
        var outline = AddOutline(rect, 6f, Theme.ButtonOutline);

        var field = rect.gameObject.AddComponent<TMP_InputField>();
        var navigation = field.navigation;
        navigation.mode = Navigation.Mode.None;
        field.navigation = navigation;
        field.transition = Selectable.Transition.None;
        field.targetGraphic = background;
        field.lineType = TMP_InputField.LineType.SingleLine;
        field.characterLimit = characterLimit;
        field.onFocusSelectAll = false;
        // TMP's default caret is invisible on a dark fill.
        field.customCaretColor = true;
        field.caretColor = Theme.Text;
        field.caretWidth = 2;
        field.selectionColor = Theme.TextSelection;

        var viewport = CreateRect("TextArea", rect).Fill(8, 0, 8, 0);
        viewport.gameObject.AddComponent<RectMask2D>();

        var hint = CreateText("Placeholder", viewport, placeholder, fontSize, Theme.TextDim);
        hint.fontStyle = FontStyles.Italic;
        hint.rectTransform.Fill();
        var text = CreateText("Text", viewport, string.Empty, fontSize, Theme.Text);
        text.overflowMode = TextOverflowModes.Overflow; // the field scrolls long text itself
        text.rectTransform.Fill();

        field.textViewport = viewport;
        field.textComponent = text;
        field.placeholder = hint;
        if (Font != null) field.fontAsset = Font;

        field.onSelect.AddListener((UnityEngine.Events.UnityAction<string>)(_ =>
        {
            outline.color = Theme.CardOutline;
            // A selected-but-inactive field shows no caret and doesn't count as typing.
            try { if (!field.isFocused) field.ActivateInputField(); } catch { }
        }));
        field.onDeselect.AddListener((UnityEngine.Events.UnityAction<string>)(_ => outline.color = Theme.ButtonOutline));
        // TMP fires onSubmit from inside its own key handling and keeps using the field afterwards, so the
        // callback (which clears and unfocuses the field, and sends commands) waits for the UI tick.
        field.onSubmit.AddListener((UnityEngine.Events.UnityAction<string>)(value => _submits.Enqueue(() =>
        {
            if (field == null) return;
            try { onSubmit?.Invoke(value); }
            catch (Exception ex) { Core.Log.LogError($"Text box '{name}' failed: {ex}"); }
        })));
        return field;
    }

    /// <summary>Per frame: run the text box submits since the last tick.</summary>
    public static void RunPendingSubmits()
    {
        while (_submits.Count > 0) _submits.Dequeue()();
    }

    /// <summary>Drop submits that can no longer run (left the game).</summary>
    public static void ClearPendingSubmits() => _submits.Clear();

    public sealed class ScrollRef
    {
        public ScrollRect Scroll;
        public RectTransform Viewport;
        public RectTransform Content;
    }

    /// <summary>Vertical scroll view filling <paramref name="parent"/>, with a scrollbar on the right.</summary>
    public static ScrollRef CreateScrollView(string name, Transform parent, float scrollbarWidth = 8f)
    {
        var root = CreateRect(name, parent);
        var scroll = root.gameObject.AddComponent<ScrollRect>();

        var viewportImage = CreatePanel("Viewport", root, new Color(0, 0, 0, 0.001f));
        var viewport = viewportImage.rectTransform.Fill(0, 0, scrollbarWidth + 4, 0);
        viewport.gameObject.AddComponent<RectMask2D>();

        var content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, 0f);

        var track = CreatePanel("Scrollbar", root, new Color(1f, 1f, 1f, 0.05f)).Rounded(scrollbarWidth / 2f);
        var trackRect = track.rectTransform;
        trackRect.anchorMin = new Vector2(1f, 0f);
        trackRect.anchorMax = new Vector2(1f, 1f);
        trackRect.pivot = new Vector2(1f, 0.5f);
        trackRect.sizeDelta = new Vector2(scrollbarWidth, 0f);
        trackRect.anchoredPosition = Vector2.zero;

        var slidingArea = CreateRect("SlidingArea", trackRect).Fill();
        var handle = CreatePanel("Handle", slidingArea, Theme.ScrollHandle).Rounded(scrollbarWidth / 2f);
        handle.rectTransform.Fill();

        var scrollbar = track.gameObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = handle.rectTransform;
        scrollbar.targetGraphic = handle;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        var navigation = scrollbar.navigation;
        navigation.mode = Navigation.Mode.None;
        scrollbar.navigation = navigation;

        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;
        scroll.inertia = false;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        return new ScrollRef { Scroll = scroll, Viewport = viewport, Content = content };
    }
}
