using System.IO;
using System.Text.Json;
using PsLauncher.Models;

namespace PsLauncher.Services;

public sealed class StorageService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly string _baseDir;
    private readonly string _gamesPath;
    private readonly string _settingsPath;

    public StorageService()
    {
        _baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PsLauncher");

        Directory.CreateDirectory(_baseDir);

        _gamesPath = Path.Combine(_baseDir, "games.json");
        _settingsPath = Path.Combine(_baseDir, "settings.json");
    }

    public async Task<List<GameEntry>> LoadGamesAsync()
    {
        if (!File.Exists(_gamesPath))
            return [];

        await using var fs = File.OpenRead(_gamesPath);
        var data = await JsonSerializer.DeserializeAsync<List<GameEntry>>(fs, JsonOpts);
        return data ?? [];
    }

    public async Task SaveGamesAsync(IEnumerable<GameEntry> games)
    {
        await using var fs = File.Create(_gamesPath);
        await JsonSerializer.SerializeAsync(fs, games, JsonOpts);
    }

    public async Task<AppSettings> LoadSettingsAsync()
    {
        if (!File.Exists(_settingsPath))
            return new AppSettings();

        await using var fs = File.OpenRead(_settingsPath);
        var data = await JsonSerializer.DeserializeAsync<AppSettings>(fs, JsonOpts);
        return data ?? new AppSettings();
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        await using var fs = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(fs, settings, JsonOpts);
    }

    public string GetDataFolder() => _baseDir;
}
