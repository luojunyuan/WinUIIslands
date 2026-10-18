namespace WinUIIslands.Windowing;

public readonly struct WindowId
{
    public ulong Value { get; }

    internal WindowId(ulong value) => Value = value;
}
