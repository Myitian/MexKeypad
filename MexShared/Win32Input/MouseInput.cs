using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace MexShared.Win32Input;

[StructLayout(LayoutKind.Sequential)]
public partial struct MouseInput
{
    public int Dx;
    public int Dy;
    public int MouseData;
    public MouseEventFlag Flags;
    public uint Time;
    public nint ExtraInfo;

    [SupportedOSPlatform("windows")]
    [LibraryImport("user32", EntryPoint = "mouse_event")]
    private static partial void MouseEvent(MouseEventFlag dwFlags, int dx, int dy, int dwData, nint dwExtraInfo);

    /// <summary>
    /// Calls <c>mouse_event</c>. Recommended to use <see cref="Input.Send"/> instead of this.
    /// </summary>
    /// <remarks>
    /// When using <see cref="KeyboardEventFlag.Unicode" /> may not produce correct results
    /// because the value is truncated to <see cref="byte" />.
    /// </remarks>
    [SupportedOSPlatform("windows")]
    public readonly void SendEvent()
    {
        MouseEvent(Flags, Dx, Dy, MouseData, ExtraInfo);
    }
}