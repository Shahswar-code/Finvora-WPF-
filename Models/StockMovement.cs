using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Finvora.Models
{
    /// <summary>
    /// One audit-trail entry for a change to a StockItem's Quantity. Never
    /// edited or deleted once written -- this is the "what actually happened"
    /// record behind the running total on StockItem.
    /// </summary>
    public class StockMovement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StockItemId { get; set; }

        [ForeignKey(nameof(StockItemId))]
        public StockItem? StockItem { get; set; }

        public StockMovementType MovementType { get; set; }

        /// <summary>Signed -- positive for Stock In, negative for Stock Out.</summary>
        public int QuantityChange { get; set; }

        public int PreviousQuantity { get; set; }

        public int NewQuantity { get; set; }

        [MaxLength(200)]
        public string? Reason { get; set; }

        /// <summary>What triggered this, e.g. "Installment", "Manual". Kept as
        /// a plain string + optional Id rather than a hard FK, since a
        /// movement's source (an installment sale today, maybe something else
        /// later) shouldn't force a schema change here.</summary>
        [MaxLength(40)]
        public string? ReferenceType { get; set; }

        public int? ReferenceId { get; set; }

        [MaxLength(200)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
} 