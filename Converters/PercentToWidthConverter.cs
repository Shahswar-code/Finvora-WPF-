using System.Globalization;
using System.Windows.Data;

namespace Finvora.Converters
{
    /// <summary>
    /// Converts a 0-100 percentage value into a pixel width, given the full
    /// track width as the converter parameter. Used to animate the splash
    /// screen's progress indicator without templating a full ProgressBar control.
    /// </summary>
    public class PercentToWidthConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            double percent = value is double d ? d : 0;
            double trackWidth = parameter is string s && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var w)
                ? w
                : 220;

            percent = Math.Clamp(percent, 0, 100);
            return percent / 100.0 * trackWidth;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("PercentToWidthConverter only supports one-way binding.");
        }
    }
}
