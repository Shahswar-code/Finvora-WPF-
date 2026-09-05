using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finvora.Models;
using Finvora.Services;
using Microsoft.VisualBasic;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Finvora.ViewModels
{
    /// <summary>
    /// Backs the Add New Customer modal. Customer + their first installment plan
    /// are captured together in one form. Picking a stock item is optional --
    /// it just pre-fills ItemName/TotalPrice from inventory; typing a plan
    /// description by hand (the original behavior) still works unchanged.
    /// Quantity defaults to 1 and is capped at the item's available stock;
    /// Total Price re-prices as DealerPrice x Quantity, and that many units
    /// are deducted from stock on successful save.
    /// </summary>
    public partial class AddCustomerViewModel : ObservableObject
    {
        private readonly CustomerService _customerService;
        private readonly NotificationService _notificationService;
        private readonly StockService _stockService;

        /// <summary>Raised when the dialog should close -- Save (success) or Cancel.</summary>
        public event Action? RequestClose;

        // ---------- Section 1: Customer info ----------
        [ObservableProperty] private string fullName = string.Empty;
        [ObservableProperty] private string phone = string.Empty;
        [ObservableProperty] private string email = string.Empty;
        [ObservableProperty] private string cnic = string.Empty;
        [ObservableProperty] private string address = string.Empty;

        // ---------- Section 2: Plan info ----------
        [ObservableProperty] private StockItem? selectedStockItem;
        [ObservableProperty] private string selectedQuantityText = "1";
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

        /// <summary>In-stock items only -- out-of-stock items are filtered out
        /// entirely rather than shown disabled, per the simpler of the two
        /// options for preventing an out-of-stock sale.</summary>
        public ObservableCollection<StockItem> StockItems { get; } = new();

        /// <summary>Live "Remaining" preview shown in the form -- Total minus Advance, never negative.</summary>
        public decimal RemainingPreview =>
            Math.Max(0, ParseDecimal(TotalPriceText) - ParseDecimal(AdvancePaidText));

        partial void OnTotalPriceTextChanged(string value) => OnPropertyChanged(nameof(RemainingPreview));
        partial void OnAdvancePaidTextChanged(string value) => OnPropertyChanged(nameof(RemainingPreview));

        /// <summary>Picking a stock item pre-fills the name and a starting
        /// price -- both stay editable afterward, this is just a shortcut.
        /// Quantity resets to 1 and Total Price is re-priced for that quantity.</summary>
        partial void OnSelectedStockItemChanged(StockItem? value)
        {
            if (value is null) return;
            ItemName = value.ItemName;
            SelectedQuantityText = "1";
            TotalPriceText = value.DealerPrice.ToString("0.##");
        }

        /// <summary>Keeps Quantity within [1, available stock] and re-prices
        /// Total Price as DealerPrice x Quantity whenever a stock item is selected.
        /// Manual (non-stock) plans are untouched -- this only fires meaningfully
        /// once SelectedStockItem is set.</summary>
        partial void OnSelectedQuantityTextChanged(string value)
        {
            if (SelectedStockItem is null) return;

            if (!int.TryParse(value, out var qty) || qty < 1)
            {
                qty = 1;
            }
            else if (qty > SelectedStockItem.Quantity)
            {
                qty = SelectedStockItem.Quantity;
            }

            var clamped = qty.ToString();
            if (SelectedQuantityText != clamped)
            {
                SelectedQuantityText = clamped; // triggers this handler again with the clamped value
                return;
            }

            TotalPriceText = (SelectedStockItem.DealerPrice * qty).ToString("0.##");
        }

        public AddCustomerViewModel(CustomerService customerService, NotificationService notificationService, StockService stockService)
        {
            _customerService = customerService;
            _notificationService = notificationService;
            _stockService = stockService;

            _ = LoadStockItemsAsync();
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

            // Re-check stock is still available right before saving -- basic
            // guard in case someone else sold the last unit in the meantime.
            var selectedQuantity = 1;
            if (SelectedStockItem is not null)
            {
                if (!int.TryParse(SelectedQuantityText, out selectedQuantity) || selectedQuantity < 1)
                {
                    ErrorMessage = "Enter a valid quantity.";
                    return;
                }

                var freshItem = await _stockService.GetByIdAsync(SelectedStockItem.Id);
                if (freshItem is null || freshItem.Quantity < selectedQuantity)
                {
                    ErrorMessage = freshItem is null
                        ? "Insufficient stock. This item is no longer available."
                        : $"Insufficient stock. Only {freshItem.Quantity} units are available.";
                    return;
                }
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
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Couldn't save: {ex.Message}";
                IsSaving = false;
                return;
            }

            // The customer is already saved at this point -- neither a
            // notification hiccup nor a stock hiccup from here on should
            // surface as "couldn't save" or stop the dialog from closing.
            try
            {
                await _notificationService.NotifyCustomerAddedAsync(customer);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AddCustomerViewModel] Notification failed after successful save: {ex.Message}");
            }

            if (SelectedStockItem is not null)
            {
                try
                {
                    await _stockService.DeductStockAsync(
                        SelectedStockItem.Id, selectedQuantity, "Customer", customer.Id, $"Sold to {customer.FullName}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AddCustomerViewModel] Stock deduction failed after successful save: {ex.Message}");
                }
            }

            IsSaving = false;
            RequestClose?.Invoke();
        }

        [RelayCommand]
        private void Cancel() => RequestClose?.Invoke();

        private async Task LoadStockItemsAsync()
        {
            var items = await _stockService.GetAllAsync();
            StockItems.Clear();
            foreach (var item in items.Where(i => i.Quantity > 0).OrderBy(i => i.ItemName))
                StockItems.Add(item);
        }

        private static decimal ParseDecimal(string s) => decimal.TryParse(s, out var v) ? v : 0;
    }
} 