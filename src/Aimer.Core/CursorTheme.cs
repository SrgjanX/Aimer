namespace Aimer.Core;

/// <summary>
/// A named set of cursors. Each role holds the same design at several sizes so the
/// OS can pick a sharp one for the current DPI and cursor-size setting.
/// Roles that are missing are left as they are.
/// </summary>
public sealed record CursorTheme(string Name, IReadOnlyDictionary<CursorRole, IReadOnlyList<CursorImage>> Cursors);
