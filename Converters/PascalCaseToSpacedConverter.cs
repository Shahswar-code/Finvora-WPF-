using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Finvora.Converters
{
    /// <summary>"BankTransfer" -> "Bank Transfer". Used to display PaymentMethod
    /// values (and similar PascalCase enums) without a raw run-together label.</summary>
    public class PascalCaseToSpacedConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var text = value?.ToString();
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var sb = new StringBuilder();
            foreach (var c in text)
            {
                if (char.IsUpper(c) && sb.Length > 0) sb.Append(' ');
                sb.Append(c);
            }
            return sb.ToString();
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}  