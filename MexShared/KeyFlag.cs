namespace MexShared;

[Flags]
public enum KeyFlag : byte
{
    Unicode = 0x0,
    VirtualKey = 0x1,
    ScanCode = 0x2,
    VirtualKeyWithScanCode = 0x3,
    Mouse = 0x4,
    KeyUp = 0x8
}