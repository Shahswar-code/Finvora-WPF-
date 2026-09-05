using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finvora.Models;
using Finvora.Services;

namespace Finvora.ViewModels
{
    /// <summary>
    /// Backs both Add New Stock and Edit Stock -- EditingItemId is null for
    /// Add, set for Edit. Same form, same validation, same save path either
    /// way; only the header text and which StockService call runs differ.
    /// </summary>
    public partial class StockFormViewModel : ObservableObject
    {
        private readonly StockService _stockService;
        private readonly int? _editingItemId;

        public event Action? RequestClose;

        public bool IsEditMode => _editingItemId.HasValue;
        public string HeaderTitle => IsEditMode ? "Edit Stock Item" : "Add New Stock";
        public string HeaderSubtitle => IsEditMode
            ? "Update inventory information for this item."
            : "Add a new inventory item to Finvora.";
        public string SaveButtonText => IsEditMode ? "Save Changes" : "Save Stock";

        // ---------- Distributor / importer ----------
        [ObservableProperty] private string distributorName = string.Empty;
        [ObservableProperty] private string distributorPhone = string.Empty;
        [ObservableProperty] private string distributorEmail = string.Empty;
        [ObservableProperty] private string distributorAddress = string.Empty;
        [ObservableProperty] private string importerReference = string.Empty;

        // ---------- Stock item details ----------
        [ObservableProperty] private string itemName = string.Empty;
        [ObservableProperty] private string referenceNumber = string.Empty;
        [ObservableProperty] private string category = string.Empty;
        [ObservableProperty] private StockCondition condition = StockCondition.New;
        [ObservableProperty] private string quantityText = "0";
        [ObservableProperty] private string minimumStockText = "0";
        [ObservableProperty] private string dealerPriceText = string.Empty;
        [ObservableProperty] private string endUserPriceText = string.Empty;
        [ObservableProperty] private string description = string.Empty;

        [ObservableProperty] private string errorMessage = string.Empty;
        [ObservableProperty] private bool isSaving;

        public ObservableCollection<StockCondition> ConditionOptions { get; } = new(Enum.GetValues<StockCondition>());

        /// <summary>A curated starting list -- picking one is optional, typing
        /// a new category is fine too since Category is just a string on the
        /// model (see StockItem.cs for why it's not a separate table).</summary>
        public ObservableCollection<string> SuggestedCategories { get; } = new()
        {
            "Auto Parts", "Electronics", "Accessories", "Machinery", "Cars", "Other"
        };

        /// <summary>Live-calculated, read-only in the form -- Quantity x Dealer Price.</summary>
        public decimal StockValuePreview => Math.Max(0, ParseInt(QuantityText)) * ParseDecimal(DealerPriceText);

        partial void OnQuantityTextChanged(string value) => OnPropertyChanged(nameof(StockValuePreview));
        partial void OnDealerPriceTextChanged(string value) => OnPropertyChanged(nameof(StockValuePreview));

        /// <summary>Add New Stock constructor.</summary>
        public StockFormViewModel(StockService stockService)
        {
            _stockService = stockService;
            _editingItemId = null;
        }

        /// <summary>Edit Stock constructor -- pre-fills every field from the
        /// item passed in, so only this exact row is ever touched.</summary>
        public StockFormViewModel(StockService stockService, StockItem itemToEdit)
        {
            _stockService = stockService;
            _editingItemId = itemToEdit.Id;

            DistributorName = itemToEdit.DistributorName;
            DistributorPhone = itemToEdit.DistributorPhone ?? string.Empty;
            DistributorEmail = itemToEdit.DistributorEmail ?? string.Empty;
            DistributorAddress = itemToEdit.DistributorAddress ?? string.Empty;
            ImporterReference = itemToEdit.ImporterReference ?? string.Empty;

            ItemName = itemToEdit.ItemName;
            ReferenceNumber = itemToEdit.ReferenceNumber;
            Category = itemToEdit.Category ?? string.Empty;
            Condition = itemToEdit.Condition;
            QuantityText = itemToEdit.Quantity.ToString();
            MinimumStockText = itemToEdit.MinimumStock.ToString();
            DealerPriceText = itemToEdit.DealerPrice.ToString("0.##");
            EndUserPriceText = itemToEdit.EndUserPrice.ToString("0.##");
            Description = itemToEdit.Description ?? string.Empty;
        }

        [RelayCommand]
        private async Task Save()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(DistributorName)) { ErrorMessage = "Distributor / importer name is required."; return; }
            if (string.IsNullOrWhiteSpace(ItemName)) { ErrorMessage = "Item name is required."; return; }
            if (string.IsNullOrWhiteSpace(ReferenceNumber)) { ErrorMessage = "Reference # is required."; return; }

            if (!int.TryParse(QuantityText, out var quantity) || quantity < 0)
            {
                ErrorMessage = "Quantity cannot be negative.";
                return;
            }

            if (!int.TryParse(MinimumStockText, out var minimumStock) || minimumStock < 0)
            {
                minimumStock = 0;
            }

            if (!decimal.TryParse(DealerPriceText, out var dealerPrice) || dealerPrice < 0)
            {
                ErrorMessage = "Dealer Price is required.";
                return;
            }

            if (!decimal.TryParse(EndUserPriceText, out var endUserPrice) || endUserPrice < 0)
            {
                ErrorMessage = "End User Price is required.";
                return;
            }

            var item = new StockItem
            {
                Id = _editingItemId ?? 0,
                DistributorName = DistributorName.Trim(),
                DistributorPhone = string.IsNullOrWhiteSpace(DistributorPhone) ? null : DistributorPhone.Trim(),
                DistributorEmail = string.IsNullOrWhiteSpace(DistributorEmail) ? null : DistributorEmail.Trim(),
                DistributorAddress = string.IsNullOrWhiteSpace(DistributorAddress) ? null : DistributorAddress.Trim(),
                ImporterReference = string.IsNullOrWhiteSpace(ImporterReference) ? null : ImporterReference.Trim(),
                ItemName = ItemName.Trim(),
                ReferenceNumber = ReferenceNumber.Trim(),
                Category = string.IsNullOrWhiteSpace(Category) ? null : Category.Trim(),
                Condition = Condition,
                Quantity = quantity,
                MinimumStock = minimumStock,
                DealerPrice = dealerPrice,
                EndUserPrice = endUserPrice,
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim()
            };

            IsSaving = true;
            try
            {
                if (IsEditMode)
                    await _stockService.UpdateAsync(item);
                else
                    await _stockService.CreateAsync(item);

                RequestClose?.Invoke();
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception)
            {
                ErrorMessage = "Something went wrong while saving this item. Please try again.";
            }
            finally
            {
                IsSaving = false;
            }
        }

        [RelayCommand]
        private void Cancel() => RequestClose?.Invoke();

        private static int ParseInt(string s) => int.TryParse(s, out var v) ? v : 0;
        private static decimal ParseDecimal(string s) => decimal.TryParse(s, out var v) ? v : 0;
    }
} 