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
2. Open its config on the plugin page, edit it and press **Save** at least once. Until then the panel writes no file and the plugin runs no groups (it logs a warning), even though the form shows the example groups.
3. The panel writes `addons/swiftlys2/configs/plugins/EA_Adverts/config.jsonc` and restarts the server.

Configs stack like other plugins: the plugin page, then the game mode, then the server. Lists are replaced, not merged, so a `Groups` list set on a game mode or a server replaces the whole list from the plugin page.

The registry builds the version list from this repository's GitHub releases, so a new tag (`v2.0.1`) is enough for a code change. A change to `5stack/ea-adverts.json` (form or defaults) has to be copied into the registry repository as well.

## Install by hand

1. Download `EA_Adverts-vX.Y.Z.zip` from [Releases](https://github.com/EsportAdria/5stack-adverts-plugin/releases).
2. Extract it into `game/csgo/addons/swiftlys2/plugins/` (the zip holds an `EA_Adverts/` folder).
3. Start the server once. The plugin writes `addons/swiftlys2/configs/plugins/EA_Adverts/config.jsonc` with the default settings and no groups; add groups like the example below.

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
        "name": "rules",
        "Enabled": true,
        "IntervalSeconds": 120,      // whole seconds, 5 to 86400
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
        "name": "community",
        "Enabled": true,
        "IntervalSeconds": 300,
        "Order": "random",
        "DisplayType": "center",
        "Target": "dead",
        "SkipPermissions": ["vip.ads.skip"],
        "Messages": [
          "[blue]You are playing on [yellow]{server_name}[blue], {player_count}/{max_players} on {map}."
        ]
      }
    ]
  }
}
```

Keys are not case sensitive. The panel's JSON view expects every group field shown above; the plugin itself fills missing ones with the defaults (enabled, 60 s, sequential, chat, all). The config is read at load, on every map change and on `sw_adverts_reload`. If the file can't be read on a map change or `sw_adverts_reload` (a JSON typo in a hand edit), the plugin logs an error and keeps the previous config. At server start there is no previous config, so a broken file stops the plugin from loading and SwiftlyS2 logs the error. Files the panel writes are always valid JSON.

Problems are logged and skipped instead of stopping the plugin:

- a group with no messages or an unknown display type is skipped;
- a group with a value of the wrong type (a decimal interval, `"Enabled": "yes"`) is skipped;
- an unknown target falls back to `all`, an unknown order to `sequential`;
- an interval under 5 seconds is raised to 5, one over 86400 lowered to 86400;
- a group without a name is called `group1`, `group2` and so on; a repeated name is skipped;
- an unknown time zone (or a server image without tz data) falls back to the server clock;
- a config with no groups, or a leftover 1.x `Main` section, is logged.

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
- `Groups` is a list with a `name` field instead of an object keyed by name, because the panel's form edits lists.
- The plugin does not convert the old file. It logs a warning while a `Main` section is still there.
- Release zips hold an `EA_Adverts/` folder instead of loose files.

## Building

```bash
dotnet test test/EA_Adverts.Tests.csproj
dotnet publish EA_Adverts.csproj -c Release -o publish/EA_Adverts
```

## License

All rights reserved. EsportAdria.
