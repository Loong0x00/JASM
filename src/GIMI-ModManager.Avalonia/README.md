# JASM — Avalonia (Linux-native) UI

A cross-platform rewrite of JASM's frontend using **[Avalonia UI](https://avaloniaui.net/) 11**
so it runs **natively on Linux with no Wine**. The original UI (`GIMI-ModManager.WinUI`) is
WinUI 3 / WindowsAppSDK and only runs on Windows; this project replaces that presentation layer
while reusing the existing cross-platform logic library (`GIMI-ModManager.Core`) unchanged.

## Why this exists

JASM manages 3Dmigoto / XXMI skin mods (Genshin, Star Rail, Wuthering Waves, Zenless Zone Zero,
Arknights: Endfield). It is a *skin manager*, not an injector — it organizes mod folders and toggles
them on/off. That work is pure file I/O, so it ports cleanly to Linux; only the WinUI shell was
Windows-locked.

## Architecture

```
GIMI-ModManager.Core        cross-platform logic (unchanged, net10.0)  ── reused as-is
GIMI-ModManager.Avalonia    the new UI (this project)                  ── replaces the WinUI app
```

The port keeps the same MVVM idiom (CommunityToolkit.Mvvm) but writes fresh, lean ViewModels that
call Core directly, rather than porting the WinUI ViewModels with shims. Windows-only shell services
are re-implemented against Avalonia:

| Concern            | WinUI                          | Avalonia replacement (this project)            |
|--------------------|--------------------------------|------------------------------------------------|
| Fluent controls    | WindowsAppSDK / WinUI 3        | Avalonia + **FluentAvalonia**                  |
| Navigation         | `Frame` / `NavigationView`     | `Services/Navigation/NavigationService`        |
| Dialogs            | `ContentDialog` + `XamlRoot`   | `Services/Dialogs/DialogService` (FluentAvalonia ContentDialog) |
| File/folder picker | WinAppSDK Storage pickers      | `Services/FilePickerService` (Avalonia `IStorageProvider`) |
| Notifications      | custom WinUI manager           | `Services/NotificationService` (`WindowNotificationManager`) |
| Settings store     | plain JSON (portable)          | `Services/Settings/LocalSettingsService` (ported verbatim) |
| Theme              | `ElementTheme`                 | Avalonia `ThemeVariant`                        |
| Open folder / URL  | `Launcher.LaunchFolderAsync`   | `Services/PlatformService` (`xdg-open`)        |

### Linux fixes made in Core (`Entities/Mod.cs`)

- **Delete**: the Windows recycle-bin API (`Microsoft.VisualBasic.FileIO`) throws on Linux. `Delete`
  now moves mods to the **freedesktop.org XDG Trash** on non-Windows platforms (still recoverable),
  falling back to a permanent delete only if the trash is unusable.
- **MoveTo**: `Path.GetPathRoot` can't detect a cross-mount move on Linux (every path shares `/`), so
  `MoveTo` now catches the cross-device `IOException` and falls back to copy-then-delete. This fixes
  importing mods extracted under `/tmp` onto a data disk.

## Build & run

Requires the **.NET 10 SDK**.

```bash
# from src/GIMI-ModManager.Avalonia
dotnet run -c Release

# or produce a distributable folder
dotnet publish -c Release -r linux-x64 --self-contained false -o out
./out/JASM.Avalonia
```

Settings live in `~/.local/share/JASM/` (same layout as the Windows build). Game asset data
(character JSON + images) ships under `Assets/Games/` next to the executable.

## Status

Implemented (the core mod-management loop):

- First-time setup (pick game + Mods folder; XXMI/3Dmigoto folder optional on Linux)
- Per-game character/weapon/object grid with images, Chinese names, mod-count badges, search
- Per-character mod list with **enable/disable** (folder `DISABLED_` rename), add-mod (folder import),
  delete-to-trash, open-folder, refresh
- Settings: switch game, change folders, theme (system/light/dark)

Not yet ported (future work): GameBanana in-app browser, auto-updater, mod presets, keyswap editor,
command runner, per-mod image/metadata editing. These were out of scope for the initial Linux MVP.
