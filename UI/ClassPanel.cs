using TMPro;
using UnityEngine;
using DragUI.Services;

namespace DragUI.UI;

// Character -> Class: every Bloodcraft class as a tile with its weapon and blood stat synergies, the
// current one lit. Clicking another asks first, naming what it costs (the first class is free), then
// sends .class s / .class c; the server's answer shows in the status line. The tiles are made once and
// restyled, like the stat windows.
internal sealed class ClassPanel : Window
{
    const float PanelWidth = 560f;
    const int Columns = 2;
    const float TileHeight = 108f;
    const float TileGap = 8f;
    const float HeaderY = ContentTop - 2f;
    const float HeadingY = HeaderY + 34f;
    const float GridY = HeadingY + 28f;
    const float StatusY = GridY + 3 * (TileHeight + TileGap);
    const float StatusHeight = 32f;
    const float ButtonsY = StatusY + StatusHeight + 2f;
    const float ButtonHeight = 28f;
    const float FooterY = ButtonsY + ButtonHeight + 6f;
    const float FooterHeight = 44f;
    const float ButtonWidth = 110f;

    sealed class Tile
    {
        public int ClassId;
        public UIFactory.ButtonRef Button;
        public TextMeshProUGUI Tag;
        public TextMeshProUGUI Weapon;
        public TextMeshProUGUI Blood;
    }

    readonly TextMeshProUGUI _current;
    readonly TextMeshProUGUI _cost;
    readonly TextMeshProUGUI _legend;
    readonly TextMeshProUGUI _status;
    readonly ConfirmDialog _prompt;
    readonly List<Tile> _tiles = new();

    public override Vector2 SavedPosition
    {
        get => Settings.ClassPanelPosition;
        set => Settings.ClassPanelPosition = value;
    }

    public ClassPanel(Transform parent)
        : base("Class", parent, "Class", "Bloodcraft classes", PanelWidth, FooterY + FooterHeight + 6f)
    {
        _current = UIFactory.CreateText("Current", Root, string.Empty, 18, Theme.Text);
        _current.fontStyle = FontStyles.Bold;
        _current.rectTransform.Place(14, HeaderY, 250, 26);
        _cost = UIFactory.CreateText("Cost", Root, string.Empty, 12, Theme.TextDim, TextAlignmentOptions.MidlineRight);
        _cost.rectTransform.Place(270, HeaderY, Width - 14 - 270, 26);

        var heading = UIFactory.CreateText("Heading", Root, "CLASSES", 11, Theme.TextDim);
        heading.characterSpacing = 3f;
        heading.rectTransform.Place(14, HeadingY, 120, 20);
        _legend = UIFactory.CreateText("Legend", Root, string.Empty, 11, Theme.TextDim, TextAlignmentOptions.MidlineRight);
        _legend.rectTransform.Place(140, HeadingY, Width - 14 - 140, 20);
        UIFactory.CreateDivider(Root, 12, HeadingY + 21, Width - 24);

        float tileWidth = (Width - 24 - TileGap * (Columns - 1)) / Columns;
        int i = 0;
        foreach (int id in ClassChoices.ClassIds)
        {
            _tiles.Add(CreateTile(id, 12 + i % Columns * (tileWidth + TileGap), GridY + i / Columns * (TileHeight + TileGap), tileWidth));
            i++;
        }

        _status = UIFactory.CreateText("Status", Root, string.Empty, 12, Theme.TextDim);
        _status.enableWordWrapping = true;
        _status.rectTransform.Place(14, StatusY, Width - 28, StatusHeight);

        var refresh = UIFactory.CreateButton("Refresh", Root, "Refresh", 13, () =>
        {
            ClassChoices.Reload();
            Refresh();
        }, radius: 7f);
        refresh.Rect.Place(12, ButtonsY, ButtonWidth, ButtonHeight);

        var footer = UIFactory.CreateText("Footer", Root,
            "Synergy stats are worth more for that class: Bloodcraft multiplies their weapon expertise and blood legacy bonuses. " +
            "Click a class to pick it (.class s, free the first time) or change to it (.class c). Read from Bloodcraft's replies, which are hidden.",
            11, Theme.TextDim);
        footer.enableWordWrapping = true;
        footer.rectTransform.Place(14, FooterY, Width - 28, FooterHeight);

        _prompt = new ConfirmDialog(Root); // last, so it covers everything above
    }

