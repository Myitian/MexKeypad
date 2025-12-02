using MexShared;
using System.Runtime.InteropServices;

namespace MexKeypad.Platforms.Windows.Win32Input;

[StructLayout(LayoutKind.Sequential)]
public partial struct KeyboardInput
{
    public ushort Vk;
    public ushort Scan;
    public uint Flags;
    public uint Time;
    public nint ExtraInfo;

    public KeyboardInput(ConsoleKey ck, bool keyUp) : this((VirtualKey)ck, keyUp)
    {
    }
    public KeyboardInput(VirtualKey vk, bool keyUp)
    {
        Vk = (ushort)vk;
        Scan = 0;
        Flags = (uint)(keyUp ? KeyboardEventFlag.KeyUp : 0);
        Time = 0;
        ExtraInfo = 0;
    }
    public KeyboardInput(ushort scan, bool keyUp)
    {
        Vk = 0;
        Scan = scan;
        Flags = (uint)(KeyboardEventFlag.ScanCode
            | (keyUp ? KeyboardEventFlag.KeyUp : 0)
            | (scan >= 0xE000 ? KeyboardEventFlag.ExtendedKey : 0));
        Time = 0;
        ExtraInfo = 0;
    }
    public KeyboardInput(char unicode, bool keyUp)
    {
        Vk = 0;
        Scan = unicode;
        Flags = (uint)(KeyboardEventFlag.Unicode
            | (keyUp ? KeyboardEventFlag.KeyUp : 0));
        Time = 0;
        ExtraInfo = 0;
    }

    [LibraryImport("user32", EntryPoint = "keybd_event")]
    private static partial void KeyboardEvent(byte bVk, byte bScan, uint dwFlags, nint dwExtraInfo);

    /// <summary>
    /// Calls <c>keybd_event</c>. Recommended to use <see cref="Input.Send"/> instead of this.
    /// </summary>
    /// <remarks>
    /// When using <see cref="KeyboardEventFlag.Unicode" /> may not produce correct results
    /// because the value is truncated to <see cref="byte" />.
    /// </remarks>
    public readonly void SendEvent()
    {
        KeyboardEvent((byte)Vk, (byte)Scan, Flags, ExtraInfo);
    }
}