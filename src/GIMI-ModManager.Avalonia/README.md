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

### Linux fixes made in Core

- **`Mod.Delete`**: the Windows recycle-bin API (`Microsoft.VisualBasic.FileIO`) throws on Linux.
  `Delete` now moves mods to the **freedesktop.org XDG Trash** on non-Windows platforms (still
  recoverable), falling back to a permanent delete only if the trash is unusable.
- **`Mod.MoveTo`**: `Path.GetPathRoot` can't detect a cross-mount move on Linux (every path shares
  `/`), so `MoveTo` now catches the cross-device `IOException` and falls back to copy-then-delete.
  This fixes importing mods extracted under `/tmp` onto a data disk.
- **`ArchiveService.ExtractArchive`**: selected the extractor by the *destination folder's* extension
  instead of the archive's, so the SharpCompress path (used off Windows) silently extracted nothing.
  On Windows this was masked by the bundled `7z.exe`. Now selects by the archive extension.
- **`CharacterModList` FileSystemWatchers**: each character created inotify watchers that threw if
  `fs.inotify.max_user_instances` was exhausted (common on Linux with a large roster). Watcher
  creation now degrades gracefully — live folder-watching is skipped when the limit is hit, and the
  manual Refresh button covers updates. Prevents startup crashes on constrained systems.

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

Implemented:

- **First-time setup** — pick game + Mods folder (XXMI/3Dmigoto folder optional on Linux)
- **5 games** — Genshin (GIMI), Star Rail (SRMI), ZZZ (ZZMI), Wuthering Waves (WWMI), Endfield (EFMI);
  in-app game switching (restarts to re-init the Core, matching the WinUI build)
- **Character grid** — category tabs (character/weapon/NPC/object), element & class filters, sort,
  search, mod-count badges, pin-to-top, hide/show characters, images + localized names
- **Character details** — two-pane (mod list + detail pane) like the original:
  - enable/disable (folder `DISABLED_` rename), multi-select batch enable/disable/delete/move,
    per-mod search, add mod (folder), install from archive (.zip/.rar/.7z), delete-to-trash,
    open-folder, refresh, hide character
  - **ModPane**: cover image (view + set), edit name/author/url/description, **key-swap editor**,
    copy path
- **Mod installer** — from a folder or an archive, with a target-character picker (also global from
  the grid)
- **Presets** — create-from-current, apply, rename, duplicate, delete, read-only
- **Mods overview** — flat list of every mod across characters, search, jump-to-character
- **Settings** — switch game, change folders, export all mods, theme (system/light/dark),
  language (中文/English)

Deliberately deferred (heavy/niche, kept out to avoid over-building):

- GameBanana in-app browser + auto mod-update checking (large, network-heavy)
- Custom character creation UI (the "Others" catch-all category already handles unlisted entities)
- App auto-updater, custom command runner, dedicated notifications-history page
- Windows-only concerns that don't apply on Linux: UAC elevator/password manager, launching the
  game/3Dmigoto `.exe` (games run via Proton on Linux)

All the above features are covered by an xUnit integration suite (`JASM.Tests`) that runs on Linux:
mod toggle, delete-to-trash, archive install, preset round-trip, export, and all-5-games loading.
