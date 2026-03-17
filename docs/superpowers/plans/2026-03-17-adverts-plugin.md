# Adverts Plugin Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a SwiftlyS2 CS2 plugin that sends configurable periodic advertisement messages to players, with group-based scheduling, player targeting, and admin commands.

**Architecture:** Monolithic plugin (Approach A). Single `BasePlugin` class with config models (`AdvertsConfig`, `AdvertGroup`) and two static helpers (`MessageFormatter`, `PlayerFilter`). No DI or service layer.

**Tech Stack:** C# / .NET 10.0, SwiftlyS2.CS2 NuGet package, JSONC config

**Spec:** `docs/superpowers/specs/2026-03-17-adverts-plugin-design.md`

**Testing note:** This is a game server plugin — the SwiftlyS2 runtime is only available inside a running CS2 server. There are no unit test facilities. Verification is done via `dotnet publish` (compilation) and manual server testing. Each task ends with a build check instead of a test run.

---

## File Structure

| File | Responsibility |
|------|---------------|
| `swiftlys2-adverts.csproj` | .NET 10.0 project, SwiftlyS2.CS2 NuGet reference |
| `.gitignore` | Exclude bin/, obj/, build/, IDE files |
| `src/Models/AdvertGroup.cs` | Config model for a single advert group |
| `src/Models/AdvertsConfig.cs` | Top-level config model (prefix + groups dictionary) |
| `src/Helpers/MessageFormatter.cs` | Variable replacement (`{map}`, `{player_count}`, etc.) and prefix prepending |
| `src/Helpers/PlayerFilter.cs` | Target filtering (all/alive/dead/t/ct) + SkipPermissions exclusion |
| `src/AdvertsPlugin.cs` | `BasePlugin` entry point: lifecycle, timer management, admin commands |
| `resources/templates/config.template.jsonc` | Default config shipped with the plugin |

---

## Chunk 1: Project Setup & Config Models

### Task 1: Scaffold the project

**Files:**
- Create: `swiftlys2-adverts.csproj`
- Create: `.gitignore`

- [ ] **Step 1: Create .gitignore**

```
bin/
obj/
build/
.vs/
*.user
*.suo
```

- [ ] **Step 2: Create the .csproj file**

We skip `dotnet new swplugin` since we're structuring manually. Create the project file:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>SwiftlyS2.Adverts</RootNamespace>
    <AssemblyName>swiftlys2-adverts</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="SwiftlyS2.CS2" Version="1.2.1" ExcludeAssets="runtime" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Create directory structure**

```bash
mkdir -p src/Models src/Helpers resources/templates
```

- [ ] **Step 4: Verify project restores**

Run: `dotnet restore`
Expected: Restore succeeds, SwiftlyS2.CS2 package downloaded.

- [ ] **Step 5: Commit**

```bash
git add .gitignore swiftlys2-adverts.csproj
git commit -m "chore: scaffold project with SwiftlyS2.CS2 dependency"
```

---

### Task 2: AdvertGroup config model

**Files:**
- Create: `src/Models/AdvertGroup.cs`

- [ ] **Step 1: Write the AdvertGroup model**

```csharp
namespace SwiftlyS2.Adverts.Models;

public class AdvertGroup
{
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 60;
    public string Order { get; set; } = "sequential";
    public string DisplayType { get; set; } = "chat";
    public string Target { get; set; } = "all";
    public List<string> SkipPermissions { get; set; } = new();
    public List<string> Messages { get; set; } = new();
}
```

- [ ] **Step 2: Build check**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/Models/AdvertGroup.cs
git commit -m "feat: add AdvertGroup config model"
```

---

### Task 3: AdvertsConfig top-level model

**Files:**
- Create: `src/Models/AdvertsConfig.cs`

- [ ] **Step 1: Write the AdvertsConfig model**

```csharp
namespace SwiftlyS2.Adverts.Models;

public class AdvertsConfig
{
    public string Prefix { get; set; } = "[Server]";
    public string PrefixColor { get; set; } = "red";
    public Dictionary<string, AdvertGroup> Groups { get; set; } = new();
}
```

- [ ] **Step 2: Build check**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/Models/AdvertsConfig.cs
git commit -m "feat: add AdvertsConfig top-level config model"
```

---

### Task 4: Default config template

**Files:**
- Create: `resources/templates/config.template.jsonc`

- [ ] **Step 1: Write the default config template**

