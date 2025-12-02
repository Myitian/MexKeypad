using System.Runtime.InteropServices;

namespace MexShared.Win32Input;

[StructLayout(LayoutKind.Sequential)]
public struct HardwareInput
{
    public uint Msg;
    public ushort ParamL;
    public ushort ParamH;
}