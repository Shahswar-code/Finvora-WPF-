using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Finvora.Models
{
    /// <summary>
    /// One installment account/plan. A customer can have many of these (multiple
    /// products/plans over time) -- unlike the legacy single-plan fields still on
    /// Customer, which are left untouched so the existing Customers screen keeps
    /// working exactly as before. Stored fields are raw facts; everything derived
    /// is [NotMapped] and computed from DownPayment + Schedule, the same rule
    /// Customer already follows.
    /// </summary>
    public class Installment
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Human-friendly reference, e.g. "FIN-INS-00042". Assigned right
        /// after the first save once Id is known (see InstallmentService.CreateAsync).</summary>
        [MaxLength(20)]
        public string InstallmentNumber { get; set; } = string.Empty;

        [Required]
        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        [Required, MaxLength(150)]
        public string ItemName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DownPayment { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal InstallmentAmount { get; set; }

        public PlanFrequency Frequency { get; set; }

        public int NumberOfInstallments { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Today;

        public DateTime FirstDueDate { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.Now;

        public bool IsCancelled { get; set; }

        public ICollection<InstallmentSchedule> Schedule { get; set; } = new List<InstallmentSchedule>();

        // ---------- Computed (never persisted) ----------

        [NotMapped]
        public decimal FinancedAmount => Math.Max(0, TotalPrice - DownPayment);

        /// <summary>DownPayment + every rupee recorded against the schedule so far.</summary>
        [NotMapped]
        public decimal PaidAmount => DownPayment + Schedule.Sum(s => s.PaidAmount);

        [NotMapped]
        public decimal OutstandingAmount => Math.Max(0, TotalPrice - PaidAmount);

        [NotMapped]
        public double ProgressPercent => TotalPrice <= 0 ? 0 : (double)(PaidAmount / TotalPrice) * 100.0;

        [NotMapped]
        public InstallmentSchedule? NextInstallment => Schedule
            .Where(s => s.PaidAmount < s.Amount)
            .OrderBy(s => s.SequenceNumber)
            .FirstOrDefault();

        [NotMapped]
        public DateTime? NextDueDate => NextInstallment?.DueDate;

        [NotMapped]
        public int DaysOverdue => Status == InstallmentStatus.Overdue && NextDueDate.HasValue
            ? Math.Max(0, (DateTime.Today - NextDueDate.Value.Date).Days)
            : 0;

        [NotMapped]
        public InstallmentStatus Status
        {
            get
            {
                if (IsCancelled) return InstallmentStatus.Cancelled;
                if (OutstandingAmount <= 0) return InstallmentStatus.Completed;

                var unpaid = Schedule.Where(s => s.PaidAmount < s.Amount).ToList();
                if (unpaid.Any(s => s.DueDate.Date < DateTime.Today)) return InstallmentStatus.Overdue;
                if (unpaid.Any(s => s.DueDate.Date == DateTime.Today)) return InstallmentStatus.DueToday;
                return InstallmentStatus.Active;
            }
        }

        /// <summary>Drives the Overview's status filter pills.</summary>
        [NotMapped]
        public string FilterCategory => Status switch
        {
            InstallmentStatus.Completed => "Completed",
            InstallmentStatus.Overdue => "Overdue",
            InstallmentStatus.DueToday => "DueToday",
            InstallmentStatus.Cancelled => "Cancelled",
            _ => "Active"
        };

        [NotMapped]
        public string CustomerName => Customer?.FullName ?? "";

        [NotMapped]
        public string CustomerPhone => Customer?.Phone ?? "";

        [NotMapped]
        public string PlanShortLabel => Frequency switch
        {
            PlanFrequency.Daily => "day",
            PlanFrequency.Weekly => "week",
            PlanFrequency.Biweekly => "2 weeks",
            PlanFrequency.Monthly => "month",
            PlanFrequency.Yearly => "year",
            _ => "period"
        };
    }
}  