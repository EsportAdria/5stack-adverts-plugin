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
}
