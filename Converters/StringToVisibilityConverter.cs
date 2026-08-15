using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Finvora.Converters
{
    /// <summary>Empty/whitespace string -> Collapsed, anything else -> Visible.
    /// Used to show/hide the inline error message in the Add Customer form.</summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
} 