# WeavyTaskbar

Dynamic visual effects rendered on the Windows taskbar background. Adds flowing animations beneath your taskbar icons while keeping everything fully clickable.

## How It Works

WeavyTaskbar creates a transparent overlay window behind the taskbar and renders animated visuals (waves, clouds, grass, fish, etc.) that show through the taskbar's transparent background. Mouse clicks pass through to your icons and system tray.

- **Windows 10**: Taskbar background is made near-transparent via DWM accent
- **Windows 11**: Same approach with adjusted transparency parameters

## Quick Start

1. Download `WeavyTaskbar.zip` from [Releases](../../releases)
2. Extract to any folder
3. Double-click `WeavyTaskbar.exe`
4. Right-click the tray icon to switch styles, adjust speed, or exit

## Built-in Styles

| Style | Description |
|-------|-------------|
| CloudDrift | Drifting fluffy clouds across a blue sky gradient |
| RainbowGradient | Flowing rainbow color sweep |
| WaveShore | Waves lapping onto a sandy beach with starfish |
| SeaFish | School of colorful fish swimming through deep water |
| StarShip | Spaceships cruising through a starfield |
| WindGrass | Three-layer swaying grass field |

## Customization

Each style is a standalone `.cs` file in the `Styles/` folder. Edit any style file with a text editor, restart the app, and your changes take effect immediately. No compilation needed.

To create a new style, copy an existing `.cs` file and modify the `Render(Bitmap, float time, int alpha, float speed)` method.

## Controls

- **Left-click tray icon** — Toggle effect on/off
- **Right-click tray icon** — Change style, set speed (0.5x–3x), enable startup, exit

## Build from Source

Requires .NET Framework 4.0+ (included with Windows 7+).

```
build.bat
```

Outputs `WeavyTaskbar.exe` at the project root.

## License

MIT
