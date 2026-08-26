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
using Microsoft.VisualBasic;

namespace Finvora.ViewModels
{
    public partial class PaymentsViewModel : ObservableObject, IDisposable
    {
        private readonly PaymentService _paymentService;
        private readonly InstallmentService _installmentService;
        private readonly CustomerService _customerService;
        private List<Payment> _allPayments = new();

        [ObservableProperty] private string searchText = "";
        [ObservableProperty] private string selectedFilter = "All";

        public ObservableCollection<Payment> FilteredPayments { get; } = new();

        [ObservableProperty] private bool isLoading;
        [ObservableProperty] private bool isEmpty;

        // ----- KPI cards -- all database-driven, nothing hard-coded -----
        [ObservableProperty] private string todaysCollections = "Rs 0";
        [ObservableProperty] private string thisMonthCollections = "Rs 0";
        [ObservableProperty] private string pendingPayments = "Rs 0";
        [ObservableProperty] private string overduePayments = "Rs 0";
        [ObservableProperty] private string totalCollected = "Rs 0";

        public PaymentsViewModel(PaymentService paymentService, InstallmentService installmentService, CustomerService customerService)
        {
            _paymentService = paymentService;
            _installmentService = installmentService;
            _customerService = customerService;

            _paymentService.PaymentsChanged += OnDataChanged;
            _installmentService.InstallmentsChanged += OnDataChanged;

            _ = LoadAsync();
        }

        partial void OnSearchTextChanged(string value) => ApplyFilter();
        partial void OnSelectedFilterChanged(string value) => ApplyFilter();

        [RelayCommand]
        private void SetFilter(string filter) => SelectedFilter = filter;

        [RelayCommand]
        private async Task Refresh() => await LoadAsync();

        [RelayCommand]
        private void RecordPayment()
        {
            var vm = new RecordPaymentViewModel(_paymentService, _installmentService, _customerService);
            var window = new RecordPaymentWindow(vm) { Owner = Application.Current.MainWindow };
            window.ShowDialog();
        }

        [RelayCommand]
        private async Task VoidPayment(Payment payment)
        {
            if (payment.Status == PaymentTransactionStatus.Voided) return;

            var confirm = MessageBox.Show(
                $"Void payment {payment.PaymentNumber} of Rs {payment.Amount:N0} for {payment.CustomerName}?\n\n" +
                "The payment stays in your records marked as Voided -- it's excluded from collection totals " +
                "and the installment balance is restored. This cannot be undone.",
                "Confirm void", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            var reason = Interaction.InputBox("Reason for voiding this payment (optional):", "Void Payment", "");
            await _paymentService.VoidAsync(payment.Id, reason);
        }

        private async void OnDataChanged(object? sender, EventArgs e) => await LoadAsync();

        private async Task LoadAsync()
        {
            IsLoading = true;
            _allPayments = await _paymentService.GetAllAsync();
            var installments = await _installmentService.GetAllAsync();
            IsLoading = false;

            var validPayments = _allPayments.Where(p => p.Status == PaymentTransactionStatus.Paid).ToList();
            var today = DateTime.Today;

            TodaysCollections = $"Rs {validPayments.Where(p => p.PaymentDate.Date == today).Sum(p => p.Amount):N0}";
            ThisMonthCollections = $"Rs {validPayments.Where(p => p.PaymentDate.Year == today.Year && p.PaymentDate.Month == today.Month).Sum(p => p.Amount):N0}";
            TotalCollected = $"Rs {validPayments.Sum(p => p.Amount):N0}";

            // Pending/Overdue describe outstanding installment balances, not
            // payment transactions -- deliberately sourced from Installments,
            // per the "a pending installment is not a payment" rule.
            var activeInstallments = installments.Where(i => !i.IsCancelled).ToList();
            PendingPayments = $"Rs {activeInstallments.Sum(i => i.OutstandingAmount):N0}";
            OverduePayments = $"Rs {activeInstallments.Where(i => i.Status == InstallmentStatus.Overdue).Sum(i => i.OutstandingAmount):N0}";

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            IEnumerable<Payment> query = _allPayments;

            query = SelectedFilter switch
            {
                "Paid" => query.Where(p => p.Status == PaymentTransactionStatus.Paid),
                "Voided" => query.Where(p => p.Status == PaymentTransactionStatus.Voided),
                _ => query
            };

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.Trim();
                query = query.Where(p =>
                    p.CustomerName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    p.PaymentNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    p.InstallmentNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (p.ReferenceNumber ?? "").Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            FilteredPayments.Clear();
            foreach (var payment in query.OrderByDescending(p => p.PaymentDate))
                FilteredPayments.Add(payment);

            IsEmpty = FilteredPayments.Count == 0;
        }

        public void Dispose()
        {
            _paymentService.PaymentsChanged -= OnDataChanged;
            _installmentService.InstallmentsChanged -= OnDataChanged;
        }
    }
}  