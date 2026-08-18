namespace Finvora.Models
{
    /// <summary>
    /// Computed from TotalPrice, DownPayment and the schedule -- never stored
    /// directly, so it can never disagree with the numbers it's based on (same
    /// rule Customer.Status already follows in the legacy single-plan model).
    /// Settled/Partial are reserved for the Settlement and Collect Payment
    /// phases -- not computed yet in this phase.
    /// </summary>
    public enum InstallmentStatus
    {
        Active,
        DueToday,
        Overdue,
        Partial,
        Completed,
        Settled,
        Cancelled
    }
}  