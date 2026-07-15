# HOWMANY

HOWMANY is a small Dalamud PvP HUD plugin for Final Fantasy XIV, built primarily for Crystalline Conflict. It shows a large, configurable number for the hostile players who currently have your character as their hard target, followed by one official in-game job icon per targeting opponent.

## Install

Add this URL under **Dalamud Settings → Experimental → Custom Plugin Repositories**:

```text
https://raw.githubusercontent.com/kittenhaswares-ui/HOWMANY/main/repo.json
```

Then search for **HOWMANY** in the Dalamud Plugin Installer.

## Use

Run `/howmany` to open the settings window. The overlay can be moved while unlocked and supports:

- number scale, job-icon size, spacing, and background opacity;
- lock and optional click-through mode;
- visibility, background, icons, and threat-color toggles;
- a temporary outside-PvP preview for positioning;
- optional display at Wolves' Den Pier.

Useful commands:

```text
/howmany
/howmany show
/howmany hide
/howmany lock
/howmany unlock
/howmany preview
/howmany reset
```

## What the number means

HOWMANY counts living hostile players currently loaded by your game client whose visible **hard target** is your character. It does not claim to detect soft targets, focus targets, mouse-over targets, AoE intent, enemies outside client range, or whether an opponent is actively pressing an attack.

The default layout is tuned for Crystalline Conflict's five-player enemy team. The overlay is enabled only in active PvP areas other than Wolves' Den Pier. Frontline remains supported as a bonus and wraps larger groups across multiple icon rows; Rival Wings should use the same game-provided hostile/target state but still needs broader live testing.

## Privacy

HOWMANY has no server and makes no network requests. It does not collect or publish character, account, combat-log, or identity data. Only local display settings are saved by Dalamud. See [PRIVACY.md](PRIVACY.md).

## Development

The plugin targets Dalamud API 15 and .NET 10. The dependency-free targeting rules have a small executable self-test:

```powershell
dotnet run --project tests/HOWMANY.Core.SelfTest -c Release
```

The actual plugin build requires a local Dalamud development installation:

```powershell
dotnet restore src/HOWMANY.Plugin/HOWMANY.Plugin.csproj --use-lock-file
dotnet build src/HOWMANY.Plugin/HOWMANY.Plugin.csproj -c Release --no-restore
```

## License

MIT. HOWMANY does not bundle FFXIV job artwork; icons are loaded from the user's local game data at runtime.
