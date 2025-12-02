using System.Runtime.InteropServices;

namespace MexKeypad.Platforms.Windows.Win32Input;

[StructLayout(LayoutKind.Sequential)]
public partial struct MouseInput
{
    public int Dx;
    public int Dy;
    public int MouseData;
    public MouseEventFlag Flags;
    public uint Time;
    public nint ExtraInfo;

    [LibraryImport("user32", EntryPoint = "mouse_event")]
    private static partial void MouseEvent(MouseEventFlag dwFlags, int dx, int dy, int dwData, nint dwExtraInfo);

    /// <summary>
    /// Calls <c>mouse_event</c>. Recommended to use <see cref="Input.Send"/> instead of this.
    /// </summary>
    /// <remarks>
    /// When using <see cref="KeyboardEventFlag.Unicode" /> may not produce correct results
    /// because the value is truncated to <see cref="byte" />.
    /// </remarks>
    public readonly void SendEvent()
    {
        MouseEvent(Flags, Dx, Dy, MouseData, ExtraInfo);
    }
}