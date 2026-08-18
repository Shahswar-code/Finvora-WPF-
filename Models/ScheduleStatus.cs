namespace Finvora.Models
{
    /// <summary>Derived from PaidAmount vs Amount vs DueDate -- never stored.</summary>
    public enum ScheduleStatus
    {
        Upcoming,
        DueToday,
        Overdue,
        Partial,
        Paid
    }
} 