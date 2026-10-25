namespace Aimer.Core;

/// <summary>Installs a theme as the system cursor on one operating system.</summary>
public interface ICursorApplier
{
    void Apply(CursorTheme theme);

    /// <summary>Puts back the cursors the user had before Aimer changed them.</summary>
    void RestoreDefaults();
}
