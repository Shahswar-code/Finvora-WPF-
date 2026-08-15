using System.IO;
using System.Threading.Tasks;
using Finvora.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Finvora.Services.Startup.Tasks
{
    /// <summary>
    /// Ensures the FINVORA LocalDB database exists at our chosen file path, then
    /// applies any pending EF Core migrations.
    ///
    /// Creates the database explicitly, ONCE, under a real (non-auto-generated) name
    /// -- "FinvoraDb" -- instead of relying on AttachDbFilename per connection, which
    /// was the actual cause of the recurring "auto-named database ... failed" error.
    ///
    /// Self-healing: if a stale "FinvoraDb" registration exists from an earlier
    /// broken run (pointing at a file that no longer exists on disk), it is dropped
    /// automatically before recreating, so this can never get stuck in that state
    /// again no matter how many times the DATA folder gets wiped during development.
    /// </summary>
    public class EnsureDatabaseTask : IStartupTask
    {
        public string StatusText => "Preparing database...";

        private const string DatabaseName = "FinvoraDb";

        public async Task ExecuteAsync()
        {
            string dbFilePath = Path.Combine(EnsureAppDataFolderTask.DatabaseFolderPath, "Finvora.mdf");
            string logFilePath = Path.ChangeExtension(dbFilePath, ".ldf");

            if (!File.Exists(dbFilePath))
            {
                await CreateDatabaseFileAsync(dbFilePath, logFilePath);
            }

            using var db = new FinvoraDbContext();
            await db.Database.MigrateAsync();
        }

        private static async Task CreateDatabaseFileAsync(string dbFilePath, string logFilePath)
        {
            const string masterConnectionString =
                @"Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;";

            using var connection = new SqlConnection(masterConnectionString);
            await connection.OpenAsync();

            // Self-heal: drop any stale registration under this name before creating.
            using (var dropCommand = connection.CreateCommand())
            {
                dropCommand.CommandText =
                    $"IF DB_ID('{DatabaseName}') IS NOT NULL DROP DATABASE [{DatabaseName}];";
                await dropCommand.ExecuteNonQueryAsync();
            }

            using var createCommand = connection.CreateCommand();
            createCommand.CommandText = $@"
                CREATE DATABASE [{DatabaseName}]
                ON PRIMARY (NAME = N'{DatabaseName}', FILENAME = N'{dbFilePath}')
                LOG ON (NAME = N'{DatabaseName}_log', FILENAME = N'{logFilePath}')";
            await createCommand.ExecuteNonQueryAsync();
        }
    }
}   