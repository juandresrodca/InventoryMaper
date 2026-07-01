# InventoryMapper

IT asset inventory system that collects hardware information from Windows machines and centralizes it through a REST API and a web dashboard.

## Projects

| Project | Type | Description |
|---|---|---|
| `InventoryMapper.Agent` | Windows Service | Runs on each managed machine, collects hardware info and sends periodic heartbeats to the API |
| `InventoryMapper.API` | ASP.NET Core Web API | Receives agent data, exposes endpoints for the web frontend |
| `InventoryMapper.Web` | ASP.NET Core MVC | Web dashboard for viewing and managing the device inventory |
| `InventoryMapper.Core` | Class Library | Domain models and business logic |
| `InventoryMapper.Infrastructure` | Class Library | EF Core + SQL Server data access, Excel export via ClosedXML |

## Requirements

- .NET 10 SDK
- SQL Server
- Windows (required for the Agent)

## Getting Started

1. Clone the repository
2. Configure the connection string in `src/InventoryMapper.API/appsettings.json`
3. Run database migrations from the solution root:
   ```
   dotnet ef database update --project src/InventoryMapper.Infrastructure --startup-project src/InventoryMapper.API
   ```
4. Start the API:
   ```
   dotnet run --project src/InventoryMapper.API
   ```
5. Start the web frontend:
   ```
   dotnet run --project src/InventoryMapper.Web
   ```
6. Deploy the agent on each machine you want to track, configured with the API URL and an agent key

## Agent Configuration

In `src/InventoryMapper.Agent/appsettings.json`, set the server URL, agent key, and heartbeat interval. The agent reads its handshake token from the Windows registry at:

```
HKEY_LOCAL_MACHINE\SOFTWARE\InventoryMapper\Agent\HandshakeToken
```

## License

MIT
