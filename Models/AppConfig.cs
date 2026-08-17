using System.Collections.Generic;

namespace MCServerManagerPP;

public class AppConfig
{
    public string Language { get; set; } = "";
    public string Theme { get; set; } = "dark";
    public string InstalledJarName { get; set; } = "";
    public string InstalledVersion { get; set; } = "";
    public string InstalledServerType { get; set; } = "";
    public BackupSettings Backup { get; set; } = new();
    public List<WebhookEntry> Webhooks { get; set; } = new();
    public bool MaintenanceModeActive { get; set; } = false;
    public string SavedMotd { get; set; } = "";
    public bool SavedWhitelistState { get; set; } = false;
    public bool MaintenanceScheduleEnabled { get; set; } = false;
    public int MaintenanceStartHour { get; set; } = 3;
    public int MaintenanceStartMinute { get; set; } = 0;
    public int MaintenanceEndHour { get; set; } = 4;
    public int MaintenanceEndMinute { get; set; } = 0;
    public bool RestartScheduleEnabled { get; set; } = false;
    public int RestartHour { get; set; } = 5;
    public int RestartMinute { get; set; } = 0;
}

public class BackupSettings
{
    public int Hour { get; set; } = 3;
    public int Minute { get; set; } = 0;
    public bool Enabled { get; set; } = true;
}