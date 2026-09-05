using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Finvora.Converters
{
    /// <summary>Null -> Collapsed, any object -> Visible. Used to show the
    /// "Quantity" picker only once a stock item has actually been selected
    /// (there's nothing to pick a quantity of otherwise).</summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("NullToVisibilityConverter only supports one-way binding.");
        }
    }
} 