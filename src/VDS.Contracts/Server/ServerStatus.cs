namespace VDS.Contracts.Server;

/// <summary>
/// Represents the current status of the Valheim server
/// </summary>
public enum ServerStatus
{
    Unknown,
    Bootstrapping,
    Downloading,
    Starting,
    Running,
    Stopping,
    Stopped,
    Error
}

/// <summary>
/// Server status information including player data
/// </summary>
public record ServerStatusInfo
{
    /// <summary>
    /// Current server status
    /// </summary>
    public ServerStatus Status { get; init; }

    /// <summary>
    /// Server name as reported by Steam query
    /// </summary>
    public string? ServerName { get; init; }

    /// <summary>
    /// Server type (Dedicated)
    /// </summary>
    public string? ServerType { get; init; }

    /// <summary>
    /// Platform (Linux)
    /// </summary>
    public string? Platform { get; init; }

    /// <summary>
    /// Number of connected players
    /// </summary>
    public int PlayerCount { get; init; }

    /// <summary>
    /// Whether the server is password protected
    /// </summary>
    public bool PasswordProtected { get; init; }

    /// <summary>
    /// Whether VAC is enabled
    /// </summary>
    public bool VacEnabled { get; init; }

    /// <summary>
    /// Server port
    /// </summary>
    public int Port { get; init; }

    /// <summary>
    /// Steam ID of the server
    /// </summary>
    public long SteamId { get; init; }

    /// <summary>
    /// Server keywords/tags
    /// </summary>
    public string? Keywords { get; init; }

    /// <summary>
    /// Game ID
    /// </summary>
    public long GameId { get; init; }

    /// <summary>
    /// List of connected players
    /// </summary>
    public List<PlayerInfo> Players { get; init; } = [];

    /// <summary>
    /// Last time status was updated
    /// </summary>
    public DateTimeOffset LastUpdate { get; init; }

    /// <summary>
    /// Error message if status retrieval failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Information about a connected player
/// </summary>
public record PlayerInfo
{
    /// <summary>
    /// Player name
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Player score
    /// </summary>
    public long Score { get; init; }

    /// <summary>
    /// Time the player has been connected
    /// </summary>
    public TimeSpan Duration { get; init; }
}