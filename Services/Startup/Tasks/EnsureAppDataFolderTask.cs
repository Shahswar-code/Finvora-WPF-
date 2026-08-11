using System;
using System.IO;
using System.Threading.Tasks;

namespace Finvora.Services.Startup.Tasks
{
    /// <summary>
    /// Ensures the local FINVORA data folder exists under the current user's
    /// AppData\Local, along with a Backups subfolder. This is where the
    /// database file (Phase 14) and backup archives (Phase 12) will live.
    /// </summary>
    public class EnsureAppDataFolderTask : IStartupTask
    {
        public string StatusText => "Preparing workspace...";

        public static string DataFolderPath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Finvora");

        public static string BackupsFolderPath { get; } = Path.Combine(DataFolderPath, "Backups");

        public Task ExecuteAsync()
        {
            Directory.CreateDirectory(DataFolderPath);
            Directory.CreateDirectory(BackupsFolderPath);

            return Task.CompletedTask;
        }
    }
}
