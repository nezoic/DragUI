using DragUI.Data;

namespace DragUI.Services;

// The V Bloods the player chose to hunt on this server. Saved as one PrefabGUID per line in hunts.txt
// in the server's data folder (see ServerData).
internal static class HuntStore
{
    static string _path;
    static readonly HashSet<int> _selected = new();

    public static event Action Changed;

    public static int Count => _selected.Count;

    public static bool IsHunted(VBloodInfo info) => _selected.Contains(info.Guid.GuidHash);

    /// <summary>Hunted V Bloods in catalog order.</summary>
    public static IEnumerable<VBloodInfo> Hunted => VBloodCatalog.All.Where(IsHunted);

    public static void SetHunted(VBloodInfo info, bool hunted)
    {
        bool changed = hunted ? _selected.Add(info.Guid.GuidHash) : _selected.Remove(info.Guid.GuidHash);
        if (!changed) return;
        Save();
        Changed?.Invoke();
    }

    public static void Toggle(VBloodInfo info) => SetHunted(info, !IsHunted(info));

    public static void Clear()
    {
        if (_selected.Count == 0) return;
        _selected.Clear();
        Save();
        Changed?.Invoke();
    }

    /// <summary>Read the current server's list (from ServerData, on joining it).</summary>
    public static void Load()
    {
        _selected.Clear();
        if (!ServerData.TryGetPath("hunts.txt", out _path)) return;
        try
        {
            if (File.Exists(_path))
            {
                foreach (var line in File.ReadAllLines(_path))
                {
                    if (int.TryParse(line.Trim(), out int guid) && VBloodCatalog.TryGet(guid, out _))
                        _selected.Add(guid);
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
            File.WriteAllLines(_path, _selected.Select(g => g.ToString()));
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"Couldn't save {_path}: {ex.Message}");
        }
    }

    public static void OnWorldTeardown()
    {
        _selected.Clear();
        _path = null;
    }
}
