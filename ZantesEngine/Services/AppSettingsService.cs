using System;
using System.IO;
using System.Text.Json;

namespace ZantesEngine.Services
{
    public sealed class AppSettings
    {
        public bool RichPresenceEnabled { get; set; } = true;
        public bool AlwaysOnTop { get; set; }
        public bool LaunchMaximized { get; set; }
        public bool AutoCheckUpdates { get; set; } = true;
        public string PreferredLanguage { get; set; } = "en";
        public bool HasSeenFirstBoot { get; set; }
        public bool HasSeenQuickBoostLanding { get; set; }
    }

    public static class AppSettingsService
    {
        private static readonly string SettingsPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZantesEngine", "app_settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new AppSettings();

                string json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // ignored: settings persistence is best-effort
            }
        }
    }
}