    Tile CreateTile(int classId, float x, float y, float width)
    {
        var tile = new Tile { ClassId = classId };
        tile.Button = UIFactory.CreateButton(ClassChoices.Label(classId), Root, ClassChoices.Label(classId), 14, () => Clicked(classId), radius: 8f);
        tile.Button.Rect.Place(x, y, width, TileHeight);
        tile.Button.Label.alignment = TextAlignmentOptions.MidlineLeft;
        tile.Button.Label.fontStyle = FontStyles.Bold;
        tile.Button.Label.rectTransform.Place(10, 8, width - 100, 20);

        tile.Tag = UIFactory.CreateText("Tag", tile.Button.Rect, string.Empty, 10, Theme.Text, TextAlignmentOptions.MidlineRight);
        tile.Tag.characterSpacing = 2f;
        tile.Tag.rectTransform.Place(width - 90, 8, 80, 20);

        tile.Weapon = UIFactory.CreateText("Weapon", tile.Button.Rect, string.Empty, 11, Theme.TextDim, TextAlignmentOptions.TopLeft);
        tile.Weapon.enableWordWrapping = true;
        tile.Weapon.rectTransform.Place(10, 32, width - 20, 34);
        tile.Blood = UIFactory.CreateText("Blood", tile.Button.Rect, string.Empty, 11, Theme.TextDim, TextAlignmentOptions.TopLeft);
        tile.Blood.enableWordWrapping = true;
        tile.Blood.rectTransform.Place(10, 68, width - 20, 34);
        return tile;
    }

    protected override void OnShown()
    {
        _prompt.Hide(); // don't come back to a question asked before the window was closed
        base.OnShown();
    }

    void Clicked(int classId)
    {
        if (ClassChoices.ClassesDisabled || !ClassChoices.CurrentKnown || ClassChoices.ChangePending) return;
        if (classId == ClassChoices.CurrentId) return;

        string name = ClassChoices.Label(classId);
        if (ClassChoices.CurrentId == 0)
        {
            _prompt.Show($"Pick {name}?",
                ClassChoices.CostKnown && !ClassChoices.CostFree
                    ? $"Your first class is free. Changing it later costs {ClassChoices.CostText}."
                    : "Your first class is free (.class s).",
                "Pick class", () => ClassChoices.Change(classId));
            return;
        }

        string cost = !ClassChoices.CostKnown
            ? "The server may charge items for it; if you can't pay, it says so and nothing changes."
            : ClassChoices.CostFree
                ? "Changing class is free on this server."
                : $"This costs {ClassChoices.CostText}, taken from your inventory. If you don't have enough, nothing changes.";
        _prompt.Show($"Change to {name}?", $"{cost} Class passives must be off (.class passives).", "Change class",
            () => ClassChoices.Change(classId));
    }

    public override void Refresh()
    {
        if (!Visible) return;
        ClassChoices.RequestAll();
        bool known = ClassChoices.CurrentKnown;
        int current = ClassChoices.CurrentId;
        if (_prompt.Visible && (ClassChoices.ChangePending || !known)) _prompt.Hide();

        Set(_current, ClassChoices.ClassesDisabled ? "Classes are off"
            : !known ? "Reading your class..."
            : current == 0 ? "No class chosen" : ClassChoices.ClassName);
        Set(_cost, !known || ClassChoices.ClassesDisabled ? string.Empty
            : current == 0 ? "First class: free"
            : !ClassChoices.CostKnown ? "Change cost: checking..."
            : ClassChoices.CostFree ? "Changing class: free" : $"Change cost: {ClassChoices.CostText}");
        string mult = ClassChoices.Multiplier;
        Set(_legend, mult.Length > 0 ? $"Synergy stats are worth x{mult} for their class" : string.Empty);

        foreach (var tile in _tiles) Restyle(tile, known, current);

        Set(_status, ClassChoices.ClassesDisabled ? "This server has Bloodcraft's classes turned off."
            : ClassChoices.ChangePending ? ClassChoices.Status ?? "Changing class..."
            : ClassChoices.Status ?? (known ? current == 0 ? "Click a class to pick it." : "Click another class to change to it." : string.Empty));
    }

    void Restyle(Tile tile, bool known, int current)
    {
        int id = tile.ClassId;
        bool synergiesKnown = ClassChoices.SynergiesKnown(id);
        Set(tile.Weapon, SynergyLine("Weapon", synergiesKnown, ClassChoices.SynergyLabels(id, true)));
        Set(tile.Blood, SynergyLine("Blood", synergiesKnown, ClassChoices.SynergyLabels(id, false)));

        bool isCurrent = known && id == current;
        Set(tile.Tag, isCurrent ? "CURRENT" : string.Empty);
        bool clickable = known && !isCurrent && !ClassChoices.ChangePending && !ClassChoices.ClassesDisabled;
        string style = isCurrent ? "current" : clickable ? "open" : "dim";
        if (tile.Button.Style == style) return;
        tile.Button.Style = style;
        if (isCurrent) tile.Button.SetColors(Theme.Accent, Theme.Accent);
        else tile.Button.SetColors(Theme.Button, clickable ? Theme.ButtonHover : Theme.Button);
        tile.Button.Label.color = style == "dim" ? Theme.TextDim : Theme.Text;
        tile.Weapon.color = tile.Blood.color = isCurrent ? Theme.Text : Theme.TextDim;
    }

    static string SynergyLine(string kind, bool known, IEnumerable<string> stats)
    {
        if (!known) return $"<b>{kind}:</b> reading...";
        string list = string.Join(", ", stats);
        return $"<b>{kind}:</b> {(list.Length > 0 ? list : "none")}";
    }

    static void Set(TextMeshProUGUI text, string value)
    {
        if (text.text != value) text.text = value;
    }
}
