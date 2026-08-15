using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finvora.Services;

namespace Finvora.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // One shared instance for the whole app session. Both CustomersViewModel and
        // (later) DashboardViewModel will use this same instance, so they react to the
        // same CustomersChanged event -- add a customer anywhere, every open screen
        // that cares updates automatically.
        private readonly CustomerService _customerService = new();

        // Placeholder until the Settings phase lets the user set their real business name.
        [ObservableProperty]
        private string businessName = "MediSoft Solutions";

        [ObservableProperty]
        private object? currentPage;

        public ObservableCollection<NavItem> NavItems { get; }

        public MainViewModel()
        {
            NavItems = new ObservableCollection<NavItem>
            {
                new("Dashboard",     "\uE80F", () => new DashboardViewModel(BusinessName, _customerService)),
                new("Customers",     "\uE77B", () => new CustomersViewModel(_customerService)),
                new("Installments",  "\uE787", () => new ComingSoonViewModel("Installments")),
                new("Payments",      "\uE8C7", () => new ComingSoonViewModel("Payments")),
                new("Notifications", "\uE7E7", () => new ComingSoonViewModel("Notifications")),
                new("Reports",       "\uE9D9", () => new ComingSoonViewModel("Reports")),
                new("Settings",      "\uE713", () => new ComingSoonViewModel("Settings")),
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

            // Stop the outgoing page's background work (e.g. event subscriptions)
            // before letting it go, so it doesn't keep running after it's off-screen.
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
    }
}  