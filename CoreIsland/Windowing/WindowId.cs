namespace CoreIsland.Windowing;

public readonly struct WindowId
{
    public uint Value { get; }

    internal WindowId(uint value) => Value = value;
}
