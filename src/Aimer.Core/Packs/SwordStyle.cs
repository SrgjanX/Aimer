namespace Aimer.Core.Packs;

/// <summary>
/// A sword pointing up-left with the click point at its tip. The hilt is drawn
/// in a darker shade of the blade color. Outline is in pixels at the 32px base size.
/// </summary>
public sealed record SwordStyle
{
    public CursorColor Color { get; init; } = new(0, 255, 0);
    public CursorColor OutlineColor { get; init; } = new(0, 0, 0);
    public double Outline { get; init; } = 1;
}
