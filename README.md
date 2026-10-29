<div align="center">
  <h2><strong>EA Adverts</strong></h2>
  <h3>Timed server messages for CS2 (SwiftlyS2), configurable from the 5stack panel.</h3>
</div>

## Features

- **Message groups**, each with its own interval, order, display type and targeting
- **Display types:** chat, center HUD, center HTML, alert
- **Targeting:** all, alive, dead, T, CT
- **Permission skip:** hide a group from VIPs or any SwiftlyS2 permission
- **Order:** sequential, or random without the same message twice in a row
- **Placeholders:** `{server_name}`, `{map}`, `{player_count}`, `{max_players}`, `{time}`, `{date}`
- **Chat colours:** SwiftlyS2 tokens such as `[red]`, `[green]`, `[default]`
- **Admin commands:** reload, list, toggle, preview, and one-off announcements (`sw_adverts_say`)
- **5stack panel:** the config is edited as a form on the panel's plugin page

## Requirements

- [SwiftlyS2](https://github.com/swiftly-solution/swiftlys2) 1.5.0 or newer
- .NET 10 runtime (part of the SwiftlyS2 `-with-runtimes` builds)

## Install on 5stack

The plugin is listed in a plugin registry as `ea-adverts` (entry: [`5stack/ea-adverts.json`](5stack/ea-adverts.json)).

1. In the panel, open **Plugins**, find **EA Adverts** and install it.
2. Edit the config on the plugin page. Settings stack the same way as other plugins: the plugin page, then the game mode, then the server's own config.
3. The panel writes `addons/swiftlys2/configs/plugins/EA_Adverts/config.jsonc` and restarts the server.

The registry builds the version list from this repository's GitHub releases, so a new tag (`v2.0.1`) is all a release needs.

## Install by hand

1. Download `EA_Adverts-vX.Y.Z.zip` from [Releases](https://github.com/EsportAdria/EA_Adverts-SwiftlyS2/releases).
2. Extract it into `game/csgo/addons/swiftlys2/plugins/` (the zip holds an `EA_Adverts/` folder).
3. Start the server once. The plugin writes an empty `addons/swiftlys2/configs/plugins/EA_Adverts/config.jsonc`; fill it in like the example below.

## Configuration

```jsonc
{
  "Adverts": {
    "Prefix": "[EA]",                // put in front of chat messages, "" for none
    "PrefixColor": "red",            // a SwiftlyS2 colour token without brackets
    "TimeZone": "Europe/Zagreb",     // IANA name for {time} and {date}, "" for the server clock
    "TimeFormat": "HH:mm",
    "DateFormat": "dd.MM.yyyy",
    "Groups": [
      {
        "Name": "rules",
        "Enabled": true,
        "IntervalSeconds": 120,      // 5 or more
        "Order": "sequential",       // "sequential" or "random"
        "DisplayType": "chat",       // "chat", "center", "center_html", "alert"
        "Target": "all",             // "all", "alive", "dead", "t", "ct"
        "SkipPermissions": [],       // players with any of these permissions skip the group
        "Messages": [
          "[green]Be respectful to other players.",
          "[red]Cheating results in a permanent ban."
        ]
      },
      {
        "Name": "community",
        "IntervalSeconds": 300,
        "Order": "random",
        "SkipPermissions": ["vip.ads.skip"],
        "Messages": [
          "[blue]You are playing on [yellow]{server_name}[blue], {player_count}/{max_players} on {map}."
        ]
      }
    ]
  }
}
```

The config is read at load, on every map change and on `sw_adverts_reload`.

Problems are logged and skipped instead of stopping the plugin:

- a group with no messages or an unknown display type is skipped;
- an unknown target falls back to `all`, an unknown order to `sequential`;
- an interval under 5 seconds is raised to 5;
- a group without a name is called `group1`, `group2` and so on; a repeated name is skipped;
- an unknown time zone (or a server image without tz data) falls back to the server clock.

### Placeholders

| Placeholder | Value |
|---|---|
| `{server_name}` | `hostname` cvar |
| `{map}` | current map |
| `{player_count}` | human players connected |
| `{max_players}` | server slots |
| `{time}` | time in `TimeZone`, formatted with `TimeFormat` |
| `{date}` | date in `TimeZone`, formatted with `DateFormat` |

## Admin commands

SwiftlyS2 adds the `sw_` prefix. A player needs the listed permission to run a command.

| Command | Permission | What it does |
|---|---|---|
| `sw_adverts_reload` | `admins.adverts.reload` | Reread the config and restart all timers |
| `sw_adverts_list` | `admins.adverts.list` | List groups with their state and settings |
| `sw_adverts_toggle <group>` | `admins.adverts.toggle` | Stop or start a group until the next map or reload |
| `sw_adverts_preview <group> [index]` | `admins.adverts.preview` | Show one message of a group to yourself (or print it in the console) |
| `sw_adverts_say <chat\|center\|center_html\|alert> <text>` | `admins.adverts.say` | Send a one-off message to every player, for example `sw_adverts_say center "Restart in 5 minutes"` |

## Upgrading from 1.x

- The config section is now `Adverts` (was `Main`).
- `Groups` is a list with a `Name` field instead of an object keyed by name, because the panel's form edits lists.
- Release zips hold an `EA_Adverts/` folder instead of loose files.

## Building

```bash
dotnet test test/EA_Adverts.Tests.csproj
dotnet publish EA_Adverts.csproj -c Release -o publish/EA_Adverts
```

## License

All rights reserved. EsportAdria.
