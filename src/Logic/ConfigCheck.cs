using SwiftlyS2.Adverts.Models;

namespace SwiftlyS2.Adverts.Logic;

/// <summary>
/// Turns the config file (hand-written or saved by the panel) into the groups
/// that can run, with a warning for everything that was skipped or changed.
/// </summary>
public static class ConfigCheck
{
    public const int MinIntervalSeconds = 5;

    public static readonly string[] DisplayTypes = { "chat", "center", "center_html", "alert" };
    public static readonly string[] Targets = { "all", "alive", "dead", "t", "ct" };
    public static readonly string[] Orders = { "sequential", "random" };

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
