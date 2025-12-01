namespace MexShared;

[Flags]
public enum KeyboardEventFlag : byte
{
    ExtendedKey = 0x01,
    KeyUp = 0x02,
    Unicode = 0x04,
    ScanCode = 0x08
}