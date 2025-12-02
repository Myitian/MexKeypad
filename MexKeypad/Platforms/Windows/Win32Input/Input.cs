using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MexKeypad.Platforms.Windows.Win32Input;

[StructLayout(LayoutKind.Sequential)]
public partial struct Input
{
    public InputType Type;
    public Union U;
    [StructLayout(LayoutKind.Explicit)]
    public struct Union
    {
        [FieldOffset(0)]
        public MouseInput MI;
        [FieldOffset(0)]
        public KeyboardInput KI;
        [FieldOffset(0)]
        public HardwareInput HI;

        public static implicit operator Union(MouseInput mi) => new() { MI = mi };
        public static implicit operator Union(KeyboardInput ki) => new() { KI = ki };
        public static implicit operator Union(HardwareInput hi) => new() { HI = hi };
        public static explicit operator MouseInput(Union u) => u.MI;
        public static explicit operator KeyboardInput(Union u) => u.KI;
        public static explicit operator HardwareInput(Union u) => u.HI;
    }
    public static readonly int Size = Unsafe.SizeOf<Input>();

    public Input(MouseInput mi)
    {
        Type = InputType.Mouse;
        U.MI = mi;
    }
    public Input(KeyboardInput ki)
    {
        Type = InputType.Keyboard;
        U.KI = ki;
    }
    public Input(HardwareInput hi)
    {
        Type = InputType.Hardware;
        U.HI = hi;
    }

    [LibraryImport("user32", SetLastError = true)]
    private static partial int SendInput(int cInputs, scoped ReadOnlySpan<Input> pInputs, int cbSize);

    public static int Send(params scoped ReadOnlySpan<Input> inputs)
    {
        return SendInput(inputs.Length, inputs, Size);
    }

    public static implicit operator Input(MouseInput mi) => new(mi);
    public static implicit operator Input(KeyboardInput ki) => new(ki);
    public static implicit operator Input(HardwareInput hi) => new(hi);
    public static explicit operator MouseInput(Input input) => input.U.MI;
    public static explicit operator KeyboardInput(Input input) => input.U.KI;
    public static explicit operator HardwareInput(Input input) => input.U.HI;
}