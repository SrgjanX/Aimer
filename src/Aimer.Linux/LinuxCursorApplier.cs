using System.Runtime.Versioning;
using Aimer.Core;

namespace Aimer.Linux;

/// <summary>
/// Placeholder. The plan: write an Xcursor theme to ~/.icons/&lt;name&gt;/cursors,
/// then select it with the desktop's own tool (gsettings on GNOME,
/// plasma-apply-cursortheme on KDE, XCURSOR_THEME elsewhere).
/// </summary>
[SupportedOSPlatform("linux")]
public sealed class LinuxCursorApplier : ICursorApplier
{
    public void Apply(CursorTheme theme) =>
        throw new PlatformNotSupportedException("Linux support is not implemented yet.");

    public void RestoreDefaults() =>
        throw new PlatformNotSupportedException("Linux support is not implemented yet.");
}
