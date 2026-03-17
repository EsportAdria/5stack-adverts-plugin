using SwiftlyS2.Shared;

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
