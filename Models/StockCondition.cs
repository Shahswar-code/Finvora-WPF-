namespace Finvora.Models
{
    /// <summary>Physical condition of a stock item. Append-only -- values are
    /// stored as plain ints, same rule as PaymentMethod/PlanFrequency.</summary>
    public enum StockCondition
    {
        New,
        Used,
        Refurbished,
        OpenBox,
        Other
    }
} 