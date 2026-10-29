namespace SwiftlyS2.Adverts.Logic;

public static class Clock
{
    /// <summary>
    /// Resolves an IANA time zone id. Returns null for an empty id, and null
    /// with an error when the id is unknown or the system has no tz data.
    /// </summary>
    public static TimeZoneInfo? Resolve(string? id, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(id))
            return null;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id.Trim());
        }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            error = $"Unknown time zone '{id}', using the server's local time.";
            return null;
        }
    }

    public static DateTime Now(TimeZoneInfo? zone, DateTime utcNow)
    {
        return zone == null
            ? utcNow.ToLocalTime()
            : TimeZoneInfo.ConvertTimeFromUtc(utcNow, zone);
    }

    /// <summary>Formats with a fallback, so a bad format string in the config never breaks a tick.</summary>
    public static string Format(DateTime value, string? format, string fallback)
    {
        if (string.IsNullOrWhiteSpace(format))
            format = fallback;

        try
        {
            return value.ToString(format);
        }
        catch (FormatException)
        {
            return value.ToString(fallback);
        }
    }
}
