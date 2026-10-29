namespace SwiftlyS2.Adverts.Logic;

public static class MessageFormatter
{
    public static readonly string[] Placeholders =
    {
        "server_name", "map", "player_count", "max_players", "time", "date",
    };

    /// <summary>
    /// Replaces {placeholder} tokens with their values and adds the coloured
    /// prefix to chat messages. Unknown tokens are left as they are.
    /// </summary>
    public static string Format(
        string template,
        IReadOnlyDictionary<string, string> values,
        string prefix,
        string prefixColor,
        string displayType)
    {
        var message = template;
        foreach (var (key, value) in values)
        {
            message = message.Replace("{" + key + "}", value);
        }

        if (displayType == "chat" && !string.IsNullOrEmpty(prefix))
        {
            message = string.IsNullOrEmpty(prefixColor)
                ? $"{prefix} {message}"
                : $"[{prefixColor}]{prefix}[default] {message}";
        }

        return message;
    }
}
