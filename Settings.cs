using BepInEx.Configuration;
using UnityEngine;
using DragUI.UI;

namespace DragUI;

internal static class Settings
{
    static ConfigEntry<KeyCode> _toggleKey;
    static ConfigEntry<float> _uiScale;
    static ConfigEntry<AccentColor> _accentColor;
    static ConfigEntry<string> _customAccentColor;
    static ConfigEntry<float> _pollSeconds;
    static ConfigEntry<int> _fallbackRespawnSeconds;
    static ConfigEntry<bool> _learnRespawnTimes;
    static ConfigEntry<bool> _autoTrackNext;
    static ConfigEntry<bool> _readChatKills;
    static ConfigEntry<string> _chatKillChannels;
    static ConfigEntry<string> _chatKillWords;
    static ConfigEntry<bool> _blockClicksOverUI;
    static ConfigEntry<bool> _diagnostics;
    static ConfigEntry<float> _hudX, _hudY, _panelX, _panelY, _barX, _barY, _famPanelX, _famPanelY;
    static ConfigEntry<bool> _hudCollapsed;
    static ConfigEntry<bool> _hudVisible;
    static ConfigEntry<float> _barWidth;
    static ConfigEntry<bool> _familiarsEnabled;
    static ConfigEntry<bool> _lockKeysWhileTyping;
    static ConfigEntry<bool> _alarmEnabled;
    static ConfigEntry<bool> _autoTeleport;
    static ConfigEntry<string> _autoTeleportOnlyFrom;
    static ConfigEntry<bool> _xpEnabled;
    static ConfigEntry<bool> _dpsEnabled;
    static ConfigEntry<bool> _sheetEnabled;
    static ConfigEntry<bool> _cooldownTesterEnabled;
    static ConfigEntry<bool> _loadoutsEnabled;
    static ConfigEntry<float> _loadoutPanelX, _loadoutPanelY;
    static ConfigEntry<bool> _statChoicesEnabled;
    static ConfigEntry<bool> _classesEnabled;
    static ConfigEntry<float> _classPanelX, _classPanelY;
    static ConfigEntry<float> _weaponStatsPanelX, _weaponStatsPanelY;
    static ConfigEntry<float> _bloodStatsPanelX, _bloodStatsPanelY;
    static ConfigEntry<float> _noctveilPanelX, _noctveilPanelY;
    static ConfigEntry<float> _themePanelX, _themePanelY;
    static ConfigEntry<float> _teleportPanelX, _teleportPanelY;
    static ConfigEntry<float> _teleportPopoutX, _teleportPopoutY;
    static ConfigEntry<bool> _teleportPopoutVisible;
    static ConfigEntry<float> _cdPanelX, _cdPanelY;
    static ConfigEntry<float> _sheetPanelX, _sheetPanelY;
    static ConfigEntry<float> _dpsFightTimeout;
    static ConfigEntry<float> _dpsPanelX, _dpsPanelY;
    static ConfigEntry<bool> _hideXpMessages;
    static ConfigEntry<float> _xpIdleMinutes;
    static ConfigEntry<float> _xpPanelX, _xpPanelY;
    static ConfigEntry<float> _alarmVolume;
    static ConfigEntry<float> _alarmLeadMinutes;
    static ConfigEntry<float> _alarmButtonMinutes;
    static ConfigEntry<float> _alarmFlashMinutes;
    static ConfigEntry<string> _alarmTimeZone;

    public static KeyCode ToggleKey => _toggleKey.Value;
    public static float UIScale => Mathf.Clamp(_uiScale.Value, 0.5f, 2.5f);
    public static AccentColor AccentColor => _accentColor.Value;
    public static string CustomAccentColor => _customAccentColor.Value?.Trim() ?? string.Empty;

    /// <summary>The accent colour setting changed (from the Theme window or a config manager).</summary>
    public static event Action AppearanceChanged;

    /// <summary>Use a palette colour (clears any custom one).</summary>
    public static void SetAccentColor(AccentColor color)
    {
        _customAccentColor.Value = string.Empty;
        _accentColor.Value = color;
    }

