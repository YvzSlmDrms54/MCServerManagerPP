using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace MCServerManagerPP;

public class ServerManager
{
    private Process? _serverProcess;
    public event Action<string>? OnLogReceived;
    public event Action? OnServerStopped;
    public event Action<List<string>>? OnPlayerListUpdated;

    public bool IsRunning => _serverProcess != null && !_serverProcess.HasExited;

    private readonly string _serverDirectory = 
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "server");
    private string CurrentJarName => 
        !string.IsNullOrEmpty(Config.InstalledJarName) ? Config.InstalledJarName : "minecraft_server.26.2.jar";
    private readonly string _javaArgs = "-Xmx4G -Xms4G";

    public DateTime? LastBackupTime { get; private set; }
    public AppConfig Config { get; private set; } = new();

    private static readonly HttpClient _httpClient = new HttpClient();

    private bool _stopRequestedByUser = false;

    public ServerManager()
    {
        EnsureServerDirectoryExists();
        InitConfig();
    }

    private void EnsureServerDirectoryExists()
    {
        if (!Directory.Exists(_serverDirectory))
        {
            Directory.CreateDirectory(_serverDirectory);

            string readmePath = Path.Combine(_serverDirectory, "please put your minecraft server files here.md");
            File.WriteAllText(readmePath, 
                "# Minecraft Server Files\n\n" +
                "Bu klasöre şunları koy:\n" +
                "- minecraft_server.26.2.jar (veya kullandığın sürüm)\n" +
                "- eula.txt (eula=true yazan)\n\n" +
                "Sonra programı yeniden başlat.");
        }
    }

    private void InitConfig()
    {
        string settingsDir = Path.Combine(_serverDirectory, "MCServerManagerPP_Settings");
        Directory.CreateDirectory(settingsDir);

        string configPath = Path.Combine(settingsDir, "config.json");

        if (File.Exists(configPath))
        {
            string json = File.ReadAllText(configPath);
            Config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        else
        {
            Config = new AppConfig();
            SaveConfig();
        }
    }

    public void SaveConfig()
    {
        string settingsDir = Path.Combine(_serverDirectory, "MCServerManagerPP_Settings");
        Directory.CreateDirectory(settingsDir);
        string configPath = Path.Combine(settingsDir, "config.json");

        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(Config, options);
        File.WriteAllText(configPath, json);
    }

    public bool IsEulaAccepted()
    {
        string eulaPath = Path.Combine(_serverDirectory, "eula.txt");
        if (!File.Exists(eulaPath)) return false;

        string content = File.ReadAllText(eulaPath);
        return content.Contains("eula=true", StringComparison.OrdinalIgnoreCase);
    }

    public void AcceptEula()
    {
        string eulaPath = Path.Combine(_serverDirectory, "eula.txt");
        string content = "#Set Up by ServerManager++\n" +
                         $"#At the date {DateTime.Now:ddd MMM dd HH:mm:ss} 2026\n" +
                         "eula=true\n";
        File.WriteAllText(eulaPath, content);
    }

    private void ParseOutputLine(string line)
    {
        if (line.Contains("players online:"))
        {
            var afterColon = line.Split("players online:")[1].Trim();
            var players = string.IsNullOrWhiteSpace(afterColon)
                ? new List<string>()
                : afterColon.Split(',').Select(p => p.Trim()).ToList();

            _lastKnownPlayers = players;
            OnPlayerListUpdated?.Invoke(players);
            
        }

        if (line.Contains("joined the game"))
        {
            string name = ExtractPlayerName(line, "joined the game");
            _ = SendWebhookNotification("join", string.Format(Lang.Get("webhook_msg_join"), name));
        }
        else if (line.Contains("left the game"))
        {
            string name = ExtractPlayerName(line, "left the game");
            _ = SendWebhookNotification("leave", string.Format(Lang.Get("webhook_msg_leave"), name));
        }
    }

    private string ExtractPlayerName(string line, string suffix)
    {
        int idx = line.IndexOf("]: ");
        string rest = idx >= 0 ? line.Substring(idx + 3) : line;
        int suffixIdx = rest.IndexOf(suffix);
        return suffixIdx >= 0 ? rest.Substring(0, suffixIdx).Trim() : "Bir oyuncu";
    }

    private System.Threading.Timer? _playerListTimer;

    public void StartPlayerListPolling()
    {
        _playerListTimer = new System.Threading.Timer(_ =>
        {
                if (IsRunning) SendCommand("list");
        }, null, 3000, 5000);
    }

    public void StopPlayerListPolling()
    {
        _playerListTimer?.Dispose();
        _playerListTimer = null;
    }

    public Dictionary<string, string> LoadProperties()
    {
        var result = new Dictionary<string, string>();
        string path = Path.Combine(_serverDirectory, "server.properties");
        if (!File.Exists(path)) return result;

        foreach (var line in File.ReadAllLines(path))
        {
            if (line.StartsWith("#") || !line.Contains('=')) continue;

            var parts = line.Split('=', 2);
            result[parts[0].Trim()] = parts[1].Trim();
        }
        return result;
    }

    public void SaveProperties(Dictionary<string, string> properties)
    {
        string path = Path.Combine(_serverDirectory, "server.properties");
        var lines = new List<string>();
        foreach (var kvp in properties)
            lines.Add($"{kvp.Key}={kvp.Value}");
        File.WriteAllLines(path, lines);
    }

    public void EnableMaintenanceMode()
{
    if (Config.MaintenanceModeActive) return;

    var props = LoadProperties();
    Config.SavedMotd = props.TryGetValue("motd", out var motd) ? motd : "A Minecraft Server";
    Config.SavedWhitelistState = props.TryGetValue("white-list", out var wl) && wl == "true";

    props["motd"] = Lang.Get("maintenance_motd");
    props["white-list"] = "true";
    SaveProperties(props);

    Config.MaintenanceModeActive = true;
    SaveConfig();

    if (IsRunning)
    {
        SendCommand("whitelist on");
        SendCommand("whitelist reload");
        KickNonOpPlayers();
    }

    _ = SendWebhookNotification("maintenance", Lang.Get("webhook_msg_maintenance_on"));
}

    public void DisableMaintenanceMode()
    {
        if (!Config.MaintenanceModeActive) return;

        var props = LoadProperties();
        props["motd"] = Config.SavedMotd;
        props["white-list"] = Config.SavedWhitelistState ? "true" : "false";
        SaveProperties(props);

        Config.MaintenanceModeActive = false;
        SaveConfig();

        if (IsRunning && !Config.SavedWhitelistState)
        {
            SendCommand("whitelist off");
        }

        _ = SendWebhookNotification("maintenance", Lang.Get("webhook_msg_maintenance_off"));
    }

    private System.Threading.Timer? _maintenanceStartTimer;
    private System.Threading.Timer? _maintenanceEndTimer;

    public void StartMaintenanceSchedule()
    {
        StopMaintenanceSchedule();

        var startTime = new TimeSpan(Config.MaintenanceStartHour, Config.MaintenanceStartMinute, 0);
        var endTime = new TimeSpan(Config.MaintenanceEndHour, Config.MaintenanceEndMinute, 0);

        _maintenanceStartTimer = CreateDailyTimer(startTime, () => EnableMaintenanceMode());
        _maintenanceEndTimer = CreateDailyTimer(endTime, () => DisableMaintenanceMode());
    }

    public void StopMaintenanceSchedule()
    {
        _maintenanceStartTimer?.Dispose();
        _maintenanceEndTimer?.Dispose();
        _maintenanceStartTimer = null;
        _maintenanceEndTimer = null;
    }

    private System.Threading.Timer CreateDailyTimer(TimeSpan targetTime, Action action)
    {
        DateTime now = DateTime.Now;
        DateTime nextRun = DateTime.Today.Add(targetTime);
        if (nextRun <= now)
            nextRun = nextRun.AddDays(1);

        TimeSpan dueTime = nextRun - now;

        return new System.Threading.Timer(_ => action(), null, dueTime, TimeSpan.FromDays(1));
    }

    private void KickNonOpPlayers()
    {
        var ops = GetOpsList().Select(o => o.ToLower()).ToHashSet();
        var online = GetOnlinePlayersSnapshot();

        foreach (var player in online)
        {
            if (!ops.Contains(player.ToLower()))
            {
                SendCommand($"kick {player} {Lang.Get("maintenance_kick_message")}");
            }
        }

    }

    private System.Threading.Timer? _restartTimer;

    public void StartRestartSchedule()
    {
        StopRestartSchedule();
        var restartTime = new TimeSpan(Config.RestartHour, Config.RestartMinute, 0);
        _restartTimer = CreateDailyTimer(restartTime, () =>
        {
            if (IsRunning)
            {
                StopServer();
                System.Threading.Thread.Sleep(3000);
                StartServer();
            }
        });
    }

    public void StopRestartSchedule()
    {
        _restartTimer?.Dispose();
        _restartTimer = null;
    }
    
    private List<string> _lastKnownPlayers = new();
    public List<string> GetOnlinePlayersSnapshot() => _lastKnownPlayers;

    public string BackupWorld()
    {
        string worldPath = Path.Combine(_serverDirectory, "world");
        if (!Directory.Exists(worldPath)) 
            return "HATA: world klasörü bulunamadı.";

        string backupsDir = Path.Combine(_serverDirectory, "backups");
        Directory.CreateDirectory(backupsDir);

        string fileName = $"world_backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.zip";
        string zipPath = Path.Combine(backupsDir, fileName);

        try
        {
            ZipFile.CreateFromDirectory(worldPath, zipPath);
            LastBackupTime = DateTime.Now;
            _ = SendWebhookNotification("backup", string.Format(Lang.Get("webhook_msg_backup"), fileName));
            return $"Yedekleme tamamlandı: {fileName}";
        }
        catch (IOException)
        {
            return "HATA: Dünya dosyaları şu an kullanılıyor (server aktif olarak yazıyor). Yedekleme atlandı.";
        }
    }

    public List<(string FileName, DateTime Date)> GetBackupList()
    {
        string backupsDir = Path.Combine(_serverDirectory, "backups");
        var result = new List<(string, DateTime)>();
        if (!Directory.Exists(backupsDir)) return result;

        foreach (var file in Directory.GetFiles(backupsDir, "*.zip"))
        {
            var info = new FileInfo(file);
            result.Add((info.Name, info.LastWriteTime));
        }
        return result.OrderByDescending(r => r.Item2).ToList();
    }

    private System.Threading.Timer? _backupTimer;

    public void StartAutoBackup(TimeSpan targetTime)
    {
        _backupTimer?.Dispose();

        DateTime now = DateTime.Now;
        DateTime nextRun = DateTime.Today.Add(targetTime);
        if (nextRun <= now)
            nextRun = nextRun.AddDays(1);

        TimeSpan dueTime = nextRun - now;

        _backupTimer = new System.Threading.Timer(_ =>
        {
            if (IsRunning)
            {
                SendCommand("save-all");
                System.Threading.Thread.Sleep(2000);
                BackupWorld();
                OnLogReceived?.Invoke($"[{DateTime.Now:HH:mm:ss}] [{Lang.Get("log_backup")}] {Lang.Get("log_backup_auto")}");
            }
        }, null, dueTime, TimeSpan.FromDays(1));
    }

    private List<string> GetNamesFromJsonFile(string fileName)
    {
        string path = Path.Combine(_serverDirectory, fileName);
        var result = new List<string>();
        if (!File.Exists(path)) return result;

        try
        {
            string json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.TryGetProperty("name", out var nameProp))
                    result.Add(nameProp.GetString() ?? "");
            }
        }
        catch
        {
            // dosya henüz yazılıyor olabilir, sessizce geç
        }
        return result;
    }   

    public List<string> GetOpsList() => GetNamesFromJsonFile("ops.json");
    public List<string> GetWhitelistList() => GetNamesFromJsonFile("whitelist.json");
    public List<string> GetBannedList() => GetNamesFromJsonFile("banned-players.json");
    public void OpPlayer(string name) => SendCommand($"op {name}");
    public void DeopPlayer(string name) => SendCommand($"deop {name}");
    public void WhitelistAdd(string name) => SendCommand($"whitelist add {name}");
    public void WhitelistRemove(string name) => SendCommand($"whitelist remove {name}");
    public void BanPlayer(string name) => SendCommand($"ban {name}");
    public void PardonPlayer(string name) => SendCommand($"pardon {name}");


    public void StopAutoBackup()
    {
        _backupTimer?.Dispose();
        _backupTimer = null;
    }

    public List<WebhookEntry> LoadWebhooks()
    {
        return Config.Webhooks;
    }

    public void SaveWebhooksToConfig(List<WebhookEntry> webhooks)
    {
        Config.Webhooks = webhooks;
        SaveConfig();
    }

    public async Task SendWebhookNotification(string eventKey, string message)
    {
        var webhooks = LoadWebhooks();
        foreach (var webhook in webhooks)
        {
            bool shouldSend = eventKey switch
            {
                "start" => webhook.NotifyServerStart,
                "stop" => webhook.NotifyServerStop,
                "join" => webhook.NotifyPlayerJoin,
                "leave" => webhook.NotifyPlayerLeave,
                "backup" => webhook.NotifyBackup,
                "maintenance" => webhook.NotifyMaintenance,
                "crash" => webhook.NotifyCrash,
                _ => false
            };

            if (!shouldSend) continue;

            try
            {
                string finalMessage = message;
                if (!string.IsNullOrWhiteSpace(webhook.MentionTarget))
                {
                    string mention = webhook.MentionTarget.Trim().ToLower() == "everyone"
                        ? "@everyone"
                        : $"<@&{webhook.MentionTarget.Trim()}>";
                    finalMessage = $"{mention}\n{message}";
                }

                var json = JsonSerializer.Serialize(new { content = finalMessage });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(webhook.Url, content);

                if (!response.IsSuccessStatusCode)
                {
                    string body = await response.Content.ReadAsStringAsync();
                    OnLogReceived?.Invoke($"[WEBHOOK HATA] {webhook.Name}: {(int)response.StatusCode} - {body}");
                }
            }
            catch (Exception ex)
            {
                OnLogReceived?.Invoke($"[WEBHOOK HATA] {webhook.Name}: {ex.Message}");
            }
        }
    }

    public string DetectServerType()
    {
        bool hasPlugins = Directory.Exists(Path.Combine(_serverDirectory, "plugins"));
        bool hasMods = Directory.Exists(Path.Combine(_serverDirectory, "mods"));

        bool hasJar = !string.IsNullOrEmpty(Config.InstalledJarName) &&
                      File.Exists(Path.Combine(_serverDirectory, Config.InstalledJarName));

        // fallback: eskiden elle kurulmuş, config'de kaydı olmayan bir jar var mı diye bak
        if (!hasJar)
        {
            var jarFiles = Directory.Exists(_serverDirectory)
                ? Directory.GetFiles(_serverDirectory, "*.jar")
                : Array.Empty<string>();
            hasJar = jarFiles.Length > 0;
        }

        if (!hasJar) return "not_installed";
        if (hasPlugins) return "paper";
        if (hasMods) return DetectModLoader();
        return string.IsNullOrEmpty(Config.InstalledServerType) ? "vanilla" : Config.InstalledServerType;
    }

    public async Task<string> InstallVanillaServer()
    {
        var installer = new ServerInstaller(_serverDirectory);
        installer.OnProgress += msg => OnLogReceived?.Invoke($"[KURULUM] {msg}");

        var result = await installer.InstallVanillaAsync();

        if (result.Success)
        {
            Config.InstalledJarName = result.JarName;
            Config.InstalledVersion = result.Version;
            Config.InstalledServerType = "vanilla";
            SaveConfig();
        }

        return result.Message;
    }

    public async Task<string> InstallPaperServer()
    {
        var installer = new ServerInstaller(_serverDirectory);
        installer.OnProgress += msg => OnLogReceived?.Invoke($"[KURULUM] {msg}");

        var result = await installer.InstallPaperAsync();

        if (result.Success)
        {
            Config.InstalledJarName = result.JarName;
            Config.InstalledVersion = result.Version;
            Config.InstalledServerType = "paper";
            SaveConfig();
        }

        return result.Message;
    }

    public async Task<string> InstallFabricServer()
    {
        var installer = new ServerInstaller(_serverDirectory);
        installer.OnProgress += msg => OnLogReceived?.Invoke($"[KURULUM] {msg}");

        var result = await installer.InstallFabricAsync();

        if (result.Success)
        {
            Config.InstalledJarName = result.JarName;
            Config.InstalledVersion = result.Version;
            Config.InstalledServerType = "fabric";
            SaveConfig();
        }

        return result.Message;
    }

    public async Task<string> InstallForgeServer()
    {
        var installer = new ServerInstaller(_serverDirectory);
        installer.OnProgress += msg => OnLogReceived?.Invoke($"[KURULUM] {msg}");

        var result = await installer.InstallForgeAsync();

        if (result.Success)
        {
            Config.InstalledJarName = result.JarName;
            Config.InstalledVersion = result.Version;
            Config.InstalledServerType = "forge";
            Config.InstalledLaunchArgs = result.LaunchArgs;
            SaveConfig();
        }   

        return result.Message;
    }

    private string DetectModLoader()
    {
        string librariesPath = Path.Combine(_serverDirectory, "libraries");
        if (Directory.Exists(librariesPath))
        {
            if (Directory.Exists(Path.Combine(librariesPath, "net", "fabricmc")))
                return "fabric";
            if (Directory.Exists(Path.Combine(librariesPath, "net", "minecraftforge")))
                return "forge";
        }

        string modsPath = Path.Combine(_serverDirectory, "mods");
        foreach (var file in Directory.GetFiles(modsPath, "*.jar"))
        {
            string name = Path.GetFileName(file).ToLower();
            if (name.Contains("fabric")) return "fabric";
            if (name.Contains("forge")) return "forge";
        }

        return "fabric_or_forge"; // ikisi de tespit edilemezse, belirsiz kalsın
    }

    public void StartServer()
    {
        if (IsRunning) return;

        bool usesLaunchArgs = !string.IsNullOrEmpty(Config.InstalledLaunchArgs);
        string jarPath = Path.Combine(_serverDirectory, CurrentJarName);

        if (!usesLaunchArgs && !File.Exists(jarPath))
        {
            OnLogReceived?.Invoke($"[{Lang.Get("log_error")}] {Lang.Get("log_jar_not_found")}: {jarPath}");
            OnLogReceived?.Invoke($"[{Lang.Get("log_error")}] {Lang.Get("log_put_jar")}");
            return;
        }
        _stopRequestedByUser = false;

        string arguments = usesLaunchArgs
            ? $"{Config.InstalledLaunchArgs} nogui"
            : $"{_javaArgs} -jar {CurrentJarName} nogui";

        var psi = new ProcessStartInfo
        {
            FileName = "java",
            Arguments = arguments,
            WorkingDirectory = _serverDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        _serverProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };

        _serverProcess.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null)
            {
                ParseOutputLine(e.Data);

                bool isPlayerListSpam = e.Data.Contains("players online:") || e.Data.Contains("There are");
                if (!isPlayerListSpam)
                {
                    OnLogReceived?.Invoke(e.Data);
                }
            }
        };

        _serverProcess.ErrorDataReceived += (s, e) =>
        {
            if (e.Data != null)
                OnLogReceived?.Invoke("[ERROR] " + e.Data);
        };

        _serverProcess.Exited += (s, e) =>
        {
            if (!_stopRequestedByUser)
            {
                string separator = new string('-', 50);
                string crashLog = $"{separator}\n💥 [{DateTime.Now:HH:mm:ss}] {Lang.Get("log_server_crashed")}\n{separator}";
                OnLogReceived?.Invoke(crashLog);
                _ = SendWebhookNotification("crash", Lang.Get("webhook_msg_crash"));
            }

            BackupWorld();
            OnLogReceived?.Invoke($"[{DateTime.Now:HH:mm:ss}] [{Lang.Get("log_backup")}] {Lang.Get("log_backup_on_close")}");
            _ = SendWebhookNotification("stop", Lang.Get("webhook_msg_stop"));
            OnServerStopped?.Invoke();

            _stopRequestedByUser = false;
        };

        _serverProcess.Start();
        _serverProcess.BeginOutputReadLine();
        _serverProcess.BeginErrorReadLine();

        _ = SendWebhookNotification("start", Lang.Get("webhook_msg_start"));
    }   

    public void SendCommand(string command)
    {
        if (!IsRunning || _serverProcess?.StandardInput == null) return;
        _serverProcess.StandardInput.WriteLine(command);
        _serverProcess.StandardInput.Flush();
    }

    public void StopServer()
    {
        if (IsRunning)
        {
            _stopRequestedByUser = true;
            SendCommand("stop");
        }
    }
}