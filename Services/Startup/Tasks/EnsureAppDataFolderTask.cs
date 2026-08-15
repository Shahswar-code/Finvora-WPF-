using System;
using System.IO;
using System.Threading.Tasks;

namespace Finvora.Services.Startup.Tasks
{
    /// <summary>
    /// Ensures the local FINVORA data folder exists under the current user's
    /// AppData\Local, along with DATA (database file) and Backups subfolders.
    /// Must run before EnsureDatabaseTask, since LocalDB will fail to attach
    /// the .mdf file if DatabaseFolderPath doesn't exist yet.
    /// </summary>
    public class EnsureAppDataFolderTask : IStartupTask
    {
        public string StatusText => "Preparing workspace...";

        public static string DataFolderPath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Finvora");

        public static string DatabaseFolderPath { get; } = Path.Combine(DataFolderPath, "DATA");

        public static string BackupsFolderPath { get; } = Path.Combine(DataFolderPath, "Backups");

        public Task ExecuteAsync()
        {
            Directory.CreateDirectory(DataFolderPath);
            Directory.CreateDirectory(DatabaseFolderPath);
            Directory.CreateDirectory(BackupsFolderPath);

            return Task.CompletedTask;
        }
    }
}  