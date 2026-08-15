using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finvora.Models;
using Microsoft.Win32;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System;

namespace Finvora.ViewModels
{
    /// <summary>
    /// Backs the read-only Invoice/Details popup opened from the View button.
    /// Mostly pure display -- the one exception is DownloadPdfCommand, which
    /// renders a branded, table-style invoice into a one-page PDF via PdfSharpCore.
    /// </summary>
    public partial class CustomerInvoiceViewModel : ObservableObject
    {
        /// <summary>Raised when the window should close.</summary>
        public event Action? RequestClose;

        public string InvoiceNumber { get; }
        public DateTime GeneratedOn { get; } = DateTime.Now;

        // ---------- Customer info ----------
        public string FullName { get; }
        public string Phone { get; }
        public string? Email { get; }
        public string? Cnic { get; }
        public string? Address { get; }

        // ---------- Plan info ----------
        public string ItemName { get; }
        public PlanFrequency Frequency { get; }
        public string PlanShortLabel { get; }
        public DateTime DateAdded { get; }
        public DateTime DueDate { get; }

        // ---------- Payment info ----------
        public decimal TotalPrice { get; }
        public decimal AdvancePaid { get; }
        public decimal AmountPaid { get; }
        public decimal RemainingBalance { get; }
        public double PaymentProgressPercent { get; }
        public PaymentStatus Status { get; }
        public bool IsOverdue { get; }

        [ObservableProperty] private string exportMessage = string.Empty;

        public CustomerInvoiceViewModel(Customer customer)
        {
            InvoiceNumber = $"INV-{customer.Id:0000}";

            FullName = customer.FullName;
            Phone = customer.Phone;
            Email = customer.Email;
            Cnic = customer.Cnic;
            Address = customer.Address;

            ItemName = customer.ItemName;
            Frequency = customer.Frequency;
            PlanShortLabel = customer.PlanShortLabel;
            DateAdded = customer.DateAdded;
            DueDate = customer.DueDate;

            TotalPrice = customer.TotalPrice;
            AdvancePaid = customer.AdvancePaid;
            AmountPaid = customer.AmountPaid;
            RemainingBalance = customer.RemainingBalance;
            PaymentProgressPercent = customer.PaymentProgressPercent;
            Status = customer.Status;
            IsOverdue = customer.IsOverdue;
        }

        [RelayCommand]
        private void Close() => RequestClose?.Invoke();

        [RelayCommand]
        private void DownloadPdf()
        {
            ExportMessage = string.Empty;

            var dialog = new SaveFileDialog
            {
                Title = "Save Invoice PDF",
                FileName = $"{InvoiceNumber}_{FullName.Replace(" ", "_")}.pdf",
                Filter = "PDF file (*.pdf)|*.pdf"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                RenderInvoicePdf(dialog.FileName);
                ExportMessage = "PDF saved successfully.";
            }
            catch (Exception ex)
            {
                ExportMessage = $"Couldn't save PDF: {ex.Message}";
            }
        }

        // ---------- PDF rendering ----------

