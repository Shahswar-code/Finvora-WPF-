using System;
using System.Globalization;
using System.Windows.Data;

namespace Finvora.Converters
{
    /// <summary>
    /// Converts [percent (0-100), track ActualWidth] into a pixel width for the
    /// filled portion of a progress bar. Unlike PercentToWidthConverter (which takes
    /// a fixed parameter), this reacts to the track's real rendered width, so it
    /// stays correct if the card is resized.
    /// </summary>
    public class PercentToWidthMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not double percent || values[1] is not double trackWidth)
                return 0.0;

            percent = Math.Clamp(percent, 0, 100);
            return percent / 100.0 * trackWidth;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("PercentToWidthMultiConverter only supports one-way binding.");
        }
    }
}  