```jsonc
{
  // SwiftlyS2 Adverts Plugin Configuration
  "Main": {
    // Prefix prepended to chat messages
    "Prefix": "[Server]",
    // Named color token for the prefix (e.g. "red", "green", "blue")
    "PrefixColor": "red",
    "Groups": {
      "rules": {
        "Enabled": true,
        "IntervalSeconds": 120,
        // "sequential" or "random"
        "Order": "sequential",
        // "chat", "center", "center_html", "alert"
        "DisplayType": "chat",
        // "all", "alive", "dead", "t", "ct"
        "Target": "all",
        // Players with any of these permissions won't see messages from this group
        "SkipPermissions": [],
        "Messages": [
          "Welcome to the server! Type !rules to see the rules.",
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
        "SkipPermissions": ["vip.ads.skip"],
        "Messages": [
          "Join our community Discord: discord.gg/example",
          "Visit our website: example.com"
        ]
      }
    }
  }
}
```

- [ ] **Step 2: Commit**

```bash
git add resources/templates/config.template.jsonc
git commit -m "feat: add default config template"
```

---

## Chunk 2: Helper Classes

### Task 5: MessageFormatter helper

**Files:**
- Create: `src/Helpers/MessageFormatter.cs`

- [ ] **Step 1: Write MessageFormatter**

```csharp
using SwiftlyS2.API;

namespace SwiftlyS2.Adverts.Helpers;

public static class MessageFormatter
{
    public static string FormatMessage(
        string template,
        string prefix,
        string prefixColor,
        string displayType,
        ISwiftlyCore core,
        string currentMap)
    {
        var message = template;

        // Replace variables with live values
        message = message.Replace("{server_name}",
            core.ConVar.Find<string>("hostname")?.Value ?? "Unknown");
        message = message.Replace("{map}", currentMap);
        message = message.Replace("{player_count}",
            core.PlayerManager.GetAllValidPlayers()
                .Count(p => !p.IsFakeClient).ToString());
        message = message.Replace("{max_players}",
            core.ConVar.Find<int>("sv_maxplayers")?.Value.ToString() ?? "0");
        message = message.Replace("{time}",
            DateTime.Now.ToString("HH:mm"));

        // Prepend prefix for chat display type
        if (displayType == "chat" && !string.IsNullOrEmpty(prefix))
        {
            message = $"[{prefixColor}]{prefix}[default] {message}";
        }

        return message;
    }
}
```

**Note:** The map name is cached in the plugin from the `OnMapLoad` event and passed in as `currentMap`, avoiding reliance on a convar that may not exist.

- [ ] **Step 2: Build check**

Run: `dotnet build`
Expected: Build succeeds. If `ISwiftlyCore` API methods don't match exactly, adjust to match the SDK's actual types.

- [ ] **Step 3: Commit**

```bash
git add src/Helpers/MessageFormatter.cs
git commit -m "feat: add MessageFormatter for variable replacement and prefix"
```

---

### Task 6: PlayerFilter helper

**Files:**
- Create: `src/Helpers/PlayerFilter.cs`

- [ ] **Step 1: Write PlayerFilter**

```csharp
using SwiftlyS2.API;

namespace SwiftlyS2.Adverts.Helpers;

public static class PlayerFilter
{
    /// <summary>
    /// Returns true if the target string is a known value.
    /// Unknown targets return false — caller should log a warning.
    /// </summary>
    public static bool IsValidTarget(string target)
    {
        return target.ToLowerInvariant() is "all" or "alive" or "dead" or "t" or "ct";
    }

    public static IEnumerable<IPlayer> FilterPlayers(
        IEnumerable<IPlayer> players,
        string target,
        List<string> skipPermissions,
        IPermission permService)
    {
        // Filter out players with null Pawn (not fully connected)
        var validPlayers = players.Where(p => p.Pawn != null);

        var filtered = target.ToLowerInvariant() switch
        {
            "alive" => validPlayers.Where(p => p.Pawn!.LifeState == 0),
            "dead" => validPlayers.Where(p => p.Pawn!.LifeState != 0),
            "t" => validPlayers.Where(p => p.Pawn!.TeamNum == 2),
            "ct" => validPlayers.Where(p => p.Pawn!.TeamNum == 3),
            _ => validPlayers // "all" or unknown defaults to all
        };

        // Exclude players with skip permissions
        if (skipPermissions.Count > 0)
        {
            filtered = filtered.Where(p =>
                !skipPermissions.Any(perm =>
                    permService.PlayerHasPermissions(p.SteamID, new[] { perm })));
        }

        return filtered;
    }
}
```

**Note:** `LifeState == 0` means alive in Source 2. `TeamNum` 2 = T, 3 = CT. Verify exact property names and types against the SwiftlyS2 SDK at build time — the spec notes these need verification. If `Pawn` or `LifeState` don't exist on `IPlayer`, check `player.Controller` properties instead. The `IsValidTarget` method is used by the plugin at startup to validate and warn about unknown targets.

