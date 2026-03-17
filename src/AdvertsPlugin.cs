using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Permissions;
using SwiftlyS2.Adverts.Helpers;
using SwiftlyS2.Adverts.Models;

namespace SwiftlyS2.Adverts;

[PluginMetadata(Id = "swiftlys2_adverts", Version = "1.0.0", Name = "Adverts", Author = "EsportAdria")]
public class AdvertsPlugin : BasePlugin
{

    private static readonly string[] ValidDisplayTypes = { "chat", "center", "center_html", "alert" };

    private readonly Dictionary<string, CancellationTokenSource> _timers = new();
    private readonly Dictionary<string, int> _sequentialIndexes = new();
    private AdvertsConfig _config = new();
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
            _sequentialIndexes.Clear();
        }

        Core.Configuration
            .InitializeJsonWithModel<AdvertsConfig>("config.jsonc", "Main")
            .Configure(builder =>
            {
                builder.AddJsonFile("config.jsonc", optional: false, reloadOnChange: false);
            });
        LoadConfig();

        Core.Registrator.Register(this);

        StartAllTimers();

        _onMapLoadHandler = (e) =>
        {
            _currentMap = e.MapName; // cache map name for MessageFormatter
            StopAllTimers();
            _sequentialIndexes.Clear();
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
        _sequentialIndexes.Clear();
    }

    private void LoadConfig()
    {
        _config = Core.Configuration.Manager.GetSection("Main").Get<AdvertsConfig>() ?? new AdvertsConfig();
    }

    private bool IsGroupValid(string groupName, AdvertGroup group)
    {
        if (group.Messages.Count == 0)
        {
            Core.Logger.LogWarning($"[Adverts] Group '{groupName}' has no messages, skipping.");
            return false;
        }

        if (group.IntervalSeconds <= 0)
        {
            Core.Logger.LogWarning($"[Adverts] Group '{groupName}' has invalid interval ({group.IntervalSeconds}s), skipping.");
            return false;
        }

        if (!ValidDisplayTypes.Contains(group.DisplayType.ToLowerInvariant()))
        {
            Core.Logger.LogWarning($"[Adverts] Group '{groupName}' has unknown display type '{group.DisplayType}', skipping.");
            return false;
        }

        if (!PlayerFilter.IsValidTarget(group.Target))
        {
            Core.Logger.LogWarning($"[Adverts] Group '{groupName}' has unknown target '{group.Target}', defaulting to 'all'.");
        }

        return true;
    }

    private void StartGroupTimer(string groupName, AdvertGroup group)
    {
        if (!_sequentialIndexes.ContainsKey(groupName))
            _sequentialIndexes[groupName] = 0;

        var token = Core.Scheduler.DelayAndRepeatBySeconds(
            group.IntervalSeconds,
            group.IntervalSeconds,
            () => OnTimerTick(groupName, group));

        Core.Scheduler.StopOnMapChange(token);
        _timers[groupName] = token;
    }

    private void StartAllTimers()
    {
        foreach (var (groupName, group) in _config.Groups)
        {
            if (!group.Enabled)
                continue;

            if (!IsGroupValid(groupName, group))
                continue;

            StartGroupTimer(groupName, group);
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
        string messageTemplate;
        if (group.Order.Equals("random", StringComparison.OrdinalIgnoreCase))
        {
            messageTemplate = group.Messages[_random.Next(group.Messages.Count)];
        }
        else
        {
            var index = _sequentialIndexes.GetValueOrDefault(groupName, 0);
            messageTemplate = group.Messages[index];
            _sequentialIndexes[groupName] = (index + 1) % group.Messages.Count;
        }

        var message = MessageFormatter.FormatMessage(
            messageTemplate,
            _config.Prefix,
            _config.PrefixColor,
            group.DisplayType,
            Core,
            _currentMap);

        var players = Core.PlayerManager.GetAllValidPlayers()
            .Where(p => !p.IsFakeClient);

        var targets = PlayerFilter.FilterPlayers(
            players,
            group.Target,
            group.SkipPermissions,
            Core.Permission);

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

    // --- Admin Commands ---

    [Command("adverts_reload", permission: "admins.adverts.reload")]
    public void OnReloadCommand(ICommandContext context)
    {
        StopAllTimers();
        LoadConfig();
        StartAllTimers();
        context.Reply("[Adverts] Configuration reloaded.");
    }

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

    [Command("adverts_toggle", permission: "admins.adverts.toggle")]
    public void OnToggleCommand(ICommandContext context)
    {
        var args = context.Args;
        if (args.Length < 1)
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
            token.Cancel();
            _timers.Remove(groupName);
            context.Reply($"[Adverts] Group '{groupName}' disabled.");
        }
        else
        {
            if (!IsGroupValid(groupName, group))
            {
                context.Reply($"[Adverts] Group '{groupName}' has invalid configuration.");
                return;
            }

            StartGroupTimer(groupName, group);
            context.Reply($"[Adverts] Group '{groupName}' enabled.");
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
        if (args.Length >= 2 && int.TryParse(args[1], out var parsed))
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
            Core,
            _currentMap);

        if (!context.IsSentByPlayer)
        {
            context.Reply($"[Adverts] Preview ({group.DisplayType}): {message}");
            return;
        }

        var player = context.Sender!;
        SendToPlayer(player, message, group.DisplayType);
        context.Reply($"[Adverts] Previewing message {index} from group '{groupName}'.");
    }
}
