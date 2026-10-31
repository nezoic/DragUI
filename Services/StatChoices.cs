using System.Text.RegularExpressions;
using ProjectM;
using ProjectM.Network;
using UnityEngine;

namespace DragUI.Services;

// The bonus stats picked for the equipped weapon (Bloodcraft expertise) or blood (legacy). All of it goes
// through Bloodcraft's player commands, so it works without Eclipse; replies to our own commands are hidden:
//   .wep get / .bl get              level, prestige, progress, and the chosen stats with their current values
//   .wep lst / .bl lst              every stat with its base cap, "1| MaxHealth: 250, 2| ..."
//   .wep cst <type> <n> / .bl cst   add stat n (1-based) to that weapon or blood type
//   .wep rst / .bl rst              clear all the choices for what's equipped (the server can charge items)
// Naming the type in cst means a stat can't land on a different weapon than the window shows. Bloodcraft
// says nothing when it refuses a stat (limit reached), so no reply means refused. It never tells the limit
// either (ExpertiseStatChoices / LegacyStatChoices), so until a refusal shows it we assume its default of 3.
// With no progress in a type, the get commands don't list its choices, so those can't be read.
internal sealed class StatChoices
{
    const int DefaultLimit = 3;
    const double QuietSeconds = 0.4;  // a reply is complete once nothing more arrives for this long
    const double ReadTimeoutSeconds = 4;
    const double ActionTimeoutSeconds = 3;

    public sealed class Stat
    {
        public string Key;   // Bloodcraft's name for it, as in its replies
        public string Label;
        public int Code;     // what cst takes
        public string Cap;   // formatted by the server; null until read
        public string Now;   // current value if chosen and known
        public int Slot;     // 1-based order chosen, 0 = not chosen
    }

    sealed record Replies(Regex Progress, Regex NoProgress, string NoChoices, Regex Chosen, Regex Reset, string CantPay,
        string[] Unavailable, string NoLegacy);

    enum Reply { None, Read, Action }

    static readonly Regex _richText = new("<[^>]*>", RegexOptions.Compiled);
    static readonly Regex _statsLine = new(@"^(?<type>\w+) Stats: (?<list>.+)$", RegexOptions.Compiled);
    static readonly Regex _statValue = new(@"(?<stat>\w+): (?<value>\S+?)(?:, |$)", RegexOptions.Compiled);
    static readonly Regex _capEntry = new(@"\d+\| (?<stat>\w+): (?<value>\S+?)(?:, |$)", RegexOptions.Compiled);
    static readonly Regex _words = new("(?<=[a-z])(?=[A-Z])", RegexOptions.Compiled);

    public static readonly StatChoices Weapon = new(".wep", "expertise",
        new[]
        {
            "Sword", "Axe", "Mace", "Spear", "Crossbow", "GreatSword", "Slashers", "Pistols", "Reaper", "Longbow", "Whip",
            "Unarmed", "FishingPole", "TwinBlades", "Daggers", "Claws",
        },
        Array.Empty<string>(),
        new[]
        {
            ("MaxHealth", "Max Health"), ("MovementSpeed", "Movement Speed"), ("PrimaryAttackSpeed", "Primary Attack Speed"),
            ("PhysicalLifeLeech", "Physical Life Leech"), ("SpellLifeLeech", "Spell Life Leech"), ("PrimaryLifeLeech", "Primary Life Leech"),
            ("PhysicalPower", "Physical Power"), ("SpellPower", "Spell Power"), ("PhysicalCritChance", "Physical Crit Chance"),
            ("PhysicalCritDamage", "Physical Crit Damage"), ("SpellCritChance", "Spell Crit Chance"), ("SpellCritDamage", "Spell Crit Damage"),
        },
        new Replies(
            new Regex(@"^Your weapon expertise is \[(?<level>\d+)\]\[(?<prestige>\d+)\] and you have \d+ expertise \((?<percent>\d+)%\) with (?<type>\w+)!$", RegexOptions.Compiled),
            new Regex(@"^You haven't gained any expertise for (?<type>\w+) yet!$", RegexOptions.Compiled),
            "No bonuses from currently equipped weapon.",
            new Regex(@"^(?<stat>\w+) has been chosen for (?<type>\w+)!$", RegexOptions.Compiled),
            new Regex(@"^Your weapon stats have been reset for (?<type>\w+)[.!]$", RegexOptions.Compiled),
            "You don't have the required item to reset your weapon stats",
            new[] { "Expertise is not enabled." },
            null),
        () =>
        {
            var character = Core.LocalCharacter;
            return character.Exists() && character.Has<Equipment>() ? LoadoutService.ItemGuid(character.Read<Equipment>().WeaponSlot) : 0;
        });

