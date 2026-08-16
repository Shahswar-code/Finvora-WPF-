using Finvora.Data;
using Finvora.Services.Startup.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Finvora.Services
{
    /// <summary>
    /// Handles real database-level Backup, Restore, and full Reset via SQL Server's
    /// own BACKUP DATABASE / RESTORE DATABASE commands -- never touches the .mdf/.ldf
    /// files directly, which avoids the corruption risk of copying files while
    /// LocalDB has them open.
    /// </summary>
    public class BackupService
    {
        private const string DatabaseName = "FinvoraDb";

        private static string MasterConnectionString =>
            @"Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;";

        /// <summary>Creates a timestamped .bak file in the Backups folder. Safe to run anytime.</summary>
        public async Task<string> BackupAsync()
        {
            string fileName = $"Finvora_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
            string filePath = Path.Combine(EnsureAppDataFolderTask.BackupsFolderPath, fileName);

            using var connection = new SqlConnection(MasterConnectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = $"BACKUP DATABASE [{DatabaseName}] TO DISK = @path WITH INIT;";
            command.Parameters.AddWithValue("@path", filePath);
            await command.ExecuteNonQueryAsync();

            return filePath;
        }

        /// <summary>
        /// Replaces the current database entirely with the given .bak file.
        /// Requires exclusive (single-user) access, so any other open connection
        /// to the database is forcibly closed for the duration of the restore.
        /// </summary>
        public async Task RestoreAsync(string backupFilePath)
        {
            string dbFilePath = Path.Combine(EnsureAppDataFolderTask.DatabaseFolderPath, "Finvora.mdf");
            string logFilePath = Path.ChangeExtension(dbFilePath, ".ldf");

            using var connection = new SqlConnection(MasterConnectionString);
            await connection.OpenAsync();

            using (var singleUserCommand = connection.CreateCommand())
            {
                singleUserCommand.CommandText = $"ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;";
                await singleUserCommand.ExecuteNonQueryAsync();
            }

            using (var restoreCommand = connection.CreateCommand())
            {
                restoreCommand.CommandText = $@"
                    RESTORE DATABASE [{DatabaseName}]
                    FROM DISK = @path
                    WITH REPLACE,
                    MOVE '{DatabaseName}' TO @dbFile,
                    MOVE '{DatabaseName}_log' TO @logFile;";
                restoreCommand.Parameters.AddWithValue("@path", backupFilePath);
                restoreCommand.Parameters.AddWithValue("@dbFile", dbFilePath);
                restoreCommand.Parameters.AddWithValue("@logFile", logFilePath);
                await restoreCommand.ExecuteNonQueryAsync();
            }

            using (var multiUserCommand = connection.CreateCommand())
            {
                multiUserCommand.CommandText = $"ALTER DATABASE [{DatabaseName}] SET MULTI_USER;";
                await multiUserCommand.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Drops the database entirely and recreates it empty (schema only, via
        /// migrations) -- same creation logic as EnsureDatabaseTask, reused here
        /// for a full wipe. This also erases the PIN, since AppSecurity lives in
        /// the same database being dropped.
        /// </summary>
        public async Task ResetAsync()
        {
            string dbFilePath = Path.Combine(EnsureAppDataFolderTask.DatabaseFolderPath, "Finvora.mdf");
            string logFilePath = Path.ChangeExtension(dbFilePath, ".ldf");

            using var connection = new SqlConnection(MasterConnectionString);
            await connection.OpenAsync();

            using (var singleUserCommand = connection.CreateCommand())
            {
                singleUserCommand.CommandText =
                    $"IF DB_ID('{DatabaseName}') IS NOT NULL ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;";
                await singleUserCommand.ExecuteNonQueryAsync();
            }

            using (var dropCommand = connection.CreateCommand())
            {
                dropCommand.CommandText = $"IF DB_ID('{DatabaseName}') IS NOT NULL DROP DATABASE [{DatabaseName}];";
                await dropCommand.ExecuteNonQueryAsync();
            }

            using (var createCommand = connection.CreateCommand())
            {
                createCommand.CommandText = $@"
                    CREATE DATABASE [{DatabaseName}]
                    ON PRIMARY (NAME = N'{DatabaseName}', FILENAME = N'{dbFilePath}')
                    LOG ON (NAME = N'{DatabaseName}_log', FILENAME = N'{logFilePath}')";
                await createCommand.ExecuteNonQueryAsync();
            }

            using var db = new FinvoraDbContext();
            await db.Database.MigrateAsync();
        }
    }
} 