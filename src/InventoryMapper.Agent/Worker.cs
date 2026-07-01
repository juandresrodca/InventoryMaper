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

                // On enrollment, the server issues a fresh per-agent secret that replaces the
                // one-time enrollment token — persist it so the next heartbeat authenticates with it.
                if (doc.RootElement.TryGetProperty("agentSecret", out var secretProp) && secretProp.ValueKind == JsonValueKind.String)
                {
                    var newSecret = secretProp.GetString();
                    if (!string.IsNullOrEmpty(newSecret))
                        StoreHandshakeToken(newSecret);
                }

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

    // Server-supplied updateUrl is untrusted input (see 0.3 in the security review): until update
    // binaries are Authenticode-signed against a pinned publisher cert, the host is allowlisted, and
    // delivery is verified via an out-of-band signed hash, this stays a no-op. Do not implement a
    // download-and-execute path here without all three controls in place — the agent runs as
    // LocalSystem, so a spoofed response would be a remote-code-execution vector on every managed machine.
    private Task InitiateUpdateAsync(string? url, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(url)) return Task.CompletedTask;

        logger.LogWarning("Update to a new version was advertised by the server but auto-update is disabled " +
            "pending signature verification and host allowlisting. Update manually. (Advertised URL: {Url})", url);
        return Task.CompletedTask;
    }

    private string GetHandshakeToken()
    {
        // First run: this holds the admin-issued, one-time enrollment token. After a successful
        // enrollment heartbeat, StoreHandshakeToken overwrites it with the persistent per-agent secret.
        var stored = Microsoft.Win32.Registry.GetValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\InventoryMapper\Agent",
            "HandshakeToken", null) as string;
        return stored ?? string.Empty;
    }

    private void StoreHandshakeToken(string token)
    {
        Microsoft.Win32.Registry.SetValue(
            @"HKEY_LOCAL_MACHINE\SOFTWARE\InventoryMapper\Agent",
            "HandshakeToken", token);
    }
}