- [ ] **Step 2: Build check**

Run: `dotnet build`
Expected: Build succeeds. Adjust property paths if SDK types differ.

- [ ] **Step 3: Commit**

```bash
git add src/Helpers/PlayerFilter.cs
git commit -m "feat: add PlayerFilter for target and permission filtering"
```

---

## Chunk 3: Main Plugin Class

### Task 7: AdvertsPlugin — lifecycle and timer management

**Files:**
- Create: `src/AdvertsPlugin.cs`

- [ ] **Step 1: Write the plugin class with Load/Unload and timer setup**

**Note on command names:** SwiftlyS2 auto-prefixes commands registered via `[Command]` with `sw_`. So `[Command("adverts_reload")]` becomes `sw_adverts_reload` in-game. Do NOT include `sw_` in the attribute string.

```csharp
using System.Threading;
using SwiftlyS2.API;
using SwiftlyS2.Adverts.Helpers;
using SwiftlyS2.Adverts.Models;

namespace SwiftlyS2.Adverts;

public class AdvertsPlugin : BasePlugin
{
    public override string Name => "Adverts";
    public override string Author => "EsportAdria";
    public override string Version => "1.0.0";

    private static readonly string[] ValidDisplayTypes = { "chat", "center", "center_html", "alert" };

    private readonly Dictionary<string, CancellationTokenSource> _timers = new();
    private readonly Dictionary<string, int> _sequentialIndexes = new();
    private AdvertsConfig _config = new();
    private readonly Random _random = new();
    private string _currentMap = "Unknown";

    public AdvertsPlugin(ISwiftlyCore core) : base(core) { }

    public override void Load(bool hotReload)
    {
        if (hotReload)
        {
            StopAllTimers();
            _sequentialIndexes.Clear();
        }

        Core.Configuration.InitializeJsonWithModel<AdvertsConfig>("config.jsonc", "Main");
        LoadConfig();

        Core.Registrator.Register(this);

        StartAllTimers();
    }

    public override void Unload()
    {
        StopAllTimers();
        _sequentialIndexes.Clear();
    }

    private void LoadConfig()
    {
        // Primary: use Core.Configuration.Get<T>(section) to retrieve deserialized config.
        // If this method doesn't exist in the SDK, fallback: read and deserialize the
        // JSONC file manually from Core.PluginPath + "/config.jsonc".
        _config = Core.Configuration.Get<AdvertsConfig>("Main") ?? new AdvertsConfig();
    }

    private void StartAllTimers()
    {
        foreach (var (groupName, group) in _config.Groups)
        {
            if (!group.Enabled)
                continue;

            if (group.Messages.Count == 0)
            {
                Core.Logger.Warning($"[Adverts] Group '{groupName}' has no messages, skipping.");
                continue;
            }

            if (group.IntervalSeconds <= 0)
            {
                Core.Logger.Warning($"[Adverts] Group '{groupName}' has invalid interval ({group.IntervalSeconds}s), skipping.");
                continue;
            }

            if (!ValidDisplayTypes.Contains(group.DisplayType.ToLowerInvariant()))
            {
                Core.Logger.Warning($"[Adverts] Group '{groupName}' has unknown display type '{group.DisplayType}', skipping.");
                continue;
            }

            if (!PlayerFilter.IsValidTarget(group.Target))
            {
                Core.Logger.Warning($"[Adverts] Group '{groupName}' has unknown target '{group.Target}', defaulting to 'all'.");
            }

            if (!_sequentialIndexes.ContainsKey(groupName))
                _sequentialIndexes[groupName] = 0;

            var token = Core.Scheduler.DelayAndRepeatBySeconds(
                group.IntervalSeconds,
                group.IntervalSeconds,
                () => OnTimerTick(groupName, group));

            Core.Scheduler.StopOnMapChange(token);
            _timers[groupName] = token;
        }
    }

    private void StopAllTimers()
    {
        foreach (var token in _timers.Values)
        {
            token.Cancel();
        }
        _timers.Clear();
    }

    private void OnTimerTick(string groupName, AdvertGroup group)
    {
        // Pick next message
        string messageTemplate;
        if (group.Order == "random")
        {
            messageTemplate = group.Messages[_random.Next(group.Messages.Count)];
        }
        else
        {
            var index = _sequentialIndexes.GetValueOrDefault(groupName, 0);
            messageTemplate = group.Messages[index];
            _sequentialIndexes[groupName] = (index + 1) % group.Messages.Count;
        }

        // Format message with variables
        var message = MessageFormatter.FormatMessage(
            messageTemplate,
            _config.Prefix,
            _config.PrefixColor,
            group.DisplayType,
            Core,
            _currentMap);

        // Get and filter players
        var players = Core.PlayerManager.GetAllValidPlayers()
            .Where(p => !p.IsFakeClient);

        var targets = PlayerFilter.FilterPlayers(
            players,
            group.Target,
            group.SkipPermissions,
            Core.Permission);

        // Send to each target
        foreach (var player in targets)
        {
            SendToPlayer(player, message, group.DisplayType);
        }
    }

    private void SendToPlayer(IPlayer player, string message, string displayType)
    {
        switch (displayType.ToLowerInvariant())
        {
            case "chat":
                player.SendChat(message);
                break;
            case "center":
                player.SendCenter(message);
                break;
            case "center_html":
                player.SendCenterHTML(message);
                break;
            case "alert":
                player.SendAlert(message);
                break;
        }
    }
}
```

