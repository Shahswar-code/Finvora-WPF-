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

namespace Finvora.ViewModels
{
    public partial class StockViewModel : ObservableObject, IDisposable
    {
        private readonly StockService _stockService;
        private List<StockItem> _allItems = new();

        [ObservableProperty] private string searchText = "";
        [ObservableProperty] private string selectedTab = "All";
        [ObservableProperty] private string selectedCategoryFilter = "All Categories";
        [ObservableProperty] private string selectedConditionFilter = "All Conditions";

        public ObservableCollection<StockItem> FilteredItems { get; } = new();
        public ObservableCollection<string> CategoryFilterOptions { get; } = new();
        public ObservableCollection<string> ConditionFilterOptions { get; } = new();

        [ObservableProperty] private bool isLoading;
        [ObservableProperty] private bool isEmpty;

        // ----- KPI cards -----
        [ObservableProperty] private string totalItemsValue = "0";
        [ObservableProperty] private string lowStockValue = "0";
        [ObservableProperty] private string outOfStockValue = "0";
        [ObservableProperty] private string totalStockValue = "Rs 0";

        // ----- Tab labels with live counts -----
        [ObservableProperty] private string allTabLabel = "All Items (0)";
        [ObservableProperty] private string lowTabLabel = "Low Stock (0)";
        [ObservableProperty] private string outTabLabel = "Out of Stock (0)";

        public StockViewModel(StockService stockService)
        {
            _stockService = stockService;
            _stockService.StockChanged += OnStockChanged;

            ConditionFilterOptions.Add("All Conditions");
            foreach (var c in Enum.GetValues<StockCondition>())
                ConditionFilterOptions.Add(c.ToString());

            _ = LoadAsync();
        }

        partial void OnSearchTextChanged(string value) => ApplyFilter();
        partial void OnSelectedTabChanged(string value) => ApplyFilter();
        partial void OnSelectedCategoryFilterChanged(string value) => ApplyFilter();
        partial void OnSelectedConditionFilterChanged(string value) => ApplyFilter();

        [RelayCommand]
        private void SetTab(string tab) => SelectedTab = tab;

        [RelayCommand]
        private async Task Refresh() => await LoadAsync();

        [RelayCommand]
        private void AddNewStock()
        {
            // Stage 3 replaces this with the real Add Stock dialog.
            MessageBox.Show("The Add New Stock form is coming in the next step.", "Coming soon",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void EditStock(StockItem item)
        {
            // Stage 3 replaces this with the real Edit Stock dialog,
            // pre-loaded with this exact item's data.
            MessageBox.Show($"Edit form for {item.ItemName} is coming in the next step.", "Coming soon",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private async Task ArchiveStock(StockItem item)
        {
            var result = MessageBox.Show(
                $"Remove {item.ItemName} from active inventory?\n\n" +
                "If it has any stock history, it will be archived (kept for your records) rather than deleted.",
                "Confirm removal", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            await _stockService.RemoveAsync(item.Id);
        }

        private async void OnStockChanged(object? sender, EventArgs e) => await LoadAsync();

        private async Task LoadAsync()
        {
            IsLoading = true;
            _allItems = await _stockService.GetAllAsync();
            IsLoading = false;

            var previousCategory = SelectedCategoryFilter;
            CategoryFilterOptions.Clear();
            CategoryFilterOptions.Add("All Categories");
            foreach (var c in _allItems.Where(i => !string.IsNullOrWhiteSpace(i.Category))
                                        .Select(i => i.Category!).Distinct().OrderBy(c => c))
                CategoryFilterOptions.Add(c);
            SelectedCategoryFilter = CategoryFilterOptions.Contains(previousCategory) ? previousCategory : "All Categories";

            var lowCount = _allItems.Count(i => i.Status == StockItemStatus.LowStock);
            var outCount = _allItems.Count(i => i.Status == StockItemStatus.OutOfStock);

            TotalItemsValue = _allItems.Count.ToString();
            LowStockValue = lowCount.ToString();
            OutOfStockValue = outCount.ToString();
            TotalStockValue = $"Rs {_allItems.Sum(i => i.StockValue):N0}";

            AllTabLabel = $"All Items ({_allItems.Count})";
            LowTabLabel = $"Low Stock ({lowCount})";
            OutTabLabel = $"Out of Stock ({outCount})";

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            IEnumerable<StockItem> query = _allItems;

            query = SelectedTab switch
            {
                "Low Stock" => query.Where(i => i.Status == StockItemStatus.LowStock),
                "Out of Stock" => query.Where(i => i.Status == StockItemStatus.OutOfStock),
                _ => query
            };

            if (SelectedCategoryFilter != "All Categories")
                query = query.Where(i => i.Category == SelectedCategoryFilter);

            if (SelectedConditionFilter != "All Conditions")
                query = query.Where(i => i.Condition.ToString() == SelectedConditionFilter);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.Trim();
                query = query.Where(i =>
                    i.ItemName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    i.ReferenceNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    i.DistributorName.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            FilteredItems.Clear();
            foreach (var item in query.OrderBy(i => i.ItemName))
                FilteredItems.Add(item);

            IsEmpty = FilteredItems.Count == 0;
        }

        public void Dispose()
        {
            _stockService.StockChanged -= OnStockChanged;
        }
    }
} 