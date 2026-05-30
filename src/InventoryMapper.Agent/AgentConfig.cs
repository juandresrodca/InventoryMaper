namespace InventoryMapper.Agent;

public class AgentConfig
{
    public string ServerUrl { get; set; } = "https://localhost:7000";
    public string AgentKey { get; set; } = Guid.NewGuid().ToString();
    public string AgentVersion { get; set; } = "1.0.0";
    public int HeartbeatIntervalSeconds { get; set; } = 60;
    public string ApiEndpoint => $"{ServerUrl.TrimEnd('/')}/api/v1/monitoring/heartbeat";
}
