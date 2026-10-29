using SwiftlyS2.Adverts.Logic;
using Xunit;

public class ClockTests
{
    [Fact]
    public void EmptyZoneUsesLocalTimeWithoutError()
    {
        Assert.Null(Clock.Resolve("", out var error));
        Assert.Null(error);
    }

    [Fact]
    public void UnknownZoneReportsAnError()
    {
        Assert.Null(Clock.Resolve("Mars/Olympus", out var error));
        Assert.Contains("Mars/Olympus", error);
    }

    [Fact]
    public void ConvertsToTheConfiguredZone()
    {
        var zone = Clock.Resolve("Europe/Zagreb", out var error);
        Assert.Null(error);

        // 29 Oct 2026 is after the switch to CET (UTC+1)
        var now = Clock.Now(zone, new DateTime(2026, 10, 29, 20, 5, 0, DateTimeKind.Utc));

        Assert.Equal("21:05", Clock.Format(now, "HH:mm", "HH:mm"));
    }

    [Fact]
    public void BadFormatFallsBack()
    {
        var value = new DateTime(2026, 10, 29, 21, 5, 0);

        Assert.Equal("29.10.2026", Clock.Format(value, "", "dd.MM.yyyy"));
        Assert.Equal("21:05", Clock.Format(value, "%", "HH:mm"));
    }
}
