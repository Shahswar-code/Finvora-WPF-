using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finvora.Services;

namespace Finvora.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly CustomerService _customerService = new();
        private readonly SettingsService _settingsService = new();
        private readonly BackupService _backupService = new();
        private readonly SecurityService _securityService = new();

        [ObservableProperty]
        private string businessName = "My Business";

        [ObservableProperty]
        private object? currentPage;

        public ObservableCollection<NavItem> NavItems { get; }

        public MainViewModel()
        {
            _settingsService.Load();
            BusinessName = _settingsService.Current.BusinessName;

            _settingsService.SettingsChanged += OnSettingsChanged;

            NavItems = new ObservableCollection<NavItem>
            {
                new("Dashboard",     "\uE80F", () => new DashboardViewModel(BusinessName, _customerService)),
                new("Customers",     "\uE77B", () => new CustomersViewModel(_customerService)),
                new("Installments",  "\uE787", () => new ComingSoonViewModel("Installments")),
                new("Payments",      "\uE8C7", () => new ComingSoonViewModel("Payments")),
                new("Notifications", "\uE7E7", () => new ComingSoonViewModel("Notifications")),
                new("Reports",       "\uE9D9", () => new ComingSoonViewModel("Reports")),
                new("Settings",      "\uE713", () => new SettingsViewModel(_settingsService, _backupService, _securityService)),
            };

            Navigate(NavItems[0]);
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

        private void OnSettingsChanged(object? sender, System.EventArgs e)
        {
            BusinessName = _settingsService.Current.BusinessName;
        }
    }
} 