using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finvora.Models;
using Finvora.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices.ActiveDirectory;
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

        // Kept so bell-icon clicks and the sidebar unread badge can both target
        // the exact same NavItem instance without re-searching NavItems each time.
        private readonly NavItem _notificationsNavItem;

        // Runs for the whole app session (unlike CustomersViewModel's own timer,
        // which only exists while the Customers page is open) so a customer can
        // go overdue while the user is on the Dashboard, Settings, anywhere --
        // and still get caught.
        private readonly DispatcherTimer _overdueCheckTimer;

        private readonly Queue<Notification> _toastQueue = new();
        private bool _isProcessingToastQueue;

        [ObservableProperty]
        private string businessName = "My Business";

        [ObservableProperty]
        private object? currentPage;

        [ObservableProperty]
        private int unreadNotificationCount;

        // ----- Toast popup state (slides down, holds ~3s, slides back up) -----
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

            // Safety-net poll: catches a due date rolling into the past while
            // the app just sits open on any screen -- mirrors the pattern
            // CustomersViewModel already uses for its own day-rollover check.
            _overdueCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            _overdueCheckTimer.Tick += async (_, _) => await RunOverdueCheckAsync();
            _overdueCheckTimer.Start();

            _ = InitializeNotificationsAsync();
        }

        [RelayCommand]
        private void Navigate(NavItem item)
        {
            foreach (var navItem in NavItems)
            {
                navItem.IsSelected = navItem == item;
            }

            if (CurrentPage is System.IDisposable disposable)
            {
                disposable.Dispose();
            }

            CurrentPage = item.CreatePageViewModel();
        }

        /// <summary>Bell icon click -- jumps straight to the Notifications page.</summary>
        [RelayCommand]
        private void OpenNotifications() => Navigate(_notificationsNavItem);

        [RelayCommand]
        private void Logout()
        {
            var result = MessageBox.Show(
                "Are you sure you want to log out?",
                "Log out",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        private void OnSettingsChanged(object? sender, EventArgs e)
        {
            BusinessName = _settingsService.Current.BusinessName;
        }

        private async Task InitializeNotificationsAsync()
        {
            UnreadNotificationCount = await _notificationService.GetUnreadCountAsync();
            _notificationsNavItem.BadgeCount = UnreadNotificationCount;

            await RunOverdueCheckAsync();
        }

        private async void OnCustomersChangedForOverdueCheck(object? sender, EventArgs e) => await RunOverdueCheckAsync();

        private async Task RunOverdueCheckAsync()
        {
            var customers = await _customerService.GetAllAsync();
            await _notificationService.CheckForOverdueAsync(customers);
        }

        private async void OnNotificationsChanged(object? sender, EventArgs e)
        {
            UnreadNotificationCount = await _notificationService.GetUnreadCountAsync();
            _notificationsNavItem.BadgeCount = UnreadNotificationCount;
        }

        /// <summary>
        /// Queues the toast; ProcessToastQueueAsync shows one at a time so a
        /// burst of notifications (e.g. several customers going overdue at
        /// once) never overlaps into a garbled popup.
        /// </summary>
        private void OnNotificationAdded(object? sender, Notification notification)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _toastQueue.Enqueue(notification);
                if (!_isProcessingToastQueue)
                {
                    _ = ProcessToastQueueAsync();
                }
            });
        }

        private async Task ProcessToastQueueAsync()
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

                // Give the slide-up animation room to finish before the next
                // toast (if any) starts sliding down, so they never collide.
                await Task.Delay(TimeSpan.FromMilliseconds(400));
            }

            _isProcessingToastQueue = false;
        }
    }
} 