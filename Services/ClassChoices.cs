using System.Globalization;
using System.Text.RegularExpressions;
using ProjectM.Network;
using UnityEngine;

namespace DragUI.Services;

// Bloodcraft classes: which one the player has, every class's weapon / blood stat synergies (stats whose
// expertise / legacy bonus the class multiplies by SynergyMultiplier), what changing class costs, and the
// change itself. Eclipse data is used when it has arrived (BloodcraftStats: the config lists every class's
// synergies, progress the current class); the rest is asked in chat, one command at a time, the replies
// hidden:
//   .class lst          "Invalid class, use '.class l' ..." (always, the empty argument doesn't parse), then the
//                       current class's "<Class> stat synergies [x1.5]:" + lines of up to 6
//                       "MaxHealth (Weapon), ..., HealingReceived (Blood)"; only the "Invalid class" with no class.
//   .class lst <Class>  the same for that class ("Couldn't find / No stat synergies found for <Class>" = none).
//   .class s <current>  with a class already chosen this changes nothing and names the cost:
//                       "You've already selected X, use '.class c [Class]' to change. (<item>x<qty>)".
//                       Only sent when the class is known, since with none it would pick that class.
//   .class s / c <Class>  pick the first class (free) / change class (costs <item>x<qty> unless the server
//                       sets no item): "You've selected X!", "Class changed to X!", "You do not have enough of
//                       the required item to change classes (<item>x<qty>)", "You have class buffs enabled ...".
// Class names in the replies are Bloodcraft's FormatClassName ("Blood Knight", coloured); commands take
// the PlayerClass name (BloodKnight). "You've selected X!" / "Class changed to X!" from a command typed by
// hand or a loadout are followed too.
internal static class ClassChoices
{
    const double QuietSeconds = 0.4;   // a reply is complete once nothing more arrives for this long
    const double TimeoutSeconds = 4;
    const double RetrySeconds = 60;    // a server that doesn't answer (old Bloodcraft) isn't asked over and over
    const string EmptyItem = "LocalizationKey.Empty"; // Bloodcraft's name for item 0 (no cost configured)

    enum Kind { Current, Synergies, Cost, Change }

    sealed class Ask
    {
        public Kind Kind;
        public int ClassId;
        public string Command;
    }

    sealed class Synergy
    {
        public readonly HashSet<string> Weapon = new(StringComparer.Ordinal);
        public readonly HashSet<string> Blood = new(StringComparer.Ordinal);
    }

    static readonly Regex _richText = new("<[^>]*>", RegexOptions.Compiled);
    static readonly Regex _header = new(@"^(?<class>.+?) stat synergies ?\[x(?<mult>[^\]]+)\]:$", RegexOptions.Compiled);
    static readonly Regex _entry = new(@"^(?<stat>\w+) \((?<kind>Weapon|Blood)\)$", RegexOptions.Compiled);
    static readonly Regex _noSynergies = new(@"^(?:Couldn't find stat synergies for|No stat synergies found for) (?<class>.+?)\.*$", RegexOptions.Compiled);
    static readonly Regex _selected = new(@"^You've selected (?<class>.+)!$", RegexOptions.Compiled);
    static readonly Regex _changed = new(@"^Class changed to (?<class>.+)!$", RegexOptions.Compiled);
    static readonly Regex _already = new(@"^You've already selected (?<class>.+?), use '\.class c \[Class\]' to change\. \((?<item>.*)x(?<qty>\d+)\)$", RegexOptions.Compiled);
    static readonly Regex _notEnough = new(@"^You do not have enough of the required item to change classes \((?<item>.*)x(?<qty>\d+)\)$", RegexOptions.Compiled);
    static readonly Regex _failedRemove = new(@"^Failed to remove enough of the item required \((?<item>.*)x(?<qty>\d+)\)$", RegexOptions.Compiled);
    const string InvalidClass = "Invalid class, use";
    const string Disabled = "Classes are not enabled.";
    const string ClassBuffsOn = "You have class buffs enabled";
    const string NoClassYet = "You haven't selected a class yet";

    // From chat (used when Eclipse's data isn't there).
    static readonly Dictionary<int, Synergy> _chatSynergies = new();
    static string _chatMultiplier = string.Empty;
    static int _chatCurrent = -1;       // -1 unknown, 0 none, else 1-based class id
    static double _chatCurrentAt = double.NegativeInfinity;
    static bool _disabled;
    static bool _costKnown;
    static string _costItem = string.Empty;
    static int _costQuantity;

    static readonly List<Ask> _queue = new();
    static readonly Dictionary<string, double> _askedAt = new(StringComparer.Ordinal);
    static Ask _inFlight;
    static bool _gotReply;
    static double _sentAt, _lastReplyAt;
    static Synergy _reading; // the class whose synergy lines are arriving

