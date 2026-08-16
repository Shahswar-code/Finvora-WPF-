namespace Finvora.Models
{
    /// <summary>
    /// The business's own identity/profile info -- shown on the Dashboard greeting
    /// and (eventually) on exported invoices. Persisted as plain JSON, completely
    /// separate from the SQL database, so a database Restore never touches it.
    /// </summary>
    public class BusinessSettings
    {
        public string BusinessName { get; set; } = "My Business";
        public string OwnerName { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string ContactAddress { get; set; } = string.Empty;

        /// <summary>Prefix used everywhere money is displayed, e.g. "Rs", "$", "PKR".</summary>
        public string CurrencySymbol { get; set; } = "Rs";
    }
} 