    public static readonly StatChoices Blood = new(".bl", "legacy",
        new[] { "Worker", "Warrior", "Scholar", "Rogue", "Mutant", "VBlood", "None", "GateBoss", "Draculin", "Immortal", "Creature", "Brute", "Corruption" },
        new[] { "VBlood", "None", "GateBoss" },
        new[]
        {
            ("HealingReceived", "Healing Received"), ("DamageReduction", "Damage Reduction"), ("PhysicalResistance", "Physical Resistance"),
            ("SpellResistance", "Spell Resistance"), ("ResourceYield", "Resource Yield"), ("ReducedBloodDrain", "Reduced Blood Drain"),
            ("SpellCooldownRecoveryRate", "Spell Cooldown"), ("WeaponCooldownRecoveryRate", "Weapon Cooldown"),
            ("UltimateCooldownRecoveryRate", "Ultimate Cooldown"), ("MinionDamage", "Minion Damage"),
            ("AbilityAttackSpeed", "Ability Attack Speed"), ("CorruptionDamageReduction", "Corruption Reduction"),
        },
        new Replies(
            new Regex(@"^You're level \[(?<level>\d+)\]\[(?<prestige>\d+)\] with \d+ essence \((?<percent>\d+)%\) in (?<type>\w+)!$", RegexOptions.Compiled),
            new Regex(@"^No progress in (?<type>\w+) yet\.$", RegexOptions.Compiled),
            "No stats selected for ",
            new Regex(@"^(?<stat>\w+) selected for (?<type>\w+)!$", RegexOptions.Compiled),
            new Regex(@"^Your blood stats have been reset for (?<type>\w+)[.!]$", RegexOptions.Compiled),
            "You do not have the required item to reset your blood stats",
            new[] { "Blood Legacies are not enabled.", "Legacies are not enabled." },
            "Invalid blood legacy."),
        () =>
        {
            var character = Core.LocalCharacter;
            return character.Exists() && character.Has<Blood>() ? character.Read<Blood>().BloodType.GuidHash : 0;
        });

    readonly string _prefix;
    readonly string _noun;
    readonly HashSet<string> _types;
    readonly HashSet<string> _noChoiceTypes;
    readonly Replies _replies;
    readonly Func<int> _equipped;
    int? _watched;
    double _readSentAt, _readLastReplyAt, _actionSentAt;
    bool _readGotReply, _readAgain;
    Stat _pendingStat; // the stat a cst is waiting on; null while a reset waits
    bool _unavailable;

    public IReadOnlyList<Stat> Stats { get; }
    /// <summary>Bloodcraft's name for what's equipped; null until read.</summary>
    public string Type { get; private set; }
    /// <summary>What's equipped may have changed since Type was read, so nothing is sent until it's read again.</summary>
    public bool Stale { get; private set; } = true;
    public bool HasProgress { get; private set; }
    public int Level { get; private set; }
    public int Prestige { get; private set; }
    public int Percent { get; private set; }
    public bool NoLegacy { get; private set; }
    public int? KnownLimit { get; private set; }
    public string Status { get; set; } = string.Empty;

    public int Count => Stats.Count(s => s.Slot > 0);
    public int Limit => KnownLimit ?? Math.Max(DefaultLimit, Count);
    public bool Busy => _actionSentAt > 0;
    public string TypeLabel => Type == null ? null : _words.Replace(Type, " ");

    StatChoices(string prefix, string noun, string[] types, string[] noChoiceTypes, (string Key, string Label)[] stats, Replies replies, Func<int> equipped)
    {
        _prefix = prefix;
        _noun = noun;
        _types = new HashSet<string>(types);
        _noChoiceTypes = new HashSet<string>(noChoiceTypes);
        Stats = stats.Select((s, i) => new Stat { Key = s.Key, Label = s.Label, Code = i + 1 }).ToList();
        _replies = replies;
        _equipped = equipped;
    }

    /// <summary>Read everything again; nothing can be chosen until the server says what's equipped.</summary>
    public void Refresh()
    {
        Stale = true;
        _watched = _equipped();
        Read();
    }

