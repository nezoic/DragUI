## 1.6.0
- New: **Character → Class**, a class window like Weapon Stats and Blood Stats. It shows every Bloodcraft class with its weapon and blood stat synergies, your current class lit, and what changing class costs on this server. Click a class to change to it: you're asked first, with the cost, and the server's answer (changed, not enough items, passives still on) shows in the window. Your first class is free. Works with or without Eclipse, and the commands' replies are hidden from chat.

## 1.5.0
- New: pick the mod's main colour in **Settings → Theme** on the control bar: Crimson (the default), Ember, Gold, Emerald, Teal, Azure, Royal, Violet, Rose or Silver, or your own as a hex code such as `#3A7BD5`. Title bars, borders, switched-on buttons, scrollbars and highlights all take shades of it, and the UI switches straight away, keeping your open windows open. It's saved as `AccentColor` / `CustomAccentColor` under `[Appearance]` in `nezoic.DragUI.cfg`. With a green colour, the class-synergy outlines turn gold so they still stand out.
- Changed: the **Server Settings** button is now **Settings**, always on the bar. It holds **Theme**, plus **Noctveil** on Noctveil.

## 1.4.0
- New: a **V Bloods** filter in the Familiars window. After **List All** has read every box, click **V Bloods** to show only your V Blood familiars; click it again to show them all.
- New: in **Weapon Stats** and **Blood Stats**, stats your Bloodcraft class has a synergy with get a green outline. Your class gets extra from those stats (Bloodcraft's synergy multiplier, e.g. x1.5). The legend above the grid names your class and the multiplier. Without Eclipse, the mod asks Bloodcraft with `.class lst` (reply hidden), and asks again after you change class.

## 1.3.2
- Fixed: the game could crash as soon as you clicked into a DragUI text box (a loadout, familiar or teleport name) or a hotkey button. On some PCs, the code that stopped your keys moving the character while you typed crashed the game. Keys typed into DragUI are still kept from the game, now without that code.

## 1.3.1
- Fixed: after clicking a hotkey button (**+ key**) for a familiar or teleport, the game ignored all your keys until you pressed one, pressed Esc or closed the window, so it could look frozen. Clicking anywhere else now cancels it too.
- With `Diagnostics = true`, the log now records each step of binding and using a hotkey, to help track down crash reports. If the game crashes for you while binding a hotkey, please send your `BepInEx/LogOutput.log` and the game's `Player.log` from straight after the crash.

## 1.3.0
- Changed: each server now has its own saved familiars and hotkeys, hunt list, respawn times, loadouts, teleport hotkeys and XP history. Window positions, settings and the boss schedule are still shared. A wiped server starts empty.
- Changed: the Server Settings button only shows on a server that has settings, which is currently Noctveil. The boss alarm only runs there too, so it can no longer go off on other servers, even if you edited its schedule for one.
- Changed: windows, the tracker, the control bar and the teleport pop-out now stay fully on screen. Before, half a window could be dragged off the edge. If the screen gets smaller (resolution or UI scale) or a panel grows, it's pushed in, then goes back where you left it once there's room. Something placed at the bottom edge grows upward.
- Changed: the control bar's menus (Character, Server Settings) open upward when there's no room below, and shift sideways to stay on screen.
- Your existing data moves to the first server you join after updating, so join the server it belongs to first. If it ends up on the wrong one, close the game and move the files between the folders in BepInEx/config/DragUI/servers (each has a server.txt naming the server).

## 1.2.0
- New: Weapon Stats and Blood Stats in the Character menu (Bloodcraft). See the 12 stats for the weapon type or blood you have now as a grid of buttons, with chosen stats lit and numbered, their current values, base caps and your level. Click a stat to choose it. The window follows when you swap weapon or change blood, and doesn't need Eclipse.
- New: a Reset stats button in both windows (.wep rst / .bl rst). It asks first, because the server can charge items for a reset.

## 1.1.3
- Fixed: reduced the risk of a crash reported when pressing Enter to name a new teleport. If it still crashes for you, please send your Player.log and BepInEx/LogOutput.log.
- Fixed: the teleport name box no longer loses your text and focus when the teleport list finishes loading while you type.
- Changed: pressing Enter in the teleport, familiar and loadout text boxes now acts a frame later, so the box has finished with the key first.
- Changed: teleport names can't contain ~ * < or >. They read back from the server as a different name, which broke Go and remove.

## 1.1.2
- Changed: the source code moved to https://github.com/nezoic/DragUI. No changes to the mod itself.

## 1.1.1
- Changed: the mod page now describes every tool up front, and lists which ones need server mods (Bloodcraft, Eclipse, ScarletTeleports, PerkShop). No changes to the mod itself.

## 1.1.0
First Thunderstore release. A client-side utility UI, installed on your PC (nothing needed on the server).

- Control bar: one small, draggable, resizable bar with a button per feature and a notification slot for things that need you (teleport requests, boss alarms).
- V Blood hunt tracker: pick the bosses you want and see whether each is up, how far away and in which direction, or when it respawns. One click switches the game's tracking trail.
- Familiars (Bloodcraft): summon by name, saved one-click buttons with hotkeys, call/dismiss, and List All to see every familiar from every box in one list.
- Loadouts: save your gear, spells, spell school passives and Bloodcraft class, and put the whole build back on with one click.
- DPS meter: real damage after every modifier, with a per-ability breakdown and your recent fights.
- Experience per hour (Bloodcraft): XP/hour on the bar, split into quests, mobs and V Bloods per prestige level.
- Character sheet: every combat stat and where it comes from, including Bloodcraft expertise/legacy bonuses and PerkShop stat perks.
- Cooldown tester: measures real skill cooldowns and shows how your cooldown stat actually applies.
- Teleports (ScarletTeleports): auto-accept or accept/deny requests, your and the server's teleports with Go buttons and hotkeys, and a pop-out teleport window.
- Boss alarm (Noctveil): a chime and a one-click teleport notice 2 minutes before each Apex/Prime boss spawns.
