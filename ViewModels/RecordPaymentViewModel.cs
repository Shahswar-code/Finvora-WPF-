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
    public partial class RecordPaymentViewModel : ObservableObject
    {
        private readonly PaymentService _paymentService;
        private readonly InstallmentService _installmentService;
        private readonly CustomerService _customerService;

        public event Action? RequestClose;

        public ObservableCollection<Customer> Customers { get; } = new();
        public ObservableCollection<Installment> CustomerInstallments { get; } = new();
        public ObservableCollection<InstallmentSchedule> OutstandingRows { get; } = new();
        public ObservableCollection<PaymentMethod> MethodOptions { get; } = new(Enum.GetValues<PaymentMethod>());

        [ObservableProperty] private Customer? selectedCustomer;
        [ObservableProperty] private Installment? selectedInstallment;
        [ObservableProperty] private InstallmentSchedule? selectedScheduleRow;

        [ObservableProperty] private string amountText = string.Empty;
        [ObservableProperty] private PaymentMethod method = PaymentMethod.Cash;
        [ObservableProperty] private string referenceNumber = string.Empty;
        [ObservableProperty] private string notes = string.Empty;
        [ObservableProperty] private DateTime paymentDate = DateTime.Now;

        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isSaving;
        [ObservableProperty] private bool isLoadingInstallments;

        // Plain bool flags for the UI -- BooleanToVisibilityConverter only
        // understands true/false, never an object, so every "show this section
        // once something is picked" binding needs one of these behind it.
        [ObservableProperty] private bool hasSelectedCustomer;
        [ObservableProperty] private bool hasSelectedInstallment;
        [ObservableProperty] private bool hasInstallments;
        [ObservableProperty] private bool hasNoInstallments;

        public bool HasSelectedRow => SelectedScheduleRow is not null;

        public RecordPaymentViewModel(PaymentService paymentService, InstallmentService installmentService, CustomerService customerService)
        {
            _paymentService = paymentService;
            _installmentService = installmentService;
            _customerService = customerService;

            _ = LoadCustomersAsync();
        }

        partial void OnSelectedCustomerChanged(Customer? value)
        {
            SelectedInstallment = null;
            CustomerInstallments.Clear();
            OutstandingRows.Clear();
            SelectedScheduleRow = null;
            ErrorMessage = string.Empty;
            HasInstallments = false;
            HasNoInstallments = false;
            HasSelectedCustomer = value is not null;

            if (value is not null) _ = LoadInstallmentsAsync(value.Id);
        }

        partial void OnSelectedInstallmentChanged(Installment? value)
        {
            OutstandingRows.Clear();
            SelectedScheduleRow = null;
            ErrorMessage = string.Empty;
            HasSelectedInstallment = value is not null;
            if (value is null) return;

            foreach (var row in value.Schedule.Where(r => r.RemainingAmount > 0).OrderBy(r => r.SequenceNumber))
                OutstandingRows.Add(row);
        }

        partial void OnSelectedScheduleRowChanged(InstallmentSchedule? value)
        {
            OnPropertyChanged(nameof(HasSelectedRow));
            AmountText = value is null ? string.Empty : value.RemainingAmount.ToString("0.##");
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        private async Task Save()
        {
            ErrorMessage = string.Empty;

            if (SelectedCustomer is null) { ErrorMessage = "Please select a customer."; return; }
            if (SelectedInstallment is null) { ErrorMessage = "Please select an installment plan."; return; }
            if (SelectedScheduleRow is null) { ErrorMessage = "Please select an installment to pay."; return; }

            if (!decimal.TryParse(AmountText, out var amount) || amount <= 0)
            {
                ErrorMessage = "Payment amount must be greater than zero.";
                return;
            }

            if (amount > SelectedScheduleRow.RemainingAmount)
            {
                ErrorMessage = $"Payment amount cannot exceed the remaining installment balance (Rs {SelectedScheduleRow.RemainingAmount:N0}).";
                return;
            }

            var payment = new Payment
            {
                CustomerId = SelectedCustomer.Id,
                InstallmentId = SelectedInstallment.Id,
                InstallmentScheduleId = SelectedScheduleRow.Id,
                Amount = amount,
                Method = Method,
                ReferenceNumber = string.IsNullOrWhiteSpace(ReferenceNumber) ? null : ReferenceNumber.Trim(),
                Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                PaymentDate = PaymentDate
            };

            IsSaving = true;
            try
            {
                await _paymentService.CreateAsync(payment);
                RequestClose?.Invoke();
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception)
            {
                ErrorMessage = "Something went wrong while recording the payment. Please try again.";
            }
            finally
            {
                IsSaving = false;
            }
        }

        [RelayCommand]
        private void Cancel() => RequestClose?.Invoke();

        private async Task LoadCustomersAsync()
        {
            var customers = await _customerService.GetAllAsync();
            Customers.Clear();
            foreach (var c in customers.OrderBy(c => c.FullName)) Customers.Add(c);
        }

        private async Task LoadInstallmentsAsync(int customerId)
        {
            IsLoadingInstallments = true;
            var installments = await _installmentService.GetByCustomerIdAsync(customerId);
            IsLoadingInstallments = false;

            CustomerInstallments.Clear();
            foreach (var i in installments.Where(i => !i.IsCancelled && i.OutstandingAmount > 0))
                CustomerInstallments.Add(i);

            HasInstallments = CustomerInstallments.Count > 0;
            HasNoInstallments = CustomerInstallments.Count == 0;

            if (CustomerInstallments.Count == 1)
                SelectedInstallment = CustomerInstallments[0];
        }
    }
} 