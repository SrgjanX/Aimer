namespace Aimer.Core.Packs;

public enum PointerShape
{
    Arrow,
    Scope,
    BowArrow,
    Sword,
    Lightsaber,
    Rocket,
}

/// <summary>
/// A pointer-style cursor with the click point at its tip, top-left
/// (or at the center, for the scope). The main part is drawn
/// in <see cref="Color"/>, details in shades of it. Outline is in pixels at the 32px base size.
/// </summary>
public sealed record PointerStyle
{
    public PointerShape Shape { get; init; } = PointerShape.Sword;
    public CursorColor Color { get; init; } = new(0, 255, 0);
    public CursorColor OutlineColor { get; init; } = new(0, 0, 0);
    public double Outline { get; init; } = 1;
}
