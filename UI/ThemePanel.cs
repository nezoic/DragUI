using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DragUI.UI;

// Settings -> Theme: the mod's accent colour. A swatch per palette colour, plus a hex box for a custom
// one. Picking one saves it to the config ([Appearance]); UIManager then rebuilds the whole UI in the
// new colour (this window included, reopened where it was).
internal sealed class ThemePanel : Window
{
    const float PanelWidth = 420f;
    const int Columns = 5;
    const float SwatchWidth = 72f;
    const float SwatchHeight = 40f;
    const float SwatchGap = 8f;
    const float GridTop = ContentTop + 28f;
    const float CustomTop = GridTop + 2 * SwatchHeight + SwatchGap + 22f;
    const float InputWidth = 150f;
    const float UseWidth = 70f;
    const float StatusTop = CustomTop + 26f + 36f;

    readonly Dictionary<AccentColor, Image> _selected = new();
    readonly TMP_InputField _custom;
    readonly TextMeshProUGUI _status;
    string _error;

    public override Vector2 SavedPosition
    {
        get => Settings.ThemePanelPosition;
        set => Settings.ThemePanelPosition = value;
    }

    public ThemePanel(Transform parent)
        : base("Theme", parent, "Theme", "the mod's colours", PanelWidth, StatusTop + 70f)
    {
        Heading("ACCENT COLOUR", ContentTop + 4);

        int i = 0;
        foreach (var (name, color) in Theme.Palette)
        {
            var accent = name;
            float x = 14 + i % Columns * (SwatchWidth + SwatchGap);
            float y = GridTop + i / Columns * (SwatchHeight + SwatchGap);
            var swatch = UIFactory.CreateButton(name.ToString(), Root, name.ToString(), 12, () => Pick(accent), radius: 7f);
            swatch.Rect.Place(x, y, SwatchWidth, SwatchHeight);
            swatch.SetColors(color, Theme.HoverOf(color));
            var ring = UIFactory.AddOutline(swatch.Rect, 7f, Theme.Text);
            ring.gameObject.SetActive(false);
            _selected[name] = ring;
            i++;
        }

        Heading("CUSTOM (HEX CODE)", CustomTop);
        _custom = UIFactory.CreateInputField("Custom", Root, "#3A7BD5", 14, UseCustom, characterLimit: 7);
        _custom.GetComponent<RectTransform>().Place(14, CustomTop + 24, InputWidth, 30);
        _custom.text = Settings.CustomAccentColor;
        var use = UIFactory.CreateButton("Use", Root, "Use", 13, () => UseCustom(_custom.text), radius: 7f);
        use.Rect.Place(14 + InputWidth + 8, CustomTop + 24, UseWidth, 30);
        use.SetColors(Theme.Accent, Theme.AccentHover);

        _status = UIFactory.CreateText("Status", Root, string.Empty, 13, Theme.Text);
        _status.rectTransform.Place(14, StatusTop, Width - 28, 22);

        var footer = UIFactory.CreateText("Footer", Root,
            "Title bars, borders, switched-on buttons and highlights are all shades of it. " +
            "Saved as AccentColor / CustomAccentColor under [Appearance] in nezoic.DragUI.cfg.",
            11, Theme.TextDim);
        footer.enableWordWrapping = true;
        footer.rectTransform.Place(14, StatusTop + 26, Width - 28, 36);
    }

    void Heading(string text, float y)
    {
        var heading = UIFactory.CreateText("Heading", Root, text, 11, Theme.TextDim);
        heading.characterSpacing = 4f;
        heading.rectTransform.Place(14, y, Width - 28, 20);
    }

    static bool CustomActive => Settings.CustomAccentColor.Length > 0 && Theme.TryParseHex(Settings.CustomAccentColor, out _);

    void Pick(AccentColor color)
    {
        _error = null;
        Settings.SetAccentColor(color);
    }

    void UseCustom(string text)
    {
        _custom.DeactivateInputField();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        text = (text ?? string.Empty).Trim();
        if (!Theme.TryParseHex(text, out _))
        {
            _error = $"'{text}' isn't a hex colour: use six digits like #3A7BD5.";
            return;
        }
        _error = null;
        Settings.SetCustomAccentColor(text.StartsWith("#") ? text.ToUpperInvariant() : "#" + text.ToUpperInvariant());
    }

    public override void Refresh()
    {
        if (!Visible) return;
        bool custom = CustomActive;
        foreach (var (name, ring) in _selected)
        {
            bool on = !custom && name == Settings.AccentColor;
            if (ring.gameObject.activeSelf != on) ring.gameObject.SetActive(on);
        }
        _status.text = _error != null
            ? $"<color=#EBBA52>{_error}</color>"
            : custom ? $"Using your colour {Settings.CustomAccentColor}." : $"Using {Settings.AccentColor}.";
    }
}
