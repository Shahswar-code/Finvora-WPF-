using System;
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
    /// <summary>
    /// Backs the "New Installment" dialog. Step 1 (pick/add customer) and Step 3
    /// (payment plan) from the spec, combined into one screen for Phase 1 --
    /// Step 2 (separate product/SKU/discount fields) is folded into a single
    /// ItemName for now, matching how the Customer module already works.
    /// </summary>
    public partial class CreateInstallmentViewModel : ObservableObject
    {
        private readonly InstallmentService _installmentService;
        private readonly CustomerService _customerService;
        private bool _isSyncingPlanFields;

        public event Action? RequestClose;

        public ObservableCollection<Customer> Customers { get; } = new();

        [ObservableProperty] private Customer? selectedCustomer;

        [ObservableProperty] private string itemName = string.Empty;
        [ObservableProperty] private string totalPriceText = string.Empty;
        [ObservableProperty] private string downPaymentText = "0";
        [ObservableProperty] private string installmentAmountText = string.Empty;
        [ObservableProperty] private string numberOfInstallmentsText = string.Empty;
        [ObservableProperty] private PlanFrequency frequency = PlanFrequency.Monthly;
        [ObservableProperty] private DateTime firstDueDate = DateTime.Today.AddMonths(1);

        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isSaving;

        public ObservableCollection<PlanFrequency> FrequencyOptions { get; } =
            new(Enum.GetValues<PlanFrequency>());

        /// <summary>Live "Financed Amount" preview -- Total minus Down Payment, never negative.</summary>
        public decimal FinancedPreview =>
            Math.Max(0, ParseDecimal(TotalPriceText) - ParseDecimal(DownPaymentText));

        public CreateInstallmentViewModel(InstallmentService installmentService, CustomerService customerService)
        {
            _installmentService = installmentService;
            _customerService = customerService;

            _ = LoadCustomersAsync();
        }

        partial void OnTotalPriceTextChanged(string value) => OnPropertyChanged(nameof(FinancedPreview));
        partial void OnDownPaymentTextChanged(string value) => OnPropertyChanged(nameof(FinancedPreview));

        // Whichever field the user edits last drives the other -- guarded so
        // setting one from the other doesn't bounce back and forth forever.
        partial void OnInstallmentAmountTextChanged(string value)
        {
            if (_isSyncingPlanFields) return;
            if (!decimal.TryParse(value, out var amount) || amount <= 0) return;

            _isSyncingPlanFields = true;
            NumberOfInstallmentsText = InstallmentCalculationService
                .CountFromAmount(FinancedPreview, amount).ToString();
            _isSyncingPlanFields = false;
        }

        partial void OnNumberOfInstallmentsTextChanged(string value)
        {
            if (_isSyncingPlanFields) return;
            if (!int.TryParse(value, out var count) || count <= 0) return;

            _isSyncingPlanFields = true;
            InstallmentAmountText = InstallmentCalculationService
                .AmountFromCount(FinancedPreview, count).ToString("0.##");
            _isSyncingPlanFields = false;
        }

        [RelayCommand]
        private void AddNewCustomer()
        {
            var vm = new AddCustomerViewModel(_customerService);
            var window = new AddCustomerWindow(vm)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();

            _ = LoadCustomersAsync(selectMostRecentlyAdded: true);
        }

        [RelayCommand]
        private async Task Save()
        {
            ErrorMessage = string.Empty;

            if (SelectedCustomer is null)
            {
                ErrorMessage = "Select a customer for this installment.";
                return;
            }

            if (string.IsNullOrWhiteSpace(ItemName))
            {
                ErrorMessage = "Item / product description is required.";
                return;
            }

            if (!decimal.TryParse(TotalPriceText, out var totalPrice) || totalPrice <= 0)
            {
                ErrorMessage = "Enter a valid total price.";
                return;
            }

            if (!decimal.TryParse(DownPaymentText, out var downPayment) || downPayment < 0)
            {
                downPayment = 0;
            }

            if (downPayment > totalPrice)
            {
                ErrorMessage = "Down payment can't be more than the total price.";
                return;
            }

            if (!decimal.TryParse(InstallmentAmountText, out var installmentAmount) || installmentAmount <= 0)
            {
                ErrorMessage = "Enter a valid installment amount.";
                return;
            }

            if (!int.TryParse(NumberOfInstallmentsText, out var count) || count <= 0)
            {
                ErrorMessage = "Enter a valid number of installments.";
                return;
            }

            var financed = Math.Max(0, totalPrice - downPayment);

            var installment = new Installment
            {
                CustomerId = SelectedCustomer.Id,
                ItemName = ItemName.Trim(),
                TotalPrice = totalPrice,
                DownPayment = downPayment,
                InstallmentAmount = installmentAmount,
                Frequency = Frequency,
                NumberOfInstallments = count,
                StartDate = DateTime.Today,
                FirstDueDate = FirstDueDate,
                DateAdded = DateTime.Now,
                Schedule = InstallmentCalculationService.GenerateSchedule(
                    financed, count, installmentAmount, FirstDueDate, Frequency)
            };

            IsSaving = true;
            try
            {
                await _installmentService.CreateAsync(installment);
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

        private async Task LoadCustomersAsync(bool selectMostRecentlyAdded = false)
        {
            var customers = await _customerService.GetAllAsync();

            var previouslySelectedId = SelectedCustomer?.Id;

            Customers.Clear();
            foreach (var customer in customers.OrderBy(c => c.FullName))
            {
                Customers.Add(customer);
            }

            if (selectMostRecentlyAdded && customers.Count > 0)
            {
                SelectedCustomer = Customers
                    .OrderByDescending(c => c.DateAdded)
                    .FirstOrDefault();
            }
            else if (previouslySelectedId.HasValue)
            {
                SelectedCustomer = Customers.FirstOrDefault(c => c.Id == previouslySelectedId.Value);
            }
        }

        private static decimal ParseDecimal(string s) => decimal.TryParse(s, out var v) ? v : 0;
    }
} 