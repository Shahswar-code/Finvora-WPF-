using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Finvora.Models
{
    /// <summary>
    /// One inventory line -- a physical product FINVORA can sell. Quantity here
    /// is the single running total; every change to it (a sale, a restock, a
    /// manual correction) must go through StockService so a matching
    /// StockMovement record is written -- never edit Quantity directly.
    /// </summary>
    public class StockItem
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(120)]
        public string ItemName { get; set; } = string.Empty;

        [Required, MaxLength(40)]
        public string ReferenceNumber { get; set; } = string.Empty;

        /// <summary>Free-text category ("Auto Parts", "Electronics", ...). Kept
        /// as a simple string rather than a separate Category table -- Finvora
        /// has no category system today and this keeps the feature to one new
        /// concept instead of two; the ComboBox in the UI still suggests a
        /// curated list so it stays tidy without forcing a fixed set.</summary>
        [MaxLength(60)]
        public string? Category { get; set; }

        // ---------- Distributor / importer ----------
        [Required, MaxLength(120)]
        public string DistributorName { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? DistributorPhone { get; set; }

        [MaxLength(100)]
        public string? DistributorEmail { get; set; }

        [MaxLength(200)]
        public string? DistributorAddress { get; set; }

        [MaxLength(80)]
        public string? ContactPerson { get; set; }

        [MaxLength(40)]
        public string? ImporterReference { get; set; }

        // ---------- Stock details ----------
        public StockCondition Condition { get; set; } = StockCondition.New;

        public int Quantity { get; set; }

        public int MinimumStock { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DealerPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal WholesalePrice { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>Soft-delete flag. Items that have ever appeared in a
        /// StockMovement are archived (IsActive = false), never hard-deleted --
        /// so historical sales keep pointing at a real row.</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // ---------- Computed (never persisted) ----------

        [NotMapped]
        public decimal StockValue => Quantity * DealerPrice;

        [NotMapped]
        public StockItemStatus Status
        {
            get
            {
                if (Quantity <= 0) return StockItemStatus.OutOfStock;
                if (Quantity <= MinimumStock) return StockItemStatus.LowStock;
                return StockItemStatus.InStock;
            }
        }

        [NotMapped]
        public bool IsOutOfStock => Quantity <= 0;
    }
} 