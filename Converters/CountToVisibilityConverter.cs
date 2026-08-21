using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Finvora.Converters
{
    /// <summary>Zero/negative -> Collapsed, anything positive -> Visible.
    /// Used for the bell icon's red dot and the sidebar unread-count badge --
    /// both need to disappear the instant the count hits zero.</summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            int count = value is int i ? i : 0;
            return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("CountToVisibilityConverter only supports one-way binding.");
        }
    }
} 