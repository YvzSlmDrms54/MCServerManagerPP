namespace MCServerManagerPP;

public class WebhookEntry
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public bool NotifyServerStart { get; set; }
    public bool NotifyServerStop { get; set; }
    public bool NotifyPlayerJoin { get; set; }
    public bool NotifyPlayerLeave { get; set; }
    public bool NotifyMaintenance { get; set; }
    public bool NotifyCrash { get; set; }
    public bool NotifyBackup { get; set; }
    public string MentionTarget { get; set; } = "";
}