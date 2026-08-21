using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Finvora.Data;
using Finvora.Models;
using Microsoft.EntityFrameworkCore;

namespace Finvora.Services
{
    /// <summary>
    /// Single source of truth for notifications. Owned once, at the shell level
    /// (MainViewModel), so the bell icon, sidebar unread count, toast popups,
    /// and the Notifications page itself all stay in sync no matter which
    /// screen the user is actually looking at.
    /// </summary>
    public class NotificationService
    {
        /// <summary>
        /// Raised whenever the persisted notification list changes in any way
        /// (a new one added, one/all marked read). Drives the unread-count
        /// badges and the Notifications page's list.
        /// </summary>
        public event EventHandler? NotificationsChanged;

        /// <summary>
        /// Raised ONLY when a brand-new notification is created -- never for
        /// read-state changes. This is what drives the slide-down toast popup,
        /// so marking things as read never re-triggers a toast.
        /// </summary>
        public event EventHandler<Notification>? NotificationAdded;

        public async Task<List<Notification>> GetAllAsync()
        {
            using var db = new FinvoraDbContext();
            return await db.Notifications
                .AsNoTracking()
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync()
        {
            using var db = new FinvoraDbContext();
            return await db.Notifications.CountAsync(n => !n.IsRead);
        }

        /// <summary>Records that a new customer was added and shows a toast for it.</summary>
        public async Task NotifyCustomerAddedAsync(Customer customer)
        {
            var notification = new Notification
            {
                Title = "New customer added",
                Message = $"{customer.FullName} was added with a {customer.ItemName} plan of Rs {customer.TotalPrice:N0}.",
                Type = NotificationType.CustomerAdded,
                RelatedCustomerId = customer.Id,
                CreatedAt = DateTime.Now
            };

            await SaveAndAnnounceAsync(notification);
        }

        /// <summary>
        /// Scans the given customers for anyone currently overdue and creates a
        /// notification for each one that doesn't already have one covering
        /// their *current* due date -- so this is safe to call repeatedly
        /// (every minute, on every customer change) without spamming duplicate
        /// alerts for the same overdue plan.
        /// </summary>
        public async Task CheckForOverdueAsync(IEnumerable<Customer> customers)
        {
            var overdueCustomers = customers.Where(c => c.IsOverdue).ToList();
            if (overdueCustomers.Count == 0) return;

            using var db = new FinvoraDbContext();

            foreach (var customer in overdueCustomers)
            {
                bool alreadyNotified = await db.Notifications.AnyAsync(n =>
                    n.RelatedCustomerId == customer.Id &&
                    n.Type == NotificationType.Overdue &&
                    n.CreatedAt >= customer.DueDate);

                if (alreadyNotified) continue;

                var notification = new Notification
                {
                    Title = "Payment overdue",
                    Message = $"{customer.FullName}'s installment of Rs {customer.RemainingBalance:N0} was due on {customer.DueDate:dd MMM yyyy}.",
                    Type = NotificationType.Overdue,
                    RelatedCustomerId = customer.Id,
                    CreatedAt = DateTime.Now
                };

                db.Notifications.Add(notification);
                await db.SaveChangesAsync();

                NotificationsChanged?.Invoke(this, EventArgs.Empty);
                NotificationAdded?.Invoke(this, notification);
            }
        }

        public async Task MarkAllAsReadAsync()
        {
            using var db = new FinvoraDbContext();
            var unread = await db.Notifications.Where(n => !n.IsRead).ToListAsync();
            if (unread.Count == 0) return;

            foreach (var n in unread) n.IsRead = true;
            await db.SaveChangesAsync();

            NotificationsChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task SaveAndAnnounceAsync(Notification notification)
        {
            using var db = new FinvoraDbContext();
            db.Notifications.Add(notification);
            await db.SaveChangesAsync();

            NotificationsChanged?.Invoke(this, EventArgs.Empty);
            NotificationAdded?.Invoke(this, notification);
        }
    }
} 