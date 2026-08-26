namespace Finvora.Models
{
    /// <summary>
    /// Status of a recorded Payment transaction -- NOT the same thing as
    /// Customer.Status/PaymentStatus (which describes an installment plan's
    /// overall paid state). A voided payment is never deleted, only excluded
    /// from collection totals and rolled back off its InstallmentSchedule row.
    /// </summary>
    public enum PaymentTransactionStatus
    {
        Paid,
        Voided
    }
} 