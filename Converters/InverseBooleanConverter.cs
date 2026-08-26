using System;
using System.Globalization;
using System.Windows.Data;

namespace Finvora.Converters
{
    /// <summary>Flips a bool -- used to disable Save/Record buttons while
    /// IsSaving is true (double-submit guard) without inverting the VM property itself.</summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => !(value is bool b && b);

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}  