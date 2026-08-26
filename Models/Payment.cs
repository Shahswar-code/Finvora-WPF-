using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Finvora.Models
{
    /// <summary>
    /// One recorded money-received transaction against a specific
    /// InstallmentSchedule row. This is the audit trail -- InstallmentSchedule
    /// still owns the running PaidAmount total (unchanged), Payment records
    /// *how it got there*, one entry per transaction, so partial payments,
    /// receipts and voids all have something concrete to point at.
    /// </summary>
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Human-friendly reference, e.g. "PAY-000042". Assigned right
        /// after the first save once Id is known (same pattern as InstallmentNumber).</summary>
        [MaxLength(20)]
        public string PaymentNumber { get; set; } = string.Empty;

        [Required]
        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        [Required]
        public int InstallmentId { get; set; }

        [ForeignKey(nameof(InstallmentId))]
        public Installment? Installment { get; set; }

        /// <summary>The specific due-date row this payment was applied to.</summary>
        [Required]
        public int InstallmentScheduleId { get; set; }

        [ForeignKey(nameof(InstallmentScheduleId))]
        public InstallmentSchedule? InstallmentSchedule { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public PaymentMethod Method { get; set; }

        [MaxLength(60)]
        public string? ReferenceNumber { get; set; }

        [MaxLength(300)]
        public string? Notes { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Paid;

        // ---------- Void audit trail (never delete a payment) ----------
        [MaxLength(300)]
        public string? VoidReason { get; set; }

        public DateTime? VoidedAt { get; set; }

        // ---------- Computed (never persisted) ----------

        [NotMapped]
        public string CustomerName => Customer?.FullName ?? "";

        [NotMapped]
        public string CustomerPhone => Customer?.Phone ?? "";

        [NotMapped]
        public string InstallmentNumber => Installment?.InstallmentNumber ?? "";

        [NotMapped]
        public bool IsVoided => Status == PaymentTransactionStatus.Voided;
        
        [NotMapped]
        public bool CanVoid => Status == PaymentTransactionStatus.Paid;
    }
}  