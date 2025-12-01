using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace MexShared;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct KeyInfo(KeyboardEventFlag flag, ushort value)
{
    public KeyboardEventFlag Flag = flag;
    public ushort Value = value;

    public static bool TryParse(scoped ReadOnlySpan<char> str, out KeyInfo keyInfo, [NotNullWhen(false)] out string? message)
    {
        keyInfo = default;
        str = str.Trim();
        int colon = str.IndexOf(':');
        if (colon == -1)
        {
            message = "按键信息缺失按键类型前缀";
            return false;
        }
        ReadOnlySpan<char> mode = str[..colon].TrimEnd();
        if (mode.Equals("vk", StringComparison.OrdinalIgnoreCase))
            keyInfo.Flag = 0;
        else if (mode.Equals("sc", StringComparison.OrdinalIgnoreCase))
            keyInfo.Flag = KeyboardEventFlag.ScanCode;
        else if (mode.Equals("u", StringComparison.OrdinalIgnoreCase))
            keyInfo.Flag = KeyboardEventFlag.Unicode;
        else
        {
            message = $"未知的按键类型：{mode}";
            return false;
        }
        ReadOnlySpan<char> value = str[(colon + 1)..].TrimStart();
        if (keyInfo.Flag == 0 && value.StartsWith('*'))
        {
            keyInfo.Flag |= KeyboardEventFlag.ExtendedKey;
            value = value[1..];
        }
        if (value.StartsWith("0x") && ushort.TryParse(value, NumberStyles.HexNumber, null, out ushort u))
            keyInfo.Value = u;
        else if (ushort.TryParse(value, out u))
            keyInfo.Value = u;
        else
        {
            switch (keyInfo.Flag)
            {
                case 0:
                case KeyboardEventFlag.ExtendedKey:
                    if (Enum.TryParse(value, true, out VirtualKey vk))
                    {
                        keyInfo.Value = (ushort)vk;
                        break;
                    }
                    else
                    {
                        message = $"未知的VirtualKey：{value}";
                        return false;
                    }
                case KeyboardEventFlag.ScanCode:
                    message = $"未知的ScanCode：{value}";
                    return false;
                case KeyboardEventFlag.Unicode:
                    if (value is [char c])
                    {
                        keyInfo.Value = c;
                        break;
                    }
                    else
                    {
                        message = $"未知的Unicode：{value}";
                        return false;
                    }
            }
        }
        message = null;
        return true;
    }
    public static KeyInfo[]? TryParseArray(scoped ReadOnlySpan<char> str, out string message)
    {
        List<KeyInfo> list = [];
        foreach (Range range in str.Split(','))
        {
            if (!TryParse(str[range], out KeyInfo ki, out string? msg))
            {
                message = msg;
                return null;
            }
            list.Add(ki);
        }
        message = "";
        return [.. list];
    }
}