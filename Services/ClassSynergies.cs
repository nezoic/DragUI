using System.Globalization;
using System.Text.RegularExpressions;
using ProjectM.Network;
using UnityEngine;

namespace DragUI.Services;

// Which weapon and blood stats the player's Bloodcraft class has a synergy with: Bloodcraft multiplies
// those stats' expertise / legacy bonuses by its SynergyMultiplier. Taken from Bloodcraft's Eclipse data
// when that has arrived (BloodcraftStats); otherwise asked with .class lst, its reply hidden from chat:
//   "<class> stat synergies [x1.5]:"   then lines of up to 6 "MaxHealth (Weapon), ..., HealingReceived (Blood)"
//   "Invalid class, use '.class l' to see options."   no class chosen (also "Classes are not enabled.")
// The stat names are Bloodcraft's WeaponStatType / BloodStatType names, the same keys StatChoices uses.
// Asked again after a "You've selected X!" / "Class changed to X!" reply.
internal static class ClassSynergies
{
    const double QuietSeconds = 0.4;   // the reply is complete once nothing more arrives for this long
    const double TimeoutSeconds = 4;
    const double RetrySeconds = 60;    // a server that doesn't answer (old Bloodcraft) isn't asked over and over

    static readonly Regex _richText = new("<[^>]*>", RegexOptions.Compiled);
    static readonly Regex _header = new(@"^(?<class>.+?) stat synergies ?\[x(?<mult>[^\]]+)\]:$", RegexOptions.Compiled);
    static readonly Regex _entry = new(@"^(?<stat>\w+) \((?<kind>Weapon|Blood)\)$", RegexOptions.Compiled);
    static readonly Regex _classChanged = new(@"^(?:You've selected .+|Class changed to .+)!$", RegexOptions.Compiled);
    static readonly string[] _noSynergies =
    {
        "Invalid class, use", "Classes are not enabled.", "Couldn't find stat synergies for", "No stat synergies found for",
    };

    static readonly HashSet<string> _weapon = new(StringComparer.Ordinal);
    static readonly HashSet<string> _blood = new(StringComparer.Ordinal);
    static bool _read;   // .class lst answered since joining / the last class change
    static string _className = string.Empty;
    static string _multiplier = string.Empty;
    static bool _awaiting, _gotReply;
    static double _sentAt, _lastReplyAt;
    static double _askedAt = double.NegativeInfinity;

    /// <summary>True once the class's synergies are known (possibly none: no class chosen).</summary>
    public static bool Known => BloodcraftStats.SynergiesKnown || _read;

    /// <summary>The class they're for, e.g. "Blood Knight"; empty when no class is chosen.</summary>
    public static string ClassName => BloodcraftStats.SynergiesKnown
        ? BloodcraftStats.ClassId == 0 ? string.Empty : BloodcraftStats.ClassLabel(BloodcraftStats.ClassId)
        : _className;

    /// <summary>The synergy multiplier as text, e.g. "1.5".</summary>
    public static string Multiplier => BloodcraftStats.SynergiesKnown
        ? BloodcraftStats.SynergyMultiplier.ToString("0.##", CultureInfo.InvariantCulture)
        : _multiplier;

    /// <summary>Does the current class have a synergy with this weapon (or blood) stat?</summary>
    public static bool Has(bool weapon, StatChoices.Stat stat) =>
        BloodcraftStats.SynergiesKnown
            ? BloodcraftStats.HasSynergy(weapon, stat.Code)
            : _read && (weapon ? _weapon : _blood).Contains(stat.Key);

    /// <summary>Ask the server if it isn't known yet (called while a stat window is open).</summary>
    public static void Request()
    {
        if (Known || _awaiting) return;
        double now = Time.realtimeSinceStartupAsDouble;
        if (now - _askedAt < RetrySeconds) return;
        _askedAt = now;
        if (!ChatCommandSender.TrySend(".class lst")) return;
        _awaiting = true;
        _gotReply = false;
        _sentAt = now;
        Core.Diag("Class synergies: asked with .class lst.");
    }

    /// <summary>Per frame: stop hiding replies once the answer is complete (or never came).</summary>
    public static void Tick()
    {
        if (!_awaiting) return;
        double now = Time.realtimeSinceStartupAsDouble;
        bool done = _gotReply ? now - _lastReplyAt >= QuietSeconds : now - _sentAt >= TimeoutSeconds;
        if (!done) return;
        _awaiting = false;
        Core.Diag(_read
            ? $"Class synergies: {(_className.Length == 0 ? "no class" : _className)}, weapon [{string.Join(", ", _weapon)}], blood [{string.Join(", ", _blood)}]."
            : "Class synergies: no answer to .class lst.");
    }

    /// <summary>Read Bloodcraft's replies. True = the reply to our .class lst, hide it from chat.</summary>
    public static bool OnServerMessage(ServerChatMessageType type, string message)
    {
        if (type != ServerChatMessageType.System || string.IsNullOrEmpty(message)) return false;
        string text = _richText.Replace(message, string.Empty).Trim();

        if (!_awaiting)
        {
            // The player changed class (by hand, or a loadout did): ask again next time a stat window is open.
            if (_classChanged.IsMatch(text)) Invalidate();
            return false;
        }

        Match header = _header.Match(text);
        if (header.Success)
        {
            _weapon.Clear();
            _blood.Clear();
            _className = header.Groups["class"].Value.Trim();
            _multiplier = header.Groups["mult"].Value.Trim();
            _read = true;
            return Replied();
        }

        var parts = text.Split(", ");
        if (_read && parts.Length > 0 && parts.All(p => _entry.IsMatch(p)))
        {
            foreach (var part in parts)
            {
                var entry = _entry.Match(part);
                (entry.Groups["kind"].Value == "Weapon" ? _weapon : _blood).Add(entry.Groups["stat"].Value);
            }
            return Replied();
        }

        if (_noSynergies.Any(n => text.StartsWith(n, StringComparison.Ordinal)))
        {
            _weapon.Clear();
            _blood.Clear();
            _className = string.Empty;
            _multiplier = string.Empty;
            _read = true;
            return Replied();
        }
        return false;
    }

    static bool Replied()
    {
        _gotReply = true;
        _lastReplyAt = Time.realtimeSinceStartupAsDouble;
        return true;
    }

    static void Invalidate()
    {
        _read = false;
        _askedAt = double.NegativeInfinity;
    }

    public static void OnWorldTeardown()
    {
        _weapon.Clear();
        _blood.Clear();
        _className = string.Empty;
        _multiplier = string.Empty;
        _read = false;
        _awaiting = false;
        _gotReply = false;
        _askedAt = double.NegativeInfinity;
    }
}
