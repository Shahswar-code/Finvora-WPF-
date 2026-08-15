using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finvora.Models;
using Finvora.Services;
using Finvora.Views;

namespace Finvora.ViewModels
{
    public partial class CustomersViewModel : ObservableObject, IDisposable
    {
        private readonly CustomerService _customerService;
        private List<Customer> _allCustomers = new();

        [ObservableProperty] private string searchText = "";
        [ObservableProperty] private string selectedFilter = "All";

        public ObservableCollection<Customer> FilteredCustomers { get; } = new();

        [ObservableProperty] private bool isLoading;
        [ObservableProperty] private bool isEmpty;

        // ----- Stat cards -----
        [ObservableProperty] private int totalCustomers;
        [ObservableProperty] private int totalPlans;
        [ObservableProperty] private string pendingBalance = "Rs 0";
        [ObservableProperty] private int overdueCount;

        public CustomersViewModel(CustomerService customerService)
        {
            _customerService = customerService;
            _customerService.CustomersChanged += OnCustomersChanged;

            _ = LoadAsync();
        }

        partial void OnSearchTextChanged(string value) => ApplyFilter();
        partial void OnSelectedFilterChanged(string value) => ApplyFilter();

        [RelayCommand]
        private void SetFilter(string filter) => SelectedFilter = filter;

        [RelayCommand]
        private async Task Refresh() => await LoadAsync();

        [RelayCommand]
        private void AddNewCustomer()
        {
            var vm = new AddCustomerViewModel(_customerService);
            var window = new AddCustomerWindow(vm)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        [RelayCommand]
        private void ExportAll()
        {
            // Step J will replace this with real PDF export.
            MessageBox.Show("Export All PDF is coming in a later step.", "Coming soon",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void ViewCustomer(Customer customer)
        {
            var vm = new CustomerInvoiceViewModel(customer);
            var window = new CustomerInvoiceWindow(vm)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        }  

        [RelayCommand]
        private void EditCustomer(Customer customer)
        {
            var vm = new EditCustomerViewModel(_customerService, customer);
            var window = new EditCustomerWindow(vm)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        [RelayCommand]
        private async Task DeleteCustomer(Customer customer)
        {
            var result = MessageBox.Show(
                $"Delete {customer.FullName}? This cannot be undone.",
                "Confirm delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            await _customerService.DeleteAsync(customer.Id);
            // FilteredCustomers refreshes automatically via CustomersChanged.
        }

        [RelayCommand]
        private void ExportCustomer(Customer customer)
        {
            // Step J will replace this with a real PDF receipt (like your screenshot).
            MessageBox.Show($"PDF receipt for {customer.FullName} is coming in a later step.", "Coming soon",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void OnCustomersChanged(object? sender, EventArgs e) => await LoadAsync();

        private async Task LoadAsync()
        {
            IsLoading = true;
            _allCustomers = await _customerService.GetAllAsync();
            IsLoading = false;

            TotalCustomers = _allCustomers.Count;
            TotalPlans = _allCustomers.Count;
            PendingBalance = $"Rs {_allCustomers.Sum(c => c.RemainingBalance):N0}";
            OverdueCount = _allCustomers.Count(c => c.IsOverdue);

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            IEnumerable<Customer> query = _allCustomers;

            query = SelectedFilter switch
            {
                "Active" => query.Where(c => c.FilterCategory == "Active"),
                "Overdue" => query.Where(c => c.FilterCategory == "Overdue"),
                "Complete" => query.Where(c => c.FilterCategory == "Complete"),
                _ => query
            };

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.Trim();
                query = query.Where(c =>
                    c.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (c.Cnic ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    c.Phone.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            FilteredCustomers.Clear();
            foreach (var customer in query.OrderByDescending(c => c.DateAdded))
            {
                FilteredCustomers.Add(customer);
            }

            IsEmpty = FilteredCustomers.Count == 0;
        }

        public void Dispose()
        {
            _customerService.CustomersChanged -= OnCustomersChanged;
        }
    }
}  