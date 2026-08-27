using System;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;


namespace MCServerManagerPP;

public class ServerInstaller
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private readonly string _serverDirectory;

    public event Action<string>? OnProgress;

    public ServerInstaller(string serverDirectory)
    {
        _serverDirectory = serverDirectory;
    }

    public async Task<(bool Success, string JarName, string Version, string Message)> InstallVanillaAsync()
    {
        try
        {
            OnProgress?.Invoke("Sürüm listesi alınıyor...");
            string manifestJson = await _httpClient.GetStringAsync("https://launchermeta.mojang.com/mc/game/version_manifest.json");
            using var manifestDoc = JsonDocument.Parse(manifestJson);

            string latestRelease = manifestDoc.RootElement.GetProperty("latest").GetProperty("release").GetString()!;

            string? versionUrl = null;
            foreach (var v in manifestDoc.RootElement.GetProperty("versions").EnumerateArray())
            {
                if (v.GetProperty("id").GetString() == latestRelease)
                {
                    versionUrl = v.GetProperty("url").GetString();
                    break;
                }
            }

            if (versionUrl == null)
                return (false, "", "", "Sürüm bilgisi bulunamadı.");

            OnProgress?.Invoke($"Sürüm {latestRelease} detayları alınıyor...");
            string versionJson = await _httpClient.GetStringAsync(versionUrl);
            using var versionDoc = JsonDocument.Parse(versionJson);

            string serverJarUrl = versionDoc.RootElement
                .GetProperty("downloads")
                .GetProperty("server")
                .GetProperty("url")
                .GetString()!;

            string jarName = $"minecraft_server.{latestRelease}.jar";
            string jarPath = Path.Combine(_serverDirectory, jarName);

            OnProgress?.Invoke($"minecraft_server.{latestRelease}.jar indiriliyor...");

            byte[] jarBytes = await _httpClient.GetByteArrayAsync(serverJarUrl);
            await File.WriteAllBytesAsync(jarPath, jarBytes);

            OnProgress?.Invoke("İndirme tamamlandı.");

            return (true, jarName, latestRelease, "Vanilla server kuruldu.");
        }
        catch (Exception ex)
        {
            return (false, "", "", $"Kurulum hatası: {ex.Message}");
        }
    }

    public async Task<(bool Success, string JarName, string Version, string Message)> InstallPaperAsync()
    {
        try
        {
            OnProgress?.Invoke("PaperMC sürüm listesi alınıyor...");
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MCServerManagerPP/1.0");

            string projectJson = await _httpClient.GetStringAsync("https://fill.papermc.io/v3/projects/paper");
            using var projectDoc = JsonDocument.Parse(projectJson);

            string latestVersion = "";
            var versionsElement = projectDoc.RootElement.GetProperty("versions");

            if (versionsElement.ValueKind == JsonValueKind.Object)
            {
                // gruplu format: { "1.21": ["1.21", "1.21.1", ...], ... } -> son grubun son elemanı
                JsonProperty lastGroup = default;
                foreach (var group in versionsElement.EnumerateObject()) lastGroup = group;
                var versionList = lastGroup.Value.EnumerateArray().ToList();
                latestVersion = versionList[versionList.Count - 1].GetString()!;
            }
            else if (versionsElement.ValueKind == JsonValueKind.Array)
            {
                var versionList = versionsElement.EnumerateArray().ToList();
                latestVersion = versionList[versionList.Count - 1].GetString()!;
            }

            if (string.IsNullOrEmpty(latestVersion))
                return (false, "", "", "PaperMC sürüm bilgisi çözümlenemedi.");

            OnProgress?.Invoke($"PaperMC {latestVersion} build bilgisi alınıyor...");
            string buildsJson = await _httpClient.GetStringAsync($"https://fill.papermc.io/v3/projects/paper/versions/{latestVersion}/builds");
            using var buildsDoc = JsonDocument.Parse(buildsJson);

            JsonElement? bestBuild = null;
            foreach (var build in buildsDoc.RootElement.EnumerateArray())
            {
                string channel = build.GetProperty("channel").GetString() ?? "";
                if (channel == "STABLE" || channel == "RECOMMENDED")
                {
                    bestBuild = build;
                    break; // builds en yeniden eskiye sıralı geliyor, ilk uygun olan en yenisi
                }
            }
            bestBuild ??= buildsDoc.RootElement.EnumerateArray().FirstOrDefault();

            if (bestBuild == null)
                return (false, "", "", "Uygun bir PaperMC build'i bulunamadı.");

            var downloadInfo = bestBuild.Value.GetProperty("downloads").GetProperty("server:default");
            string downloadUrl = downloadInfo.GetProperty("url").GetString()!;
            string remoteJarName = downloadInfo.GetProperty("name").GetString()!;

            string localJarName = $"paper-{latestVersion}.jar";
            string jarPath = Path.Combine(_serverDirectory, localJarName);

            OnProgress?.Invoke($"{remoteJarName} indiriliyor...");

            byte[] jarBytes = await _httpClient.GetByteArrayAsync(downloadUrl);
            await File.WriteAllBytesAsync(jarPath, jarBytes);

            OnProgress?.Invoke("İndirme tamamlandı.");

            return (true, localJarName, latestVersion, "PaperMC server kuruldu.");
        }
        catch (Exception ex)
        {
            return (false, "", "", $"Kurulum hatası: {ex.Message}");
        }
    }
    public async Task<(bool Success, string JarName, string Version, string Message)> InstallFabricAsync()
    {
        try
        {
            OnProgress?.Invoke("Minecraft sürüm listesi alınıyor...");
            string manifestJson = await _httpClient.GetStringAsync("https://launchermeta.mojang.com/mc/game/version_manifest.json");
            using var manifestDoc = JsonDocument.Parse(manifestJson);
            string latestMcVersion = manifestDoc.RootElement.GetProperty("latest").GetProperty("release").GetString()!;

            OnProgress?.Invoke("Fabric loader sürümü alınıyor...");
            string loaderJson = await _httpClient.GetStringAsync($"https://meta.fabricmc.net/v2/versions/loader/{latestMcVersion}");
            using var loaderDoc = JsonDocument.Parse(loaderJson);
            var loaderArray = loaderDoc.RootElement.EnumerateArray().ToList();

            if (loaderArray.Count == 0)
                return (false, "", "", $"Fabric, {latestMcVersion} sürümünü henüz desteklemiyor.");

            string loaderVersion = loaderArray[0].GetProperty("loader").GetProperty("version").GetString()!;

            OnProgress?.Invoke("Fabric installer sürümü alınıyor...");
            string installerJson = await _httpClient.GetStringAsync("https://meta.fabricmc.net/v2/versions/installer");
            using var installerDoc = JsonDocument.Parse(installerJson);
            string installerVersion = installerDoc.RootElement[0].GetProperty("version").GetString()!;

            string downloadUrl = $"https://meta.fabricmc.net/v2/versions/loader/{latestMcVersion}/{loaderVersion}/{installerVersion}/server/jar";

            string localJarName = $"fabric-server-{latestMcVersion}-{loaderVersion}.jar";
            string jarPath = Path.Combine(_serverDirectory, localJarName);

            OnProgress?.Invoke($"{localJarName} indiriliyor...");

            byte[] jarBytes = await _httpClient.GetByteArrayAsync(downloadUrl);
            await File.WriteAllBytesAsync(jarPath, jarBytes);

            OnProgress?.Invoke("İndirme tamamlandı.");

            return (true, localJarName, latestMcVersion, "Fabric server kuruldu.");
        }
        catch (Exception ex)
        {
            return (false, "", "", $"Kurulum hatası: {ex.Message}");
        }
    }

    public async Task<(bool Success, string JarName, string Version, string Message, string LaunchArgs)> InstallForgeAsync()
    {
        try
        {
            OnProgress?.Invoke("Minecraft sürüm listesi alınıyor...");
            string manifestJson = await _httpClient.GetStringAsync("https://launchermeta.mojang.com/mc/game/version_manifest.json");
            using var manifestDoc = JsonDocument.Parse(manifestJson);
            string latestMcVersion = manifestDoc.RootElement.GetProperty("latest").GetProperty("release").GetString()!;

            OnProgress?.Invoke("Forge sürüm bilgisi alınıyor...");
            string promoJson = await _httpClient.GetStringAsync("https://files.minecraftforge.net/net/minecraftforge/forge/promotions_slim.json");
            using var promoDoc = JsonDocument.Parse(promoJson);

            var promos = promoDoc.RootElement.GetProperty("promos");
            string key = $"{latestMcVersion}-recommended";
            if (!promos.TryGetProperty(key, out var forgeVersionElement))
            {
                key = $"{latestMcVersion}-latest";
                if (!promos.TryGetProperty(key, out forgeVersionElement))
                    return (false, "", "", $"Forge, {latestMcVersion} sürümünü henüz desteklemiyor.", "");
            }

            string forgeVersion = forgeVersionElement.GetString()!;
            string fullVersion = $"{latestMcVersion}-{forgeVersion}";

            string installerUrl = $"https://maven.minecraftforge.net/net/minecraftforge/forge/{fullVersion}/forge-{fullVersion}-installer.jar";
            string installerName = $"forge-{fullVersion}-installer.jar";
            string installerPath = Path.Combine(_serverDirectory, installerName);

            OnProgress?.Invoke($"{installerName} indiriliyor...");
            byte[] installerBytes = await _httpClient.GetByteArrayAsync(installerUrl);
            await File.WriteAllBytesAsync(installerPath, installerBytes);

            OnProgress?.Invoke("Forge installer çalıştırılıyor, bu biraz sürebilir...");

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-jar \"{installerName}\" --installServer",
                WorkingDirectory = _serverDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true
            };

            using var process = System.Diagnostics.Process.Start(psi)!;

            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5));
            var exitTask = process.WaitForExitAsync();
            var completedTask = await Task.WhenAny(exitTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                try { process.Kill(true); } catch { }
                return (false, "", "", "Forge kurulumu zaman aşımına uğradı.", "");
            }

            OnProgress?.Invoke("Forge server dosyaları hazırlanıyor...");

            // Modern Forge (1.17+) doğrudan bir jar üretmiyor; win_args.txt argüman dosyası
            // ile birlikte çalıştırılması gerekiyor. Bu dosyanın yolunu bulup kaydediyoruz.
            string librariesPath = Path.Combine(_serverDirectory, "libraries", "net", "minecraftforge", "forge", fullVersion);
            string argsFileName = "win_args.txt";
            string argsFilePath = Path.Combine(librariesPath, argsFileName);

            string launchArgs;
            string jarNameForConfig;

            if (File.Exists(argsFilePath))
            {
                // Modern Forge: özel argüman dosyası ile başlat
                string relativeArgsPath = $"libraries/net/minecraftforge/forge/{fullVersion}/{argsFileName}";
                launchArgs = $"@user_jvm_args.txt @{relativeArgsPath}";
                jarNameForConfig = ""; // jar yerine launchArgs kullanılacak
            }
            else
            {
                // Eski Forge sürümleri: doğrudan bir server jar üretir
                string[] possibleJars = Directory.GetFiles(_serverDirectory, "forge-*-universal.jar");
                jarNameForConfig = possibleJars.Length > 0 ? Path.GetFileName(possibleJars[0]) : $"forge-{fullVersion}.jar";
                launchArgs = "";
            }

            try { File.Delete(installerPath); } catch { }
            try { File.Delete(installerPath + ".log"); } catch { }

            OnProgress?.Invoke("İndirme ve kurulum tamamlandı.");

            return (true, jarNameForConfig, latestMcVersion, "Forge server kuruldu.", launchArgs);
        }
        catch (Exception ex)
        {
            return (false, "", "", $"Kurulum hatası: {ex.Message}", "");
        }
    }
}