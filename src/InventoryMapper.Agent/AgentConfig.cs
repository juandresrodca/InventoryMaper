namespace InventoryMapper.Agent;

public class AgentConfig
{
    public string ServerUrl { get; set; } = "https://localhost:7000";
    public string AgentKey { get; set; } = "";
    public string AgentVersion { get; set; } = "1.0.0";
    public int HeartbeatIntervalSeconds { get; set; } = 60;
    public string ApiEndpoint => $"{ServerUrl.TrimEnd('/')}/api/v1/monitoring/heartbeat";
}

// The agent key identifies this machine across restarts. If not set in configuration,
// a generated key is persisted to ProgramData so the server doesn't register a new
// agent on every process start.
public static class AgentKeyStore
{
    private static readonly string KeyFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "InventoryMapper", "agent.key");

    public static string Resolve(string? configuredKey)
    {
        if (!string.IsNullOrWhiteSpace(configuredKey)) return configuredKey;

        try
        {
            if (File.Exists(KeyFile))
            {
                var existing = File.ReadAllText(KeyFile).Trim();
                if (!string.IsNullOrEmpty(existing)) return existing;
            }
        }
        catch (Exception)
        {
            // Unreadable store — fall through and generate a fresh key
        }

        var key = Guid.NewGuid().ToString();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(KeyFile)!);
            File.WriteAllText(KeyFile, key);
        }
        catch (Exception)
        {
            // Not persistable (e.g. no write access) — key lives for this process only
        }
        return key;
    }
}
