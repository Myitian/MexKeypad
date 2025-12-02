namespace MexKeypad.Platforms.Windows.Win32Input;

[Flags]
public enum KeyboardEventFlag
{
    ExtendedKey = 0x01,
    KeyUp = 0x02,
    Unicode = 0x04,
    ScanCode = 0x08
}