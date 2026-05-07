using System.Globalization;
using System.Windows.Data;
using Moloko.Models;

namespace Moloko.Infrastructure;

public sealed class EnumTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            BatchStatus status => RussianText.BatchStatus(status),
            QualityConclusion conclusion => RussianText.QualityConclusion(conclusion),
            StockOperationType operation => RussianText.StockOperation(operation),
            UserRole role => RussianText.Role(role),
            _ => value?.ToString() ?? string.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
