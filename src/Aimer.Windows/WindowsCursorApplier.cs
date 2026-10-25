using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using Aimer.Core;
using Aimer.Core.Formats;
using Microsoft.Win32;

namespace Aimer.Windows;

/// <summary>
/// Applies themes by writing .cur files and pointing HKCU\Control Panel\Cursors at them,
/// then asking Windows to reload. Takes effect immediately, no admin rights needed.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsCursorApplier : ICursorApplier
{
    private const string CursorsKeyPath = @"Control Panel\Cursors";
    private const uint SPI_SETCURSORS = 0x0057;
    private const uint SPIF_UPDATEINIFILE = 0x01;
    private const uint SPIF_SENDCHANGE = 0x02;

    private readonly string _dataDir;

    public WindowsCursorApplier(string? dataDir = null)
    {
        _dataDir = dataDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aimer");
    }

    private string CursorsDir => Path.Combine(_dataDir, "Cursors");
    private string BackupPath => Path.Combine(_dataDir, "original-cursors.json");

    public void Apply(CursorTheme theme)
    {
        using var key = OpenCursorsKey();
        BackupOriginalScheme(key);

        Directory.CreateDirectory(CursorsDir);
        DeleteOldCursorFiles();

        // A fresh file name per apply, so Windows never serves a cached copy of the previous one.
        var stamp = DateTime.UtcNow.Ticks;
        foreach (var (role, images) in theme.Cursors)
        {
            var path = Path.Combine(CursorsDir, $"{role}-{stamp}.cur");
            using (var file = File.Create(path))
                CurWriter.Write(file, images);
            key.SetValue(role.ToString(), path, RegistryValueKind.ExpandString);
        }
        key.SetValue("", theme.Name);

        ReloadSystemCursors();
    }

    public void RestoreDefaults()
    {
        using var key = OpenCursorsKey();

        if (File.Exists(BackupPath))
        {
            var backup = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(BackupPath)) ?? [];
            foreach (var (name, value) in backup)
                key.SetValue(name, value, RegistryValueKind.ExpandString);
            File.Delete(BackupPath);
        }
        else
        {
            // Empty values make Windows fall back to its built-in cursors.
            foreach (var role in Enum.GetValues<CursorRole>())
                key.SetValue(role.ToString(), "", RegistryValueKind.ExpandString);
            key.SetValue("", "");
        }

        ReloadSystemCursors();
        DeleteOldCursorFiles();
    }

    private void BackupOriginalScheme(RegistryKey key)
    {
        // Only the first apply is backed up; later applies would just save Aimer's own files.
        if (File.Exists(BackupPath))
            return;

        var backup = new Dictionary<string, string>
        {
            [""] = key.GetValue("", "", RegistryValueOptions.DoNotExpandEnvironmentNames) as string ?? "",
        };
        foreach (var role in Enum.GetValues<CursorRole>())
        {
            var name = role.ToString();
            backup[name] = key.GetValue(name, "", RegistryValueOptions.DoNotExpandEnvironmentNames) as string ?? "";
        }

        Directory.CreateDirectory(_dataDir);
        File.WriteAllText(BackupPath, JsonSerializer.Serialize(backup));
    }

    private void DeleteOldCursorFiles()
    {
        if (!Directory.Exists(CursorsDir))
            return;

        foreach (var file in Directory.EnumerateFiles(CursorsDir, "*.cur"))
        {
            try { File.Delete(file); }
            catch (IOException) { } // still in use by Windows; cleaned up next time
            catch (UnauthorizedAccessException) { }
        }
    }

    private static RegistryKey OpenCursorsKey() =>
        Registry.CurrentUser.OpenSubKey(CursorsKeyPath, writable: true)
        ?? Registry.CurrentUser.CreateSubKey(CursorsKeyPath);

    private static void ReloadSystemCursors()
    {
        if (!SystemParametersInfo(SPI_SETCURSORS, 0, IntPtr.Zero, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE))
        {
            var error = Marshal.GetLastWin32Error();
            if (error != 0)
                throw new Win32Exception(error);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);
}