    /// <summary>Class ids, 1-based (Bloodcraft's PlayerClass order).</summary>
    public static IEnumerable<int> ClassIds => Enumerable.Range(1, BloodcraftStats.ClassNames.Length);

    public static string Label(int classId) => BloodcraftStats.ClassLabel(classId);

    /// <summary>True when the server says classes are off.</summary>
    public static bool ClassesDisabled => _disabled;

    /// <summary>True once the player's class is known (possibly none).</summary>
    public static bool CurrentKnown => BloodcraftStats.ProgressKnown || _chatCurrent >= 0;

    /// <summary>The player's class id (0 = none chosen), -1 while unknown. The newer of Eclipse's data and chat news.</summary>
    public static int CurrentId =>
        BloodcraftStats.ProgressKnown && BloodcraftStats.ProgressAt >= _chatCurrentAt ? BloodcraftStats.ClassId : _chatCurrent;

    /// <summary>The current class's name, e.g. "Blood Knight"; empty when none is chosen or it isn't known.</summary>
    public static string ClassName => CurrentId > 0 ? Label(CurrentId) : string.Empty;

    /// <summary>The synergy multiplier as text, e.g. "1.5"; empty until known.</summary>
    public static string Multiplier => BloodcraftStats.ConfigKnown
        ? BloodcraftStats.SynergyMultiplier.ToString("0.##", CultureInfo.InvariantCulture)
        : _chatMultiplier;

    /// <summary>True once the current class's synergies are known (or that there's no class).</summary>
    public static bool Known => CurrentKnown && (CurrentId == 0 || SynergiesKnown(CurrentId));

    /// <summary>Does the current class have a synergy with this weapon (or blood) stat?</summary>
    public static bool Has(bool weapon, StatChoices.Stat stat) => CurrentId > 0 && HasSynergy(CurrentId, weapon, stat);

    public static bool SynergiesKnown(int classId) => BloodcraftStats.ConfigKnown || _chatSynergies.ContainsKey(classId);

    static bool HasSynergy(int classId, bool weapon, StatChoices.Stat stat)
    {
        if (BloodcraftStats.ConfigKnown)
            return BloodcraftStats.TryGetSynergies(classId, out var w, out var b) && (weapon ? w : b).Contains(stat.Code);
        return _chatSynergies.TryGetValue(classId, out var synergy) && (weapon ? synergy.Weapon : synergy.Blood).Contains(stat.Key);
    }

    /// <summary>A class's synergy stats, as StatChoices labels (empty until known).</summary>
    public static IEnumerable<string> SynergyLabels(int classId, bool weapon) =>
        (weapon ? StatChoices.Weapon : StatChoices.Blood).Stats.Where(s => HasSynergy(classId, weapon, s)).Select(s => s.Label);

    /// <summary>True once the cost of changing class is known.</summary>
    public static bool CostKnown => _costKnown;

    /// <summary>True when changing class costs nothing on this server (valid when CostKnown).</summary>
    public static bool CostFree => _costQuantity <= 0 || _costItem.Length == 0 || _costItem == EmptyItem;

    /// <summary>What changing class costs, e.g. "Greater Stygian Shards x750" (valid when CostKnown and not free).</summary>
    public static string CostText => $"{_costItem} x{_costQuantity}";

    /// <summary>The outcome of the last change, for the Class window; null when there's nothing to say.</summary>
    public static string Status { get; private set; }

    /// <summary>A change has been sent (or is queued) and hasn't been answered yet.</summary>
    public static bool ChangePending => _inFlight?.Kind == Kind.Change || _queue.Any(a => a.Kind == Kind.Change);

    /// <summary>For the stat windows: ask for the current class and its synergies if they aren't known.</summary>
    public static void Request()
    {
        if (_disabled) return;
        if (!CurrentKnown) Enqueue(Kind.Current, 0, ".class lst");
        else if (CurrentId > 0 && !SynergiesKnown(CurrentId)) Enqueue(Kind.Synergies, CurrentId, $".class lst {BloodcraftStats.ClassNames[CurrentId - 1]}");
    }

    /// <summary>For the Class window: also every class's synergies and the cost of changing.</summary>
    public static void RequestAll()
    {
        Request();
        if (_disabled) return;
        foreach (int id in ClassIds)
            if (!SynergiesKnown(id)) Enqueue(Kind.Synergies, id, $".class lst {BloodcraftStats.ClassNames[id - 1]}");
        if (!_costKnown && CurrentKnown && CurrentId > 0)
            Enqueue(Kind.Cost, CurrentId, $".class s {BloodcraftStats.ClassNames[CurrentId - 1]}");
    }

