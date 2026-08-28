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
    /// Single source of truth for inventory. Quantity on StockItem is never
    /// written directly by callers -- every path here that changes it also
    /// writes a StockMovement in the same SaveChangesAsync call, so the two
    /// never drift out of sync.
    /// </summary>
    public class StockService
    {
        /// <summary>Raised after any stock item or quantity change.</summary>
        public event EventHandler? StockChanged;

        public async Task<List<StockItem>> GetAllAsync(bool includeArchived = false)
        {
            using var db = new FinvoraDbContext();
            var query = db.StockItems.AsNoTracking().AsQueryable();
            if (!includeArchived) query = query.Where(s => s.IsActive);
            return await query.OrderBy(s => s.ItemName).ToListAsync();
        }

        public async Task<StockItem?> GetByIdAsync(int id)
        {
            using var db = new FinvoraDbContext();
            return await db.StockItems.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<StockMovement>> GetHistoryAsync(int stockItemId)
        {
            using var db = new FinvoraDbContext();
            return await db.StockMovements
                .AsNoTracking()
                .Where(m => m.StockItemId == stockItemId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        /// <summary>Creates a new stock item. If it starts with a quantity
        /// greater than zero, a "Stock In / Initial Stock" movement is
        /// recorded so the history isn't missing where the first units came from.</summary>
        public async Task<StockItem> CreateAsync(StockItem item)
        {
            if (item.Quantity < 0) throw new InvalidOperationException("Quantity cannot be negative.");
            if (item.DealerPrice < 0 || item.WholesalePrice < 0) throw new InvalidOperationException("Prices cannot be negative.");

            using var db = new FinvoraDbContext();

            item.CreatedAt = DateTime.Now;
            item.UpdatedAt = DateTime.Now;
            item.IsActive = true;

            db.StockItems.Add(item);

            if (item.Quantity > 0)
            {
                db.StockMovements.Add(new StockMovement
                {
                    StockItem = item,
                    MovementType = StockMovementType.StockIn,
                    QuantityChange = item.Quantity,
                    PreviousQuantity = 0,
                    NewQuantity = item.Quantity,
                    Reason = "Initial stock",
                    ReferenceType = "Manual",
                    CreatedAt = DateTime.Now
                });
            }

            await db.SaveChangesAsync();
            StockChanged?.Invoke(this, EventArgs.Empty);
            return item;
        }

        /// <summary>Updates a stock item's editable fields. If Quantity was
        /// changed, records an Adjustment movement for the difference --
        /// callers should use DeductStockAsync instead for a sale, so that
        /// gets tagged as StockOut with a proper reference, not a generic
        /// adjustment.</summary>
        public async Task UpdateAsync(StockItem updated, string? adjustmentReason = null)
        {
            if (updated.Quantity < 0) throw new InvalidOperationException("Quantity cannot be negative.");
            if (updated.DealerPrice < 0 || updated.WholesalePrice < 0) throw new InvalidOperationException("Prices cannot be negative.");

            using var db = new FinvoraDbContext();

            var existing = await db.StockItems.FirstOrDefaultAsync(s => s.Id == updated.Id);
            if (existing is null) throw new InvalidOperationException("This stock item no longer exists.");

            var previousQuantity = existing.Quantity;

            existing.ItemName = updated.ItemName;
            existing.ReferenceNumber = updated.ReferenceNumber;
            existing.Category = updated.Category;
            existing.DistributorName = updated.DistributorName;
            existing.DistributorPhone = updated.DistributorPhone;
            existing.DistributorEmail = updated.DistributorEmail;
            existing.DistributorAddress = updated.DistributorAddress;
            existing.ContactPerson = updated.ContactPerson;
            existing.ImporterReference = updated.ImporterReference;
            existing.Condition = updated.Condition;
            existing.Quantity = updated.Quantity;
            existing.MinimumStock = updated.MinimumStock;
            existing.DealerPrice = updated.DealerPrice;
            existing.WholesalePrice = updated.WholesalePrice;
            existing.Description = updated.Description;
            existing.UpdatedAt = DateTime.Now;

            if (updated.Quantity != previousQuantity)
            {
                db.StockMovements.Add(new StockMovement
                {
                    StockItemId = existing.Id,
                    MovementType = StockMovementType.Adjustment,
                    QuantityChange = updated.Quantity - previousQuantity,
                    PreviousQuantity = previousQuantity,
                    NewQuantity = updated.Quantity,
                    Reason = string.IsNullOrWhiteSpace(adjustmentReason) ? "Manual edit" : adjustmentReason,
                    ReferenceType = "Manual",
                    CreatedAt = DateTime.Now
                });
            }

            await db.SaveChangesAsync();
            StockChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Deducts stock for a sale (e.g. an installment plan being
        /// saved). Re-checks the live quantity right before writing -- basic
        /// concurrency guard, same pattern as PaymentService.CreateAsync.
        /// Throws a friendly InvalidOperationException the caller can show
        /// directly if there isn't enough stock.</summary>
        public async Task DeductStockAsync(int stockItemId, int quantity, string referenceType, int? referenceId, string reason)
        {
            if (quantity <= 0) throw new InvalidOperationException("Quantity must be greater than zero.");

            using var db = new FinvoraDbContext();

            var item = await db.StockItems.FirstOrDefaultAsync(s => s.Id == stockItemId);
            if (item is null) throw new InvalidOperationException("Selected stock item is no longer available.");

            if (quantity > item.Quantity)
                throw new InvalidOperationException($"Insufficient stock. Only {item.Quantity} units are available.");

            var previousQuantity = item.Quantity;
            item.Quantity -= quantity;
            item.UpdatedAt = DateTime.Now;

            db.StockMovements.Add(new StockMovement
            {
                StockItemId = item.Id,
                MovementType = StockMovementType.StockOut,
                QuantityChange = -quantity,
                PreviousQuantity = previousQuantity,
                NewQuantity = item.Quantity,
                Reason = reason,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                CreatedAt = DateTime.Now
            });

            await db.SaveChangesAsync();
            StockChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Archives (soft-deletes) a stock item if it has any
        /// movement history -- hard-deletes it only if it's never been
        /// touched, so historical sales never end up pointing at nothing.</summary>
        public async Task RemoveAsync(int stockItemId)
        {
            using var db = new FinvoraDbContext();

            var item = await db.StockItems.FirstOrDefaultAsync(s => s.Id == stockItemId);
            if (item is null) return;

            var hasHistory = await db.StockMovements.AnyAsync(m => m.StockItemId == stockItemId);

            if (hasHistory)
            {
                item.IsActive = false;
                item.UpdatedAt = DateTime.Now;
            }
            else
            {
                db.StockItems.Remove(item);
            }

            await db.SaveChangesAsync();
            StockChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}  