using Finvora.Models;
using Microsoft.EntityFrameworkCore;

namespace Finvora.Data
{
    public class FinvoraDbContext : DbContext
    {
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<AppSecurity> AppSecurity => Set<AppSecurity>();
        public DbSet<Installment> Installments => Set<Installment>();
        public DbSet<InstallmentSchedule> InstallmentSchedules => Set<InstallmentSchedule>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connectionString =
                @"Server=(localdb)\MSSQLLocalDB;Database=FinvoraDb;Trusted_Connection=True;MultipleActiveResultSets=true";

            optionsBuilder.UseSqlServer(connectionString);
        }
    }
} 