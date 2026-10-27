using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Aimer.Core;
using Aimer.Core.Packs;
using Aimer.Linux;
using Aimer.Windows;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aimer.App.ViewModels;

public sealed record ColorOption(string Name, CursorColor Color);

public enum CursorShape
{
    Crosshair,
    Arrow,
    Scope,
    Dot,
    Square,
    Triangle,
    BowArrow,
    Sword,
    Lightsaber,
    Rocket,
}

public partial class MainViewModel : ViewModelBase
{
    private const int PreviewSize = 128;
    private static readonly int[] CursorSizes = [32, 48, 64, 96, 128];

    private readonly ICursorApplier? _applier = CreateApplier();

    /// <summary>The crosshair-drawn shapes each keep their own slider settings; outline is shared by all shapes.</summary>
    private readonly Dictionary<CursorShape, CrosshairStyle> _crosshairSettings = new()
    {
        [CursorShape.Crosshair] = new CrosshairStyle(),
        [CursorShape.Scope] = CrosshairRenderer.Scope,
        [CursorShape.Dot] = CrosshairRenderer.Dot,
        [CursorShape.Square] = CrosshairRenderer.Square,
        [CursorShape.Triangle] = CrosshairRenderer.Triangle,
    };

    public MainViewModel()
    {
        SelectedColor = Colors[0];
        UpdatePreview();
    }

    public IReadOnlyList<CursorShape> Shapes { get; } = Enum.GetValues<CursorShape>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCrosshair), nameof(HasArms), nameof(IsScope), nameof(ShowDotSize))]
    public partial CursorShape SelectedShape { get; set; }

    /// <summary>True for the shapes drawn by the crosshair renderer: crosshair, scope, dot, square and triangle.</summary>
    public bool IsCrosshair => _crosshairSettings.ContainsKey(SelectedShape);

    /// <summary>Length, gap, thickness and the center dot toggle only apply to the crosshair and the scope.</summary>
    public bool HasArms => IsCrosshair && !IsBare(SelectedShape);

    /// <summary>The ring settings only apply to the scope.</summary>
    public bool IsScope => SelectedShape == CursorShape.Scope;

    /// <summary>The bare shapes are always just their mark; the others only have a dot when the center dot is on.</summary>
    public bool ShowDotSize => IsBare(SelectedShape) || (HasArms && CenterDot);

    public IReadOnlyList<ColorOption> Colors { get; } =
    [
        new("Green", new(0, 255, 0)),
        new("Cyan", new(0, 255, 255)),
        new("Red", new(255, 40, 40)),
        new("Yellow", new(255, 230, 0)),
        new("Magenta", new(255, 0, 255)),
        new("White", new(255, 255, 255)),
    ];

    [ObservableProperty]
    public partial ColorOption SelectedColor { get; set; }

    [ObservableProperty]
    public partial double Length { get; set; } = 6;

    [ObservableProperty]
    public partial double Gap { get; set; } = 3;

    [ObservableProperty]
    public partial double Thickness { get; set; } = 2;

    [ObservableProperty]
    public partial double Outline { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDotSize))]
    public partial bool CenterDot { get; set; }

    [ObservableProperty]
    public partial double DotSize { get; set; } = 2;

    [ObservableProperty]
    public partial double RingRadius { get; set; }

    [ObservableProperty]
    public partial double RingThickness { get; set; } = 1;

    [ObservableProperty]
    public partial Bitmap? Preview { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; } = "";

    [RelayCommand]
    private void Apply()
    {
        Run(() =>
        {
            var name = $"Aimer {SelectedShape}";
            _applier!.Apply(IsCrosshair
                ? CrosshairRenderer.RenderTheme(BuildStyle(), name, CursorSizes)
                : PointerRenderer.RenderTheme(BuildPointerStyle(), name, CursorSizes));
            return $"{SelectedShape} applied.";
        });
    }

    [RelayCommand]
    private void Restore()
    {
        Run(() =>
        {
            _applier!.RestoreDefaults();
            return "Original cursors restored.";
        });
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is not (nameof(Preview) or nameof(Status) or nameof(IsCrosshair) or nameof(HasArms) or nameof(IsScope) or nameof(ShowDotSize)))
            UpdatePreview();
    }

    partial void OnSelectedShapeChanged(CursorShape oldValue, CursorShape newValue)
    {
        if (_crosshairSettings.ContainsKey(oldValue))
            _crosshairSettings[oldValue] = BuildStyle(oldValue);
        if (!_crosshairSettings.TryGetValue(newValue, out var style))
            return;

        Length = style.Length;
        Gap = style.Gap;
        Thickness = style.Thickness;
        CenterDot = style.CenterDot;
        DotSize = style.DotSize;
        RingRadius = style.RingRadius;
        RingThickness = style.RingThickness;
    }

    private void Run(Func<string> action)
    {
        if (_applier is null)
        {
            Status = "This operating system is not supported yet.";
            return;
        }

        try
        {
            Status = action();
        }
        catch (Exception ex)
        {
            Status = $"Failed: {ex.Message}";
        }
    }

    /// <summary>The shapes that are a lone mark on the aim point, with no arms.</summary>
    private static bool IsBare(CursorShape shape) => shape is CursorShape.Dot or CursorShape.Square or CursorShape.Triangle;

    private CrosshairStyle BuildStyle() => BuildStyle(SelectedShape);

    /// <summary>
    /// The current slider settings as drawn for <paramref name="shape"/>. Hidden sliders clamp
    /// their values to their own ranges, so settings a shape does not show are overridden here.
    /// </summary>
    private CrosshairStyle BuildStyle(CursorShape shape) => new()
    {
        Color = SelectedColor.Color,
        Arms = !IsBare(shape),
        Length = Length,
        Gap = Gap,
        Thickness = Thickness,
        Outline = Outline,
        CenterDot = IsBare(shape) || CenterDot,
        DotSize = DotSize,
        DotShape = shape switch
        {
            CursorShape.Dot => DotShape.Round,
            CursorShape.Triangle => DotShape.Triangle,
            _ => DotShape.Square,
        },
        RingRadius = shape == CursorShape.Scope ? RingRadius : 0,
        RingThickness = RingThickness,
    };

    private PointerStyle BuildPointerStyle() => new()
    {
        Shape = SelectedShape switch
        {
            CursorShape.Arrow => PointerShape.Arrow,
            CursorShape.BowArrow => PointerShape.BowArrow,
            CursorShape.Lightsaber => PointerShape.Lightsaber,
            CursorShape.Rocket => PointerShape.Rocket,
            _ => PointerShape.Sword,
        },
        Color = SelectedColor.Color,
        Outline = Outline,
    };

    private void UpdatePreview()
    {
        if (SelectedColor is null)
            return;
        Preview = ToBitmap(IsCrosshair
            ? CrosshairRenderer.Render(BuildStyle(), PreviewSize)
            : PointerRenderer.Render(BuildPointerStyle(), PreviewSize));
    }

    private static Bitmap ToBitmap(CursorImage image)
    {
        var bitmap = new WriteableBitmap(
            new PixelSize(image.Width, image.Height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
        using var buffer = bitmap.Lock();
        var rowBytes = image.Width * 4;
        for (var y = 0; y < image.Height; y++)
            Marshal.Copy(image.Pixels, y * rowBytes, buffer.Address + y * buffer.RowBytes, rowBytes);
        return bitmap;
    }

    private static ICursorApplier? CreateApplier()
    {
        if (OperatingSystem.IsWindows())
            return new WindowsCursorApplier();
        if (OperatingSystem.IsLinux())
            return new LinuxCursorApplier();
        return null;
    }
}
