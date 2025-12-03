using MexShared.Win32Input;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MexShared;

// [Flag ][Extra][Value    ]
// [uint8][uint8][uint16 LE]
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
public struct KeyInfo(KeyFlag flag, ushort value, byte extra = 0) : IEquatable<KeyInfo>, IEqualityOperators<KeyInfo, KeyInfo, bool>
{
    public KeyFlag Flag = flag;
    public byte Extra = extra;
    public ushort value = BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value);
    public ushort Value
    {
        readonly get => BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value);
        set => this.value = BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value);
    }
    public readonly bool Equals(KeyInfo other)
    {
        return Flag == other.Flag && Extra == other.Extra && value == other.value;
    }
    public override readonly bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is KeyInfo other && Equals(other);
    }
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Flag, Extra, value);
    }
    public override readonly string ToString()
    {
        return $"{{{Extra:X2}:{Value:X4}, {Flag}}}";
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
            if (ki.Flag is KeyFlag.Unicode)
            {
                ki.Flag |= KeyFlag.KeyUp;
                list.Add(ki);
            }
        }
        message = "";
        return [.. list];
    }

    [SupportedOSPlatform("windows")]
    public static void SendInput(params ReadOnlySpan<KeyInfo> keys)
    {
        Span<Input> inputs = stackalloc Input[keys.Length];
        int i = 0;
        while (i < keys.Length)
        {
            KeyInfo ki = keys[i];
            switch (ki.Flag & ~KeyFlag.KeyUp)
            {
                case KeyFlag.Unicode:
                    inputs[i] = new KeyboardInput((char)ki.Value, ki.Flag < 0);
                    break;
                case KeyFlag.VirtualKey:
                    inputs[i] = new KeyboardInput((VirtualKey)ki.Value, ki.Flag < 0);
                    break;
                case KeyFlag.ScanCode:
                    inputs[i] = new KeyboardInput(ki.Value, ki.Flag < 0);
                    break;
                case KeyFlag.VirtualKeyWithScanCode:
                    inputs[i] = new KeyboardInput((VirtualKey)ki.Extra, ki.Value, ki.Flag < 0);
                    break;
                case KeyFlag.Mouse:
                    inputs[i] = new MouseInput()
                    {
                        // Step#1: expand flag 0b00000_1111 to MOUSEEVENTF 0b_01_01_01_01_0
                        // Step#2: if KeyUp, shift left by 1 bit to use XxxUp instead of XxxDown
                        Flags = (MouseEventFlag)(MouseFlagMap[ki.Extra] << ((int)ki.Flag >> 7)),
                        MouseData = ki.Value
                    };
                    break;
                default:
                    continue;
            }
            i++;
        }
        Input.Send(inputs[..i]);
    }
    // Expand 0b000001111 to 0b010101010 (insert 0 after bit 0-3)
    public static ReadOnlySpan<int> MouseFlagMap => [
        0b_00_00_00_00_0,
        0b_00_00_00_01_0,
        0b_00_00_01_00_0,
        0b_00_00_01_01_0,
        0b_00_01_00_00_0,
        0b_00_01_00_01_0,
        0b_00_01_01_00_0,
        0b_00_01_01_01_0,
        0b_01_00_00_00_0,
        0b_01_00_00_01_0,
        0b_01_00_01_00_0,
        0b_01_00_01_01_0,
        0b_01_01_00_00_0,
        0b_01_01_00_01_0,
        0b_01_01_01_00_0,
        0b_01_01_01_01_0,];
    public static bool operator ==(KeyInfo left, KeyInfo right)
    {
        return left.Equals(right);
    }
    public static bool operator !=(KeyInfo left, KeyInfo right)
    {
        return !left.Equals(right);
    }
}