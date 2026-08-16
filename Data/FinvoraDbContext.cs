using Finvora.Models;
using Microsoft.EntityFrameworkCore;

namespace Finvora.Data
{
    public class FinvoraDbContext : DbContext
    {
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<AppSecurity> AppSecurity => Set<AppSecurity>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // No AttachDbFilename here on purpose. AttachDbFilename re-attaches the
            // .mdf under an auto-generated name on every single connection, which
            // conflicts if anything else still has the file open. Instead we connect
            // by database name -- EnsureDatabaseTask creates it once, explicitly, at
            // our chosen file path, and every connection after that is a normal
            // by-name connection with no attach step at all.
            string connectionString =
                @"Server=(localdb)\MSSQLLocalDB;Database=FinvoraDb;Trusted_Connection=True;MultipleActiveResultSets=true";

            optionsBuilder.UseSqlServer(connectionString);
        }
    }
}  