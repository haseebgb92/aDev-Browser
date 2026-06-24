using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace DevBrowser.Models
{
    public class AppSettings
    {
        public string DefaultNewTabUrl { get; set; } = "about:blank";
        public string DevToolsPosition { get; set; } = "Bottom";
        public List<int> LocalhostPorts { get; set; } = new List<int> { 3000, 5173, 8000, 8080 };
        public bool AlwaysVisibleConsole { get; set; } = false;
        public bool ScrollSyncDefault { get; set; } = false;
        public bool ConfirmCloseMultipleTabs { get; set; } = true;

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DevBrowser", "settings.json");

        public static AppSettings Load()
        {
            if (!File.Exists(SettingsPath)) return new AppSettings();
            try
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (directory != null && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch { /* Ignore errors in saving settings */ }
        }
    }
}
