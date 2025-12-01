using System.Globalization;

namespace MexKeypad;

public class NotEqualsConverter : NoConvertBackValueConverter
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return !Equals(value, parameter);
    }
}