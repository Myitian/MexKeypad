using System.Runtime.InteropServices;

namespace MexKeypad.Platforms.Windows.Win32Input;

[StructLayout(LayoutKind.Sequential)]
public struct HardwareInput
{
    public uint Msg;
    public ushort ParamL;
    public ushort ParamH;
}