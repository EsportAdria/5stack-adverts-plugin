using SwiftlyS2.Adverts.Logic;
using SwiftlyS2.Adverts.Models;
using Xunit;

public class ConfigCheckTests
{
    private static AdvertGroup Group(string name, params string[] messages) => new()
    {
        Name = name,
        Messages = messages.ToList(),
    };

    [Fact]
    public void KeepsAValidGroupAsItIs()
    {
        var warnings = new List<string>();
        var config = new AdvertsConfig { Groups = { Group("rules", "a", "b") } };

        var groups = ConfigCheck.Normalize(config, warnings);

        Assert.Empty(warnings);
        var g = Assert.Single(groups);
        Assert.Equal("rules", g.Name);
        Assert.Equal(60, g.IntervalSeconds);
        Assert.Equal(new[] { "a", "b" }, g.Messages);
    }

    [Fact]
    public void NamesUnnamedGroupsByPosition()
    {
        var groups = ConfigCheck.Normalize(
            new AdvertsConfig { Groups = { Group("", "a"), Group("  ", "b") } },
            new List<string>());

        Assert.Equal(new[] { "group1", "group2" }, groups.Select(g => g.Name));
    }

    [Fact]
    public void SkipsDuplicateNamesIgnoringCase()
    {
        var warnings = new List<string>();
        var groups = ConfigCheck.Normalize(
            new AdvertsConfig { Groups = { Group("Rules", "a"), Group("rules", "b") } },
            warnings);

        Assert.Equal("a", Assert.Single(groups).Messages[0]);
        Assert.Contains(warnings, w => w.Contains("more than once"));
    }

    [Fact]
    public void DropsBlankMessagesAndSkipsEmptyGroups()
    {
        var warnings = new List<string>();
        var groups = ConfigCheck.Normalize(
            new AdvertsConfig { Groups = { Group("one", "a", "", "  "), Group("two", "") } },
            warnings);

        Assert.Equal(new[] { "a" }, Assert.Single(groups).Messages);
        Assert.Contains(warnings, w => w.Contains("'two' has no messages"));
    }

    [Fact]
    public void SkipsUnknownDisplayType()
    {
        var g = Group("x", "a");
        g.DisplayType = "hud";
        var warnings = new List<string>();

        Assert.Empty(ConfigCheck.Normalize(new AdvertsConfig { Groups = { g } }, warnings));
        Assert.Contains(warnings, w => w.Contains("display type"));
    }

    [Fact]
    public void FallsBackOnUnknownTargetAndOrder()
    {
        var g = Group("x", "a");
        g.Target = "spectators";
        g.Order = "shuffle";
        var warnings = new List<string>();

        var result = Assert.Single(ConfigCheck.Normalize(new AdvertsConfig { Groups = { g } }, warnings));

        Assert.Equal("all", result.Target);
        Assert.Equal("sequential", result.Order);
        Assert.Equal(2, warnings.Count);
    }

    [Fact]
    public void LowercasesEnumValues()
    {
        var g = Group("x", "a");
        g.DisplayType = "Center_HTML";
        g.Target = "CT";
        g.Order = "Random";

        var result = Assert.Single(ConfigCheck.Normalize(new AdvertsConfig { Groups = { g } }, new List<string>()));

        Assert.Equal("center_html", result.DisplayType);
        Assert.Equal("ct", result.Target);
        Assert.Equal("random", result.Order);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(4)]
    public void RaisesShortIntervalsToTheMinimum(int interval)
    {
        var g = Group("x", "a");
        g.IntervalSeconds = interval;
        var warnings = new List<string>();

        var result = Assert.Single(ConfigCheck.Normalize(new AdvertsConfig { Groups = { g } }, warnings));

        Assert.Equal(ConfigCheck.MinIntervalSeconds, result.IntervalSeconds);
        Assert.Single(warnings);
    }

    [Fact]
    public void LowersLongIntervalsToTheMaximum()
    {
        var g = Group("x", "a");
        g.IntervalSeconds = ConfigCheck.MaxIntervalSeconds + 1;
        var warnings = new List<string>();

        var result = Assert.Single(ConfigCheck.Normalize(new AdvertsConfig { Groups = { g } }, warnings));

        Assert.Equal(ConfigCheck.MaxIntervalSeconds, result.IntervalSeconds);
        Assert.Single(warnings);
    }

    [Fact]
    public void KeepsDisabledGroupsSoTheyCanBeToggled()
    {
        var g = Group("x", "a");
        g.Enabled = false;

        var result = Assert.Single(ConfigCheck.Normalize(new AdvertsConfig { Groups = { g } }, new List<string>()));

        Assert.False(result.Enabled);
    }
}
