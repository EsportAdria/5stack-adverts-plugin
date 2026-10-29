using SwiftlyS2.Adverts.Logic;
using Xunit;

public class MessageFormatterTests
{
    private static readonly Dictionary<string, string> Values = new()
    {
        ["server_name"] = "EA Retake #1",
        ["map"] = "de_mirage",
        ["player_count"] = "7",
        ["max_players"] = "10",
        ["time"] = "21:05",
        ["date"] = "29.10.2026",
    };

    [Fact]
    public void ReplacesEveryPlaceholder()
    {
        var template = string.Join(" ", MessageFormatter.Placeholders.Select(p => "{" + p + "}"));

        var message = MessageFormatter.Format(template, Values, "", "", "center");

        Assert.Equal("EA Retake #1 de_mirage 7 10 21:05 29.10.2026", message);
    }

    [Fact]
    public void LeavesUnknownTokensAlone()
    {
        Assert.Equal("{next_map}", MessageFormatter.Format("{next_map}", Values, "", "", "chat"));
    }

    [Fact]
    public void PrefixesChatOnly()
    {
        Assert.Equal("[red][EA][default] hi", MessageFormatter.Format("hi", Values, "[EA]", "red", "chat"));
        Assert.Equal("hi", MessageFormatter.Format("hi", Values, "[EA]", "red", "center"));
        Assert.Equal("hi", MessageFormatter.Format("hi", Values, "[EA]", "red", "alert"));
    }

    [Fact]
    public void PrefixWithoutColour()
    {
        Assert.Equal("[EA] hi", MessageFormatter.Format("hi", Values, "[EA]", "", "chat"));
    }

    private const string Coloured = "[blue]You are playing on [yellow]{server_name}[blue], [EA] {map}[default].";

    [Fact]
    public void ChatKeepsColourTokens()
    {
        Assert.Equal(
            "[blue]You are playing on [yellow]EA Retake #1[blue], [EA] de_mirage[default].",
            MessageFormatter.Format(Coloured, Values, "", "", "chat"));
    }

    [Theory]
    [InlineData("center")]
    [InlineData("alert")]
    public void CenterAndAlertDropColourTokens(string display)
    {
        Assert.Equal(
            "You are playing on EA Retake #1, [EA] de_mirage.",
            MessageFormatter.Format(Coloured, Values, "", "", display));
    }

    [Fact]
    public void TokensIgnoreCase()
    {
        Assert.Equal("hi", MessageFormatter.Format("[RED]hi[Default]", Values, "", "", "center"));
    }

    [Fact]
    public void CenterHtmlTurnsTokensIntoFonts()
    {
        Assert.Equal(
            "<font color='#4B69FF'>You are playing on </font><font color='#FFFF40'>EA Retake #1</font>"
            + "<font color='#4B69FF'>, [EA] de_mirage</font>.",
            MessageFormatter.Format(Coloured, Values, "", "", "center_html"));
    }

    [Fact]
    public void CenterHtmlClosesAnOpenFont()
    {
        Assert.Equal("<font color='#FF4040'>hi</font>", MessageFormatter.Format("[red]hi", Values, "", "", "center_html"));
        Assert.Equal("plain", MessageFormatter.Format("[default]plain", Values, "", "", "center_html"));
    }

    [Fact]
    public void CenterHtmlEncodesValuesButNotTheTemplate()
    {
        var values = new Dictionary<string, string> { ["server_name"] = "<EA> & co" };

        Assert.Equal(
            "<b>&lt;EA&gt; &amp; co</b>",
            MessageFormatter.Format("<b>{server_name}</b>", values, "", "", "center_html"));
    }

    [Fact]
    public void EveryColourHasAHexValue()
    {
        foreach (var (name, hex) in MessageFormatter.Colours)
        {
            Assert.Matches(name == "default" ? "^$" : "^#[0-9A-F]{6}$", hex);
        }
    }

    // The prefix colour enum in the panel form must only offer tokens the plugin knows
    [Fact]
    public void PrefixColourEnumMatchesTheColours()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "5stack", "ea-adverts.json");
        var colours = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path)).RootElement
            .GetProperty("config_schema").GetProperty("properties")
            .GetProperty("Adverts").GetProperty("properties")
            .GetProperty("PrefixColor").GetProperty("enum")
            .EnumerateArray().Select(e => e.GetString()!);

        Assert.Equal(MessageFormatter.Colours.Keys.OrderBy(k => k), colours.OrderBy(k => k));
    }
}
