# SwiftlyS2 Adverts Plugin — Design Spec

## Overview

A CS2 server plugin built on SwiftlyS2 that sends configurable periodic messages to players. Messages are organized into groups, each with independent intervals, display types, targeting rules, and ordering.

## Architecture

**Approach A: Monolithic Plugin.** Single `BasePlugin` class with config models and two small helper classes. No DI, no service layer — scope doesn't warrant it.

## Config Model

Single JSONC config file. Top-level `Main` section with a global prefix and a `Groups` dictionary.

```jsonc
{
  "Main": {
    "Prefix": "[Server]",
    "PrefixColor": "red",
    "Groups": {
      "rules": {
        "Enabled": true,
        "IntervalSeconds": 120,
        "Order": "sequential",
        "DisplayType": "chat",
        "Target": "all",
        "SkipPermissions": ["vip.ads.skip"],
        "Messages": [
          "Please follow the server rules. Type !rules for more info.",
          "Cheating results in a permanent ban.",
          "Be respectful to other players."
        ]
      },
      "promotions": {
        "Enabled": true,
        "IntervalSeconds": 300,
        "Order": "random",
        "DisplayType": "chat",
        "Target": "all",
        "SkipPermissions": [],
        "Messages": [
          "Join our Discord: discord.gg/example",
          "Visit our website: example.com"
        ]
      }
    }
  }
}
```

### Config Fields

**Global:**
| Field | Type | Description |
|-------|------|-------------|
| `Prefix` | string | Text prepended to chat messages |
| `PrefixColor` | string | Named color token for the prefix (e.g. `"red"`, `"green"`) |

**Per Group:**
| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `Enabled` | bool | `true` | Whether the group is active |
| `IntervalSeconds` | int | `60` | Seconds between messages |
| `Order` | string | `"sequential"` | `"sequential"` or `"random"` |
| `DisplayType` | string | `"chat"` | `"chat"`, `"center"`, `"center_html"`, `"alert"` |
| `Target` | string | `"all"` | `"all"`, `"alive"`, `"dead"`, `"t"`, `"ct"` |
| `SkipPermissions` | string[] | `[]` | Players with any of these permissions skip this group |
| `Messages` | string[] | required | List of message texts |

### Message Variables

Replaced at send time:
- `{server_name}` — server hostname
- `{map}` — current map name
- `{player_count}` — current player count
- `{max_players}` — max player slots
- `{time}` — current server time

Color codes via SwiftlyS2 formatting use **square brackets**: `[red]`, `[green]`, `[blue]`, `[yellow]`, `[default]`, etc.

## Plugin Core & Timers

### Lifecycle

1. `Load(bool hotReload)`:
   - If `hotReload == true`, cancel all existing timers before recreating
   - Initialize config via `Core.Configuration.InitializeJsonWithModel<AdvertsConfig>("config.jsonc", "Main")`
   - Register attribute-based commands via `Core.Registrator.Register(this)`
   - For each enabled group, create a timer via `Core.Scheduler.DelayAndRepeatBySeconds(interval, interval, callback)`
   - Register each timer for map-change cleanup via `Core.Scheduler.StopOnMapChange(token)`
   - Store timer tokens in `Dictionary<string, CancellationTokenSource>`

2. `Unload()`:
   - Cancel all active timers via stored `CancellationTokenSource` tokens
   - Clear the timer dictionary and group state

### Timer Callback (per group)

1. Determine next message (sequential index tracked per group, or random pick)
2. Replace variables with live server values via `MessageFormatter`
3. Get valid, non-bot players via `Core.PlayerManager.GetAllValidPlayers()`, filtering out `IsFakeClient`
4. Filter players by group `Target` via `PlayerFilter`
5. Exclude players matching any `SkipPermissions` via `Core.Permission.PlayerHasPermissions(player.SteamID, [perm])`
6. Send to remaining players via the appropriate method based on `DisplayType`:
   - `chat` → `player.SendChat(prefixedMessage)`
   - `center` → `player.SendCenter(message)`
   - `center_html` → `player.SendCenterHTML(message)`
   - `alert` → `player.SendAlert(message)`

