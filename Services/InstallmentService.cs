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
    /// Single source of truth for installment data -- same role CustomerService
    /// plays for customers. Always loads Customer + Schedule together so every
    /// computed property on Installment (Status, PaidAmount, NextDueDate...)
    /// has what it needs.
    /// </summary>
    public class InstallmentService
    {
        public event EventHandler? InstallmentsChanged;

        public async Task<List<Installment>> GetAllAsync()
        {
            using var db = new FinvoraDbContext();
            return await db.Installments
                .Include(i => i.Customer)
                .Include(i => i.Schedule)
                .AsNoTracking()
                .OrderByDescending(i => i.DateAdded)
                .ToListAsync();
        }

        public async Task<Installment?> GetByIdAsync(int id)
        {
            using var db = new FinvoraDbContext();
            return await db.Installments
                .Include(i => i.Customer)
                .Include(i => i.Schedule)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<List<Installment>> GetByCustomerIdAsync(int customerId)
        {
            using var db = new FinvoraDbContext();
            return await db.Installments
                .Include(i => i.Schedule)
                .AsNoTracking()
                .Where(i => i.CustomerId == customerId)
                .OrderByDescending(i => i.DateAdded)
                .ToListAsync();
        }

        /// <summary>Persists a new Installment together with its already-generated
        /// Schedule, then assigns the human-friendly InstallmentNumber once the
        /// real Id is known.</summary>
        public async Task<Installment> CreateAsync(Installment installment)
        {
            using var db = new FinvoraDbContext();
            db.Installments.Add(installment);
            await db.SaveChangesAsync();

            installment.InstallmentNumber = $"FIN-INS-{installment.Id:D5}";
            await db.SaveChangesAsync();

            InstallmentsChanged?.Invoke(this, EventArgs.Empty);
            return installment;
        }
    }
}  