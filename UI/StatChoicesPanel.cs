using TMPro;
using UnityEngine;
using DragUI.Services;

namespace DragUI.UI;

// Pick the Bloodcraft bonus stats for what's equipped: a grid with one tile per stat, lit and numbered once
// chosen. Stats your Bloodcraft class has a synergy with (worth the synergy multiplier more) get a green
// outline, explained by the legend on the heading row. Reset stats asks first, since the server can charge items for it. The tiles are made once and
// restyled, so nothing is created or destroyed while the window is open.
internal abstract class StatChoicesPanel : Window
{
    const float PanelWidth = 540f;
    const int Columns = 3;
    const int Rows = 4;
    const float TileHeight = 58f;
    const float TileGap = 6f;
    const float BadgeSize = 18f;
    const float HeaderY = ContentTop - 2f;
    const float BarY = HeaderY + 28f;
    const float BarHeight = 14f;
    const float HeadingY = BarY + BarHeight + 8f;
    const float GridY = HeadingY + 26f;
    const float StatusY = GridY + Rows * (TileHeight + TileGap);
    const float StatusHeight = 32f;
    const float ButtonsY = StatusY + StatusHeight + 2f;
    const float ButtonHeight = 28f;
    const float FooterY = ButtonsY + ButtonHeight + 6f;
    const float FooterHeight = 44f;
    const float ButtonWidth = 110f;
    const float LegendX = 200f;

    sealed class Tile
    {
        public StatChoices.Stat Stat;
        public UIFactory.ButtonRef Button;
        public TextMeshProUGUI Value;
        public RectTransform Badge;
        public TextMeshProUGUI BadgeText;
        public RectTransform Synergy; // green outline, on while the class has a synergy with this stat
    }

    readonly StatChoices _choices;
    readonly bool _weapon;
    readonly string _none;
    readonly TextMeshProUGUI _type;
    readonly TextMeshProUGUI _level;
    readonly RectTransform _bar;
    readonly RectTransform _fill;
    readonly TextMeshProUGUI _barLabel;
    readonly TextMeshProUGUI _heading;
    readonly TextMeshProUGUI _legend;
    readonly RectTransform _legendSwatch;
    readonly TextMeshProUGUI _status;
    readonly UIFactory.ButtonRef _reset;
    readonly ConfirmDialog _prompt;
    readonly List<Tile> _tiles = new();
    string _promptType; // what the open reset prompt is about

    /// <param name="none">Shown for the type when nothing is known yet or this has no choices (e.g. a blood with no legacy).</param>
    protected StatChoicesPanel(string name, Transform parent, string title, string hint, StatChoices choices, string none, string footerText)
        : base(name, parent, title, hint, PanelWidth, FooterY + FooterHeight + 6f)
    {
        _choices = choices;
        _weapon = choices == StatChoices.Weapon;
        _none = none;

        _type = UIFactory.CreateText("Type", Root, none, 18, Theme.Text);
        _type.fontStyle = FontStyles.Bold;
        _type.rectTransform.Place(14, HeaderY, Width - 200, 26);
        _level = UIFactory.CreateText("Level", Root, string.Empty, 13, Theme.TextDim, TextAlignmentOptions.MidlineRight);
        _level.rectTransform.Place(Width - 14 - 180, HeaderY, 180, 26);

        _bar = UIFactory.CreatePanel("Progress", Root, Theme.Button, raycast: false).Rounded(5f).rectTransform;
        _bar.Place(14, BarY, Width - 28, BarHeight);
        _fill = UIFactory.CreatePanel("Fill", _bar, Theme.Accent, raycast: false).Rounded(4f).rectTransform;
        _fill.Place(1, 1, 0, BarHeight - 2);
        _barLabel = UIFactory.CreateText("Label", _bar, string.Empty, 10, Theme.Text, TextAlignmentOptions.Center);
        _barLabel.rectTransform.Fill();

        _heading = UIFactory.CreateText("Heading", Root, string.Empty, 11, Theme.TextDim);
        _heading.characterSpacing = 3f;
        _heading.rectTransform.Place(14, HeadingY, LegendX - 14, 20);
        _legend = UIFactory.CreateText("Legend", Root, string.Empty, 11, Theme.TextDim, TextAlignmentOptions.MidlineRight);
        _legend.rectTransform.Place(LegendX, HeadingY, Width - 14 - LegendX, 20);
        _legendSwatch = UIFactory.CreatePanel("LegendSwatch", Root, Theme.Button, raycast: false).Rounded(3f).rectTransform;
        _legendSwatch.Place(LegendX, HeadingY + 3, 14, 14);
        UIFactory.AddOutline(_legendSwatch, 3f, Theme.Synergy);
        _legendSwatch.gameObject.SetActive(false);
        UIFactory.CreateDivider(Root, 12, HeadingY + 21, Width - 24);

        float tileWidth = (Width - 24 - TileGap * (Columns - 1)) / Columns;
        for (int i = 0; i < choices.Stats.Count; i++)
            _tiles.Add(CreateTile(choices.Stats[i], 12 + i % Columns * (tileWidth + TileGap), GridY + i / Columns * (TileHeight + TileGap), tileWidth));

        _status = UIFactory.CreateText("Status", Root, string.Empty, 12, Theme.TextDim);
        _status.enableWordWrapping = true;
        _status.rectTransform.Place(14, StatusY, Width - 28, StatusHeight);

        var refresh = UIFactory.CreateButton("Refresh", Root, "Refresh", 13, () =>
        {
            _choices.Refresh();
            Refresh();
        }, radius: 7f);
        refresh.Rect.Place(12, ButtonsY, ButtonWidth, ButtonHeight);
        _reset = UIFactory.CreateButton("Reset", Root, "Reset stats", 13, AskReset, radius: 7f);
        _reset.Rect.Place(Width - 12 - ButtonWidth, ButtonsY, ButtonWidth, ButtonHeight);
        _reset.SetColors(Theme.Button, Theme.AccentHover);

        var footer = UIFactory.CreateText("Footer", Root, footerText, 11, Theme.TextDim);
        footer.enableWordWrapping = true;
        footer.rectTransform.Place(14, FooterY, Width - 28, FooterHeight);

        _prompt = new ConfirmDialog(Root); // last, so it covers everything above
    }

