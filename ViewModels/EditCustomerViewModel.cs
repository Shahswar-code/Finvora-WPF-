using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finvora.Models;
using Finvora.Services;
using System;
using System.Threading.Tasks;

namespace Finvora.ViewModels
{
    /// <summary>
    /// Backs the Edit Customer popup. Shows a read-only payment snapshot of the
    /// customer clicked, plus two actions: mark the whole plan paid in one tap,
    /// or record a partial payment amount. Mirrors AddCustomerViewModel's
    /// open-as-dialog / RequestClose pattern.
    /// </summary>
    public partial class EditCustomerViewModel : ObservableObject
    {
        private readonly CustomerService _customerService;
        private readonly int _customerId;

        /// <summary>Raised when the dialog should close -- any successful action or Cancel.</summary>
        public event Action? RequestClose;

        // ---------- Read-only snapshot for display ----------
        [ObservableProperty] private string fullName = string.Empty;
        [ObservableProperty] private string itemName = string.Empty;
        [ObservableProperty] private decimal totalPrice;
        [ObservableProperty] private decimal amountPaid;
        [ObservableProperty] private decimal remainingBalance;
        [ObservableProperty] private PaymentStatus status;

        /// <summary>True once fully paid -- XAML shows the "already paid" panel when this is true.</summary>
        public bool IsFullyPaid => Status == PaymentStatus.Paid;

        /// <summary>Inverse of IsFullyPaid -- XAML shows the two payment cards when this is true.</summary>
        public bool NeedsPayment => !IsFullyPaid;

        // ---------- Partial payment entry ----------
        [ObservableProperty] private string partialAmountText = string.Empty;

        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isSaving;

        public EditCustomerViewModel(CustomerService customerService, Customer customer)
        {
            _customerService = customerService;
            _customerId = customer.Id;

            FullName = customer.FullName;
            ItemName = customer.ItemName;
            TotalPrice = customer.TotalPrice;
            AmountPaid = customer.AmountPaid;
            RemainingBalance = customer.RemainingBalance;
            Status = customer.Status;
        }

        [RelayCommand]
        private async Task MarkFullyPaid()
        {
            ErrorMessage = string.Empty;
            IsSaving = true;
            try
            {
                await _customerService.MarkFullyPaidAsync(_customerId);
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Couldn't update: {ex.Message}";
            }
            finally
            {
                IsSaving = false;
            }
        }

        [RelayCommand]
        private async Task RecordPartialPayment()
        {
            ErrorMessage = string.Empty;

            if (!decimal.TryParse(PartialAmountText, out var amount) || amount <= 0)
            {
                ErrorMessage = "Enter a valid payment amount.";
                return;
            }

            if (amount > RemainingBalance)
            {
                ErrorMessage = $"Amount can't exceed the remaining balance (Rs {RemainingBalance:N0}).";
                return;
            }

            IsSaving = true;
            try
            {
                await _customerService.RecordPaymentAsync(_customerId, amount);
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Couldn't save payment: {ex.Message}";
            }
            finally
            {
                IsSaving = false;
            }
        }

        [RelayCommand]
        private void Cancel() => RequestClose?.Invoke();

        partial void OnStatusChanged(PaymentStatus value)
        {
            OnPropertyChanged(nameof(IsFullyPaid));
            OnPropertyChanged(nameof(NeedsPayment));
        }
    }
}  