    /// <summary>Forget what was read from chat and ask again (the Class window's Refresh).</summary>
    public static void Reload()
    {
        _chatSynergies.Clear();
        _chatMultiplier = string.Empty;
        _chatCurrent = -1;
        _chatCurrentAt = double.NegativeInfinity;
        _disabled = false;
        _costKnown = false;
        _askedAt.Clear();
        _queue.RemoveAll(a => a.Kind != Kind.Change);
        Status = null;
        RequestAll();
    }

    /// <summary>Pick (no class yet, free) or change to (costs the server's item) class <paramref name="classId"/>.</summary>
    public static void Change(int classId)
    {
        if (classId < 1 || classId > BloodcraftStats.ClassNames.Length || ChangePending || !CurrentKnown) return;
        string name = BloodcraftStats.ClassNames[classId - 1];
        bool first = CurrentId == 0;
        _queue.Insert(0, new Ask { Kind = Kind.Change, ClassId = classId, Command = first ? $".class s {name}" : $".class c {name}" });
        Status = $"{(first ? "Picking" : "Changing to")} {Label(classId)}...";
        Pump();
    }

    static void Enqueue(Kind kind, int classId, string command)
    {
        if (_inFlight?.Command == command || _queue.Any(a => a.Command == command)) return;
        if (_askedAt.TryGetValue(command, out double at) && Time.realtimeSinceStartupAsDouble - at < RetrySeconds) return;
        _queue.Add(new Ask { Kind = kind, ClassId = classId, Command = command });
        Pump();
    }

    static void Pump()
    {
        while (_inFlight == null && _queue.Count > 0)
        {
            var ask = _queue[0];
            _queue.RemoveAt(0);
            double now = Time.realtimeSinceStartupAsDouble;
            _askedAt[ask.Command] = now;
            if (!ChatCommandSender.TrySend(ask.Command))
            {
                if (ask.Kind == Kind.Change) Status = "Couldn't send the command (not in game?).";
                continue;
            }
            _inFlight = ask;
            _gotReply = false;
            _sentAt = now;
            _reading = null;
            Core.Diag($"Classes: sent {ask.Command}.");
        }
    }

    /// <summary>Per frame: finish the command in flight once its reply is complete (or never came), send the next.</summary>
    public static void Tick()
    {
        if (_inFlight == null) return;
        double now = Time.realtimeSinceStartupAsDouble;
        bool done = _gotReply ? now - _lastReplyAt >= QuietSeconds : now - _sentAt >= TimeoutSeconds;
        if (!done) return;
        if (!_gotReply)
        {
            Core.Diag($"Classes: no answer to {_inFlight.Command}.");
            if (_inFlight.Kind == Kind.Change) Status = "No answer from the server: is Bloodcraft running with classes on?";
        }
        else Core.Diag($"Classes: {_inFlight.Command} answered; class {(CurrentKnown ? CurrentId.ToString() : "?")}, " +
                       $"synergies for {string.Join(", ", ClassIds.Where(SynergiesKnown))}, cost {(_costKnown ? CostFree ? "free" : CostText : "?")}.");
        _inFlight = null;
        _reading = null;
        Pump();
    }

    /// <summary>Read Bloodcraft's replies. True = a reply to one of our commands, hide it from chat.</summary>
    public static bool OnServerMessage(ServerChatMessageType type, string message)
    {
        if (type != ServerChatMessageType.System || string.IsNullOrEmpty(message)) return false;
        string text = _richText.Replace(message, string.Empty).Trim();

        if (_inFlight != null && Claim(_inFlight, text))
        {
            _gotReply = true;
            _lastReplyAt = Time.realtimeSinceStartupAsDouble;
            return true;
        }

        // A class picked or changed some other way (typed, or a loadout).
        var news = _selected.Match(text);
        if (!news.Success) news = _changed.Match(text);
        if (news.Success && ParseClass(news.Groups["class"].Value) is var id and > 0) SetCurrent(id);
        return false;
    }

    static bool Claim(Ask ask, string text)
    {
        switch (ask.Kind)
        {
            case Kind.Current:
            case Kind.Synergies:
                return ClaimSynergies(ask, text);
            case Kind.Cost:
                return ClaimCost(text);
            default:
                return ClaimChange(ask, text);
        }
    }

