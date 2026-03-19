<div align="center">
  <img src="https://pan.samyyc.dev/s/VYmMXE" />
  <h2><strong>EA Adverts</strong></h2>
  <h3>Configurable periodic advertisement messages for your CS2 server.</h3>
</div>

<p align="center">
  <img src="https://img.shields.io/badge/build-passing-brightgreen" alt="Build Status">
  <img src="https://img.shields.io/github/downloads/EsportAdria/SwiftlyS2-EA_Adverts/total" alt="Downloads">
  <img src="https://img.shields.io/github/stars/EsportAdria/SwiftlyS2-EA_Adverts?style=flat&logo=github" alt="Stars">
</p>

## Features

- **Message groups** with independent intervals, display types, and targeting
- **Display types:** chat, center HUD, center HTML, alert
- **Player targeting:** all, alive, dead, T-side, CT-side
- **Permission-based skip:** hide ads from VIPs or specific groups
- **Message ordering:** sequential or random per group
- **Variables:** `{server_name}`, `{map}`, `{player_count}`, `{max_players}`, `{time}`
- **Color codes:** SwiftlyS2 color tokens via square brackets (`[red]`, `[green]`, etc.)
- **Admin commands:** reload, list, toggle, preview
- **Hot-reload support**

## Requirements

- [SwiftlyS2](https://github.com/swiftly-solution/swiftlys2) v1.2.1+
- .NET 10.0 runtime (included with SwiftlyS2 `-with-runtimes` builds)

## Installation

1. Download the latest release from [Releases](https://github.com/EsportAdria/SwiftlyS2-EA_Adverts/releases)
2. Extract the zip into your server's `game/csgo/addons/swiftlys2/plugins/` directory
3. Restart the server or hot-reload the plugin
4. Edit the generated `config.jsonc` to configure your messages

## Configuration

The plugin creates a `config.jsonc` file on first load. Example:

```jsonc
{
  "Main": {
    // Prefix prepended to chat messages
    "Prefix": "[Server]",
    // Named color token for the prefix (e.g. "red", "green", "blue")
    "PrefixColor": "red",
    "Groups": {
      "rules": {
        "Enabled": true,
        "IntervalSeconds": 120,
        "Order": "sequential",       // "sequential" or "random"
        "DisplayType": "chat",        // "chat", "center", "center_html", "alert"
        "Target": "all",              // "all", "alive", "dead", "t", "ct"
        "SkipPermissions": [],        // players with these permissions skip this group
        "Messages": [
          "[green]Please follow the server rules. Type [yellow]!rules[green] for info.",
          "[red]Cheating results in a permanent ban.",
          "[green]Be respectful to other players."
        ]
      },
      "promotions": {
        "Enabled": true,
        "IntervalSeconds": 300,
        "Order": "random",
        "DisplayType": "chat",
        "Target": "all",
        "SkipPermissions": ["vip.ads.skip"],
        "Messages": [
          "[blue]Join our Discord: [yellow]discord.gg/example",
          "[blue]Visit our website: [yellow]example.com"
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
| `PrefixColor` | string | Color token for the prefix (`red`, `green`, `blue`, etc.) |

**Per Group:**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `Enabled` | bool | `true` | Whether the group is active |
| `IntervalSeconds` | int | `60` | Seconds between messages |
| `Order` | string | `"sequential"` | `"sequential"` or `"random"` |
| `DisplayType` | string | `"chat"` | `"chat"`, `"center"`, `"center_html"`, `"alert"` |
| `Target` | string | `"all"` | `"all"`, `"alive"`, `"dead"`, `"t"`, `"ct"` |
| `SkipPermissions` | string[] | `[]` | Players with any of these permissions won't see messages |
| `Messages` | string[] | *required* | List of message texts |

### Variables

Use these placeholders in messages — they're replaced with live values at send time:

| Variable | Description |
|----------|-------------|
| `{server_name}` | Server hostname |
| `{map}` | Current map name |
| `{player_count}` | Current player count |
| `{max_players}` | Max player slots |
| `{time}` | Current server time (HH:mm) |

## Admin Commands

All commands require the corresponding permission. SwiftlyS2 auto-prefixes with `sw_`.

| Command | Permission | Description |
|---------|-----------|-------------|
| `sw_adverts_reload` | `admins.adverts.reload` | Reload config and restart all timers |
| `sw_adverts_list` | `admins.adverts.list` | List all groups with status and settings |
| `sw_adverts_toggle <group>` | `admins.adverts.toggle` | Enable/disable a group at runtime (not persisted) |
| `sw_adverts_preview <group> [index]` | `admins.adverts.preview` | Preview a message from a group |

## Building from Source

```bash
git clone https://github.com/EsportAdria/SwiftlyS2-EA_Adverts.git
cd EA_Adverts
dotnet publish --configuration Release
```

Output will be in `bin/Release/net10.0/publish/`.

## License

All rights reserved. EsportAdria.
