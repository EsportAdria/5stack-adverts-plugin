using System.Text.RegularExpressions;

namespace SwiftlyS2.Adverts.Logic;

public static class MessageFormatter
{
    public static readonly string[] Placeholders =
    {
        "server_name", "map", "player_count", "max_players", "time", "date",
    };

    /// <summary>
    /// SwiftlyS2's chat colour tokens with a close hex value. Only chat turns
    /// the tokens into colours, so center and alert drop them and center_html
    /// turns them into &lt;font&gt; tags. "default" ends the colour.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> Colours =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["default"] = "",
            ["white"] = "#FFFFFF",
            ["red"] = "#FF4040",
            ["lightred"] = "#EB4B4B",
            ["darkred"] = "#9B0000",
            ["green"] = "#40FF40",
            ["lime"] = "#A2FF47",
            ["olive"] = "#B2DB5E",
            ["blue"] = "#4B69FF",
            ["lightblue"] = "#5E98D9",
            ["darkblue"] = "#2B3CA0",
            ["bluegrey"] = "#B0C3D9",
            ["purple"] = "#8847FF",
            ["lightpurple"] = "#BE7DFF",
            ["magenta"] = "#D32CE6",
            ["yellow"] = "#FFFF40",
            ["lightyellow"] = "#FFFFA0",
            ["gold"] = "#E4AE39",
            ["orange"] = "#F08C3C",
            ["grey"] = "#C6CBD0",
            ["gray"] = "#C6CBD0",
            ["silver"] = "#B0B0B0",
        };

    // Known colour names only, so text such as "[EA]" stays as written
    private static readonly Regex ColourTag = new(
        @"\[(" + string.Join("|", Colours.Keys) + @")\]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Spellings SwiftlyS2 accepts that the panel form doesn't offer
    public static readonly string[] Aliases = { "gray" };

    /// <summary>
    /// The value for {server_name}: the configured name, or the hostname cvar
    /// when none is set (5stack leaves it at "Counter-Strike 2").
    /// </summary>
    public static string ServerName(string? configured, string? hostname)
    {
        return string.IsNullOrWhiteSpace(configured)
            ? (string.IsNullOrEmpty(hostname) ? "Unknown" : hostname)
            : configured.Trim();
    }

    /// <summary>
    /// Replaces {placeholder} tokens with their values, adds the coloured
    /// prefix to chat messages and converts colour tokens for the other
    /// displays. Unknown tokens are left as they are.
    /// </summary>
    public static string Format(
        string template,
        IReadOnlyDictionary<string, string> values,
        string prefix,
        string prefixColor,
        string displayType)
    {
        var html = displayType == "center_html";
        var message = template;
        foreach (var (key, value) in values)
        {
            // a "<" in a server name must not break the HTML. Only these three:
            // the HUD may not decode numeric entities such as &#233;
            message = message.Replace("{" + key + "}", html ? EscapeText(value) : value);
        }

        switch (displayType)
        {
            case "chat":
                if (!string.IsNullOrEmpty(prefix))
                {
                    message = string.IsNullOrEmpty(prefixColor)
                        ? $"{prefix} {message}"
                        : $"[{prefixColor}]{prefix}[default] {message}";
                }
                return message;
            case "center_html":
                return ToHtml(message);
            default:
                return ColourTag.Replace(message, "");
        }
    }

    private static string EscapeText(string value)
    {
        return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    // Each colour token closes the previous <font>; [default] only closes it
    private static string ToHtml(string message)
    {
        var open = false;
        var html = ColourTag.Replace(message, match =>
        {
            var close = open ? "</font>" : "";
            var hex = Colours[match.Groups[1].Value];
            open = hex != "";
            return open ? $"{close}<font color='{hex}'>" : close;
        });
        return open ? html + "</font>" : html;
    }
}