    static bool ClaimSynergies(Ask ask, string text)
    {
        var header = _header.Match(text);
        if (header.Success)
        {
            int id = ParseClass(header.Groups["class"].Value);
            if (id == 0) id = ask.ClassId;
            _chatMultiplier = header.Groups["mult"].Value.Trim();
            _reading = id > 0 ? _chatSynergies[id] = new Synergy() : null;
            if (ask.Kind == Kind.Current && id > 0) SetCurrent(id);
            return true;
        }

        var parts = text.Split(", ");
        if (_gotReply && parts.All(p => _entry.IsMatch(p)))
        {
            if (_reading != null)
                foreach (var part in parts)
                {
                    var entry = _entry.Match(part);
                    (entry.Groups["kind"].Value == "Weapon" ? _reading.Weapon : _reading.Blood).Add(entry.Groups["stat"].Value);
                }
            return true;
        }

        var none = _noSynergies.Match(text);
        if (none.Success)
        {
            int id = ParseClass(none.Groups["class"].Value);
            if (id == 0) id = ask.ClassId;
            if (id > 0) _chatSynergies[id] = new Synergy();
            if (ask.Kind == Kind.Current && id > 0) SetCurrent(id);
            return true;
        }

        if (text.StartsWith(InvalidClass, StringComparison.Ordinal))
        {
            // With no argument this comes first even with a class; the header that follows sets it.
            if (ask.Kind == Kind.Current) SetCurrent(0);
            else if (ask.ClassId > 0) _chatSynergies[ask.ClassId] = new Synergy();
            return true;
        }
        if (text == Disabled)
        {
            _disabled = true;
            if (ask.Kind == Kind.Current) SetCurrent(0);
            return true;
        }
        return false;
    }

    static bool ClaimCost(string text)
    {
        var already = _already.Match(text);
        if (already.Success)
        {
            if (ParseClass(already.Groups["class"].Value) is var id and > 0) SetCurrent(id);
            SetCost(already);
            return true;
        }
        var selected = _selected.Match(text);
        if (selected.Success)
        {
            // We thought a class was chosen and there wasn't one: this just picked it (free).
            if (ParseClass(selected.Groups["class"].Value) is var id and > 0) SetCurrent(id);
            Core.Log.LogInfo($"Classes: the cost check picked {selected.Groups["class"].Value}, as no class was chosen yet.");
            return true;
        }
        return text.StartsWith(InvalidClass, StringComparison.Ordinal) || text == Disabled;
    }

    static bool ClaimChange(Ask ask, string text)
    {
        var done = _changed.Match(text);
        if (!done.Success) done = _selected.Match(text);
        if (done.Success)
        {
            int id = ParseClass(done.Groups["class"].Value);
            if (id == 0) id = ask.ClassId;
            SetCurrent(id);
            Status = $"You're now {Label(id)}.";
            return true;
        }
        var notEnough = _notEnough.Match(text);
        if (notEnough.Success)
        {
            SetCost(notEnough);
            Status = $"Not enough items: changing class costs {CostText}. Nothing changed.";
            return true;
        }
        var failed = _failedRemove.Match(text);
        if (failed.Success)
        {
            SetCost(failed);
            Status = "The server couldn't take the items, so nothing changed.";
            return true;
        }
        var already = _already.Match(text);
        if (already.Success)
        {
            // We sent .class s, but a class was already chosen.
            if (ParseClass(already.Groups["class"].Value) is var id and > 0) SetCurrent(id);
            SetCost(already);
            Status = $"You already have a class ({already.Groups["class"].Value}). Click again to change it.";
            return true;
        }
        if (text.StartsWith(ClassBuffsOn, StringComparison.Ordinal))
        {
            Status = "Your class passives are on: turn them off with .class passives, then try again.";
            return true;
        }
        if (text.StartsWith(NoClassYet, StringComparison.Ordinal))
        {
            SetCurrent(0);
            Status = "You have no class yet. Click it again to pick it (free).";
            return true;
        }
        if (text.StartsWith(InvalidClass, StringComparison.Ordinal))
        {
            Status = "The server didn't recognise that class.";
            return true;
        }
        if (text == Disabled)
        {
            _disabled = true;
            Status = "Classes are turned off on this server.";
            return true;
        }
        return false;
    }

    static void SetCurrent(int id)
    {
        _chatCurrent = id;
        _chatCurrentAt = Time.realtimeSinceStartupAsDouble;
    }

    static void SetCost(Match match)
    {
        _costItem = match.Groups["item"].Value.Trim();
        _costQuantity = int.TryParse(match.Groups["qty"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int q) ? q : 0;
        _costKnown = true;
    }

    /// <summary>"Blood Knight" / "BloodKnight" -> 1-based class id; 0 if it isn't one.</summary>
    static int ParseClass(string name)
    {
        string compact = name.Replace(" ", string.Empty).Trim();
        int index = Array.FindIndex(BloodcraftStats.ClassNames, c => string.Equals(c, compact, StringComparison.OrdinalIgnoreCase));
        return index + 1;
    }

    public static void OnWorldTeardown()
    {
        _chatSynergies.Clear();
        _chatMultiplier = string.Empty;
        _chatCurrent = -1;
        _chatCurrentAt = double.NegativeInfinity;
        _disabled = false;
        _costKnown = false;
        _costItem = string.Empty;
        _costQuantity = 0;
        _queue.Clear();
        _askedAt.Clear();
        _inFlight = null;
        _reading = null;
        _gotReply = false;
        Status = null;
    }
}
