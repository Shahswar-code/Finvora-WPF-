namespace Finvora.Models
{
    /// <summary>
    /// How a payment was received. IMPORTANT: only ever APPEND new values at the
    /// end -- enums are stored as plain ints (see PlanFrequency for the same rule).
    /// </summary>
    public enum PaymentMethod
    {
        Cash,
        BankTransfer,
        Card,
        JazzCash,
        Easypaisa,
        Other
    }
} 