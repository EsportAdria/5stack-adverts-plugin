using SwiftlyS2.Adverts.Logic;
using Xunit;

public class BindTests
{
    [Theory]
    [InlineData("60.5")]
    [InlineData("99999999999")]
    [InlineData("\"soon\"")]
    public void WarnsAboutAGroupTheBinderDrops(string interval)
    {
        var warnings = new List<string>();
        var json = $$$"""
            {"Adverts": {"Groups": [
              {"name": "ok", "Messages": ["a"]},
              {"name": "bad", "IntervalSeconds": {{{interval}}}, "Messages": ["b"]}
            ]}}
            """;

        var config = RegistryEntryTests.Bind(json, warnings);

        Assert.Equal("ok", Assert.Single(config.Groups).Name);
        Assert.Contains(warnings, w => w.StartsWith("1 of 2 group(s)"));
    }

    [Fact]
    public void WarnsAboutAnOldMainSection()
    {
        var warnings = new List<string>();

        RegistryEntryTests.Bind("""{"Main": {"Groups": {"rules": {}}}, "Adverts": {}}""", warnings);

        Assert.Contains(warnings, w => w.Contains("\"Main\""));
    }

    [Fact]
    public void KeysAreCaseInsensitive()
    {
        var warnings = new List<string>();

        var config = RegistryEntryTests.Bind(
            """{"Adverts": {"prefix": "[X]", "Groups": [{"NAME": "a", "displaytype": "alert", "Messages": ["m"]}]}}""",
            warnings);

        Assert.Empty(warnings);
        Assert.Equal("[X]", config.Prefix);
        Assert.Equal("a", config.Groups[0].Name);
        Assert.Equal("alert", config.Groups[0].DisplayType);
    }
}
