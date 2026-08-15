using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Finvora.Models
{
    /// <summary>
    /// One customer + their single installment plan.
    /// Stored fields are the raw facts; everything derived (status, remaining balance,
    /// overdue flag, filter bucket) is [NotMapped] and computed on read, so it's
    /// impossible for it to disagree with the numbers it's based on.
    /// </summary>
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        // ---------- Section 1: Customer info ----------
        [Required, MaxLength(120)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(120)]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? Cnic { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }

        // ---------- Section 2: Plan info ----------
        [Required, MaxLength(150)]
        public string ItemName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        /// <summary>Down payment taken at signup.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal AdvancePaid { get; set; }

        /// <summary>
        /// Running total paid to date (starts equal to AdvancePaid at creation;
        /// grows as installments are recorded once the Payments module exists).
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal InstallmentAmount { get; set; }

        public PlanFrequency Frequency { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.Now;

        /// <summary>Next installment due date.</summary>
        public DateTime DueDate { get; set; }

        // ---------- Computed (never persisted) ----------

        [NotMapped]
        public string Initial => string.IsNullOrWhiteSpace(FullName) ? "?" : FullName.Trim()[0].ToString().ToUpper();

        [NotMapped]
        public decimal RemainingBalance => Math.Max(0, TotalPrice - AmountPaid);

        [NotMapped]
        public double PaymentProgressPercent => TotalPrice <= 0 ? 0 : (double)(AmountPaid / TotalPrice) * 100.0;

        [NotMapped]
        public PaymentStatus Status =>
            AmountPaid <= 0 ? PaymentStatus.Unpaid :
            AmountPaid >= TotalPrice ? PaymentStatus.Paid :
            PaymentStatus.Partial;

        /// <summary>True only while balance remains and the due date has passed.</summary>
        [NotMapped]
        public bool IsOverdue => RemainingBalance > 0 && DueDate.Date < DateTime.Today;

        /// <summary>Drives the "All / Active / Overdue / Complete" filter pills.</summary>
        [NotMapped]
        public string FilterCategory =>
            RemainingBalance <= 0 ? "Complete" :
            IsOverdue ? "Overdue" :
            "Active";

        [NotMapped]
        public string PlanShortLabel => Frequency switch
        {
            PlanFrequency.Daily => "day",
            PlanFrequency.Weekly => "week",
            PlanFrequency.Monthly => "month",
            PlanFrequency.Yearly => "year",
            _ => "period"
        };
    }
}  