    /// <summary>While the window is open: read again when the weapon or blood changes.</summary>
    public void Watch()
    {
        int current = _equipped();
        if (current != _watched) Refresh();
    }

    void Read()
    {
        if (_readSentAt > 0)
        {
            _readAgain = true;
            return;
        }
        if (!ChatCommandSender.TrySend($"{_prefix} get")) return;
        if (Stats.Any(s => s.Cap == null)) ChatCommandSender.TrySend($"{_prefix} lst");
        _readSentAt = Time.realtimeSinceStartupAsDouble;
        _readGotReply = false;
    }

    /// <summary>Why a stat can't be chosen or reset right now, else null.</summary>
    string Problem()
    {
        if (_unavailable) return Status;
        if (NoLegacy) return $"This blood has no {_noun}, so it has no stats to choose.";
        if (Type == null || Stale) return "Still reading what you have equipped.";
        return Busy ? "Waiting for the server..." : null;
    }

    public string ResetProblem()
    {
        string problem = Problem();
        if (problem != null) return problem;
        return HasProgress && Count == 0 ? "No stats are chosen, so there's nothing to reset." : null;
    }

    public void Choose(Stat stat)
    {
        string problem = Problem();
        if (problem != null)
        {
            Status = problem;
            return;
        }
        if (stat.Slot > 0)
        {
            Status = $"{stat.Label} is already chosen. Bloodcraft can't remove one stat: Reset stats clears them all.";
            return;
        }
        if (KnownLimit is { } limit && Count >= limit)
        {
            Status = $"The server allows {limit} stats and they're all chosen. Reset stats clears them.";
            return;
        }
        if (!ChatCommandSender.TrySend($"{_prefix} cst {Type} {stat.Code}")) return;
        _pendingStat = stat;
        _actionSentAt = Time.realtimeSinceStartupAsDouble;
        Status = $"Choosing {stat.Label}...";
    }

    public void Reset()
    {
        string problem = ResetProblem();
        if (problem != null)
        {
            Status = problem;
            return;
        }
        if (!ChatCommandSender.TrySend($"{_prefix} rst")) return;
        _pendingStat = null;
        _actionSentAt = Time.realtimeSinceStartupAsDouble;
        Status = $"Resetting {TypeLabel} stats...";
    }

    public void Tick()
    {
        double now = Time.realtimeSinceStartupAsDouble;
        if (_actionSentAt > 0 && now - _actionSentAt >= ActionTimeoutSeconds)
        {
            if (_pendingStat == null) Status = $"Bloodcraft didn't answer {_prefix} rst.";
            else
            {
                if (HasProgress && Count > 0) KnownLimit = Count;
                Status = KnownLimit is { } limit
                    ? $"Bloodcraft refused {_pendingStat.Label}: the server allows {limit} stats. Reset stats clears them."
                    : $"Bloodcraft refused {_pendingStat.Label}: it may already be chosen or the limit reached.";
            }
            FinishAction();
            Read();
        }

        if (_readSentAt > 0 && (_readGotReply ? now - _readLastReplyAt >= QuietSeconds : now - _readSentAt >= ReadTimeoutSeconds))
        {
            _readSentAt = 0;
            if (!_readGotReply) Status = $"Bloodcraft didn't answer {_prefix} get.";
            if (_readAgain)
            {
                _readAgain = false;
                Read();
            }
        }
    }

    void FinishAction()
    {
        _actionSentAt = 0;
        _pendingStat = null;
    }

    /// <summary>Follow Bloodcraft's replies, even to commands typed in chat. True = a reply to our command: hide it.</summary>
    public bool OnServerMessage(ServerChatMessageType type, string message)
    {
        if (type != ServerChatMessageType.System || string.IsNullOrEmpty(message)) return false;
        double now = Time.realtimeSinceStartupAsDouble;
        switch (ReadReply(_richText.Replace(message, string.Empty).Trim()))
        {
            case Reply.Read when _readSentAt > 0:
                _readGotReply = true;
                _readLastReplyAt = now;
                return true;
            case Reply.Action when _actionSentAt > 0:
                FinishAction();
                return true;
            default:
                return false;
        }
    }

