using MexKeypad.Platforms.Windows.Win32;
using MexShared;
using System.Runtime.InteropServices;

namespace MexKeypad.Platforms.Windows;

public static partial class Win32Handler
{
    [LibraryImport("user32", SetLastError = true)]
    private static partial nint SetWindowLongPtrW(nint hWnd, int nIndex, nint dwNewLong);
    [LibraryImport("user32", SetLastError = true)]
    private static partial nint GetWindowLongPtrW(nint hWnd, int nIndex);
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    public static void SetWindow(Window? window, bool toolWindow, bool noActivate)
    {
        const int GWL_EXSTYLE = -20;
        const nint WS_EX_TOOLWINDOW = 0x00000080;
        const nint WS_EX_APPWINDOW = 0x00040000;
        const nint WS_EX_NOACTIVATE = 0x08000000;
        const nint HWND_TOPMOST = -1;
        const nint HWND_NOTOPMOST = -2;
        const uint SWP_NOSIZE = 1;
        const uint SWP_NOMOVE = 2;

        if ((window?.Handler?.PlatformView as MauiWinUIWindow)?.WindowHandle is not nint hWnd)
            return;
        nint currentExStyle = GetWindowLongPtrW(hWnd, GWL_EXSTYLE) & ~(WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
        if (toolWindow)
            currentExStyle |= WS_EX_TOOLWINDOW;
        if (noActivate)
            currentExStyle |= WS_EX_NOACTIVATE;
        SetWindowLongPtrW(hWnd, GWL_EXSTYLE, currentExStyle | WS_EX_APPWINDOW);
        SetWindowPos(hWnd, noActivate ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
    }

    public static void HandleKeys(bool? keyUp, params ReadOnlySpan<KeyInfo> keys)
    {
        Span<Input> inputs = stackalloc Input[keys.Length];
        int i = 0;
        while (i < keys.Length)
        {
            KeyInfo ki = keys[i];
            if (ki.Flag > (KeyboardEventFlag)0xF)
                continue;
            else if (ki.Flag.HasFlag(KeyboardEventFlag.Unicode))
                inputs[i] = new KeyboardInput((char)ki.Value, keyUp ?? ki.Flag.HasFlag(KeyboardEventFlag.KeyUp));
            else if (ki.Flag.HasFlag(KeyboardEventFlag.ScanCode))
                inputs[i] = new KeyboardInput(ki.Value, keyUp ?? ki.Flag.HasFlag(KeyboardEventFlag.KeyUp));
            else
                inputs[i] = new KeyboardInput((VirtualKey)ki.Value, keyUp ?? ki.Flag.HasFlag(KeyboardEventFlag.KeyUp));
            i++;
        }
        Input.Send(inputs[..i]);
    }
}