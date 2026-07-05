# JASM — Linux / Avalonia fork (project notes)

This repo is a **Linux-native fork** of JASM (Just Another Skin Manager), a 3Dmigoto / XXMI
skin-mod manager for HoYo-style games. The WinUI 3 (Windows-only) frontend was rewritten in
**Avalonia 11** so it runs natively on Linux with no Wine. Lineage:
Jorixon/JASM → Moonholder/JASM (added games + zh-cn) → this fork (`Loong0x00/JASM`, branch
`linux-avalonia-port`). Upstream is near-dormant (Jorixon last code 2026-01, Moonholder 2026-04),
so this fork is effectively the active downstream — don't expect to rebase much from upstream.

## Layout
- `src/GIMI-ModManager.Core/` — cross-platform logic (net10.0), **reused as-is** + a few Linux patches.
- `src/GIMI-ModManager.Avalonia/` — the new UI (this fork). Replaces `GIMI-ModManager.WinUI`.
- `src/GIMI-ModManager.WinUI/Assets/Games/` — **the single source of truth for all game data**
  (character rosters, images, localization). The Avalonia csproj links this dir via
  `Content Include`, so editing it here updates both projects. Do NOT duplicate it.

## Build / run / test
```bash
cd src/GIMI-ModManager.Avalonia && dotnet run -c Release        # run
dotnet publish -c Release -r linux-x64 --self-contained false   # standalone ELF
dotnet test src/JASM.Tests                                       # 13 Linux tests, keep green
```
Settings live in `~/.local/share/JASM/`. ⚠️ Don't `pkill -f JASM.Avalonia` (matches your own
shell) — use `pkill -x JASM.Avalonia`.

## Game character data — how it's stored
Per game (`Genshin`, `Honkai` = Star Rail, `ZZZ`, `WuWa`, `Endfield`) under `Assets/Games/<Game>/`:
- `characters.json` — the roster. Array of objects, **2-space indent**, field order:
  `Keys, ReleaseDate, Image, Rarity, Element, Class, Region, ModFilesName, InGameSkins, InternalName, DisplayName`.
  New characters are **appended at the end** of the array.
- `Languages/zh-cn/characters.json` — parallel `{InternalName, DisplayName}` list (Chinese names).
  The first few entries are pseudo-categories (`Others`/`Weapons`/`Gliders`), so this list is a
  bit longer than `characters.json`. **Every character needs an entry in BOTH files.**
- `Images/Characters/<Image>` — the portrait referenced by the entry's `Image` field.
  `elements.json`, `weapons.json`, `weaponClasses.json`, `regions.json`, `game.json` define the
  valid enums per game (Element/Class/Region strings must match these).

### Field meanings
- `InternalName` — unique id; also the fallback display text. `DisplayName` — English name.
- `ModFilesName` — **drives mod auto-detection**: a mod folder is matched to a character when its
  name `StartsWith(ModFilesName)` (case-insensitive, `ModCrawlerService`). Get this right or mods
  won't auto-sort. Usually = the character's common name (often = `InternalName`).
- `Keys` — search aliases (tokens of the name). Not used for mod matching.
- `Element` = combat attribute (Genshin Vision / HSR Combat Type / ZZZ Attribute / WuWa Element /
  Endfield Element). `Class` = Genshin weapon / HSR Path / ZZZ Specialty / WuWa weapon / Endfield class.
- `Rarity` — 5 for top rarity (5★ / S-rank), 4 otherwise. `Region` — array of faction/region.
- `ReleaseDate` — `"YYYY-MM-DDT00:00:00"`. `InGameSkins` — usually `[]`.

### ⚠️ Linux case-sensitivity gotcha (already bit us)
`GetImageUri` (Core/`MapperHelpers.cs`) resolves the portrait by exact `Path.Combine + File.Exists`
— **no case-insensitive fallback**. On Windows `"rover.png"` matches `Rover.png`; on Linux it does
NOT, so the character renders with a blank image. When adding/fixing entries, the `Image` field must
match the actual filename's **case and extension exactly**. (Fixed 19 such WuWa entries on 2026-07-05.)
A missing image is otherwise graceful — Core logs a warning and the UI shows blank, no crash.

### Adding characters
1. Append the entry to `Assets/Games/<Game>/characters.json` (correct field order + 2-space indent).
2. Append `{InternalName, DisplayName:<中文名>}` to `Languages/zh-cn/characters.json`.
3. Drop a square portrait at `Images/Characters/<Image>` (or leave it — shows blank until added).
4. Element/Class/Region must be valid enum strings for that game. Rebuild and check the grid.

Today's roster data is community-maintained and can lag real releases; verify current rosters against
the game wikis (Fandom / Prydwen / Honey Hunter) — releases after the training cutoff must be
web-confirmed, not recalled from memory.