- [ ] **Step 2: Build check**

Run: `dotnet build`
Expected: Build succeeds. Fix any SDK type mismatches (method signatures, property names).

- [ ] **Step 3: Commit**

```bash
git add src/AdvertsPlugin.cs
git commit -m "feat: add AdvertsPlugin with lifecycle and timer management"
```

---

### Task 8: Admin commands

**Files:**
- Modify: `src/AdvertsPlugin.cs` (add command methods at the end of the class)

- [ ] **Step 1: Add reload command**

Add to `AdvertsPlugin.cs` before the closing brace:

```csharp
    [Command("adverts_reload", permission: "admins.adverts.reload")]
    public void OnReloadCommand(ICommandContext context)
    {
        StopAllTimers();
        LoadConfig();
        StartAllTimers();
        context.Reply("[Adverts] Configuration reloaded.");
    }
```

- [ ] **Step 2: Add list command**

```csharp
    [Command("adverts_list", permission: "admins.adverts.list")]
    public void OnListCommand(ICommandContext context)
    {
        if (_config.Groups.Count == 0)
        {
            context.Reply("[Adverts] No groups configured.");
            return;
        }

        foreach (var (name, group) in _config.Groups)
        {
            var configState = group.Enabled ? "enabled" : "disabled";
            var runtimeState = _timers.ContainsKey(name) ? "running" : "stopped";
            context.Reply($"[Adverts] {name}: config={configState} runtime={runtimeState} | interval: {group.IntervalSeconds}s | messages: {group.Messages.Count} | display: {group.DisplayType} | target: {group.Target}");
        }
    }
```

- [ ] **Step 3: Add toggle command**

```csharp
    [Command("adverts_toggle", permission: "admins.adverts.toggle")]
    public void OnToggleCommand(ICommandContext context)
    {
        var args = context.GetArgs();
        if (args.Count < 1)
        {
            context.Reply("[Adverts] Usage: sw_adverts_toggle <group>");
            return;
        }

        var groupName = args[0];
        if (!_config.Groups.TryGetValue(groupName, out var group))
        {
            context.Reply($"[Adverts] Group '{groupName}' not found.");
            return;
        }

        if (_timers.TryGetValue(groupName, out var token))
        {
            // Currently active — disable
            token.Cancel();
            _timers.Remove(groupName);
            context.Reply($"[Adverts] Group '{groupName}' disabled.");
        }
        else
        {
            // Currently inactive — enable
            if (group.Messages.Count == 0)
            {
                context.Reply($"[Adverts] Group '{groupName}' has no messages.");
                return;
            }

            var newToken = Core.Scheduler.DelayAndRepeatBySeconds(
                group.IntervalSeconds,
                group.IntervalSeconds,
                () => OnTimerTick(groupName, group));
            Core.Scheduler.StopOnMapChange(newToken);
            _timers[groupName] = newToken;
            context.Reply($"[Adverts] Group '{groupName}' enabled.");
        }
    }
```

- [ ] **Step 4: Add preview command**

