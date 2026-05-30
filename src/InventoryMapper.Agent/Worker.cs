using System.Text;
using System.Text.Json;

namespace InventoryMapper.Agent;

public class Worker(ILogger<Worker> logger, AgentConfig config, HttpClient http) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("InventoryMapper Agent v{Version} starting. Server: {Server}",
            config.AgentVersion, config.ServerUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendHeartbeatAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning("Heartbeat failed: {Message}", ex.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(config.HeartbeatIntervalSeconds), stoppingToken);
        }
    }

    private async Task SendHeartbeatAsync(CancellationToken ct)
    {
        DeviceInfo device;
        try
        {
            device = HardwareCollector.Collect();
        }
        catch
        {
            device = new DeviceInfo { Hostname = Environment.MachineName };
        }

        var payload = new
        {
            agentKey = config.AgentKey,
            hostname = device.Hostname,
            ipAddress = device.IpAddress,
            macAddress = device.MacAddress,
            organizationalUnit = device.OrganizationalUnit ?? "",
            agentVersion = config.AgentVersion,
            operatingSystem = device.OperatingSystem,
            osVersion = device.OsVersion,
            handshakeToken = GetHandshakeToken()
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await http.PostAsync(config.ApiEndpoint, content, ct);

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            try 
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("updateRequired", out var updateProp) && updateProp.GetBoolean())
                {
                    var newVersion = doc.RootElement.GetProperty("targetVersion").GetString();
                    var downloadUrl = doc.RootElement.GetProperty("updateUrl").GetString();
                    
                    logger.LogWarning("UPDATE DETECTED: Upgrading from {Current} to {New}", 
                        config.AgentVersion, newVersion);
                    
                    await InitiateUpdateAsync(downloadUrl, ct);
                }
            }
            catch { /* No update info in response */ }
            
            logger.LogDebug("Heartbeat sent successfully at {Time}", DateTime.UtcNow);
        }
        else
            logger.LogWarning("Heartbeat returned {StatusCode}", response.StatusCode);
    }

    private async Task InitiateUpdateAsync(string? url, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(url)) return;
        
        logger.LogInformation("Downloading update from {Url}...", url);
        // Implementation would download the file and execute a silent installer
        // that kills this process and replaces the binaries.
    }

    private string GetHandshakeToken()
    {
        // In production: load from secure storage/registry
        var stored = Microsoft.Win32.Registry.GetValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\InventoryMapper\Agent",
            "HandshakeToken", null) as string;
        return stored ?? string.Empty;
    }
}
