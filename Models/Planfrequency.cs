namespace Finvora.Models
{
    /// <summary>
    /// How often an installment is due.
    /// IMPORTANT: only ever APPEND new values at the end. Enums are stored as
    /// plain ints -- inserting a value in the middle would silently reinterpret
    /// every Frequency already saved in the database.
    /// </summary>
    public enum PlanFrequency
    {
        Daily,
        Weekly,
        Monthly,
        Yearly,
        Biweekly
    }
} 