```csharp
    [Command("adverts_preview", permission: "admins.adverts.preview")]
    public void OnPreviewCommand(ICommandContext context)
    {
        var args = context.GetArgs();
        if (args.Count < 1)
        {
            context.Reply("[Adverts] Usage: sw_adverts_preview <group> [index]");
            return;
        }

        var groupName = args[0];
        if (!_config.Groups.TryGetValue(groupName, out var group))
        {
            context.Reply($"[Adverts] Group '{groupName}' not found.");
            return;
        }

        if (group.Messages.Count == 0)
        {
            context.Reply($"[Adverts] Group '{groupName}' has no messages.");
            return;
        }

        int index;
        if (args.Count >= 2 && int.TryParse(args[1], out var parsed))
        {
            index = Math.Clamp(parsed, 0, group.Messages.Count - 1);
        }
        else
        {
            index = _sequentialIndexes.GetValueOrDefault(groupName, 0);
        }

        var message = MessageFormatter.FormatMessage(
            group.Messages[index],
            _config.Prefix,
            _config.PrefixColor,
            group.DisplayType,
            Core);

        if (!context.IsSentByPlayer)
        {
            context.Reply($"[Adverts] Preview ({group.DisplayType}): {message}");
            return;
        }

        var player = context.Sender!;
        SendToPlayer(player, message, group.DisplayType);
        context.Reply($"[Adverts] Previewing message {index} from group '{groupName}'.");
    }
```

- [ ] **Step 5: Build check**

Run: `dotnet build`
Expected: Build succeeds.

**Note:** `context.GetArgs()` is assumed — verify the actual SwiftlyS2 API for parsing command arguments. May need to parse `context.Args` string directly or use a different method.

- [ ] **Step 6: Commit**

```bash
git add src/AdvertsPlugin.cs
git commit -m "feat: add admin commands (reload, list, toggle, preview)"
```

---

## Chunk 4: Map Change Handling & Final Build

### Task 9: Map change re-registration

**Files:**
- Modify: `src/AdvertsPlugin.cs` (add event listener in Load)

- [ ] **Step 1: Add map change handler field to the plugin class**

Add this field alongside the other private fields in `AdvertsPlugin`:

```csharp
    private Action<IOnMapLoadEvent>? _onMapLoadHandler;
```

- [ ] **Step 2: Add OnMapLoad event listener in Load()**

In `Load()`, after `StartAllTimers()`, add:

```csharp
        _onMapLoadHandler = (e) =>
        {
            _currentMap = e.MapName; // cache map name for MessageFormatter
            StopAllTimers();
            _sequentialIndexes.Clear();
            StartAllTimers();
        };
        Core.Event.OnMapLoad += _onMapLoadHandler;
```

**Note:** Verify the exact event type name (`IOnMapLoadEvent`) and whether it has a `MapName` property. Adjust based on SDK types. If the event doesn't carry the map name, use `Core.ConVar.Find<string>("host_map")?.Value` as fallback.

- [ ] **Step 3: Unsubscribe in Unload()**

Update `Unload()` to:

```csharp
    public override void Unload()
    {
        if (_onMapLoadHandler != null)
        {
            Core.Event.OnMapLoad -= _onMapLoadHandler;
            _onMapLoadHandler = null;
        }
        StopAllTimers();
        _sequentialIndexes.Clear();
    }
```

This prevents handler accumulation on hot-reload.

- [ ] **Step 4: Build check**

Run: `dotnet build`
Expected: Build succeeds. If `OnMapLoad` doesn't exist, check for `OnMapStart` or similar event names in the SDK.

- [ ] **Step 5: Commit**

```bash
git add src/AdvertsPlugin.cs
git commit -m "feat: re-register timers on map change with proper cleanup"
```

---

### Task 10: Final build verification

- [ ] **Step 1: Full publish build**

Run: `dotnet publish`
Expected: Build succeeds, output in `build/publish/` or `bin/Release/net10.0/publish/`.

- [ ] **Step 2: Verify output files**

Run: `ls build/publish/` (or wherever publish outputs)
Expected: `swiftlys2-adverts.dll` and config template present.

- [ ] **Step 3: Commit any final adjustments**

If any files were adjusted, add them by name:

```bash
git add <specific-files>
git commit -m "chore: final build adjustments"
```

---

## Implementation Notes

- **Config loading (Task 7):** Uses `Core.Configuration.Get<AdvertsConfig>("Main")`. If this method doesn't exist in the SDK, fallback: read and deserialize the JSONC file manually from `Core.PluginPath + "/config.jsonc"` using `System.Text.Json`.
- **Command argument parsing (Task 8):** `context.GetArgs()` is assumed. If the SDK uses a different API (e.g. `context.Args` as raw string), parse manually: `context.Args.Split(' ', StringSplitOptions.RemoveEmptyEntries)`.
- **Property paths (Task 6):** `player.Pawn?.LifeState` and `player.Pawn?.TeamNum` need SDK verification. If `Pawn` doesn't expose these, check `player.Controller.Team` (enum) and `player.Controller.PawnIsAlive` (bool) as alternatives.
- **Map name (Task 9):** Cached from the `OnMapLoad` event and passed to `MessageFormatter`. If the event doesn't carry a map name property, fallback: `Core.ConVar.Find<string>("host_map")?.Value`.
