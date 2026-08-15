namespace Finvora.Models
{
    /// <summary>
    /// Derived from AmountPaid vs TotalPrice — never stored directly, always computed,
    /// so it can never go stale after a payment is recorded (see Customer.Status).
    /// </summary>
    public enum PaymentStatus
    {
        Unpaid,
        Partial,
        Paid
    }
}  