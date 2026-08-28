namespace Finvora.Models
{
    /// <summary>Computed only -- never stored. See StockItem.Status.</summary>
    public enum StockItemStatus
    {
        InStock,
        LowStock,
        OutOfStock
    }
} 