### Thread Safety

`Scheduler.DelayAndRepeatBySeconds` callbacks run on the game tick (main thread), so player messaging methods are safe to call directly without `NextTick` or `Async` variants.

## Player Filtering

`PlayerFilter` is a static helper with one method:

```
FilterPlayers(IEnumerable<IPlayer> players, string target, List<string> skipPermissions, IPermission permService) → IEnumerable<IPlayer>
```

**Target filter logic** (null-check `player.Controller` and `player.Pawn` before access):
- `"all"` — no filter
- `"alive"` — `player.Pawn?.LifeState == alive` (verify exact property at implementation)
- `"dead"` — inverse of alive check
- `"t"` — `player.Pawn?.TeamNum == 2` (or `player.Controller.Team == Team.Terrorist`)
- `"ct"` — `player.Pawn?.TeamNum == 3` (or `player.Controller.Team == Team.CT`)

**Skip permissions:** After target filter, exclude any player where `Core.Permission.PlayerHasPermissions(player.SteamID, [perm])` returns true for any permission in `SkipPermissions`.

## Message Formatting

`MessageFormatter` is a static helper with one method:

```
FormatMessage(string template, string prefix, string prefixColor, string displayType, ISwiftlyCore core) → string
```

1. Replace `{server_name}`, `{map}`, `{player_count}`, `{max_players}`, `{time}` with live values
   - `{server_name}` → `Core.ConVar.Find<string>("hostname")?.Value`
   - `{map}` → current map name (from map load event or engine API)
   - `{player_count}` → `Core.PlayerManager.GetAllValidPlayers().Count(p => !p.IsFakeClient)`
   - `{max_players}` → `Core.ConVar.Find<int>("sv_maxplayers")?.Value`
   - `{time}` → `DateTime.Now.ToString("HH:mm")`
2. If `displayType == "chat"`, prepend `[{prefixColor}]{prefix}[default] ` to the message
3. Return formatted string

## Admin Commands

All commands registered via `[Command("name", permission: "perm.string")]` attribute. Permission checked via SwiftlyS2's built-in permission parameter.

| Command | Permission | Description |
|---------|-----------|-------------|
| `sw_adverts_reload` | `admins.adverts.reload` | Cancel all timers, re-read config, recreate timers |
| `sw_adverts_list` | `admins.adverts.list` | Reply with all groups: name, enabled, interval, message count |
| `sw_adverts_toggle <group>` | `admins.adverts.toggle` | Enable/disable group at runtime (cancel/create timer). Not persisted to config — a reload resets toggle state. |
| `sw_adverts_preview <group> [index]` | `admins.adverts.preview` | Send a specific message (or next in sequence) to the command sender only, using the group's display type |

## File Layout

```
swiftlys2-adverts/
  src/
    AdvertsPlugin.cs          # BasePlugin: Load(), timers, commands
    Models/
      AdvertsConfig.cs        # Top-level config model
      AdvertGroup.cs          # Single group config model
    Helpers/
      MessageFormatter.cs     # Variable replacement + prefix
      PlayerFilter.cs         # Target + SkipPermissions filtering
  resources/
    templates/
      config.template.jsonc   # Default config shipped with plugin
  swiftlys2-adverts.csproj    # .NET 10.0, SwiftlyS2.CS2 NuGet reference
```

## Build & Deploy

1. `dotnet new swplugin -n "swiftlys2-adverts" --PluginName "Adverts" --PluginAuthor "EsportAdria" --PluginVersion "1.0.0"`
2. `dotnet publish` → outputs `build/publish/swiftlys2-adverts.zip`
3. Extract to server's `addons/swiftlys2/plugins/` directory
4. Config auto-generated on first load at plugin's config path

## Error Handling

- Invalid group config (missing messages, bad display type) → log warning via `Core.Logger`, skip that group
- Unknown target value → default to `"all"`, log warning
- Empty messages list → skip group, log warning
- Toggle/preview for nonexistent group → reply with error message to admin
- Map change → timers auto-cancelled via `StopOnMapChange`. Plugin re-registers timers from config on new map load via `OnMapLoad` event listener.
