using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finvora.Models;
using Finvora.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Finvora.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly CustomerService _customerService = new();
        private readonly InstallmentService _installmentService = new();
        private readonly SettingsService _settingsService = new();
        private readonly BackupService _backupService = new();
        private readonly SecurityService _securityService = new();
        private readonly NotificationService _notificationService = new();

        private readonly NavItem _notificationsNavItem;
        private readonly DispatcherTimer _overdueCheckTimer;
        private readonly Queue<Notification> _toastQueue = new();
        private bool _isProcessingToastQueue;

        [ObservableProperty] private string businessName = "My Business";
        [ObservableProperty] private object? currentPage;
        [ObservableProperty] private int unreadNotificationCount;
        [ObservableProperty] private bool isToastVisible;
        [ObservableProperty] private string toastTitle = string.Empty;
        [ObservableProperty] private string toastMessage = string.Empty;
        [ObservableProperty] private NotificationType toastType;

        public ObservableCollection<NavItem> NavItems { get; }

        public MainViewModel()
        {
            _settingsService.Load();
            BusinessName = _settingsService.Current.BusinessName;

            _settingsService.SettingsChanged += OnSettingsChanged;
            _notificationService.NotificationAdded += OnNotificationAdded;
            _notificationService.NotificationsChanged += OnNotificationsChanged;
            _customerService.CustomersChanged += OnCustomersChangedForOverdueCheck;

            _notificationsNavItem = new("Notifications", "\uE7E7", () => new NotificationsViewModel(_notificationService));

            NavItems = new ObservableCollection<NavItem>
            {
                new("Dashboard",     "\uE80F", () => new DashboardViewModel(BusinessName, _customerService)),
                new("Customers",     "\uE77B", () => new CustomersViewModel(_customerService, _notificationService)),
                new("Installments",  "\uE787", () => new InstallmentsViewModel(_installmentService, _customerService, _notificationService)),
                new("Payments",      "\uE8C7", () => new ComingSoonViewModel("Payments")),
                _notificationsNavItem,
                new("Reports",       "\uE9D9", () => new ComingSoonViewModel("Reports")),
                new("Settings",      "\uE713", () => new SettingsViewModel(_settingsService, _backupService, _securityService)),
            };

            Navigate(NavItems[0]);

            _overdueCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            _overdueCheckTimer.Tick += async (_, _) => await RunOverdueCheckAsync();
            _overdueCheckTimer.Start();

            _ = InitializeNotificationsAsync();
        }

        [RelayCommand]
        private void Navigate(NavItem item)
        {
            foreach (var navItem in NavItems)
                navItem.IsSelected = navItem == item;

            if (CurrentPage is IDisposable disposable)
                disposable.Dispose();

            CurrentPage = item.CreatePageViewModel();
        }

        [RelayCommand]
        private void OpenNotifications() => Navigate(_notificationsNavItem);

        [RelayCommand]
        private void Logout()
        {
            var result = MessageBox.Show("Are you sure you want to log out?", "Log out", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
                Application.Current.Shutdown();
        }

        private void OnSettingsChanged(object? sender, EventArgs e)
        {
            BusinessName = _settingsService.Current.BusinessName;
        }

        private async Task InitializeNotificationsAsync()
        {
            try
            {
                UnreadNotificationCount = await _notificationService.GetUnreadCountAsync();
                _notificationsNavItem.BadgeCount = UnreadNotificationCount;
                await RunOverdueCheckAsync();
            }
            catch (Exception ex)
            {
                LogNotificationError("Notification initialization failed", ex);
                UnreadNotificationCount = 0;
                _notificationsNavItem.BadgeCount = 0;
            }
        }

        private async void OnCustomersChangedForOverdueCheck(object? sender, EventArgs e)
        {
            try
            {
                await RunOverdueCheckAsync();
            }
            catch (Exception ex)
            {
                LogNotificationError("Overdue check after customer change failed", ex);
            }
        }

        private async Task RunOverdueCheckAsync()
        {
            try
            {
                var customers = await _customerService.GetAllAsync();
                await _notificationService.CheckForOverdueAsync(customers);
            }
            catch (Exception ex)
            {
                LogNotificationError("Background overdue check failed", ex);
            }
        }

        private async void OnNotificationsChanged(object? sender, EventArgs e)
        {
            try
            {
                UnreadNotificationCount = await _notificationService.GetUnreadCountAsync();
                _notificationsNavItem.BadgeCount = UnreadNotificationCount;
            }
            catch (Exception ex)
            {
                LogNotificationError("Refreshing notification badge failed", ex);
            }
        }

        private void OnNotificationAdded(object? sender, Notification notification)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _toastQueue.Enqueue(notification);
                    if (!_isProcessingToastQueue)
                        _ = ProcessToastQueueAsync();
                });
            }
            catch (Exception ex)
            {
                LogNotificationError("Notification toast dispatch failed", ex);
            }
        }

        private async Task ProcessToastQueueAsync()
        {
            try
            {
                _isProcessingToastQueue = true;

                while (_toastQueue.Count > 0)
                {
                    var next = _toastQueue.Dequeue();
                    ToastTitle = next.Title;
                    ToastMessage = next.Message;
                    ToastType = next.Type;
                    IsToastVisible = true;

                    await Task.Delay(TimeSpan.FromSeconds(3));
                    IsToastVisible = false;
                    await Task.Delay(TimeSpan.FromMilliseconds(400));
                }
            }
            catch (Exception ex)
            {
                LogNotificationError("Notification toast processing failed", ex);
                IsToastVisible = false;
            }
            finally
            {
                _isProcessingToastQueue = false;
            }
        }

        private static void LogNotificationError(string operation, Exception ex)
        {
            Debug.WriteLine($"[Finvora Notifications] {operation}: {ex}");
        }
    }
}