using Finvora.Models;
using Finvora.Services.Startup.Tasks;
using System;
using System.IO;
using System.Text.Json;

namespace Finvora.Services
{
    /// <summary>
    /// Single source of truth for business-profile settings. Backed by a plain
    /// JSON file (not the SQL database) so it survives a database Restore and
    /// never requires a DB round-trip just to read the business name.
    /// Synchronous by design -- this file is tiny and local, so there's no
    /// benefit to async here, and it lets MainViewModel load it in its
    /// constructor with zero risk of a stale value flashing on first paint.
    /// </summary>
    public class SettingsService
    {
        /// <summary>Raised after Save succeeds, so subscribers (e.g. MainViewModel) refresh.</summary>
        public event EventHandler? SettingsChanged;

        private static string FilePath => Path.Combine(EnsureAppDataFolderTask.DataFolderPath, "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        /// <summary>In-memory cache -- always reflects the last Load or Save.</summary>
        public BusinessSettings Current { get; private set; } = new();

        public void Load()
        {
            if (!File.Exists(FilePath))
            {
                Current = new BusinessSettings();
                return;
            }

            try
            {
                var json = File.ReadAllText(FilePath);
                Current = JsonSerializer.Deserialize<BusinessSettings>(json) ?? new BusinessSettings();
            }
            catch
            {
                // Corrupted or unreadable settings file -- fall back to defaults
                // rather than crashing the whole app on startup.
                Current = new BusinessSettings();
            }
        }

        public void Save(BusinessSettings settings)
        {
            Current = settings;

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(FilePath, json);

            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}