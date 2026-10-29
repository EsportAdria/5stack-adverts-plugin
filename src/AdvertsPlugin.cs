using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Adverts.Helpers;
using SwiftlyS2.Adverts.Logic;
using SwiftlyS2.Adverts.Models;

namespace SwiftlyS2.Adverts;

[PluginMetadata(Id = "EA_Adverts", Version = "__RELEASE_VERSION__", Name = "Adverts", Author = "EsportAdria")]
public class AdvertsPlugin : BasePlugin
{
    // The panel writes this section to configs/plugins/EA_Adverts/config.jsonc
    private const string ConfigSection = "Adverts";

    private readonly Dictionary<string, CancellationTokenSource> _timers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AdvertRotation> _rotations = new(StringComparer.OrdinalIgnoreCase);
    private AdvertsConfig _config = new();
    private List<AdvertGroup> _groups = new();
    private TimeZoneInfo? _timeZone;
    private readonly Random _random = new();
    private string _currentMap = "Unknown";
    private EventDelegates.OnMapLoad? _onMapLoadHandler;

    public AdvertsPlugin(ISwiftlyCore core) : base(core) { }

    public override void Load(bool hotReload)
    {
        if (hotReload)
        {
            if (_onMapLoadHandler != null)
            {
                Core.Event.OnMapLoad -= _onMapLoadHandler;
                _onMapLoadHandler = null;
            }
            StopAllTimers();

            // no OnMapLoad until the next map, so read the current one now
            try
            {
                var map = Core.Engine.GlobalVars.MapName.Value;
                if (!string.IsNullOrEmpty(map))
                    _currentMap = map;
            }
            catch (Exception e)
            {
                Core.Logger.LogWarning($"[Adverts] Could not read the current map: {e.Message}");
            }
        }

        // optional: a server without a panel config still loads and writes the defaults
        Core.Configuration
            .InitializeJsonWithModel<AdvertsConfig>("config.jsonc", ConfigSection)
            .Configure(builder =>
            {
                builder.AddJsonFile("config.jsonc", optional: true, reloadOnChange: true);
            });
        LoadConfig();

        Core.Registrator.Register(this);

        StartAllTimers();

        _onMapLoadHandler = (e) =>
        {
            _currentMap = e.MapName;
            StopAllTimers();
            // pick up edits made since the last map
            LoadConfig();
            StartAllTimers();
        };
        Core.Event.OnMapLoad += _onMapLoadHandler;
    }

    public override void Unload()
    {
        if (_onMapLoadHandler != null)
        {
            Core.Event.OnMapLoad -= _onMapLoadHandler;
            _onMapLoadHandler = null;
        }
        StopAllTimers();
    }

    private void LoadConfig()
    {
        _config = Core.Configuration.Manager.GetSection(ConfigSection).Get<AdvertsConfig>() ?? new AdvertsConfig();

        var warnings = new List<string>();
        _groups = ConfigCheck.Normalize(_config, warnings);
        _timeZone = Clock.Resolve(_config.TimeZone, out var zoneError);
        if (zoneError != null)
            warnings.Add(zoneError);

        foreach (var warning in warnings)
        {
            Core.Logger.LogWarning($"[Adverts] {warning}");
        }
    }