    public static void SetCustomAccentColor(string hex) => _customAccentColor.Value = hex.Trim();
    public static float PollSeconds => Mathf.Max(2f, _pollSeconds.Value);
    public static int FallbackRespawnSeconds => Math.Max(30, _fallbackRespawnSeconds.Value);
    public static bool LearnRespawnTimes => _learnRespawnTimes.Value;
    public static bool AutoTrackNext => _autoTrackNext.Value;
    public static bool ReadChatKills => _readChatKills.Value;
    public static string ChatKillChannels => _chatKillChannels.Value ?? string.Empty;
    public static string ChatKillWords => _chatKillWords.Value ?? string.Empty;
    public static bool BlockClicksOverUI => _blockClicksOverUI.Value;
    public static bool Diagnostics => _diagnostics.Value;
    public static bool FamiliarsEnabled => _familiarsEnabled.Value;
    public static bool LockKeysWhileTyping => _lockKeysWhileTyping.Value;
    public static bool XpEnabled => _xpEnabled.Value;
    public static bool DpsEnabled => _dpsEnabled.Value;
    public static bool SheetEnabled => _sheetEnabled.Value;
    public static bool CooldownTesterEnabled => _cooldownTesterEnabled.Value;
    public static bool LoadoutsEnabled => _loadoutsEnabled.Value;
    public static bool StatChoicesEnabled => _statChoicesEnabled.Value;
    public static bool ClassesEnabled => _classesEnabled.Value;
    public static float DpsFightTimeoutSeconds => Mathf.Clamp(_dpsFightTimeout.Value, 2f, 60f);
    public static bool HideXpMessages => _hideXpMessages.Value;
    public static float XpIdleMinutes => Mathf.Max(1f, _xpIdleMinutes.Value);
    public static bool AutoTeleportEnabled { get => _autoTeleport.Value; set => _autoTeleport.Value = value; }
    public static string AutoTeleportOnlyFrom => _autoTeleportOnlyFrom.Value ?? string.Empty;
    public static bool AlarmEnabled { get => _alarmEnabled.Value; set => _alarmEnabled.Value = value; }
    public static float AlarmVolume => Mathf.Clamp01(_alarmVolume.Value);
    public static float AlarmLeadMinutes => Mathf.Clamp(_alarmLeadMinutes.Value, 0f, 60f);
    public static float AlarmButtonMinutes => Mathf.Max(0.5f, _alarmButtonMinutes.Value);
    public static float AlarmFlashMinutes => Mathf.Max(0f, _alarmFlashMinutes.Value);
    public static string AlarmTimeZone => _alarmTimeZone.Value ?? string.Empty;

    public static Vector2 HudPosition
    {
        get => new(_hudX.Value, _hudY.Value);
        set { _hudX.Value = Mathf.Round(value.x); _hudY.Value = Mathf.Round(value.y); }
    }

    public static Vector2 PanelPosition
    {
        get => new(_panelX.Value, _panelY.Value);
        set { _panelX.Value = Mathf.Round(value.x); _panelY.Value = Mathf.Round(value.y); }
    }
    public static Vector2 BarPosition
    {
        get => new(_barX.Value, _barY.Value);
        set { _barX.Value = Mathf.Round(value.x); _barY.Value = Mathf.Round(value.y); }
    }

    public static Vector2 FamiliarPanelPosition
    {
        get => new(_famPanelX.Value, _famPanelY.Value);
        set { _famPanelX.Value = Mathf.Round(value.x); _famPanelY.Value = Mathf.Round(value.y); }
    }

    public static Vector2 CooldownPanelPosition
    {
        get => new(_cdPanelX.Value, _cdPanelY.Value);
        set { _cdPanelX.Value = Mathf.Round(value.x); _cdPanelY.Value = Mathf.Round(value.y); }
    }

    public static Vector2 CharacterPanelPosition
    {
        get => new(_sheetPanelX.Value, _sheetPanelY.Value);
        set { _sheetPanelX.Value = Mathf.Round(value.x); _sheetPanelY.Value = Mathf.Round(value.y); }
    }

    public static Vector2 LoadoutPanelPosition
    {
        get => new(_loadoutPanelX.Value, _loadoutPanelY.Value);
        set { _loadoutPanelX.Value = Mathf.Round(value.x); _loadoutPanelY.Value = Mathf.Round(value.y); }
    }