        private void RenderInvoicePdf(string filePath)
        {
            using var document = new PdfDocument();
            document.Info.Title = $"Invoice {InvoiceNumber}";
            document.Info.Author = "Finvora";

            var page = document.AddPage();
            using var gfx = XGraphics.FromPdfPage(page);

            double pageWidth = page.Width.Point;
            double pageHeight = page.Height.Point;
            double margin = 40;
            double contentWidth = pageWidth - margin * 2;

            // ---- Brand palette (matches the app's Colors.xaml) ----
            var navy = XColor.FromArgb(11, 17, 32);
            var accentBlue = XColor.FromArgb(56, 189, 248);
            var accentPurple = XColor.FromArgb(139, 92, 246);
            var green = XColor.FromArgb(34, 197, 94);
            var amber = XColor.FromArgb(245, 158, 11);
            var red = XColor.FromArgb(239, 68, 68);
            var gray = XColor.FromArgb(107, 114, 128);
            var lightPanel = XColor.FromArgb(243, 244, 246);
            var divider = XColor.FromArgb(229, 231, 235);
            var black = XColor.FromArgb(17, 24, 39);

            var whiteBrush = XBrushes.White;
            var grayBrush = new XSolidBrush(gray);
            var blackBrush = new XSolidBrush(black);
            var lightPanelBrush = new XSolidBrush(lightPanel);
            var dividerPen = new XPen(divider, 1);

            var logoFont = new XFont("Segoe UI", 20, XFontStyle.Bold);
            var tagFont = new XFont("Segoe UI", 8, XFontStyle.Regular);
            var titleFont = new XFont("Segoe UI", 22, XFontStyle.Bold);
            var metaFont = new XFont("Segoe UI", 9, XFontStyle.Regular);
            var sectionFont = new XFont("Segoe UI", 10.5, XFontStyle.Bold);
            var labelFont = new XFont("Segoe UI", 9.5, XFontStyle.Regular);
            var valueFont = new XFont("Segoe UI", 9.5, XFontStyle.Bold);
            var nameFont = new XFont("Segoe UI", 15, XFontStyle.Bold);
            var badgeFont = new XFont("Segoe UI", 9, XFontStyle.Bold);
            var balanceLabelFont = new XFont("Segoe UI", 10, XFontStyle.Bold);
            var balanceValueFont = new XFont("Segoe UI", 20, XFontStyle.Bold);
            var footerFont = new XFont("Segoe UI", 8, XFontStyle.Italic);

            // ---- Header band ----
            const double headerHeight = 92;
            gfx.DrawRectangle(new XSolidBrush(navy), 0, 0, pageWidth, headerHeight);
            gfx.DrawString("FINVORA", logoFont, whiteBrush, new XPoint(margin, 38));
            gfx.DrawString("BUSINESS SUITE", tagFont, new XSolidBrush(XColor.FromArgb(148, 163, 184)), new XPoint(margin, 54));

            gfx.DrawString("INVOICE", titleFont, whiteBrush, new XRect(margin, 18, contentWidth, 30), XStringFormats.TopRight);
            gfx.DrawString(InvoiceNumber, metaFont, new XSolidBrush(XColor.FromArgb(203, 213, 225)),
                new XRect(margin, 50, contentWidth, 16), XStringFormats.TopRight);
            gfx.DrawString($"Generated {GeneratedOn:dd MMM yyyy, hh:mm tt}", metaFont,
                new XSolidBrush(XColor.FromArgb(203, 213, 225)), new XRect(margin, 66, contentWidth, 16), XStringFormats.TopRight);

            gfx.DrawRectangle(new XSolidBrush(accentBlue), 0, headerHeight, pageWidth, 4);

            double y = headerHeight + 4 + 28;

            // ---- Status badge ----
            string badgeText = IsOverdue ? "OVERDUE"
                : Status switch { PaymentStatus.Paid => "PAID", PaymentStatus.Partial => "PARTIALLY PAID", _ => "UNPAID" };
            var badgeColor = IsOverdue ? red
                : Status switch { PaymentStatus.Paid => green, PaymentStatus.Partial => amber, _ => amber };

            var badgeSize = gfx.MeasureString(badgeText, badgeFont);
            double badgeWidth = badgeSize.Width + 22;
            double badgeX = pageWidth - margin - badgeWidth;
            gfx.DrawRoundedRectangle(new XSolidBrush(badgeColor), new XRect(badgeX, y - 15, badgeWidth, 22), new XSize(11, 11));
            gfx.DrawString(badgeText, badgeFont, whiteBrush, new XRect(badgeX, y - 15, badgeWidth, 22), XStringFormats.Center);

            // ---- Bill To ----
            gfx.DrawString("BILL TO", sectionFont, new XSolidBrush(accentPurple), new XPoint(margin, y));
            y += 20;
            gfx.DrawString(FullName, nameFont, blackBrush, new XPoint(margin, y));
            y += 20;
            gfx.DrawString(Phone, labelFont, grayBrush, new XPoint(margin, y));
            y += 15;
            if (!string.IsNullOrWhiteSpace(Email)) { gfx.DrawString(Email!, labelFont, grayBrush, new XPoint(margin, y)); y += 15; }
            if (!string.IsNullOrWhiteSpace(Cnic)) { gfx.DrawString(Cnic!, labelFont, grayBrush, new XPoint(margin, y)); y += 15; }
            if (!string.IsNullOrWhiteSpace(Address)) { gfx.DrawString(Address!, labelFont, grayBrush, new XPoint(margin, y)); y += 15; }

            y += 12;
            gfx.DrawLine(dividerPen, margin, y, pageWidth - margin, y);
            y += 24;

            // ---- Plan Details ----
            gfx.DrawString("PLAN DETAILS", sectionFont, new XSolidBrush(accentPurple), new XPoint(margin, y));
            y += 20;
            DrawRow(gfx, "Item / Plan", ItemName, margin, contentWidth, labelFont, valueFont, lightPanelBrush, ref y, shaded: true);
            DrawRow(gfx, "Frequency", Frequency.ToString(), margin, contentWidth, labelFont, valueFont, lightPanelBrush, ref y, shaded: false);
            DrawRow(gfx, "Started On", DateAdded.ToString("dd MMM yyyy"), margin, contentWidth, labelFont, valueFont, lightPanelBrush, ref y, shaded: true);
            DrawRow(gfx, "Next / Last Due Date", DueDate.ToString("dd MMM yyyy"), margin, contentWidth, labelFont, valueFont, lightPanelBrush, ref y, shaded: false);

            y += 12;
            gfx.DrawLine(dividerPen, margin, y, pageWidth - margin, y);
            y += 24;

            // ---- Payment Summary ----
            gfx.DrawString("PAYMENT SUMMARY", sectionFont, new XSolidBrush(accentPurple), new XPoint(margin, y));
            y += 20;
            DrawRow(gfx, "Total Price", $"Rs {TotalPrice:N0}", margin, contentWidth, labelFont, valueFont, lightPanelBrush, ref y, shaded: true);
            DrawRow(gfx, "Advance Paid", $"Rs {AdvancePaid:N0}", margin, contentWidth, labelFont, valueFont, lightPanelBrush, ref y, shaded: false);
            DrawRow(gfx, "Total Amount Paid", $"Rs {AmountPaid:N0}", margin, contentWidth, labelFont, valueFont, lightPanelBrush, ref y, shaded: true);

            // ---- Progress bar ----
            y += 8;
            const double barHeight = 8;
            gfx.DrawRoundedRectangle(lightPanelBrush, new XRect(margin, y, contentWidth, barHeight), new XSize(4, 4));
            double filledWidth = contentWidth * Math.Clamp(PaymentProgressPercent / 100.0, 0, 1);
            if (filledWidth > 0)
            {
                gfx.DrawRoundedRectangle(new XSolidBrush(accentBlue), new XRect(margin, y, filledWidth, barHeight), new XSize(4, 4));
            }
            y += barHeight + 6;
            gfx.DrawString($"{PaymentProgressPercent:N0}% paid", labelFont, grayBrush, new XPoint(margin, y));

            y += 22;

            // ---- Remaining balance panel ----
            var balanceColor = RemainingBalance <= 0 ? green : (IsOverdue ? red : amber);
            const double panelHeight = 54;
            gfx.DrawRoundedRectangle(lightPanelBrush, new XRect(margin, y, contentWidth, panelHeight), new XSize(6, 6));
            gfx.DrawRectangle(new XSolidBrush(balanceColor), margin, y, 5, panelHeight);
            gfx.DrawString("REMAINING BALANCE", balanceLabelFont, grayBrush, new XPoint(margin + 20, y + 16));
            gfx.DrawString($"Rs {RemainingBalance:N0}", balanceValueFont, new XSolidBrush(balanceColor),
                new XRect(margin + 20, y + 10, contentWidth - 40, panelHeight - 10), XStringFormats.CenterRight);

            // ---- Footer ----
            double footerLineY = pageHeight - 60;
            gfx.DrawLine(dividerPen, margin, footerLineY, pageWidth - margin, footerLineY);
            gfx.DrawString("Thank you for your business!", footerFont, grayBrush, new XPoint(margin, footerLineY + 16));
            gfx.DrawString("Finvora Business Suite — Installment Management", footerFont, grayBrush, new XPoint(margin, footerLineY + 30));
            gfx.DrawString("Page 1 of 1", footerFont, grayBrush,
                new XRect(margin, footerLineY + 16, contentWidth, 16), XStringFormats.TopRight);

            document.Save(filePath);
        }

        private static void DrawRow(XGraphics gfx, string label, string value, double x, double width,
            XFont labelFont, XFont valueFont, XSolidBrush shadeBrush, ref double y, bool shaded)
        {
            const double rowHeight = 22;
            if (shaded)
            {
                gfx.DrawRectangle(shadeBrush, x - 8, y - 3, width + 16, rowHeight);
            }

            gfx.DrawString(label, labelFont, XBrushes.Black, new XPoint(x, y + 12));
            gfx.DrawString(value, valueFont, XBrushes.Black, new XRect(x, y + 12, width, 16), XStringFormats.TopRight);
            y += rowHeight;
        }
    }
}  