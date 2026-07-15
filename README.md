# HOWMANY

HOWMANY is a small Dalamud PvP HUD plugin for Final Fantasy XIV, built primarily for Crystalline Conflict. It shows a large, configurable number for opponents who are focusing or actively pressuring your character, followed by one official in-game job icon per opponent.

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
- an adjustable recent-pressure duration for Crystalline Conflict.

Useful commands:

```text
/howmany
/howmany show
/howmany hide
/howmany lock
/howmany unlock
/howmany preview
/howmany debug
/howmany reset
```

## What the number means

HOWMANY combines two local signals:

- visible hostile players whose native hard target or active cast target is your character; and
- visible opponents whose harmful action affected your character within the configured recent-pressure duration.

The pressure fallback recognizes damage, blocks, parries, misses, full resists, and invulnerability/Guard outcomes. It ignores healing and buffs. This is necessary because Crystalline Conflict does not consistently expose opponents' current hard-target state to the client. An AoE can therefore keep an opponent visible briefly even when their hard target is another player. Soft targets, focus targets, mouse-over targets, and enemies outside client range remain undetectable.

The default layout and three-second pressure duration are tuned for Crystalline Conflict's five-player enemy team. The overlay is enabled in every active PvP duty other than Wolves' Den Pier. Frontline remains supported as a bonus and wraps larger groups across multiple icon rows; Rival Wings uses the same detection path but still needs broader live testing. Run `/howmany debug` for an anonymous live diagnostic line if the overlay behaves unexpectedly.

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
