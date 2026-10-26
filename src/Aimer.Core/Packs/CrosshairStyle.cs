namespace Aimer.Core.Packs;

/// <summary>
/// A classic four-arm crosshair, optionally with a ring around it (a scope).
/// Lengths are in pixels at the 32px base size and scale up with larger cursor sizes.
/// </summary>
public sealed record CrosshairStyle
{
    public CursorColor Color { get; init; } = new(0, 255, 0);
    public CursorColor OutlineColor { get; init; } = new(0, 0, 0);
    public double Length { get; init; } = 6;
    public double Gap { get; init; } = 3;
    public double Thickness { get; init; } = 2;
    public double Outline { get; init; } = 1;
    public bool CenterDot { get; init; }
    public double DotSize { get; init; } = 2;

    /// <summary>Radius of the ring through the arms, from the center to the middle of its line; 0 for none.</summary>
    public double RingRadius { get; init; }
    public double RingThickness { get; init; } = 1;
}
