using System;
using System.Linq;
using System.Threading.Tasks;
using Finvora.Data;
using Finvora.Models;
using Finvora.Services;
using Microsoft.EntityFrameworkCore;

namespace Finvora.Services.Startup.Tasks
{
    /// <summary>
    /// One-time, idempotent migration: gives every existing Customer (who
    /// predates the Installments module) a matching Installment record, so the
    /// new Installments Overview shows their real plan instead of an empty list.
    ///
    /// Honesty rule: we only know each customer's current totals, not their
    /// actual payment history, so we don't fabricate past schedule rows.
    ///   - DownPayment is set to the customer's FULL AmountPaid to date
    ///     (everything paid so far, as one lump sum -- there's no more granular
    ///     record than that).
    ///   - The generated Schedule covers only what's still OUTSTANDING, starting
    ///     at the customer's existing DueDate, at their existing InstallmentAmount.
    /// This reconciles exactly: PaidAmount on the new Installment always equals
    /// the customer's current AmountPaid.
    ///
    /// Runs every startup but is a no-op after the first time -- it only ever
    /// touches customers who don't already have at least one Installment.
    /// </summary>
    public class SeedInstallmentsFromLegacyCustomersTask : IStartupTask
    {
        public string StatusText => "Preparing installment plans...";

        public async Task ExecuteAsync()
        {
            using var db = new FinvoraDbContext();

            var customersAlreadyMigrated = await db.Installments
                .Select(i => i.CustomerId)
                .Distinct()
                .ToListAsync();

            var pendingCustomers = await db.Customers
                .Where(c => !customersAlreadyMigrated.Contains(c.Id))
                .ToListAsync();

            if (pendingCustomers.Count == 0) return;

            foreach (var customer in pendingCustomers)
            {
                if (customer.TotalPrice <= 0) continue; // nothing to migrate

                var outstanding = Math.Max(0, customer.TotalPrice - customer.AmountPaid);

                var installment = new Installment
                {
                    CustomerId = customer.Id,
                    ItemName = customer.ItemName,
                    TotalPrice = customer.TotalPrice,
                    DownPayment = customer.AmountPaid,
                    InstallmentAmount = customer.InstallmentAmount,
                    Frequency = customer.Frequency,
                    StartDate = customer.DateAdded,
                    FirstDueDate = customer.DueDate,
                    DateAdded = customer.DateAdded
                };

                if (outstanding > 0 && customer.InstallmentAmount > 0)
                {
                    installment.NumberOfInstallments =
                        InstallmentCalculationService.CountFromAmount(outstanding, customer.InstallmentAmount);

                    installment.Schedule = InstallmentCalculationService.GenerateSchedule(
                        outstanding, installment.NumberOfInstallments, customer.InstallmentAmount,
                        customer.DueDate, customer.Frequency);
                }

                db.Installments.Add(installment);
            }

            await db.SaveChangesAsync();

            // Assign human-friendly numbers now that every new row has a real Id.
            foreach (var installment in db.ChangeTracker.Entries<Installment>()
                         .Select(e => e.Entity)
                         .Where(i => string.IsNullOrEmpty(i.InstallmentNumber)))
            {
                installment.InstallmentNumber = $"FIN-INS-{installment.Id:D5}";
            }

            await db.SaveChangesAsync();
        }
    }
}  