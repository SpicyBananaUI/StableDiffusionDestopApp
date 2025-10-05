using System;
using System.IO;

namespace myApp.Services;

using System.Text.Json;

public class AppSettings
{
    public string ApiKey { get; set; } = string.Empty;
}

public static class ConfigManager
{
    private static string ConfigDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "myApp");

    private static string ConfigPath => Path.Combine(ConfigDir, "config.json");

    public static AppSettings Settings { get; private set; } = new();

    public static void Load()
    {
        if (!Directory.Exists(ConfigDir))
            Directory.CreateDirectory(ConfigDir);

        if (File.Exists(ConfigPath))
        {
            string json = File.ReadAllText(ConfigPath);
            Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
    }

    public static void Save()
    {
        if (!Directory.Exists(ConfigDir))
            Directory.CreateDirectory(ConfigDir);

        string json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }
}
