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
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<Payment> Payments => Set<Payment>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connectionString =
                @"Server=(localdb)\MSSQLLocalDB;Database=FinvoraDb;Trusted_Connection=True;MultipleActiveResultSets=true";

            optionsBuilder.UseSqlServer(connectionString);
        }

        // Payment keeps a direct, cascading link to Customer (so deleting a
        // customer cleanly removes their payment history too), but its links
        // to Installment/InstallmentSchedule are restricted -- without this,
        // SQL Server sees two cascade routes into the Payments table
        // (Customer->Payment, and Customer->Installment->Schedule->Payment)
        // and refuses to create the constraints at all.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Installment)
                .WithMany()
                .HasForeignKey(p => p.InstallmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.InstallmentSchedule)
                .WithMany()
                .HasForeignKey(p => p.InstallmentScheduleId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}  