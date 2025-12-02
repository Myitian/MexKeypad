using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MexShared.Win32Input;

[StructLayout(LayoutKind.Sequential)]
public partial struct KeyboardInput
{
    public ushort Vk;
    public ushort Scan;
    public KeyboardEventFlag Flags;
    public uint Time;
    public nint ExtraInfo;

    public KeyboardInput(ConsoleKey ck, bool keyUp) : this(ck, 0, keyUp)
    {
    }
    public KeyboardInput(ConsoleKey ck, ushort scan, bool keyUp) : this((VirtualKey)ck, scan, keyUp)
    {
    }
    public KeyboardInput(VirtualKey vk, bool keyUp) : this(vk, 0, keyUp)
    {
    }
    public KeyboardInput(VirtualKey vk, ushort scan, bool keyUp)
    {
        Vk = (ushort)vk;
        Scan = (byte)scan;
        Flags = (keyUp ? KeyboardEventFlag.KeyUp : 0)
            | ((scan & 0xFF00) == 0xE000 ? KeyboardEventFlag.ExtendedKey : 0);
        Time = 0;
        ExtraInfo = 0;
    }
    public KeyboardInput(ushort scan, bool keyUp)
    {
        Vk = 0;
        Scan = (byte)scan;
        Flags = KeyboardEventFlag.ScanCode
            | (keyUp ? KeyboardEventFlag.KeyUp : 0)
            | ((scan & 0xFF00) == 0xE000 ? KeyboardEventFlag.ExtendedKey : 0);
        Time = 0;
        ExtraInfo = 0;
    }
    public KeyboardInput(char unicode, bool keyUp)
    {
        Vk = 0;
        Scan = unicode;
        Flags = KeyboardEventFlag.Unicode
            | (keyUp ? KeyboardEventFlag.KeyUp : 0);
        Time = 0;
        ExtraInfo = 0;
    }

    [SupportedOSPlatform("windows")]
    [LibraryImport("user32", EntryPoint = "keybd_event")]
    private static partial void KeyboardEvent(byte bVk, byte bScan, KeyboardEventFlag dwFlags, nint dwExtraInfo);

    /// <summary>
    /// Calls <c>keybd_event</c>. Recommended to use <see cref="Input.Send"/> instead of this.
    /// </summary>
    /// <remarks>
    /// When using <see cref="KeyboardEventFlag.Unicode" /> may not produce correct results
    /// because the value is truncated to <see cref="byte" />.
    /// </remarks>
    [SupportedOSPlatform("windows")]
    public readonly void SendEvent()
    {
        KeyboardEvent((byte)Vk, (byte)Scan, Flags, ExtraInfo);
    }
}