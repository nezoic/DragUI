using HarmonyLib;
using ProjectM;
using ProjectM.UI;
using Unity.Entities;
using UnityEngine;
using DragUI.UI;

namespace DragUI.Patches;

// Keeps keystrokes typed into our text boxes from also moving the character, casting or opening
// menus. Approach adapted from Raphael (MIT, KDavidP1987), which found that:
//   * the game's input runs through an ordered stack of IInputContext consumers on InputActionSystem;
//     while the native chat is focused, ClientChatSystem adds its ChatFocusedInputContext (order 99),
//     which consumes every game action for the contexts below it;
//   * registering our OWN instance of that game class (order 100) while we type gives the same lock
//     with no injected types and no managed callbacks - an injected IInputContext crashed natively;
//   * the right InputActionSystem is the one GameplayInputSystem holds (a world lookup found none).
// That context alone is the whole lock: it filters movement, casts and keybinds before
// GameplayInputSystem and AbilityInputSystem run (as ChatPlus also found). Up to 1.3.1 we also skipped
// GameplayInputSystem and cleared the character's EntityInput from its prefix while typing; on some
// PCs that write crashed the game with an AccessViolationException (uncatchable), and skipping the
// system could leave a key latched as held. So nothing here touches game components or skips a system.
// Add/remove happen on our Ticker, never inside a game system. Teardown only drops references: the
// dying world's InputActionSystem takes the registration with it.
[HarmonyPatch]
internal static class TypingLock
{
    const double ReleaseGraceSeconds = 0.2; // the Enter that submits a field mustn't reach the game

    static InputActionSystem _inputActions;
    static World _inputWorld;
    static ClientChatSystem.ChatFocusedInputContext _contextObject;
    static IInputContext _context;
    static bool _registered;
    static double _releaseAt;
    static double _retryAt;
    static bool _loggedOnce;

    /// <summary>True while one of our text boxes has the keyboard (plus a short grace after).</summary>
    public static bool Active { get; private set; }

    /// <summary>Only finds the InputActionSystem (once per world). Never skips the system.</summary>
    [HarmonyPatch(typeof(GameplayInputSystem), nameof(GameplayInputSystem.OnUpdate))]
    [HarmonyPrefix]
    static void CaptureInputActions(GameplayInputSystem __instance)
    {
        if (_inputActions != null) return;
        try
        {
            var inputActions = __instance._InputActionSystem;
            if (inputActions == null) return;
            _inputActions = inputActions;
            _inputWorld = inputActions.World;
        }
        catch (Exception ex)
        {
            Core.Diag($"Typing lock: couldn't find the input system yet: {ex.Message}");
        }
    }

    public static void Tick()
    {
        double now = Time.realtimeSinceStartupAsDouble;
        bool typing = Settings.LockKeysWhileTyping && Core.InGame && UIManager.TypingInUI;
        if (typing) _releaseAt = now + ReleaseGraceSeconds;
        Active = typing || now < _releaseAt;

        if (Active == _registered || _inputActions == null || now < _retryAt) return;
        try
        {
            if (_inputWorld == null || !_inputWorld.IsCreated)
            {
                OnWorldTeardown();
                return;
            }

            if (Active)
            {
                // Diag lines before and after each game call: if the game crashes in one, the log's last
                // line says which.
                Core.Diag("Typing lock: adding the input context...");
                _contextObject ??= new ClientChatSystem.ChatFocusedInputContext();
                _context ??= _contextObject.Cast<IInputContext>();
                if (!_inputActions.IsContextRegistered(_context))
                    _inputActions.AddInputContext(_context, _inputWorld, (int)InputContextOrder.ChatInput + 1);
                _registered = true;
                Core.Diag("Typing lock: on.");
                if (!_loggedOnce) Core.Log.LogInfo("Typing lock engaged (game keys are blocked while typing in the mod).");
                _loggedOnce = true;
            }
            else
            {
                Core.Diag("Typing lock: removing the input context...");
                if (_context != null && _inputActions.IsContextRegistered(_context))
                    _inputActions.RemoveInputContext(_context);
                _registered = false;
                Core.Diag("Typing lock: off.");
            }
        }
        catch (Exception ex)
        {
            // A failed remove keeps retrying, so the keyboard can't stay locked.
            Core.Diag($"Typing lock {(Active ? "add" : "remove")} failed, retrying: {ex.Message}");
            _retryAt = now + 0.5;
        }
    }

    public static void OnWorldTeardown()
    {
        _inputActions = null;
        _inputWorld = null;
        _contextObject = null;
        _context = null;
        _registered = false;
        _releaseAt = 0;
        _retryAt = 0;
        Active = false;
    }
}
