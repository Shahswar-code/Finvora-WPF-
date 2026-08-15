using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finvora.Models;
using Finvora.Services;
using Microsoft.VisualBasic;
using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading.Tasks;

namespace Finvora.ViewModels
{
    /// <summary>
    /// Backs the Add New Customer modal. Customer + their first installment plan
    /// are captured together in one form, per the confirmed scope. All numeric
    /// fields are bound as text (not decimal) so the TextBox can hold an empty or
    /// in-progress value like "12." without fighting the binding -- parsing and
    /// validation happen once, on Save.
    /// </summary>
    public partial class AddCustomerViewModel : ObservableObject
    {
        private readonly CustomerService _customerService;

        /// <summary>Raised when the dialog should close -- Save (success) or Cancel.</summary>
        public event Action? RequestClose;

        // ---------- Section 1: Customer info ----------
        [ObservableProperty] private string fullName = string.Empty;
        [ObservableProperty] private string phone = string.Empty;
        [ObservableProperty] private string email = string.Empty;
        [ObservableProperty] private string cnic = string.Empty;
        [ObservableProperty] private string address = string.Empty;

        // ---------- Section 2: Plan info ----------
        [ObservableProperty] private string itemName = string.Empty;
        [ObservableProperty] private string totalPriceText = string.Empty;
        [ObservableProperty] private string advancePaidText = "0";
        [ObservableProperty] private string installmentAmountText = string.Empty;
        [ObservableProperty] private PlanFrequency frequency = PlanFrequency.Monthly;
        [ObservableProperty] private DateTime dueDate = DateTime.Today.AddMonths(1);

        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isSaving;

        public ObservableCollection<PlanFrequency> FrequencyOptions { get; } =
            new(Enum.GetValues<PlanFrequency>());

        /// <summary>Live "Remaining" preview shown in the form -- Total minus Advance, never negative.</summary>
        public decimal RemainingPreview =>
            Math.Max(0, ParseDecimal(TotalPriceText) - ParseDecimal(AdvancePaidText));

        partial void OnTotalPriceTextChanged(string value) => OnPropertyChanged(nameof(RemainingPreview));
        partial void OnAdvancePaidTextChanged(string value) => OnPropertyChanged(nameof(RemainingPreview));

        public AddCustomerViewModel(CustomerService customerService)
        {
            _customerService = customerService;
        }

        [RelayCommand]
        private async Task Save()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Phone))
            {
                ErrorMessage = "Full name and phone are required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(ItemName))
            {
                ErrorMessage = "Item / plan description is required.";
                return;
            }

            if (!decimal.TryParse(TotalPriceText, out var totalPrice) || totalPrice <= 0)
            {
                ErrorMessage = "Enter a valid total price.";
                return;
            }

            if (!decimal.TryParse(AdvancePaidText, out var advancePaid) || advancePaid < 0)
            {
                advancePaid = 0;
            }

            if (advancePaid > totalPrice)
            {
                ErrorMessage = "Paid amount can't be more than the total price.";
                return;
            }

            if (!decimal.TryParse(InstallmentAmountText, out var installmentAmount) || installmentAmount <= 0)
            {
                ErrorMessage = "Enter a valid installment amount.";
                return;
            }

            var customer = new Customer
            {
                FullName = FullName.Trim(),
                Phone = Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                Cnic = string.IsNullOrWhiteSpace(Cnic) ? null : Cnic.Trim(),
                Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
                ItemName = ItemName.Trim(),
                TotalPrice = totalPrice,
                AdvancePaid = advancePaid,
                AmountPaid = advancePaid,
                InstallmentAmount = installmentAmount,
                Frequency = Frequency,
                DateAdded = DateTime.Now,
                DueDate = DueDate
            };

            IsSaving = true;
            try
            {
                await _customerService.AddAsync(customer);
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Couldn't save: {ex.Message}";
            }
            finally
            {
                IsSaving = false;
            }
        }

        [RelayCommand]
        private void Cancel() => RequestClose?.Invoke();

        private static decimal ParseDecimal(string s) => decimal.TryParse(s, out var v) ? v : 0;
    }
} 