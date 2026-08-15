using System;
using System.Globalization;
using System.Windows.Data;

namespace Finvora.Converters
{
    /// <summary>
    /// Returns true if the bound string equals ConverterParameter. Powers the
    /// filter pills (each RadioButton's IsChecked is bound one-way to SelectedFilter).
    /// </summary>
    public class StringEqualsConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value?.ToString() == parameter?.ToString();
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("StringEqualsConverter only supports one-way binding.");
        }
    }
}  