    public static Vector2 WeaponStatsPanelPosition
    {
        get => new(_weaponStatsPanelX.Value, _weaponStatsPanelY.Value);
        set { _weaponStatsPanelX.Value = Mathf.Round(value.x); _weaponStatsPanelY.Value = Mathf.Round(value.y); }
    }

    public static Vector2 BloodStatsPanelPosition
    {
        get => new(_bloodStatsPanelX.Value, _bloodStatsPanelY.Value);
        set { _bloodStatsPanelX.Value = Mathf.Round(value.x); _bloodStatsPanelY.Value = Mathf.Round(value.y); }
    }

    public static Vector2 TeleportPanelPosition
    {
        get => new(_teleportPanelX.Value, _teleportPanelY.Value);
        set { _teleportPanelX.Value = Mathf.Round(value.x); _teleportPanelY.Value = Mathf.Round(value.y); }
    }

    public static Vector2 TeleportPopoutPosition
    {
        get => new(_teleportPopoutX.Value, _teleportPopoutY.Value);
        set { _teleportPopoutX.Value = Mathf.Round(value.x); _teleportPopoutY.Value = Mathf.Round(value.y); }
    }

    public static bool TeleportPopoutVisible { get => _teleportPopoutVisible.Value; set => _teleportPopoutVisible.Value = value; }

    public static Vector2 NoctveilPanelPosition
    {
        get => new(_noctveilPanelX.Value, _noctveilPanelY.Value);
        set { _noctveilPanelX.Value = Mathf.Round(value.x); _noctveilPanelY.Value = Mathf.Round(value.y); }
    }

    public static Vector2 ClassPanelPosition
    {
        get => new(_classPanelX.Value, _classPanelY.Value);
        set { _classPanelX.Value = Mathf.Round(value.x); _classPanelY.Value = Mathf.Round(value.y); }
    }

    public static Vector2 ThemePanelPosition
    {
        get => new(_themePanelX.Value, _themePanelY.Value);
        set { _themePanelX.Value = Mathf.Round(value.x); _themePanelY.Value = Mathf.Round(value.y); }
    }

    public static Vector2 DpsPanelPosition
    {
        get => new(_dpsPanelX.Value, _dpsPanelY.Value);
        set { _dpsPanelX.Value = Mathf.Round(value.x); _dpsPanelY.Value = Mathf.Round(value.y); }
    }

    public static Vector2 XpPanelPosition
    {
        get => new(_xpPanelX.Value, _xpPanelY.Value);
        set { _xpPanelX.Value = Mathf.Round(value.x); _xpPanelY.Value = Mathf.Round(value.y); }
    }

    public static bool HudCollapsed { get => _hudCollapsed.Value; set => _hudCollapsed.Value = value; }
    public static bool HudVisible { get => _hudVisible.Value; set => _hudVisible.Value = value; }
    /// <summary>Width of the control bar's button area; 0 = all buttons in one row.</summary>
    public static float BarWidth { get => Mathf.Max(0f, _barWidth.Value); set => _barWidth.Value = Mathf.Round(Mathf.Max(0f, value)); }