    private AdvertGroup? FindGroup(string name)
    {
        return _groups.FirstOrDefault(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private void StartGroupTimer(AdvertGroup group)
    {
        _rotations[group.Name] = new AdvertRotation(group.Order, group.Messages.Count, _random);

        var token = Core.Scheduler.DelayAndRepeatBySeconds(
            group.IntervalSeconds,
            group.IntervalSeconds,
            () => OnTimerTick(group));

        Core.Scheduler.StopOnMapChange(token);
        _timers[group.Name] = token;
    }

    private void StartAllTimers()
    {
        foreach (var group in _groups)
        {
            if (group.Enabled)
                StartGroupTimer(group);
        }
    }

    private void StopAllTimers()
    {
        foreach (var token in _timers.Values)
        {
            token.Cancel();
        }
        _timers.Clear();
        _rotations.Clear();
    }

    private void OnTimerTick(AdvertGroup group)
    {
        if (!_rotations.TryGetValue(group.Name, out var rotation))
            return;

        var message = Render(group.Messages[rotation.Next()], group.DisplayType);

        var targets = PlayerFilter.FilterPlayers(
            HumanPlayers(),
            group.Target,
            group.SkipPermissions,
            Core.Permission);

        foreach (var player in targets)
        {
            SendToPlayer(player, message, group.DisplayType);
        }
    }

    private IEnumerable<IPlayer> HumanPlayers()
    {
        return Core.PlayerManager.GetAllValidPlayers().Where(p => !p.IsFakeClient);
    }

    private string Render(string template, string displayType)
    {
        var now = Clock.Now(_timeZone, DateTime.UtcNow);
        var values = new Dictionary<string, string>
        {
            ["server_name"] = Core.ConVar.Find<string>("hostname")?.Value ?? "Unknown",
            ["map"] = _currentMap,
            ["player_count"] = HumanPlayers().Count().ToString(),
            ["max_players"] = Core.PlayerManager.MaxPlayers.ToString(),
            ["time"] = Clock.Format(now, _config.TimeFormat, "HH:mm"),
            ["date"] = Clock.Format(now, _config.DateFormat, "dd.MM.yyyy"),
        };

        return MessageFormatter.Format(template, values, _config.Prefix, _config.PrefixColor, displayType);
    }

    private void SendToPlayer(IPlayer player, string message, string displayType)
    {
        switch (displayType)
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

    // --- Admin Commands ---

    [Command("adverts_reload", permission: "admins.adverts.reload")]
    public void OnReloadCommand(ICommandContext context)
    {
        StopAllTimers();
        LoadConfig();
        StartAllTimers();
        context.Reply($"[Adverts] Configuration reloaded, {_timers.Count} group(s) running.");
    }

    [Command("adverts_list", permission: "admins.adverts.list")]
    public void OnListCommand(ICommandContext context)
    {
        if (_groups.Count == 0)
        {
            context.Reply("[Adverts] No groups configured.");
            return;
        }

        foreach (var group in _groups)
        {
            var configState = group.Enabled ? "enabled" : "disabled";
            var runtimeState = _timers.ContainsKey(group.Name) ? "running" : "stopped";
            context.Reply($"[Adverts] {group.Name}: config={configState} runtime={runtimeState} | interval: {group.IntervalSeconds}s | messages: {group.Messages.Count} | display: {group.DisplayType} | target: {group.Target}");
        }
    }

    [Command("adverts_toggle", permission: "admins.adverts.toggle")]
    public void OnToggleCommand(ICommandContext context)
    {
        var args = context.Args;
        if (args.Length < 1)
        {
            context.Reply("[Adverts] Usage: sw_adverts_toggle <group>");
            return;
        }

        var group = FindGroup(args[0]);
        if (group == null)
        {
            context.Reply($"[Adverts] Group '{args[0]}' not found.");
            return;
        }

        if (_timers.TryGetValue(group.Name, out var token))
        {
            token.Cancel();
            _timers.Remove(group.Name);
            _rotations.Remove(group.Name);
            context.Reply($"[Adverts] Group '{group.Name}' stopped until the next map or reload.");
        }
        else
        {
            StartGroupTimer(group);
            context.Reply($"[Adverts] Group '{group.Name}' started until the next map or reload.");
        }
    }

    [Command("adverts_preview", permission: "admins.adverts.preview")]
    public void OnPreviewCommand(ICommandContext context)
    {
        var args = context.Args;
        if (args.Length < 1)
        {
            context.Reply("[Adverts] Usage: sw_adverts_preview <group> [index]");
            return;
        }

        var group = FindGroup(args[0]);
        if (group == null)
        {
            context.Reply($"[Adverts] Group '{args[0]}' not found.");
            return;
        }

        int index;
        if (args.Length >= 2 && int.TryParse(args[1], out var parsed))
        {
            index = Math.Clamp(parsed, 0, group.Messages.Count - 1);
        }
        else
        {
            index = _rotations.TryGetValue(group.Name, out var rotation) ? rotation.Peek : 0;
        }

        var message = Render(group.Messages[index], group.DisplayType);

        if (!context.IsSentByPlayer)
        {
            context.Reply($"[Adverts] Preview ({group.DisplayType}): {message}");
            return;
        }

        SendToPlayer(context.Sender!, message, group.DisplayType);
        context.Reply($"[Adverts] Previewing message {index} from group '{group.Name}'.");
    }

    // One-off announcement to everyone, e.g. over RCON: sw_adverts_say center "Restart in 5 minutes"
    [Command("adverts_say", permission: "admins.adverts.say")]
    public void OnSayCommand(ICommandContext context)
    {
        var args = context.Args;
        var display = args.Length >= 1 ? args[0].ToLowerInvariant() : "";
        if (args.Length < 2 || !ConfigCheck.DisplayTypes.Contains(display))
        {
            context.Reply($"[Adverts] Usage: sw_adverts_say <{string.Join("|", ConfigCheck.DisplayTypes)}> <text>");
            return;
        }

        var message = Render(string.Join(' ', args.Skip(1)), display);
        var count = 0;
        foreach (var player in HumanPlayers())
        {
            SendToPlayer(player, message, display);
            count++;
        }
        context.Reply($"[Adverts] Sent to {count} player(s).");
    }
}
