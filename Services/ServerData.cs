using ProjectM;

namespace DragUI.Services;

// Where the current server's data lives: BepInEx/config/DragUI/servers/<session id>/, with a server.txt naming
// the server. The session id comes from the server's hail message and is kept with its save, so it survives
// restarts and a new address, and a wipe starts clean. Files from before data was split by server sit directly
// in BepInEx/config/DragUI: the first server to ask for one takes it over.
internal static class ServerData
{
    const string UnknownServer = "unknown";

    static string _directory;

    /// <summary>The name the current server goes by (empty until we're in game).</summary>
    public static string ServerName { get; private set; } = string.Empty;

    /// <summary>By name, not session id: a wipe changes the id, and the name is how its players know it.</summary>
    public static bool IsNoctveil => ServerName.Contains("Noctveil", StringComparison.OrdinalIgnoreCase);

    /// <summary>The current server's copy of <paramref name="fileName"/>. False until we're in game.</summary>
    public static bool TryGetPath(string fileName, out string path)
    {
        path = null;
        if (!EnsureJoined()) return false;
        path = Path.Combine(_directory, fileName);
        AdoptLegacyFile(fileName, path);
        return true;
    }

    /// <summary>Called every frame in game: on joining a server, loads the lists that belong to it.</summary>
    public static void Tick() => EnsureJoined();

    static bool EnsureJoined()
    {
        if (_directory != null) return true;
        if (!Core.InGame) return false;

        string key = ReadServerKey(out string name);
        ServerName = name;
        _directory = Path.Combine(Plugin.DataDirectory, "servers", key);
        Core.Log.LogInfo($"Using the data for server '{name}' ({key}).");
        WriteServerName(name);

        FamiliarStore.Load();
        HuntStore.Load();
        RespawnMemory.Load();
        return true;
    }

    static string ReadServerKey(out string name)
    {
        name = string.Empty;
        try
        {
            var bootstrap = Core.Client.GetExistingSystemManaged<ClientBootstrapSystem>();
            var hail = bootstrap.HailMessageStruct;
            name = hail.Name.Value;
            if (Guid.TryParse(hail.SessionGuid.ToString(), out var session) && session != Guid.Empty) return session.ToString();

            string address = bootstrap._LastConnectData.ConnectAddress.ToString();
            Core.Log.LogWarning($"The server sent no session id; telling it apart by its address ({address}).");
            string safe = string.Concat(address.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
            if (!string.IsNullOrWhiteSpace(safe)) return safe;
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"Couldn't tell which server this is: {ex.Message}");
        }
        return UnknownServer;
    }

    static void WriteServerName(string name)
    {
        try
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(Path.Combine(_directory, "server.txt"), name);
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"Couldn't save the server's name in {_directory}: {ex.Message}");
        }
    }

    static void AdoptLegacyFile(string fileName, string path)
    {
        string legacy = Path.Combine(Plugin.DataDirectory, fileName);
        try
        {
            if (File.Exists(path) || !File.Exists(legacy)) return;
            File.Move(legacy, path);
            Core.Log.LogInfo($"Moved {fileName} to this server's data.");
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"Couldn't move {legacy} to {path}: {ex.Message}");
        }
    }

    /// <summary>Leaving the server: the next login may be another one. Plain field resets only.</summary>
    public static void OnWorldTeardown()
    {
        _directory = null;
        ServerName = string.Empty;
        FamiliarStore.OnWorldTeardown();
        HuntStore.OnWorldTeardown();
        RespawnMemory.OnWorldTeardown();
    }
}