    Tile CreateTile(StatChoices.Stat stat, float x, float y, float width)
    {
        var tile = new Tile { Stat = stat };
        tile.Button = UIFactory.CreateButton(stat.Key, Root, stat.Label, 13, () =>
        {
            _choices.Choose(stat);
            Refresh();
        }, radius: 8f);
        tile.Button.Rect.Place(x, y, width, TileHeight);
        tile.Button.Label.alignment = TextAlignmentOptions.MidlineLeft;
        tile.Button.Label.fontStyle = FontStyles.Bold;
        tile.Button.Label.rectTransform.Place(10, 8, width - 36, 20);

        tile.Value = UIFactory.CreateText("Value", tile.Button.Rect, string.Empty, 11, Theme.TextDim);
        tile.Value.rectTransform.Place(10, 30, width - 20, 18);

        var badge = UIFactory.CreateDot(tile.Button.Rect, width - BadgeSize - 7, 7, BadgeSize, Theme.Text);
        tile.Badge = badge.rectTransform;
        tile.BadgeText = UIFactory.CreateText("Number", tile.Badge, string.Empty, 11, Theme.Accent, TextAlignmentOptions.Center);
        tile.BadgeText.fontStyle = FontStyles.Bold;
        tile.BadgeText.rectTransform.Fill();
        tile.Badge.gameObject.SetActive(false);

        tile.Synergy = UIFactory.AddOutline(tile.Button.Rect, 8f, Theme.Synergy).rectTransform;
        tile.Synergy.gameObject.SetActive(false);
        return tile;
    }

    protected override void OnShown()
    {
        _prompt.Hide(); // don't come back to a question asked before the window was closed
        _choices.Refresh();
        base.OnShown();
    }

    void AskReset()
    {
        string problem = _choices.ResetProblem();
        if (problem != null)
        {
            _choices.Status = problem;
            Refresh();
            return;
        }
        _promptType = _choices.Type;
        _prompt.Show($"Reset {_choices.TypeLabel} stats?",
            $"This clears all your stat choices for {_choices.TypeLabel}. The server may charge items for it; if you can't pay, it says so and nothing changes.",
            "Reset stats", _choices.Reset);
    }

