using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Finvora.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private record PeriodSnapshot(
            string TotalRevenue, string PendingBalance, string OverdueAmount, string Collected,
            double Paid, double Pending, double Overdue, double Partial,
            double[] CashFlow);

        private static readonly Dictionary<string, PeriodSnapshot> MockData = new()
        {
            ["Daily"] = new PeriodSnapshot(
                "Rs 12,400", "Rs 28,000", "Rs 9,500", "Rs 2,150",
                0, 3, 1, 0,
                new double[] { 0, 0, 0, 0, 0, 40, 25, 0, 0, 0, 0, 0 }),
            ["Weekly"] = new PeriodSnapshot(
                "Rs 84,000", "Rs 165,000", "Rs 62,000", "Rs 9,800",
                1, 6, 3, 0,
                new double[] { 0, 0, 0, 0, 0, 140, 60, 0, 0, 0, 0, 0 }),
            ["Monthly"] = new PeriodSnapshot(
                "Rs 320,000", "Rs 780,000", "Rs 780,000", "Rs 20,000",
                0, 12, 10, 0,
                new double[] { 0, 0, 0, 0, 0, 300, 20, 0, 0, 0, 0, 0 }),
            ["Yearly"] = new PeriodSnapshot(
                "Rs 3,840,000", "Rs 1,240,000", "Rs 940,000", "Rs 245,000",
                8, 9, 5, 0,
                new double[] { 180, 220, 260, 300, 340, 600, 520, 300, 280, 260, 240, 300 }),
        };

        [ObservableProperty] private string businessName;

        public string Greeting => System.DateTime.Now.Hour switch
        {
            < 12 => "Good Morning",
            < 17 => "Good Afternoon",
            _ => "Good Evening"
        };

        [ObservableProperty] private string totalRevenue;
        [ObservableProperty] private string pendingBalance;
        [ObservableProperty] private string overdueAmount;
        [ObservableProperty] private string collectedThisPeriod;

        [ObservableProperty] private int totalCustomers = 2;
        [ObservableProperty] private int activeCustomers = 2;
        [ObservableProperty] private int completedPlans = 0;
        [ObservableProperty] private int totalPlans;

        public ObservableCollection<string> Periods { get; } = new() { "Daily", "Weekly", "Monthly", "Yearly" };

        [ObservableProperty] private string selectedPeriod = "Monthly";

        partial void OnSelectedPeriodChanged(string value) => LoadData();

        // ----- Installment Summary donut -----
        private readonly ObservableCollection<double> _paidValue = new() { 0 };
        private readonly ObservableCollection<double> _pendingValue = new() { 0 };
        private readonly ObservableCollection<double> _overdueValue = new() { 0 };
        private readonly ObservableCollection<double> _partialValue = new() { 0 };

        public ISeries[] InstallmentSeries { get; }

        [ObservableProperty] private string paidSummary;
        [ObservableProperty] private string pendingSummary;
        [ObservableProperty] private string overdueSummary;
        [ObservableProperty] private string partialSummary;

        // ----- Cash Flow Overview (line/area) -----
        private readonly ObservableCollection<double> _cashFlowValues =
            new() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public ISeries[] CashFlowSeries { get; }
        public Axis[] CashFlowXAxes { get; }
        public Axis[] CashFlowYAxes { get; }

        // ----- Monthly Revenue vs Pending (always "current year", independent of the period selector) -----
        public ISeries[] MonthlySeries { get; }
        public Axis[] MonthlyXAxes { get; }
        public Axis[] MonthlyYAxes { get; }

        private static readonly string[] MonthLabels =
            { "Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec" };

        public DashboardViewModel(string businessName)
        {
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
                new ColumnSeries<double>
                {
                    Name = "Revenue",
                    Values = new double[] { 40,55,60,50,65,70,58,62,68,72,80,85 },
                    Fill = new SolidColorPaint(SKColor.Parse("#38BDF8"))
                },
                new ColumnSeries<double>
                {
                    Name = "Pending",
                    Values = new double[] { 20,18,25,22,30,28,26,24,20,18,15,12 },
                    Fill = new SolidColorPaint(SKColor.Parse("#FBBF24"))
                }
            };
            MonthlyXAxes = new[]
            {
                new Axis { Labels = MonthLabels, LabelsPaint = new SolidColorPaint(SKColor.Parse("#94A3B8")), SeparatorsPaint = null }
            };
            MonthlyYAxes = BuildYAxis();

            LoadData();
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
        private void Refresh() => LoadData();

        /// <summary>
        /// Pulls the snapshot for the currently selected period and pushes it into the UI.
        /// This is the ONLY place dashboard numbers change — no background timers, no randomness.
        /// Once the SQL database is wired up, this method is where the real query goes.
        /// </summary>
        private void LoadData()
        {
            if (!MockData.TryGetValue(SelectedPeriod, out var snapshot))
            {
                return;
            }

            TotalRevenue = snapshot.TotalRevenue;
            PendingBalance = snapshot.PendingBalance;
            OverdueAmount = snapshot.OverdueAmount;
            CollectedThisPeriod = snapshot.Collected;

            _paidValue[0] = snapshot.Paid;
            _pendingValue[0] = snapshot.Pending;
            _overdueValue[0] = snapshot.Overdue;
            _partialValue[0] = snapshot.Partial;

            TotalPlans = (int)(snapshot.Paid + snapshot.Pending + snapshot.Overdue + snapshot.Partial);

            for (var i = 0; i < _cashFlowValues.Count && i < snapshot.CashFlow.Length; i++)
            {
                _cashFlowValues[i] = snapshot.CashFlow[i];
            }

            PaidSummary = FormatSlice("Paid", snapshot.Paid);
            PendingSummary = FormatSlice("Pending", snapshot.Pending);
            OverdueSummary = FormatSlice("Overdue", snapshot.Overdue);
            PartialSummary = FormatSlice("Partial", snapshot.Partial);
        }

        private string FormatSlice(string label, double value)
        {
            var percent = TotalPlans == 0 ? 0 : value / TotalPlans * 100;
            return $"{label}  {value:0} ({percent:0.0}%)";
        }
    }
}