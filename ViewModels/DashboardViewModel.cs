using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finvora.Models;
using Finvora.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Finvora.ViewModels
{
    public partial class DashboardViewModel : ObservableObject, IDisposable
    {
        private readonly CustomerService _customerService;

        [ObservableProperty] private string businessName;

        public string Greeting => DateTime.Now.Hour switch
        {
            < 12 => "Good Morning",
            < 17 => "Good Afternoon",
            _ => "Good Evening"
        };

        [ObservableProperty] private string totalRevenue = "Rs 0";
        [ObservableProperty] private string pendingBalance = "Rs 0";
        [ObservableProperty] private string overdueAmount = "Rs 0";
        [ObservableProperty] private string collectedThisPeriod = "Rs 0";

        [ObservableProperty] private int totalCustomers;
        [ObservableProperty] private int activeCustomers;
        [ObservableProperty] private int completedPlans;
        [ObservableProperty] private int totalPlans;

        public ObservableCollection<string> Periods { get; } = new() { "Daily", "Weekly", "Monthly", "Yearly" };

        [ObservableProperty] private string selectedPeriod = "Monthly";

        partial void OnSelectedPeriodChanged(string value) => _ = LoadDataAsync();

        // ----- Installment Summary donut -----
        private readonly ObservableCollection<double> _paidValue = new() { 0 };
        private readonly ObservableCollection<double> _pendingValue = new() { 0 };
        private readonly ObservableCollection<double> _overdueValue = new() { 0 };
        private readonly ObservableCollection<double> _partialValue = new() { 0 };

        public ISeries[] InstallmentSeries { get; }

        [ObservableProperty] private string paidSummary = "";
        [ObservableProperty] private string pendingSummary = "";
        [ObservableProperty] private string overdueSummary = "";
        [ObservableProperty] private string partialSummary = "";

        // ----- Cash Flow Overview: money collected per month, last 12 months -----
        private readonly ObservableCollection<double> _cashFlowValues =
            new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public ISeries[] CashFlowSeries { get; }
        public Axis[] CashFlowXAxes { get; }
        public Axis[] CashFlowYAxes { get; }

        // ----- Monthly Revenue vs Pending: current calendar year -----
        private readonly ObservableCollection<double> _monthlyRevenueValues =
            new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        private readonly ObservableCollection<double> _monthlyPendingValues =
            new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public ISeries[] MonthlySeries { get; }
        public Axis[] MonthlyXAxes { get; }
        public Axis[] MonthlyYAxes { get; }

        private static readonly string[] MonthLabels =
            { "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec" };

        public DashboardViewModel(string businessName, CustomerService customerService)
        {
            _customerService = customerService;
            BusinessName = businessName;

            InstallmentSeries = new ISeries[]
            {
                new PieSeries<double> { Values = _paidValue,    Name = "Paid",    InnerRadius = 55, Fill = new SolidColorPaint(SKColor.Parse("#34D399")) },
                new PieSeries<double> { Values = _pendingValue, Name = "Pending", InnerRadius = 55, Fill = new SolidColorPaint(SKColor.Parse("#FBBF24")) },
                new PieSeries<double> { Values = _overdueValue, Name = "Overdue", InnerRadius = 55, Fill = new SolidColorPaint(SKColor.Parse("#F87171")) },
                new PieSeries<double> { Values = _partialValue, Name = "Partial", InnerRadius = 55, Fill = new SolidColorPaint(SKColor.Parse("#A78BFA")) },
            };

            CashFlowSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = _cashFlowValues,
                    Name = "Cash Flow",
                    Stroke = new SolidColorPaint(SKColor.Parse("#38BDF8"), 3),
                    Fill = new SolidColorPaint(SKColor.Parse("#2E38BDF8")),
                    GeometryStroke = new SolidColorPaint(SKColor.Parse("#38BDF8"), 2),
                    GeometryFill = new SolidColorPaint(SKColor.Parse("#0B1120")),
                    GeometrySize = 7,
                    LineSmoothness = 0.6
                }
            };
            CashFlowXAxes = new[]
            {
                new Axis { Labels = MonthLabels, LabelsPaint = new SolidColorPaint(SKColor.Parse("#94A3B8")), SeparatorsPaint = null }
            };
            CashFlowYAxes = BuildYAxis();

            MonthlySeries = new ISeries[]
            {
                new ColumnSeries<double> { Name = "Revenue", Values = _monthlyRevenueValues, Fill = new SolidColorPaint(SKColor.Parse("#38BDF8")) },
                new ColumnSeries<double> { Name = "Pending", Values = _monthlyPendingValues, Fill = new SolidColorPaint(SKColor.Parse("#FBBF24")) }
            };
            MonthlyXAxes = new[]
            {
                new Axis { Labels = MonthLabels, LabelsPaint = new SolidColorPaint(SKColor.Parse("#94A3B8")), SeparatorsPaint = null }
            };
            MonthlyYAxes = BuildYAxis();

            // Refresh automatically whenever a customer is added/edited/deleted anywhere in the app.
            _customerService.CustomersChanged += OnCustomersChanged;

            _ = LoadDataAsync();
        }

        private static Axis[] BuildYAxis() => new[]
        {
            new Axis
            {
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#94A3B8")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#232B42")) { StrokeThickness = 1 }
            }
        };

        [RelayCommand]
        private async Task Refresh() => await LoadDataAsync();

        private async void OnCustomersChanged(object? sender, EventArgs e) => await LoadDataAsync();

        /// <summary>
        /// Pulls every customer from the database and recomputes every number
        /// and chart on the dashboard. This is the ONLY place dashboard data
        /// is calculated — no mock data, no background timers.
        /// </summary>
        private async Task LoadDataAsync()
        {
            var customers = await _customerService.GetAllAsync();
            var (rangeStart, rangeEnd) = GetPeriodRange(SelectedPeriod);

            // ----- Top stat cards -----
            TotalCustomers = customers.Count;
            ActiveCustomers = customers.Count(c => c.Status != PaymentStatus.Paid);
            CompletedPlans = customers.Count(c => c.Status == PaymentStatus.Paid);
            TotalPlans = customers.Count;

            TotalRevenue = FormatCurrency(customers
                .Where(c => c.DateAdded >= rangeStart && c.DateAdded < rangeEnd)
                .Sum(c => c.TotalPrice));

            PendingBalance = FormatCurrency(customers.Sum(c => c.RemainingBalance));

            OverdueAmount = FormatCurrency(customers
                .Where(c => c.IsOverdue)
                .Sum(c => c.RemainingBalance));

            // Estimate for now: total paid on plans created within this period.
            // Once Payments has its own history ledger, this becomes an exact
            // "money actually collected in this date range" figure.
            CollectedThisPeriod = FormatCurrency(customers
                .Where(c => c.DateAdded >= rangeStart && c.DateAdded < rangeEnd)
                .Sum(c => c.AmountPaid));

            // ----- Installment Summary donut -----
            double paid = customers.Count(c => c.Status == PaymentStatus.Paid);
            double overdue = customers.Count(c => c.IsOverdue);
            double partial = customers.Count(c => c.Status == PaymentStatus.Partial && !c.IsOverdue);
            double pending = customers.Count(c => c.Status == PaymentStatus.Unpaid && !c.IsOverdue);

            _paidValue[0] = paid;
            _pendingValue[0] = pending;
            _overdueValue[0] = overdue;
            _partialValue[0] = partial;

            var donutTotal = paid + pending + overdue + partial;
            PaidSummary = FormatSlice("Paid", paid, donutTotal);
            PendingSummary = FormatSlice("Pending", pending, donutTotal);
            OverdueSummary = FormatSlice("Overdue", overdue, donutTotal);
            PartialSummary = FormatSlice("Partial", partial, donutTotal);

            // ----- Cash Flow: last 12 months, ending this month -----
            var today = DateTime.Now;
            for (int i = 0; i < 12; i++)
            {
                var monthStart = new DateTime(today.Year, today.Month, 1).AddMonths(i - 11);
                var monthEnd = monthStart.AddMonths(1);

                _cashFlowValues[i] = (double)customers
                    .Where(c => c.DateAdded >= monthStart && c.DateAdded < monthEnd)
                    .Sum(c => c.AmountPaid);
            }

            // ----- Monthly Revenue vs Pending: current calendar year -----
            for (int month = 1; month <= 12; month++)
            {
                var monthCustomers = customers
                    .Where(c => c.DateAdded.Year == today.Year && c.DateAdded.Month == month)
                    .ToList();

                _monthlyRevenueValues[month - 1] = (double)monthCustomers.Sum(c => c.TotalPrice);
                _monthlyPendingValues[month - 1] = (double)monthCustomers.Sum(c => c.RemainingBalance);
            }
        }

        private static (DateTime start, DateTime end) GetPeriodRange(string period)
        {
            var now = DateTime.Now;
            return period switch
            {
                "Daily" => (now.Date, now.Date.AddDays(1)),
                "Weekly" => (now.Date.AddDays(-(int)now.DayOfWeek), now.Date.AddDays(7 - (int)now.DayOfWeek)),
                "Yearly" => (new DateTime(now.Year, 1, 1), new DateTime(now.Year + 1, 1, 1)),
                _ => (new DateTime(now.Year, now.Month, 1), new DateTime(now.Year, now.Month, 1).AddMonths(1)), // Monthly
            };
        }

        private static string FormatCurrency(decimal amount) => $"Rs {amount:N0}";

        private static string FormatSlice(string label, double value, double total)
        {
            var percent = total == 0 ? 0 : value / total * 100;
            return $"{label}  {value:0} ({percent:0.0}%)";
        }

        public void Dispose()
        {
            _customerService.CustomersChanged -= OnCustomersChanged;
        }
    }
}  