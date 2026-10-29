using Microsoft.Extensions.Configuration;
using SwiftlyS2.Adverts.Models;

namespace SwiftlyS2.Adverts.Logic;

/// <summary>
/// Turns the config file (hand-written or saved by the panel) into the groups
/// that can run, with a warning for everything that was skipped or changed.
/// </summary>
public static class ConfigCheck
{
    public const int MinIntervalSeconds = 5;
    public const int MaxIntervalSeconds = 86400;

    public static readonly string[] DisplayTypes = { "chat", "center", "center_html", "alert" };
    public static readonly string[] Targets = { "all", "alive", "dead", "t", "ct" };
    public static readonly string[] Orders = { "sequential", "random" };

    /// <summary>
    /// Binds the section and warns about what the binder hides: it drops a
    /// list element whose value it can't convert (a decimal or huge interval,
    /// "Enabled": "yes") without an error, and it ignores the 1.x "Main" section.
    /// </summary>
    public static AdvertsConfig Bind(IConfiguration configuration, string section, List<string> warnings)
    {
        var config = configuration.GetSection(section).Get<AdvertsConfig>() ?? new AdvertsConfig();

        var written = configuration.GetSection($"{section}:Groups").GetChildren().Count();
        if (written > config.Groups.Count)
            warnings.Add($"{written - config.Groups.Count} of {written} group(s) have a value of the wrong type (for example a decimal interval) and were skipped.");

        if (configuration.GetSection("Main").Exists())
            warnings.Add($"config.jsonc still has a 1.x \"Main\" section, which is ignored. Move the groups to \"{section}\" (see the README).");

        return config;
    }

    public static List<AdvertGroup> Normalize(AdvertsConfig config, List<string> warnings)
    {
        var groups = new List<AdvertGroup>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < config.Groups.Count; i++)
        {
            var source = config.Groups[i];
            if (source == null)
                continue;

            var name = string.IsNullOrWhiteSpace(source.Name) ? $"group{i + 1}" : source.Name.Trim();
            if (!names.Add(name))
            {
                warnings.Add($"Group '{name}' appears more than once, skipping the copy at position {i + 1}.");
                continue;
            }

            var messages = (source.Messages ?? new()).Where(m => !string.IsNullOrWhiteSpace(m)).ToList();
            if (messages.Count == 0)
            {
                warnings.Add($"Group '{name}' has no messages, skipping.");
                continue;
            }

            var display = (source.DisplayType ?? "").Trim().ToLowerInvariant();
            if (!DisplayTypes.Contains(display))
            {
                warnings.Add($"Group '{name}' has unknown display type '{source.DisplayType}', skipping.");
                continue;
            }

            var target = (source.Target ?? "").Trim().ToLowerInvariant();
            if (!Targets.Contains(target))
            {
                warnings.Add($"Group '{name}' has unknown target '{source.Target}', using 'all'.");
                target = "all";
            }

            var order = (source.Order ?? "").Trim().ToLowerInvariant();
            if (!Orders.Contains(order))
            {
                warnings.Add($"Group '{name}' has unknown order '{source.Order}', using 'sequential'.");
                order = "sequential";
            }

            var interval = source.IntervalSeconds;
            if (interval < MinIntervalSeconds)
            {
                warnings.Add($"Group '{name}' interval {interval}s is below {MinIntervalSeconds}s, using {MinIntervalSeconds}s.");
                interval = MinIntervalSeconds;
            }
            else if (interval > MaxIntervalSeconds)
            {
                warnings.Add($"Group '{name}' interval {interval}s is above {MaxIntervalSeconds}s, using {MaxIntervalSeconds}s.");
                interval = MaxIntervalSeconds;
            }

            groups.Add(new AdvertGroup
            {
                Name = name,
                Enabled = source.Enabled,
                IntervalSeconds = interval,
                Order = order,
                DisplayType = display,
                Target = target,
                SkipPermissions = (source.SkipPermissions ?? new())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p.Trim())
                    .ToList(),
                Messages = messages,
            });
        }

        return groups;
    }
}
