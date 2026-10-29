namespace SwiftlyS2.Adverts.Models;

public class AdvertsConfig
{
    public string Prefix { get; set; } = "[Server]";
    public string PrefixColor { get; set; } = "red";
    // {server_name}; empty uses the hostname cvar, which 5stack leaves at "Counter-Strike 2"
    public string ServerName { get; set; } = "";
    // IANA id such as "Europe/Zagreb"; empty uses the server's local time
    public string TimeZone { get; set; } = "";
    public string TimeFormat { get; set; } = "HH:mm";
    public string DateFormat { get; set; } = "dd.MM.yyyy";
    public List<AdvertGroup> Groups { get; set; } = new();
}
