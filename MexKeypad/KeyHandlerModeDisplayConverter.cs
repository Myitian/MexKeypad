using System.Globalization;

namespace MexKeypad;

public class KeyHandlerModeDisplayConverter : NoConvertBackValueConverter
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            KeyHandlerMode.Local => "确认",
            KeyHandlerMode.Client => "连接",
            KeyHandlerMode.Server => "启动",
            _ => ""
        };
    }
}