    Reply ReadReply(string text)
    {
        Match match;
        if ((match = _replies.Progress.Match(text)).Success)
        {
            SetEquipped(match.Groups["type"].Value);
            HasProgress = true;
            Level = int.Parse(match.Groups["level"].Value);
            Prestige = int.Parse(match.Groups["prestige"].Value);
            Percent = int.Parse(match.Groups["percent"].Value);
            return Reply.Read;
        }
        if ((match = _replies.NoProgress.Match(text)).Success)
        {
            // This reply doesn't list the choices, so keep any seen being made for this type.
            SetEquipped(match.Groups["type"].Value, keepChosen: match.Groups["type"].Value == Type);
            if (!NoLegacy && Count == 0) Status = $"No {_noun} in {TypeLabel} yet, so stats chosen earlier can't be read.";
            return Reply.Read;
        }
        if (text.StartsWith(_replies.NoChoices, StringComparison.Ordinal)) return Reply.Read;

        if ((match = _statsLine.Match(text)).Success && _types.Contains(match.Groups["type"].Value))
        {
            foreach (Match entry in _statValue.Matches(match.Groups["list"].Value))
            {
                var stat = Find(entry.Groups["stat"].Value);
                if (stat == null) continue;
                AddChosen(stat);
                stat.Now = entry.Groups["value"].Value;
            }
            return Reply.Read;
        }

        bool isCapList = false;
        foreach (Match entry in _capEntry.Matches(text))
        {
            var stat = Find(entry.Groups["stat"].Value);
            if (stat == null) continue;
            stat.Cap = entry.Groups["value"].Value;
            isCapList = true;
        }
        if (isCapList) return Reply.Read;

        if ((match = _replies.Chosen.Match(text)).Success && Find(match.Groups["stat"].Value) is { } chosen)
        {
            if (match.Groups["type"].Value == Type) AddChosen(chosen);
            if (KnownLimit < Count) KnownLimit = null; // a slow reply, not the refusal it looked like
            Status = $"{chosen.Label} chosen for {_words.Replace(match.Groups["type"].Value, " ")}.";
            Read(); // for its value
            return Reply.Action;
        }
        if ((match = _replies.Reset.Match(text)).Success)
        {
            ClearChosen();
            Status = $"{_words.Replace(match.Groups["type"].Value, " ")} stats reset.";
            Read();
            return Reply.Action;
        }
        if (text.StartsWith(_replies.CantPay, StringComparison.Ordinal))
        {
            Status = text;
            return Reply.Action;
        }

        if (_replies.Unavailable.Contains(text))
        {
            _unavailable = true;
            Status = text;
            return _actionSentAt > 0 ? Reply.Action : Reply.Read;
        }
        if (text == _replies.NoLegacy && _readSentAt > 0)
        {
            SetEquipped(null);
            NoLegacy = true;
            return Reply.Read;
        }
        if (_actionSentAt > 0 && text.StartsWith("Invalid ", StringComparison.Ordinal))
        {
            Status = text;
            return Reply.Action;
        }
        return Reply.None;
    }

    // A get reply starts over: it lists every chosen stat after this.
    void SetEquipped(string type, bool keepChosen = false)
    {
        if (Stale) Status = string.Empty; // about what was equipped before
        Type = type;
        Stale = false;
        HasProgress = false;
        Level = Prestige = Percent = 0;
        NoLegacy = type != null && _noChoiceTypes.Contains(type);
        _unavailable = false;
        if (!keepChosen) ClearChosen();
    }

    Stat Find(string key) => Stats.FirstOrDefault(s => s.Key == key);

    void AddChosen(Stat stat)
    {
        if (stat.Slot == 0) stat.Slot = Count + 1;
    }

    void ClearChosen()
    {
        foreach (var stat in Stats)
        {
            stat.Slot = 0;
            stat.Now = null;
        }
    }

    public static void TickAll()
    {
        Weapon.Tick();
        Blood.Tick();
    }

    public static bool OnServerMessageAll(ServerChatMessageType type, string message) =>
        Weapon.OnServerMessage(type, message) || Blood.OnServerMessage(type, message);

    public static void OnWorldTeardown()
    {
        Weapon.Clear();
        Blood.Clear();
    }

    void Clear()
    {
        ClearChosen();
        foreach (var stat in Stats) stat.Cap = null;
        FinishAction();
        _readSentAt = 0;
        _readAgain = false;
        _watched = null;
        _unavailable = false;
        Type = null;
        Stale = true;
        HasProgress = false;
        Level = Prestige = Percent = 0;
        NoLegacy = false;
        KnownLimit = null;
        Status = string.Empty;
    }
}
