using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finvora.Models;
using Finvora.Services;
using Finvora.Views;

namespace Finvora.ViewModels
{
    public partial class InstallmentsViewModel : ObservableObject, IDisposable
    {
        private readonly InstallmentService _installmentService;
        private readonly CustomerService _customerService;
        private List<Installment> _allInstallments = new();

        // Same day-rollover safety net as CustomersViewModel -- Installment is a
        // plain model whose Status/DaysOverdue getters compare against
        // DateTime.Today, so nothing re-evaluates them until this VM re-runs.
        private DateTime _lastCheckedDate = DateTime.Today;
        private readonly DispatcherTimer _dueDateTimer;

        [ObservableProperty] private string searchText = "";
        [ObservableProperty] private string selectedFilter = "All";

        public ObservableCollection<Installment> FilteredInstallments { get; } = new();

        [ObservableProperty] private bool isLoading;
        [ObservableProperty] private bool isEmpty;

        // ----- Stat cards -----
        [ObservableProperty] private int totalInstallments;
        [ObservableProperty] private int activeCount;
        [ObservableProperty] private int dueTodayCount;
        [ObservableProperty] private int overdueCount;
        [ObservableProperty] private int completedCount;
        [ObservableProperty] private string totalOutstanding = "Rs 0";

        public InstallmentsViewModel(InstallmentService installmentService, CustomerService customerService)
        {
            _installmentService = installmentService;
            _customerService = customerService;
            _installmentService.InstallmentsChanged += OnInstallmentsChanged;

            _dueDateTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            _dueDateTimer.Tick += (_, _) => CheckForDayRollover();
            _dueDateTimer.Start();

            Application.Current.Activated += OnAppActivated;

            _ = LoadAsync();
        }

        partial void OnSearchTextChanged(string value) => ApplyFilter();
        partial void OnSelectedFilterChanged(string value) => ApplyFilter();

        [RelayCommand]
        private void SetFilter(string filter) => SelectedFilter = filter;

        [RelayCommand]
        private async Task Refresh() => await LoadAsync();

        [RelayCommand]
        private void NewInstallment()
        {
            var vm = new CreateInstallmentViewModel(_installmentService, _customerService);
            var window = new CreateInstallmentWindow(vm)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        [RelayCommand]
        private void ViewSchedule(Installment installment)
        {
            var vm = new InstallmentScheduleViewModel(installment);
            var window = new InstallmentScheduleWindow(vm)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        private async void OnInstallmentsChanged(object? sender, EventArgs e) => await LoadAsync();

        private void OnAppActivated(object? sender, EventArgs e) => CheckForDayRollover();

        private void CheckForDayRollover()
        {
            if (DateTime.Today == _lastCheckedDate) return;

            _lastCheckedDate = DateTime.Today;
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            IsLoading = true;
            _allInstallments = await _installmentService.GetAllAsync();
            IsLoading = false;

            TotalInstallments = _allInstallments.Count;
            ActiveCount = _allInstallments.Count(i => i.Status is InstallmentStatus.Active or InstallmentStatus.Partial);
            DueTodayCount = _allInstallments.Count(i => i.Status == InstallmentStatus.DueToday);
            OverdueCount = _allInstallments.Count(i => i.Status == InstallmentStatus.Overdue);
            CompletedCount = _allInstallments.Count(i => i.Status == InstallmentStatus.Completed);
            TotalOutstanding = $"Rs {_allInstallments.Sum(i => i.OutstandingAmount):N0}";

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            IEnumerable<Installment> query = _allInstallments;

            query = SelectedFilter switch
            {
                "Active" => query.Where(i => i.FilterCategory == "Active"),
                "DueToday" => query.Where(i => i.FilterCategory == "DueToday"),
                "Overdue" => query.Where(i => i.FilterCategory == "Overdue"),
                "Completed" => query.Where(i => i.FilterCategory == "Completed"),
                _ => query
            };

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.Trim();
                query = query.Where(i =>
                    i.CustomerName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    i.CustomerPhone.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    i.InstallmentNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    i.ItemName.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            FilteredInstallments.Clear();
            foreach (var installment in query.OrderByDescending(i => i.DateAdded))
            {
                FilteredInstallments.Add(installment);
            }

            IsEmpty = FilteredInstallments.Count == 0;
        }

        public void Dispose()
        {
            _installmentService.InstallmentsChanged -= OnInstallmentsChanged;
            Application.Current.Activated -= OnAppActivated;
            _dueDateTimer.Stop();
        }
    }
} 