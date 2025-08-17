namespace VDS.Contracts.Server;

/// <summary>
/// Configuration for the Valheim dedicated server
/// </summary>
public record ServerConfiguration
{
    /// <summary>
    /// The name of the world to load
    /// </summary>
    public string WorldName { get; init; } = "Dedicated";

    /// <summary>
    /// The name of the server as it appears in the server list
    /// </summary>
    public string ServerName { get; init; } = "My Server";

    /// <summary>
    /// The port the server listens on
    /// </summary>
    public int ServerPort { get; init; } = 2456;

    /// <summary>
    /// The query port for server information
    /// </summary>
    public int ServerQueryPort => ServerPort + 1;

    /// <summary>
    /// The password required to join the server (empty for no password)
    /// </summary>
    public string ServerPassword { get; init; } = "secret";

    /// <summary>
    /// Additional arguments to pass to the server
    /// </summary>
    public string ServerArgs { get; init; } = string.Empty;

    /// <summary>
    /// Whether the server is listed publicly (1) or private (0)
    /// </summary>
    public int ServerPublic { get; init; } = 1;

    /// <summary>
    /// Whether crossplay is enabled
    /// </summary>
    public bool Crossplay { get; init; } = false;

    /// <summary>
    /// Whether to use the public test branch
    /// </summary>
    public bool PublicTest { get; init; } = false;

    /// <summary>
    /// Arguments for steamcmd
    /// </summary>
    public string SteamCmdArgs { get; init; } = "validate";

    /// <summary>
    /// User ID for file permissions
    /// </summary>
    public int Puid { get; init; } = 0;

    /// <summary>
    /// Group ID for file permissions
    /// </summary>
    public int Pgid { get; init; } = 0;

    /// <summary>
    /// Timezone for the container
    /// </summary>
    public string TimeZone { get; init; } = "Etc/UTC";
}