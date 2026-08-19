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
using Microsoft.Win32;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace Finvora.ViewModels
{
    public partial class CustomersViewModel : ObservableObject, IDisposable
    {
        private readonly CustomerService _customerService;
        private List<Customer> _allCustomers = new();

        // Tracks the last calendar day we recalculated Overdue/status against.
        // Customer is a plain model (no INotifyPropertyChanged) and its
        // IsOverdue/FilterCategory getters compare against DateTime.Today, so
        // nothing re-evaluates them automatically as real time passes -- only
        // when this ViewModel re-runs ApplyFilter/LoadAsync. Without this, a
        // customer that crosses their due date while the app just sits open
        // never flips to "Overdue" until some unrelated Add/Edit/Delete happens
        // to fire CustomersChanged.
        private DateTime _lastCheckedDate = DateTime.Today;
        private readonly DispatcherTimer _dueDateTimer;

        [ObservableProperty] private string searchText = "";
        [ObservableProperty] private string selectedFilter = "All";

        public ObservableCollection<Customer> FilteredCustomers { get; } = new();

        [ObservableProperty] private bool isLoading;
        [ObservableProperty] private bool isEmpty;

        // ----- Stat cards -----
        [ObservableProperty] private int totalCustomers;
        [ObservableProperty] private int totalPlans;
        [ObservableProperty] private string pendingBalance = "Rs 0";
        [ObservableProperty] private int overdueCount;

        public CustomersViewModel(CustomerService customerService)
        {
            _customerService = customerService;
            _customerService.CustomersChanged += OnCustomersChanged;

            // Safety-net poll: catches the day rolling over while the app is left
            // open and idle. Ticks every minute but only does real work (a
            // refetch + re-filter) on the minute the calendar day actually changes.
            _dueDateTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            _dueDateTimer.Tick += (_, _) => CheckForDayRollover();
            _dueDateTimer.Start();

            // Covers the more common real-world case: the app was left open
            // overnight (or the PC slept) and the user just clicked back into it.
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
        private void AddNewCustomer()
        {
            var vm = new AddCustomerViewModel(_customerService);
            var window = new AddCustomerWindow(vm)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        [RelayCommand]
        private void ExportAll()
        {
            // Exports every customer on record (not just the current filter/search
            // results) -- "Export All" means the whole dataset, as a single
            // multi-page tabular PDF report.
            if (_allCustomers.Count == 0)
            {
                MessageBox.Show("There are no customers to export yet.", "Nothing to export",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Export All Customers",
                FileName = $"Finvora_Customers_{DateTime.Now:yyyyMMdd_HHmm}.pdf",
                Filter = "PDF file (*.pdf)|*.pdf"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                RenderAllCustomersPdf(dialog.FileName);
                MessageBox.Show(
                    $"Exported {_allCustomers.Count} customer{(_allCustomers.Count == 1 ? "" : "s")} to:\n{dialog.FileName}",
                    "Export complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Couldn't export: {ex.Message}", "Export failed",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenderAllCustomersPdf(string fileName)
        {
            var document = new PdfDocument();
            document.Info.Title = "Finvora - All Customers";
            document.Info.Subject = "Complete customer list";

            const double margin = 30;
            const double rowHeight = 25;

            PdfPage page = document.AddPage();
            page.Size = PageSize.A4;
            page.Orientation = PageOrientation.Landscape;

            XGraphics gfx = XGraphics.FromPdfPage(page);

            var titleFont = new XFont("Arial", 18, XFontStyle.Bold);
            var headerFont = new XFont("Arial", 9, XFontStyle.Bold);
            var bodyFont = new XFont("Arial", 8, XFontStyle.Regular);

            double y = margin;

            void DrawHeader()
            {
                gfx.DrawString(
                    "FINVORA - ALL CUSTOMERS",
                    titleFont,
                    XBrushes.Black,
                    new XRect(margin, y, page.Width - margin * 2, 30),
                    XStringFormats.TopLeft);

                y += 35;

                gfx.DrawString(
                    $"Generated: {DateTime.Now:dd MMM yyyy hh:mm tt}",
                    bodyFont,
                    XBrushes.Gray,
                    new XRect(margin, y, page.Width - margin * 2, 20),
                    XStringFormats.TopLeft);

                y += 30;

                double[] widths =
                {
            35,   // #
            170,  // Customer
            100,  // CNIC
            100,  // Phone
            100,  // Remaining
            80,   // Status
            100   // Date Added
        };

                string[] headers =
                {
            "#",
            "Customer",
            "CNIC",
            "Phone",
            "Remaining",
            "Status",
            "Date Added"
        };

                double x = margin;

                for (int i = 0; i < headers.Length; i++)
                {
                    gfx.DrawRectangle(
                        XBrushes.LightGray,
                        x,
                        y,
                        widths[i],
                        rowHeight);

                    gfx.DrawString(
                        headers[i],
                        headerFont,
                        XBrushes.Black,
                        new XRect(x + 4, y + 6, widths[i] - 8, rowHeight),
                        XStringFormats.TopLeft);

                    x += widths[i];
                }

                y += rowHeight;
            }

            DrawHeader();

            int number = 1;

            foreach (var customer in _allCustomers.OrderBy(c => c.FullName))
            {
                // Create another page when the current page is full
                if (y + rowHeight > page.Height - margin)
                {
                    gfx.Dispose();

                    page = document.AddPage();
                    page.Size = PageSize.A4;
                    page.Orientation = PageOrientation.Landscape;

                    gfx = XGraphics.FromPdfPage(page);

                    y = margin;

                    DrawHeader();
                }

                string status;

                if (customer.IsOverdue)
                {
                    status = "Overdue";
                }
                else if (customer.RemainingBalance <= 0)
                {
                    status = "Complete";
                }
                else
                {
                    status = "Active";
                }

                string[] values =
                {
            number.ToString(),
            customer.FullName ?? "",
            customer.Cnic ?? "",
            customer.Phone ?? "",
            $"Rs {customer.RemainingBalance:N0}",
            status,
            customer.DateAdded.ToString("dd MMM yyyy")
        };

                double[] widths =
                {
            35,
            170,
            100,
            100,
            100,
            80,
            100
        };

                double x = margin;

                for (int i = 0; i < values.Length; i++)
                {
                    gfx.DrawRectangle(
                        XPens.LightGray,
                        x,
                        y,
                        widths[i],
                        rowHeight);

                    gfx.DrawString(
                        values[i],
                        bodyFont,
                        XBrushes.Black,
                        new XRect(x + 4, y + 6, widths[i] - 8, rowHeight),
                        XStringFormats.TopLeft);

                    x += widths[i];
                }

                y += rowHeight;
                number++;
            }

            gfx.Dispose();

            document.Save(fileName);
            document.Close();
        } 

        [RelayCommand]
        private void ViewCustomer(Customer customer)
        {
            var vm = new CustomerInvoiceViewModel(customer);
            var window = new CustomerInvoiceWindow(vm)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        [RelayCommand]
        private void EditCustomer(Customer customer)
        {
            var vm = new EditCustomerViewModel(_customerService, customer);
            var window = new EditCustomerWindow(vm)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        [RelayCommand]
        private async Task DeleteCustomer(Customer customer)
        {
            var result = MessageBox.Show(
                $"Delete {customer.FullName}? This cannot be undone.",
                "Confirm delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            await _customerService.DeleteAsync(customer.Id);
            // FilteredCustomers refreshes automatically via CustomersChanged.
        }

        private async void OnCustomersChanged(object? sender, EventArgs e) => await LoadAsync();

        private void OnAppActivated(object? sender, EventArgs e) => CheckForDayRollover();

        /// <summary>
        /// If the calendar day has moved on since we last checked, re-pull and
        /// re-filter so every card's Overdue/Complete/Active badge and the stat
        /// cards reflect "today" correctly -- without waiting for an unrelated
        /// Add/Edit/Delete to trigger it.
        /// </summary>
        private void CheckForDayRollover()
        {
            if (DateTime.Today == _lastCheckedDate) return;

            _lastCheckedDate = DateTime.Today;
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            IsLoading = true;
            _allCustomers = await _customerService.GetAllAsync();
            IsLoading = false;

            TotalCustomers = _allCustomers.Count;
            TotalPlans = _allCustomers.Count;
            PendingBalance = $"Rs {_allCustomers.Sum(c => c.RemainingBalance):N0}";
            OverdueCount = _allCustomers.Count(c => c.IsOverdue);

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            IEnumerable<Customer> query = _allCustomers;

            query = SelectedFilter switch
            {
                "Active" => query.Where(c => c.FilterCategory == "Active"),
                "Overdue" => query.Where(c => c.FilterCategory == "Overdue"),
                "Complete" => query.Where(c => c.FilterCategory == "Complete"),
                _ => query
            };

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.Trim();
                query = query.Where(c =>
                    c.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (c.Cnic ?? "").Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    c.Phone.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            FilteredCustomers.Clear();
            foreach (var customer in query.OrderByDescending(c => c.DateAdded))
            {
                FilteredCustomers.Add(customer);
            }

            IsEmpty = FilteredCustomers.Count == 0;
        }

        public void Dispose()
        {
            _customerService.CustomersChanged -= OnCustomersChanged;
            Application.Current.Activated -= OnAppActivated;
            _dueDateTimer.Stop();
        }
    }
} 