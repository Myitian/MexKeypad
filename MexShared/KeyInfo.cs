using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace MexShared;

// [Flag ][Extra][Value    ]
// [uint8][uint8][uint16 LE]
[StructLayout(LayoutKind.Sequential)]
public struct KeyInfo(KeyFlag flag, ushort value, byte extra = 0)
{
    public KeyFlag Flag = flag;
    public byte Extra = extra;
    public ushort _value = BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value);
    public ushort Value
    {
        readonly get => BitConverter.IsLittleEndian ? _value : BinaryPrimitives.ReverseEndianness(_value);
        set => _value = BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value);
    }
    public static bool TryParse(scoped ReadOnlySpan<char> str, out KeyInfo keyInfo, [NotNullWhen(false)] out string? message)
    {
        keyInfo = default;
        str = str.Trim();
        int colon = str.IndexOf(':');
        if (colon < 0)
        {
            message = "按键信息缺失按键类型前缀";
            return false;
        }
        ReadOnlySpan<char> mode = str[..colon].TrimEnd();
        ReadOnlySpan<char> value = str[(colon + 1)..].TrimStart();
        if (mode.Equals("u", StringComparison.OrdinalIgnoreCase))
        {
            keyInfo.Flag = KeyFlag.Unicode;
            if (TryParse(value, out ushort u))
                keyInfo.Value = u;
            else if (value is [char c])
                keyInfo.Value = c;
            else
            {
                message = $"未知的Unicode：{value}";
                return false;
            }
        }
        else if (mode.Equals("vk", StringComparison.OrdinalIgnoreCase))
        {
            keyInfo.Flag = KeyFlag.VirtualKey;
            if (TryParse(value, out ushort u))
                keyInfo.Value = u;
            else if (Enum.TryParse(value, true, out VirtualKey vk))
                keyInfo.Value = (ushort)vk;
            else
            {
                message = $"无效的VirtualKey：{value}";
                return false;
            }
        }
        else if (mode.Equals("sc", StringComparison.OrdinalIgnoreCase))
        {
            keyInfo.Flag = KeyFlag.ScanCode;
            if (TryParse(value, out ushort u))
                keyInfo.Value = u;
            else
            {
                message = $"无效的ScanCode：{value}";
                return false;
            }
        }
        else if (mode.Equals("vksc", StringComparison.OrdinalIgnoreCase))
        {
            keyInfo.Flag = KeyFlag.VirtualKeyWithScanCode;
            int sep = value.IndexOf(':');
            if (sep < 0)
            {
                message = $"VirtualKey+ScanCode模式缺失第二个值：{value}";
                return false;
            }
            ReadOnlySpan<char> v0 = value[..sep];
            if (TryParse(v0, out ushort u0) || u0 > byte.MaxValue)
                keyInfo.Extra = (byte)u0;
            else if (Enum.TryParse(v0, true, out VirtualKey vk) || (ushort)vk > byte.MaxValue)
                keyInfo.Extra = (byte)vk;
            else
            {
                message = $"无效的VirtualKey：{v0}";
                return false;
            }
            ReadOnlySpan<char> v1 = value[(sep + 1)..];
            if (TryParse(v1, out ushort u1))
                keyInfo.Value = u1;
            else
            {
                message = $"无效的ScanCode：{v1}";
                return false;
            }
        }
        else if (mode.Equals("m", StringComparison.OrdinalIgnoreCase))
        {
            keyInfo.Flag = KeyFlag.Mouse;
            int sep = value.IndexOf(':');
            ReadOnlySpan<char> v0 = sep < 0 ? value : value[..sep];
            if (TryParse(v0, out byte u0))
                keyInfo.Value = u0;
            else if (Enum.TryParse(v0, true, out MouseButton mb))
                keyInfo.Extra = (byte)mb;
            else
            {
                message = $"无效的鼠标按钮：{v0}";
                return false;
            }
            if (sep < 0)
                keyInfo.Value = 0;
            else
            {
                ReadOnlySpan<char> v1 = value[(sep + 1)..];
                if (TryParse(v1, out ushort u1))
                    keyInfo.Value = u1;
                else
                {
                    message = $"无效的鼠标事件信息：{v1}";
                    return false;
                }
            }
        }
        else
        {
            message = $"未知的按键类型：{mode}";
            return false;
        }
        message = null;
        return true;
    }

    private static bool TryParse(ReadOnlySpan<char> value, out byte u)
    {
        if (value.StartsWith("0x") && byte.TryParse(value[2..], NumberStyles.HexNumber, null, out u))
            return true;
        if (byte.TryParse(value, out u))
            return true;
        return false;
    }
    private static bool TryParse(ReadOnlySpan<char> value, out ushort u)
    {
        if (value.StartsWith("0x") && ushort.TryParse(value[2..], NumberStyles.HexNumber, null, out u))
            return true;
        if (ushort.TryParse(value, out u))
            return true;
        return false;
    }

    public static KeyInfo[]? TryParseArray(scoped ReadOnlySpan<char> str, out string message)
    {
        List<KeyInfo> list = [];
        foreach (Range range in str.Split(';'))
        {
            if (!TryParse(str[range], out KeyInfo ki, out string? msg))
            {
                message = msg;
                return null;
            }
            list.Add(ki);
            if (ki.Flag.HasFlag(KeyFlag.Unicode))
            {
                ki.Flag |= KeyFlag.KeyUp;
                list.Add(ki);
            }
        }
        message = "";
        return [.. list];
    }
}