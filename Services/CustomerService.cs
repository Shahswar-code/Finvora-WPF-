using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Finvora.Data;
using Finvora.Models;
using Microsoft.EntityFrameworkCore;

namespace Finvora.Services
{
    /// <summary>
    /// Single source of truth for customer data. Both the Dashboard and the
    /// Customers screen read through this service instead of talking to the
    /// database directly, so every screen reflects the same data and can stay
    /// in sync via the CustomersChanged event.
    /// </summary>
    public class CustomerService
    {
        /// <summary>
        /// Raised after any Add/Update/Delete/payment succeeds, so subscribers
        /// (e.g. the Dashboard) know to reload their numbers.
        /// </summary>
        public event EventHandler? CustomersChanged;

        public async Task<List<Customer>> GetAllAsync()
        {
            using var db = new FinvoraDbContext();
            return await db.Customers
                .AsNoTracking()
                .OrderByDescending(c => c.DateAdded)
                .ToListAsync();
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            using var db = new FinvoraDbContext();
            return await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AddAsync(Customer customer)
        {
            using var db = new FinvoraDbContext();
            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            CustomersChanged?.Invoke(this, EventArgs.Empty);
        }

        public async Task UpdateAsync(Customer customer)
        {
            using var db = new FinvoraDbContext();
            db.Customers.Update(customer);
            await db.SaveChangesAsync();

            CustomersChanged?.Invoke(this, EventArgs.Empty);
        }

        public async Task DeleteAsync(int id)
        {
            using var db = new FinvoraDbContext();
            var customer = await db.Customers.FindAsync(id);
            if (customer is null) return;

            db.Customers.Remove(customer);
            await db.SaveChangesAsync();

            CustomersChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Adds `amount` to a customer's AmountPaid, clamped so it can never
        /// exceed TotalPrice. Backs the "Partially Paid" action in the Edit screen.
        /// </summary>
        public async Task RecordPaymentAsync(int customerId, decimal amount)
        {
            using var db = new FinvoraDbContext();
            var customer = await db.Customers.FindAsync(customerId);
            if (customer is null) return;

            customer.AmountPaid = Math.Min(customer.AmountPaid + amount, customer.TotalPrice);
            await db.SaveChangesAsync();

            CustomersChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Backs the "Fully Paid" action in the Edit screen.</summary>
        public async Task MarkFullyPaidAsync(int customerId)
        {
            using var db = new FinvoraDbContext();
            var customer = await db.Customers.FindAsync(customerId);
            if (customer is null) return;

            customer.AmountPaid = customer.TotalPrice;
            await db.SaveChangesAsync();

            CustomersChanged?.Invoke(this, EventArgs.Empty);
        }
    }
} 