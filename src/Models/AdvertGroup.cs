namespace SwiftlyS2.Adverts.Models;

public class AdvertGroup
{
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 60;
    public string Order { get; set; } = "sequential";
    public string DisplayType { get; set; } = "chat";
    public string Target { get; set; } = "all";
    public List<string> SkipPermissions { get; set; } = new();
    public List<string> Messages { get; set; } = new();
}
