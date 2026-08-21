namespace Finvora.Models
{
    /// <summary>
    /// What kind of event a Notification represents. Stored as a string-safe
    /// int is fine here (unlike PlanFrequency) because notifications are never
    /// bulk re-seeded from legacy data -- append new members at the end if more
    /// are added later.
    /// </summary>
    public enum NotificationType
    {
        CustomerAdded,
        Overdue
    }
} 