namespace SwiftlyS2.Adverts.Models;

public class AdvertsConfig
{
    public string Prefix { get; set; } = "[Server]";
    public string PrefixColor { get; set; } = "red";
    // IANA id such as "Europe/Zagreb"; empty uses the server's local time
    public string TimeZone { get; set; } = "";
    public string TimeFormat { get; set; } = "HH:mm";
    public string DateFormat { get; set; } = "dd.MM.yyyy";
    public List<AdvertGroup> Groups { get; set; } = new();
}
