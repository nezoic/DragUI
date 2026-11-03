namespace DragUI.Services;

// The familiar names the player has summoned by name on this server, newest first, so they become
// one-click buttons, each with an optional hotkey. Saved one per line ("name" or "name<TAB>hotkey") in
// familiars.txt in the server's data folder (see ServerData).
internal static class FamiliarStore
{
    const int MaxSaved = 40;

    public sealed class Entry
    {
        public string Name;
        public Hotkey Hotkey;
    }

    static string _path;
    static readonly List<Entry> _entries = new();

    public static event Action Changed;

    public static IReadOnlyList<Entry> Entries => _entries;

    /// <summary>Add a name at the top (or move it there if already saved, keeping its hotkey).</summary>
    public static void Remember(string name)
    {
        int existing = IndexOf(name);
        if (existing == 0) return;
        var entry = existing > 0 ? _entries[existing] : new Entry { Name = name, Hotkey = Hotkey.None };
        if (existing > 0) _entries.RemoveAt(existing);
        _entries.Insert(0, entry);
        if (_entries.Count > MaxSaved) _entries.RemoveRange(MaxSaved, _entries.Count - MaxSaved);
        Save();
        Changed?.Invoke();
    }

    public static void Forget(string name)
    {
        int index = IndexOf(name);
        if (index < 0) return;
        _entries.RemoveAt(index);
        Save();
        Changed?.Invoke();
    }

    /// <summary>Bind <paramref name="hotkey"/> to a saved name (None clears it). A key moves off any other name.</summary>
    public static void SetHotkey(string name, Hotkey hotkey)
    {
        int index = IndexOf(name);
        if (index < 0) return;
        if (!hotkey.IsNone)
            foreach (var other in _entries)
                if (other.Hotkey == hotkey) other.Hotkey = Hotkey.None;
        _entries[index].Hotkey = hotkey;
        Save();
        Changed?.Invoke();
    }

    /// <summary>The saved name whose hotkey was pressed this frame, if any.</summary>
    public static bool TryGetPressed(out string name)
    {
        foreach (var entry in _entries)
        {
            if (!entry.Hotkey.IsDown()) continue;
            name = entry.Name;
            return true;
        }
        name = null;
        return false;
    }

    static int IndexOf(string name) =>
        _entries.FindIndex(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Read the current server's list (from ServerData, on joining it).</summary>
    public static void Load()
    {
        _entries.Clear();
        if (!ServerData.TryGetPath("familiars.txt", out _path)) return;
        try
        {
            if (File.Exists(_path))
            {
                foreach (var line in File.ReadAllLines(_path))
                {
                    var parts = line.Split('\t');
                    string name = FamiliarService.CleanName(parts[0]);
                    if (name.Length == 0 || IndexOf(name) >= 0 || _entries.Count >= MaxSaved) continue;
                    var hotkey = parts.Length > 1 && Hotkey.TryParse(parts[1], out var parsed) ? parsed : Hotkey.None;
                    if (!hotkey.IsNone && _entries.Any(e => e.Hotkey == hotkey)) hotkey = Hotkey.None;
                    _entries.Add(new Entry { Name = name, Hotkey = hotkey });
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"Couldn't read {_path}: {ex.Message}");
        }
        Changed?.Invoke(); // the UI outlives a server switch
    }

    static void Save()
    {
        if (_path == null) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            File.WriteAllLines(_path, _entries.Select(e => e.Hotkey.IsNone ? e.Name : $"{e.Name}\t{e.Hotkey}"));
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"Couldn't save {_path}: {ex.Message}");
        }
    }

    public static void OnWorldTeardown()
    {
        _entries.Clear();
        _path = null;
    }
}
