using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Finvora.Models
{
    /// <summary>
    /// One due date in an Installment's payment plan. PaidAmount is the only
    /// thing the future Collect Payment step will ever write here -- Status is
    /// always derived from it, never stored, so it can't go stale.
    /// </summary>
    public class InstallmentSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InstallmentId { get; set; }

        [ForeignKey(nameof(InstallmentId))]
        public Installment? Installment { get; set; }

        public int SequenceNumber { get; set; }

        public DateTime DueDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }

        public DateTime? PaidDate { get; set; }

        [NotMapped]
        public decimal RemainingAmount => Math.Max(0, Amount - PaidAmount);

        [NotMapped]
        public ScheduleStatus Status
        {
            get
            {
                if (Amount > 0 && PaidAmount >= Amount) return ScheduleStatus.Paid;
                if (PaidAmount > 0) return ScheduleStatus.Partial;
                if (DueDate.Date < DateTime.Today) return ScheduleStatus.Overdue;
                if (DueDate.Date == DateTime.Today) return ScheduleStatus.DueToday;
                return ScheduleStatus.Upcoming;
            }
        }
    }
}  