# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Aimer is a desktop tray app (Avalonia, .NET 10) that renders custom system cursors — crosshairs, a scope, dots, and themed pointers (sword, lightsaber, rocket…) — and installs them as the OS cursor. Windows is implemented; Linux is a stub.

## Commands

Run from the repo root (where `Aimer.sln` lives; the VS Code workspace opens `src/`, so `cd ..` first if needed).

```sh
dotnet build Aimer.sln
dotnet run --project src/Aimer.App
dotnet test tests/Aimer.Core.Tests
dotnet test tests/Aimer.Core.Tests --filter "FullyQualifiedName~CrosshairRendererTests.HotspotIsCentred"
```

Running the app and pressing Apply changes the real system cursor (HKCU registry). Use "Restore defaults" (window or tray menu) to undo it. Closing the window only hides it to the tray; quit from the tray's Exit item.

## Architecture

Projects and how they depend on each other:

- **Aimer.Core**: has no platform or UI dependencies. It holds the model, the renderers and the file format writer, and it is the only project with tests.
  - `CursorImage`: a BGRA bitmap with **straight (unpremultiplied) alpha**, rows top-down, plus a hotspot. All drawing goes through `SetPixel`/`Blend`/`FillRect`, and `CursorImage.Over` does alpha compositing.
  - `CursorTheme`: maps each `CursorRole` to several sizes of the same design, so the OS can pick one per DPI. A role missing from the theme keeps its current cursor. Both renderers replace only `Arrow` and `Crosshair`.
  - `Packs/CrosshairRenderer` + `CrosshairStyle`: arms are drawn as pixel-exact rects. The ring and round/triangle dots are anti-aliased with 4×4 supersampling. The outline is drawn first as an enlarged version of each shape, then the fill goes on top. The presets (`Scope`, `Dot`, `Square`, `Triangle`) are static `CrosshairStyle` instances.
  - `Packs/PointerRenderer` + `PointerStyle`: each shape is a list of `Part`s built from signed-distance functions (`Polygon`, `Box`, `Circle`, `Capsule`). Parts are laid out in a (u, v) frame rotated 45° from the tip: u runs toward the bottom-right, v across it. `Xy(...)` converts plain x/y points into that frame. Later parts draw over earlier ones. Detail colors come from the style color through `Shade`/`Tint`.
  - Style lengths are in pixels **at a 32px base size**, and both renderers scale them by `size / 32`.
  - `Formats/CurWriter`: writes multi-image Windows `.cur` files (32-bit DIBs, bottom-up rows, an AND mask derived from alpha).
- **Aimer.Windows**: `WindowsCursorApplier` writes `.cur` files to `%LOCALAPPDATA%\Aimer\Cursors` with a fresh timestamped name on every apply, which stops Windows from serving a cached file. It points `HKCU\Control Panel\Cursors` values (named after `CursorRole`) at those files and calls `SystemParametersInfo(SPI_SETCURSORS)`. No admin rights are needed. The user's original scheme is backed up to `original-cursors.json` **only on the first apply**, and `RestoreDefaults` puts it back from that backup.
- **Aimer.Linux**: `LinuxCursorApplier` throws `PlatformNotSupportedException`. Its doc comment describes the planned Xcursor approach.
- **Aimer.App**: Avalonia + CommunityToolkit.Mvvm. A single `MainViewModel` is shared by the main window and the tray menu (wired up in `App.axaml.cs`), so tray "Apply" uses the current settings. `CreateApplier()` picks the applier by OS at runtime.

### MainViewModel shape model

The UI-level `CursorShape` enum is separate from the Core enums and maps onto them:

- Crosshair, Scope, Dot, Square, Triangle → `CrosshairRenderer`. `IsBare` marks Dot, Square and Triangle as arm-less.
- Arrow, BowArrow, Sword, Lightsaber, Rocket → `PointerRenderer` (`PointerShape`).

Each crosshair-drawn shape keeps its own slider values in `_crosshairSettings`. They are saved and reloaded in `OnSelectedShapeChanged`. Outline and color are shared by all shapes. `BuildStyle(shape)` overrides values the current shape doesn't show, because hidden sliders clamp to their own ranges. Any property change other than the derived/visibility properties regenerates the 128px preview, and that exclusion list lives in `OnPropertyChanged`.

To add a shape, touch: the Core style/renderer (a preset or a `Shapes` entry), `CursorShape`, the mapping in `BuildStyle`/`BuildPointerStyle`, and, for a crosshair-type shape, `_crosshairSettings` and `IsBare`. Slider visibility in `MainWindow.axaml` is bound to `HasArms`, `IsScope` and `ShowDotSize`.

## Conventions

- Comments explain *why* or describe geometry, in plain sentences, and public members carry `<summary>` docs. Match this density.
- Tests (xUnit) check exact pixel colors/alpha at computed coordinates. When changing rendering geometry, update the coordinate comments in the tests along with the expected values.
