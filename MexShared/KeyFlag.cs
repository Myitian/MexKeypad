namespace MexShared;

// [KeyUp][Unused][Type ]
// [1 bit][4 bit ][3 bit]
[Flags]
public enum KeyFlag : sbyte
{
    Unicode = 0,
    VirtualKey = 1,
    ScanCode = 2,
    VirtualKeyWithScanCode = 3,
    Mouse = 4,
    // 5,6,7: Unassigned
    KeyUp = unchecked((sbyte)(byte)0b10000000),
    Reversed = unchecked((sbyte)(byte)0b11111111)
}