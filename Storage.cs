using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace WeaponPaints;

/// <summary>
/// JSON file-based storage for player skins, knives, and gloves.
/// Replaces MySQL database dependency.
/// </summary>
public class Storage
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private ConcurrentDictionary<string, PlayerData> _data = new();

    public Storage(string filePath)
    {
        _filePath = filePath;
        Load();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _data = JsonConvert.DeserializeObject<ConcurrentDictionary<string, PlayerData>>(json)
                        ?? new ConcurrentDictionary<string, PlayerData>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WeaponPaints] Error loading player data: {ex.Message}");
            _data = new ConcurrentDictionary<string, PlayerData>();
        }
    }

    private void Save()
    {
        try
        {
            lock (_lock)
            {
                var json = JsonConvert.SerializeObject(_data, Formatting.Indented);
                var dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(_filePath, json);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WeaponPaints] Error saving player data: {ex.Message}");
        }
    }

    public PlayerData? GetPlayerData(string steamId)
    {
        _data.TryGetValue(steamId, out var data);
        return data;
    }

    public void SavePlayerData(string steamId, PlayerData data)
    {
        _data[steamId] = data;
        Save();
    }

    public void SavePlayerKnife(string steamId, Dictionary<int, string> knife)
    {
        var data = _data.GetOrAdd(steamId, _ => new PlayerData());
        data.Knife = knife;
        Save();
    }

    public void SavePlayerGloves(string steamId, Dictionary<int, ushort> gloves)
    {
        var data = _data.GetOrAdd(steamId, _ => new PlayerData());
        data.Gloves = gloves;
        Save();
    }

    public void SavePlayerSkins(string steamId, Dictionary<int, Dictionary<int, WeaponInfo>> skins)
    {
        var data = _data.GetOrAdd(steamId, _ => new PlayerData());
        data.Skins = skins;
        Save();
    }
}

/// <summary>
/// Represents all saved data for a single player.
/// </summary>
public class PlayerData
{
    [JsonProperty("knife")]
    public Dictionary<int, string> Knife { get; set; } = new();

    [JsonProperty("gloves")]
    public Dictionary<int, ushort> Gloves { get; set; } = new();

    [JsonProperty("skins")]
    public Dictionary<int, Dictionary<int, WeaponInfo>> Skins { get; set; } = new();
}