    public override void Refresh()
    {
        if (!Visible) return;
        _choices.Watch();
        // The prompt names what it resets, and the reset applies to what's equipped now.
        if (_prompt.Visible && (_choices.Stale || _choices.Type != _promptType)) _prompt.Hide();

        bool known = _choices.Type != null && !_choices.NoLegacy;
        Set(_type, known ? _choices.TypeLabel : _none);
        Set(_level, _choices.HasProgress
            ? _choices.Prestige > 0 ? $"Level {_choices.Level}  ·  Prestige {_choices.Prestige}" : $"Level {_choices.Level}"
            : string.Empty);
        float progress = _choices.HasProgress ? Mathf.Clamp01(_choices.Percent / 100f) : 0f;
        _fill.sizeDelta = new Vector2((Width - 28 - 2) * progress, BarHeight - 2);
        Set(_barLabel, _choices.HasProgress ? $"{_choices.Percent}% to next level" : string.Empty);

        int count = _choices.Count;
        int limit = _choices.Limit;
        Set(_heading, known ? $"CHOSEN {count} OF {limit}" : "CHOSEN -");
        ClassSynergies.Request();
        UpdateLegend();
        foreach (var tile in _tiles) Restyle(tile, known, count >= limit);

        string resetStyle = _choices.ResetProblem() == null ? "on" : "off";
        if (_reset.Style != resetStyle)
        {
            _reset.Style = resetStyle;
            _reset.Label.color = resetStyle == "on" ? Theme.Text : Theme.TextDim;
        }
        Set(_status, _choices.Status);
    }

    void UpdateLegend()
    {
        bool hasClass = ClassSynergies.Known && ClassSynergies.ClassName.Length > 0;
        string text = !ClassSynergies.Known ? string.Empty
            : !hasClass ? "No class chosen: no synergies"
            : ClassSynergies.Multiplier.Length > 0 ? $"{ClassSynergies.ClassName} synergy: worth x{ClassSynergies.Multiplier}"
            : $"{ClassSynergies.ClassName} synergy";
        if (_legend.text == text && _legendSwatch.gameObject.activeSelf == hasClass) return;
        _legend.text = text;
        _legendSwatch.gameObject.SetActive(hasClass);
        // The swatch sits just left of the right-aligned text.
        float textWidth = _legend.GetPreferredValues(text).x;
        _legendSwatch.Place(Mathf.Max(LegendX, Width - 14 - textWidth - 6 - 14), HeadingY + 3, 14, 14);
    }

    void Restyle(Tile tile, bool known, bool full)
    {
        var stat = tile.Stat;
        bool synergy = ClassSynergies.Known && ClassSynergies.Has(_weapon, stat);
        if (tile.Synergy.gameObject.activeSelf != synergy) tile.Synergy.gameObject.SetActive(synergy);
        string cap = stat.Cap != null ? $"base cap {stat.Cap}" : string.Empty;
        Set(tile.Value, stat.Slot > 0 && stat.Now != null
            ? stat.Cap != null ? $"+{stat.Now}  ·  {cap}" : $"+{stat.Now}"
            : cap);

        bool chosen = stat.Slot > 0;
        bool shown = tile.Badge.gameObject.activeSelf;
        if (shown != chosen) tile.Badge.gameObject.SetActive(chosen);
        if (chosen) Set(tile.BadgeText, stat.Slot.ToString());

        string style = chosen ? "chosen" : known && !full ? "open" : "dim";
        if (tile.Button.Style == style) return;
        tile.Button.Style = style;
        if (chosen) tile.Button.SetColors(Theme.Accent, Theme.AccentHover);
        else tile.Button.SetColors(Theme.Button, Theme.ButtonHover);
        tile.Button.Label.color = style == "dim" ? Theme.TextDim : Theme.Text;
        tile.Value.color = chosen ? Theme.Text : Theme.TextDim;
    }

    static void Set(TextMeshProUGUI text, string value)
    {
        if (text.text != value) text.text = value;
    }
}

internal sealed class WeaponStatsPanel : StatChoicesPanel
{
    public override Vector2 SavedPosition
    {
        get => Settings.WeaponStatsPanelPosition;
        set => Settings.WeaponStatsPanelPosition = value;
    }

    public WeaponStatsPanel(Transform parent)
        : base("WeaponStats", parent, "Weapon Stats", "Bloodcraft expertise", StatChoices.Weapon, "Weapon",
            "Click a stat to add it to the weapon type you have equipped. Each type keeps its own choices, and values grow with its level. " +
            "Bloodcraft can't remove a single stat: Reset stats (.wep rst) clears them all. Base caps don't include class or prestige bonuses.")
    {
    }
}

internal sealed class BloodStatsPanel : StatChoicesPanel
{
    public override Vector2 SavedPosition
    {
        get => Settings.BloodStatsPanelPosition;
        set => Settings.BloodStatsPanelPosition = value;
    }

    public BloodStatsPanel(Transform parent)
        : base("BloodStats", parent, "Blood Stats", "Bloodcraft legacy", StatChoices.Blood, "No blood legacy",
            "Click a stat to add it to the blood type you have now. Each type keeps its own choices, and values grow with its level. " +
            "Bloodcraft can't remove a single stat: Reset stats (.bl rst) clears them all. Base caps don't include class or prestige bonuses.")
    {
    }
}
