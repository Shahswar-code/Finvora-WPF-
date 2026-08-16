using System;
using System.ComponentModel.DataAnnotations;

namespace Finvora.Models
{
    /// <summary>
    /// Single-row table holding the hashed PIN that protects the "Reset All Data"
    /// action. Never stores the PIN itself -- only a PBKDF2 hash + its salt, so
    /// even direct database access can't recover the real PIN.
    /// </summary>
    public class AppSecurity
    {
        [Key]
        public int Id { get; set; }

        public string PinHash { get; set; } = string.Empty;
        public string PinSalt { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
} 