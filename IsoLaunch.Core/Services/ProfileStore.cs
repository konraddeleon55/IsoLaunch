using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using IsoLaunch.Core.Models;

namespace IsoLaunch.Core.Services;

/// <summary>
/// Persists application profiles to a local JSON file.
/// </summary>
public sealed class ProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath;

    public ProfileStore(string? filePath = null)
    {
        _filePath = string.IsNullOrWhiteSpace(filePath)
            ? GetDefaultStorePath()
            : filePath;
    }

    public string FilePath => _filePath;

    public static string GetDefaultStorePath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IsoLaunch",
            "profiles.json");
    }

    public List<AppProfile> Load()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        var json = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<AppProfile>>(json, JsonOptions) ?? [];
    }

    public void Save(IEnumerable<AppProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(profiles, JsonOptions);
        File.WriteAllText(_filePath, json);
    }
}
