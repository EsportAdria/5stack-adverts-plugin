using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SwiftlyS2.Adverts.Logic;
using SwiftlyS2.Adverts.Models;
using Xunit;

// The panel writes config_default (edited) as the plugin's config.jsonc,
// so it has to bind to the model the plugin reads.
public class RegistryEntryTests
{
    private static JsonElement Entry()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "5stack", "ea-adverts.json");
        return JsonDocument.Parse(File.ReadAllText(path)).RootElement;
    }

    internal static AdvertsConfig Bind(string json, List<string> warnings)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)))
            .Build();
        return ConfigCheck.Bind(configuration, "Adverts", warnings);
    }

    [Fact]
    public void DefaultConfigBindsAndPassesTheCheck()
    {
        var warnings = new List<string>();
        var config = Bind(Entry().GetProperty("config_default").GetRawText(), warnings);

        var groups = ConfigCheck.Normalize(config, warnings);

        Assert.Empty(warnings);
        Assert.Equal(new[] { "rules", "community" }, groups.Select(g => g.Name));
        Assert.Equal("[EA]", config.Prefix);
        Assert.Equal(new[] { "vip.ads.skip" }, groups[1].SkipPermissions);
    }

    [Fact]
    public void SchemaEnumsMatchThePlugin()
    {
        var group = Entry()
            .GetProperty("config_schema").GetProperty("properties")
            .GetProperty("Adverts").GetProperty("properties")
            .GetProperty("Groups").GetProperty("items").GetProperty("properties");

        string[] Enum(string field) => group.GetProperty(field).GetProperty("enum")
            .EnumerateArray().Select(e => e.GetString()!).ToArray();

        Assert.Equal(ConfigCheck.DisplayTypes, Enum("DisplayType"));
        Assert.Equal(ConfigCheck.Targets, Enum("Target"));
        Assert.Equal(ConfigCheck.Orders, Enum("Order"));
        Assert.Equal(ConfigCheck.MinIntervalSeconds, group.GetProperty("IntervalSeconds").GetProperty("minimum").GetInt32());
        Assert.Equal(ConfigCheck.MaxIntervalSeconds, group.GetProperty("IntervalSeconds").GetProperty("maximum").GetInt32());
    }

    // The panel seeds a new list item only with required keys, and titles it by a lowercase "name"
    [Fact]
    public void NewGroupsInThePanelGetDefaultsAndATitle()
    {
        var items = Entry()
            .GetProperty("config_schema").GetProperty("properties")
            .GetProperty("Adverts").GetProperty("properties")
            .GetProperty("Groups").GetProperty("items");
        var required = items.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToHashSet();

        Assert.True(items.GetProperty("properties").TryGetProperty("name", out _));
        foreach (var property in items.GetProperty("properties").EnumerateObject())
        {
            if (property.Value.TryGetProperty("default", out _))
                Assert.Contains(property.Name, required);
        }
    }

    [Fact]
    public void ConfigPathMatchesThePluginId()
    {
        Assert.Equal(
            "addons/swiftlys2/configs/plugins/EA_Adverts/config.jsonc",
            Entry().GetProperty("config_path").GetString());
    }

    [Fact]
    public void AMissingSectionGivesNoGroups()
    {
        var warnings = new List<string>();
        var config = Bind("{}", warnings);

        Assert.Empty(ConfigCheck.Normalize(config, warnings));
        Assert.Empty(warnings);
    }
}
