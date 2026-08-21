using System;
using System.ComponentModel.DataAnnotations;

namespace Finvora.Models
{
    /// <summary>
    /// One notification: a new customer being added, or a customer's plan
    /// going overdue. Persisted so the Notifications page survives app
    /// restarts; IsRead drives the unread badge on the bell icon and the
    /// Notifications nav item.
    /// </summary>
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(400)]
        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; }

        /// <summary>Optional link back to the customer this notification is about.</summary>
        public int? RelatedCustomerId { get; set; }

        /// <summary>Icon glyph (Segoe MDL2 Assets) shown next to this notification.</summary>
        public string Glyph => Type switch
        {
            NotificationType.CustomerAdded => "\uE77B",
            NotificationType.Overdue => "\uE7BA",
            _ => "\uE7E7"
        };
    }
}  