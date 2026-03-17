namespace SwiftlyS2.Adverts.Models;

public class AdvertsConfig
{
    public string Prefix { get; set; } = "[Server]";
    public string PrefixColor { get; set; } = "red";
    public Dictionary<string, AdvertGroup> Groups { get; set; } = new();
}
