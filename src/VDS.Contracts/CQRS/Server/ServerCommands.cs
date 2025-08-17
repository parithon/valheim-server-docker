using VDS.Contracts.Server;

namespace VDS.Contracts.CQRS.Server;

// Commands for server lifecycle management

/// <summary>
/// Start the Valheim server
/// </summary>
public record StartServerCommand : Command<Result>;

/// <summary>
/// Stop the Valheim server
/// </summary>
public record StopServerCommand : Command<Result>;

/// <summary>
/// Restart the Valheim server
/// </summary>
public record RestartServerCommand : Command<Result>;

/// <summary>
/// Update the Valheim server from Steam
/// </summary>
public record UpdateServerCommand : Command<Result>
{
    /// <summary>
    /// Force update even if players are connected
    /// </summary>
    public bool Force { get; init; } = false;
}

/// <summary>
/// Bootstrap the server environment
/// </summary>
public record BootstrapServerCommand : Command<Result>
{
    /// <summary>
    /// Whether to wipe existing data (debug mode)
    /// </summary>
    public bool StartFresh { get; init; } = false;
}

/// <summary>
/// Update server configuration
/// </summary>
public record UpdateServerConfigurationCommand : Command<Result>
{
    /// <summary>
    /// New server configuration
    /// </summary>
    public ServerConfiguration Configuration { get; init; } = new();
}

// Queries for server information

/// <summary>
/// Get current server status
/// </summary>
public record GetServerStatusQuery : Query<ServerStatusInfo>;

/// <summary>
/// Get server configuration
/// </summary>
public record GetServerConfigurationQuery : Query<ServerConfiguration>;

/// <summary>
/// Check if server is idle (no players connected)
/// </summary>
public record IsServerIdleQuery : Query<bool>;

/// <summary>
/// Get server process information
/// </summary>
public record GetServerProcessInfoQuery : Query<ServerProcessInfo>;

/// <summary>
/// Server process information
/// </summary>
public record ServerProcessInfo
{
    /// <summary>
    /// Process ID (0 if not running)
    /// </summary>
    public int ProcessId { get; init; }

    /// <summary>
    /// Whether the server process is running
    /// </summary>
    public bool IsRunning { get; init; }

    /// <summary>
    /// Server uptime
    /// </summary>
    public TimeSpan Uptime { get; init; }

    /// <summary>
    /// Memory usage in bytes
    /// </summary>
    public long MemoryUsageBytes { get; init; }

    /// <summary>
    /// CPU usage percentage
    /// </summary>
    public double CpuUsagePercent { get; init; }

    /// <summary>
    /// Last activity timestamp
    /// </summary>
    public DateTimeOffset LastActivity { get; init; }
}