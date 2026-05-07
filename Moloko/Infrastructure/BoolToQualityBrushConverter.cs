using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Moloko.Infrastructure;

public sealed class BoolToQualityBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true
            ? new SolidColorBrush(Color.FromRgb(174, 40, 47))
            : new SolidColorBrush(Color.FromRgb(34, 114, 80));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