    public static void Initialize(ConfigFile config)
    {
        _toggleKey = config.Bind("General", "ToggleListKey", KeyCode.None,
            "Optional hotkey that opens/closes the V Blood list (e.g. F8). None = use the on-screen button only. Avoid keys you type in chat.");
        _uiScale = config.Bind("General", "UIScale", 1f,
            "Scale of the hunt tracker and V Blood list (0.5 - 2.5).");
        _accentColor = config.Bind("Appearance", "AccentColor", AccentColor.Crimson,
            "The mod's main colour: buttons that are switched on, title bars, window borders and highlights are all shades of it. " +
            "Pick it in game under Settings -> Theme on the control bar.");
        _customAccentColor = config.Bind("Appearance", "CustomAccentColor", "",
            "Use your own main colour instead of one from AccentColor, as a hex code like #3A7BD5. Empty = use AccentColor. " +
            "Mid-dark colours work best: the button text is light.");
        _accentColor.SettingChanged += (_, _) => AppearanceChanged?.Invoke();
        _customAccentColor.SettingChanged += (_, _) => AppearanceChanged?.Invoke();
        _pollSeconds = config.Bind("Tracking", "PollSeconds", 5f,
            "How often (seconds, min 2) to ask the server which V Bloods are alive. This is the same request the game's V Blood menu makes.");
        _fallbackRespawnSeconds = config.Bind("Tracking", "FallbackRespawnSeconds", 600,
            "Respawn estimate (seconds) for a V Blood with no known default and no observed respawn yet.");
        _learnRespawnTimes = config.Bind("Tracking", "LearnRespawnTimes", true,
            "Remember respawn durations measured in-game (from V Blood ghosts and observed respawns) and use them for future estimates.");
        _autoTrackNext = config.Bind("Tracking", "AutoTrackNext", false,
            "When nothing is tracked (e.g. your tracked V Blood just died), automatically track the nearest alive V Blood on your hunt list.");
        _readChatKills = config.Bind("Chat", "ReadChatKills", true,
            "Start a respawn countdown when a server chat message announces a V Blood kill (from kill-feed/announcement mods). " +
            "A message counts when it names exactly one V Blood and contains a kill word.");
        _chatKillChannels = config.Bind("Chat", "ChatKillChannels", "System",
            "Chat channels to read kill announcements from, comma-separated: System, Global, Region, Team, Local, Lore. " +
            "Announcement mods use System; adding Global lets player chat like 'killed alpha' start timers too.");
        _chatKillWords = config.Bind("Chat", "ExtraChatKillWords", "",
            "Extra words that mark a chat message as a kill announcement, on top of the built-in ones " +
            "(killed, slain, defeated, beating, ...). Comma-separated, whole words, case-insensitive.");
        _blockClicksOverUI = config.Bind("General", "BlockClicksOverUI", true,
            "Stop clicks on this mod's buttons from also firing your primary attack / casts in the world.");
        _lockKeysWhileTyping = config.Bind("General", "LockKeysWhileTyping", true,
            "While you type in one of this mod's text boxes, stop the keys from also moving your character or firing abilities " +
            "(uses the same input lock as the game's chat box). Turn off only if typing in the mod causes problems.");
        _familiarsEnabled = config.Bind("Familiars", "Enabled", true,
            "Show the Familiars window (quick summon for Bloodcraft familiars). It sends Bloodcraft's chat commands (.fam sb, .fam t) " +
            "for you, so it only works on servers running Bloodcraft with familiars enabled. On other servers the commands " +
            "would show up as local chat, so turn this off there.");
        _cooldownTesterEnabled = config.Bind("Character", "CooldownTester", true,
            "Show a CD Test button on the control bar: a window that times your skills' real cooldowns against their base, to see how cooldown stats really work.");
        _loadoutsEnabled = config.Bind("Character", "Loadouts", true,
            "Show Loadouts in the control bar's Character menu: save your build (equipped gear, spell passives, Bloodcraft class and " +
            "shift spell) and put a saved one back on with one click. Bloodcraft parts need a Bloodcraft server.");
        _statChoicesEnabled = config.Bind("Character", "StatChoices", true,
            "Show Weapon Stats and Blood Stats in the control bar's Character menu: pick and reset your Bloodcraft expertise and legacy stats. " +
            "They send Bloodcraft's chat commands (.wep, .bl) for you, so they only work on servers running Bloodcraft.");
        _classesEnabled = config.Bind("Character", "Classes", true,
            "Show Class in the control bar's Character menu: every Bloodcraft class with its weapon and blood stat synergies, and " +
            "one click to pick or change class (.class s / .class c, with the cost shown first). Only works on servers running Bloodcraft.");
        _sheetEnabled = config.Bind("Character", "Enabled", true,
            "Show a Stats button on the control bar that opens a full character sheet: every stat and its sources (gear, buffs, Bloodcraft bonuses).");
        _dpsEnabled = config.Bind("Dps", "Enabled", true,
            "Show a DPS meter on the control bar: damage you (and your minions/familiar) deal per fight, per ability, from the damage numbers the server sends.");
        _dpsFightTimeout = config.Bind("Dps", "FightTimeoutSeconds", 6f,
            "A fight ends this many seconds (2 - 60) after your last hit; the next hit starts a new one.");
        _xpEnabled = config.Bind("Experience", "Enabled", true,
            "Show experience per hour on the control bar, with a breakdown (quests / mobs / V Bloods) per prestige. " +
            "Reads Bloodcraft's leveling log, so it needs a Bloodcraft server and the log on ('.lvl log', or the button in the XP window).");
        _hideXpMessages = config.Bind("Experience", "HideXpMessages", true,
            "Hide Bloodcraft's '+123 experience (45%)' chat lines (the XP window counts them instead of them filling chat).");
        _xpIdleMinutes = config.Bind("Experience", "IdleMinutes", 5f,
            "A gap longer than this (minutes) without any experience isn't counted as play time, so being AFK doesn't lower XP/hour.");
        _autoTeleport = config.Bind("AutoTeleport", "Enabled", false,
            "Automatically accept ScarletTeleports teleport requests (sends '.stp tpa <name>' when someone requests to teleport to you). " +
            "Toggle it in the Teleport window (Teleport on the control bar); with it off, requests wait there and in the notification slot.");
        _autoTeleportOnlyFrom = config.Bind("AutoTeleport", "OnlyFrom", "",
            "Only auto-accept requests from these players (comma-separated character names). Empty = anyone who requests.");
        _alarmEnabled = config.Bind("BossAlarm", "Enabled", false,
            "Sound an alarm and show a teleport button at each time in BepInEx/config/DragUI/boss-schedule.txt. " +
            "Only runs on a server with Noctveil in its name: the default schedule is its Apex/Prime bosses. Toggle it in Settings -> Noctveil on the control bar.");
        _alarmVolume = config.Bind("BossAlarm", "Volume", 0.35f,
            "Alarm chime volume, 0 - 1 (0 = silent). Played through Windows, so the game's volume sliders don't affect it.");
        _alarmLeadMinutes = config.Bind("BossAlarm", "LeadMinutes", 2f,
            "How many minutes before each boss spawns the alarm sounds and its teleport button appears (time to get out of combat).");
        _alarmButtonMinutes = config.Bind("BossAlarm", "ButtonMinutes", 15f,
            "How long (minutes) the teleport button stays on the control bar after the boss spawns.");
        _alarmFlashMinutes = config.Bind("BossAlarm", "FlashMinutes", 2f,
            "How long (minutes) the teleport button flashes after the alarm sounds. With LeadMinutes 2 it flashes until the boss spawns.");
        _alarmTimeZone = config.Bind("BossAlarm", "TimeZone", "",
            "Time zone the schedule's times are in, as a Windows time zone id (e.g. 'Eastern Standard Time', 'Central Standard Time'). " +
            "Empty = this PC's time zone.");
        _diagnostics = config.Bind("Debug", "Diagnostics", false,
            "Log what the mod sees (position responses, ghosts, tracking buffs). Turn on when reporting a problem.");
        _hudX = config.Bind("Layout", "HudX", 16f, "Hunt tracker left edge, in reference pixels from the left of the screen. Drag the tracker header to change.");
        _hudY = config.Bind("Layout", "HudY", -220f, "Hunt tracker top edge, in reference pixels from the top of the screen (negative = down).");
        _panelX = config.Bind("Layout", "PanelX", 0f, "V Blood list window offset from the screen centre (x). Drag its title bar to change.");
        _panelY = config.Bind("Layout", "PanelY", 0f, "V Blood list window offset from the screen centre (y).");
        _hudCollapsed = config.Bind("Layout", "HudCollapsed", false,
            "Whether the hunt tracker rows are collapsed (only the header button shows).");
        _hudVisible = config.Bind("Layout", "HudVisible", true,
            "Whether the hunt tracker is shown. Toggle it with the Tracker button on the control bar.");
        _barX = config.Bind("Layout", "BarX", 16f, "Control bar left edge, in reference pixels from the left of the screen. Drag its grip to change.");
        _barY = config.Bind("Layout", "BarY", -176f, "Control bar top edge, in reference pixels from the top of the screen (negative = down).");
        _barWidth = config.Bind("Layout", "BarWidth", 0f,
            "Width of the control bar's button area, in reference pixels: the buttons wrap into as many rows as they need. " +
            "0 = one row; anything narrower than the widest button = one column. Drag the strip on the bar's right edge to change, " +
            "or click its grip to switch between one row and one column.");
        _cdPanelX = config.Bind("Layout", "CooldownPanelX", 0f, "Cooldown tester window offset from the screen centre (x). Drag its title bar to change.");
        _cdPanelY = config.Bind("Layout", "CooldownPanelY", -60f, "Cooldown tester window offset from the screen centre (y).");
        _sheetPanelX = config.Bind("Layout", "CharacterPanelX", 300f, "Character sheet window offset from the screen centre (x). Drag its title bar to change.");
        _sheetPanelY = config.Bind("Layout", "CharacterPanelY", 0f, "Character sheet window offset from the screen centre (y).");
        _loadoutPanelX = config.Bind("Layout", "LoadoutPanelX", -300f, "Loadouts window offset from the screen centre (x). Drag its title bar to change.");
        _loadoutPanelY = config.Bind("Layout", "LoadoutPanelY", 60f, "Loadouts window offset from the screen centre (y).");
        _weaponStatsPanelX = config.Bind("Layout", "WeaponStatsPanelX", -280f, "Weapon Stats window offset from the screen centre (x). Drag its title bar to change.");
        _weaponStatsPanelY = config.Bind("Layout", "WeaponStatsPanelY", 0f, "Weapon Stats window offset from the screen centre (y).");
        _bloodStatsPanelX = config.Bind("Layout", "BloodStatsPanelX", 280f, "Blood Stats window offset from the screen centre (x). Drag its title bar to change.");
        _bloodStatsPanelY = config.Bind("Layout", "BloodStatsPanelY", 0f, "Blood Stats window offset from the screen centre (y).");
        _teleportPanelX = config.Bind("Layout", "TeleportPanelX", -340f, "Teleport window offset from the screen centre (x). Drag its title bar to change.");
        _teleportPanelY = config.Bind("Layout", "TeleportPanelY", 0f, "Teleport window offset from the screen centre (y).");
        _teleportPopoutX = config.Bind("Layout", "TeleportPopoutX", 16f, "Teleport pop-out left edge, in reference pixels from the left of the screen. Drag its header to change.");
        _teleportPopoutY = config.Bind("Layout", "TeleportPopoutY", -420f, "Teleport pop-out top edge, in reference pixels from the top of the screen (negative = down).");
        _teleportPopoutVisible = config.Bind("Layout", "TeleportPopoutVisible", false,
            "Whether the teleport pop-out (quick teleport buttons) is open. Open it with Pop out in the Teleport window.");
        _noctveilPanelX = config.Bind("Layout", "NoctveilPanelX", 320f, "Noctveil server settings window offset from the screen centre (x). Drag its title bar to change.");
        _noctveilPanelY = config.Bind("Layout", "NoctveilPanelY", 40f, "Noctveil server settings window offset from the screen centre (y).");
        _classPanelX = config.Bind("Layout", "ClassPanelX", 0f, "Class window offset from the screen centre (x). Drag its title bar to change.");
        _classPanelY = config.Bind("Layout", "ClassPanelY", 0f, "Class window offset from the screen centre (y).");
        _themePanelX = config.Bind("Layout", "ThemePanelX", 0f, "Theme window offset from the screen centre (x). Drag its title bar to change.");
        _themePanelY = config.Bind("Layout", "ThemePanelY", 80f, "Theme window offset from the screen centre (y).");
        _dpsPanelX = config.Bind("Layout", "DpsPanelX", -320f, "DPS meter window offset from the screen centre (x). Drag its title bar to change.");
        _dpsPanelY = config.Bind("Layout", "DpsPanelY", 0f, "DPS meter window offset from the screen centre (y).");
        _xpPanelX = config.Bind("Layout", "XpPanelX", 0f, "Experience window offset from the screen centre (x). Drag its title bar to change.");
        _xpPanelY = config.Bind("Layout", "XpPanelY", 120f, "Experience window offset from the screen centre (y).");
        _famPanelX = config.Bind("Layout", "FamiliarPanelX", 380f, "Familiars window offset from the screen centre (x). Drag its title bar to change.");
        _famPanelY = config.Bind("Layout", "FamiliarPanelY", 0f, "Familiars window offset from the screen centre (y).");
    }
}
