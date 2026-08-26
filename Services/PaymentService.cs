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
    /// Single source of truth for payment transactions. A payment is always
    /// recorded against one InstallmentSchedule row -- CreateAsync re-loads that
    /// row fresh from the database right before applying the payment (basic
    /// concurrency guard against two windows paying the same installment) and
    /// both the Payment insert and the InstallmentSchedule update are saved in
    /// one SaveChangesAsync call, so they succeed or fail together.
    /// </summary>
    public class PaymentService
    {
        private readonly InstallmentService _installmentService;

        /// <summary>Raised after a payment is recorded or voided.</summary>
        public event EventHandler? PaymentsChanged;

        public PaymentService(InstallmentService installmentService)
        {
            _installmentService = installmentService;
        }

        public async Task<List<Payment>> GetAllAsync()
        {
            using var db = new FinvoraDbContext();
            return await db.Payments
                .Include(p => p.Customer)
                .Include(p => p.Installment)
                .Include(p => p.InstallmentSchedule)
                .AsNoTracking()
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        public async Task<List<Payment>> GetByInstallmentIdAsync(int installmentId)
        {
            using var db = new FinvoraDbContext();
            return await db.Payments
                .Include(p => p.Customer)
                .Include(p => p.InstallmentSchedule)
                .AsNoTracking()
                .Where(p => p.InstallmentId == installmentId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        /// <summary>
        /// Records a payment against InstallmentScheduleId. Throws
        /// InvalidOperationException with a user-friendly message if the
        /// schedule row no longer exists or the amount doesn't fit the
        /// remaining balance -- callers should show that message directly.
        /// </summary>
        public async Task<Payment> CreateAsync(Payment payment)
        {
            if (payment.Amount <= 0)
                throw new InvalidOperationException("Payment amount must be greater than zero.");

            using var db = new FinvoraDbContext();

            // Re-check the live balance right before writing -- protects against
            // a second window having already paid this row down in the meantime.
            var row = await db.InstallmentSchedules
                .FirstOrDefaultAsync(s => s.Id == payment.InstallmentScheduleId);

            if (row is null)
                throw new InvalidOperationException("Selected installment is no longer available.");

            var remaining = Math.Max(0, row.Amount - row.PaidAmount);
            if (payment.Amount > remaining)
                throw new InvalidOperationException(
                    $"Payment amount cannot exceed the remaining installment balance (Rs {remaining:N0}).");

            payment.InstallmentId = row.InstallmentId;
            payment.Status = PaymentTransactionStatus.Paid;

            db.Payments.Add(payment);

            row.PaidAmount += payment.Amount;
            if (row.PaidAmount >= row.Amount)
                row.PaidDate = DateTime.Now;

            await db.SaveChangesAsync();

            payment.PaymentNumber = $"PAY-{payment.Id:D6}";
            await db.SaveChangesAsync();

            _installmentService.RaiseChanged();
            PaymentsChanged?.Invoke(this, EventArgs.Empty);
            return payment;
        }

        /// <summary>
        /// Voids a payment: the row stays in the database with Status = Voided,
        /// and its amount is rolled back off the InstallmentSchedule row it was
        /// applied to. Never physically deletes a payment.
        /// </summary>
        public async Task VoidAsync(int paymentId, string? reason)
        {
            using var db = new FinvoraDbContext();

            var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId);
            if (payment is null || payment.Status == PaymentTransactionStatus.Voided) return;

            var row = await db.InstallmentSchedules
                .FirstOrDefaultAsync(s => s.Id == payment.InstallmentScheduleId);

            if (row is not null)
            {
                row.PaidAmount = Math.Max(0, row.PaidAmount - payment.Amount);
                if (row.PaidAmount < row.Amount) row.PaidDate = null;
            }

            payment.Status = PaymentTransactionStatus.Voided;
            payment.VoidReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
            payment.VoidedAt = DateTime.Now;

            await db.SaveChangesAsync();

            _installmentService.RaiseChanged();
            PaymentsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
} 