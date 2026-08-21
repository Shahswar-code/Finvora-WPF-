using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finvora.Models;
using Finvora.Services;

namespace Finvora.ViewModels
{
    public partial class NotificationsViewModel : ObservableObject, IDisposable
    {
        private readonly NotificationService _notificationService;

        public ObservableCollection<Notification> Notifications { get; } = new();

        [ObservableProperty] private bool isLoading;
        [ObservableProperty] private bool isEmpty;

        public NotificationsViewModel(NotificationService notificationService)
        {
            _notificationService = notificationService;
            _notificationService.NotificationsChanged += OnNotificationsChanged;

            _ = LoadThenMarkReadAsync();
        }

        [RelayCommand]
        private async Task Refresh() => await LoadAsync();

        private async void OnNotificationsChanged(object? sender, EventArgs e) => await LoadAsync();

        /// <summary>
        /// Loads the list first so the page never opens empty, then marks
        /// everything read -- which fires NotificationsChanged again, refreshing
        /// this list's IsRead flags and clearing the bell/sidebar badge via
        /// MainViewModel's own subscription to the same event.
        /// </summary>
        private async Task LoadThenMarkReadAsync()
        {
            await LoadAsync();

            if (Notifications.Any(n => !n.IsRead))
            {
                await _notificationService.MarkAllAsReadAsync();
            }
        }

        private async Task LoadAsync()
        {
            IsLoading = true;
            var all = await _notificationService.GetAllAsync();
            IsLoading = false;

            Notifications.Clear();
            foreach (var notification in all)
            {
                Notifications.Add(notification);
            }

            IsEmpty = Notifications.Count == 0;
        }

        public void Dispose()
        {
            _notificationService.NotificationsChanged -= OnNotificationsChanged;
        }
    }
} 