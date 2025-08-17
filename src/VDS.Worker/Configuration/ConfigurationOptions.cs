using VDS.Contracts.Server;
using VDS.Contracts.Backup;

namespace VDS.Worker.Configuration;

/// <summary>
/// Mutable server configuration for dependency injection binding
/// </summary>
public class ServerOptions
{
    public string WorldName { get; set; } = "Dedicated";
    public string ServerName { get; set; } = "My Server";
    public int ServerPort { get; set; } = 2456;
    public string ServerPassword { get; set; } = "secret";
    public string ServerArgs { get; set; } = string.Empty;
    public int ServerPublic { get; set; } = 1;
    public bool Crossplay { get; set; } = false;
    public bool PublicTest { get; set; } = false;
    public string SteamCmdArgs { get; set; } = "validate";
    public int Puid { get; set; } = 0;
    public int Pgid { get; set; } = 0;
    public string TimeZone { get; set; } = "Etc/UTC";

    /// <summary>
    /// Convert to immutable ServerConfiguration
    /// </summary>
    public ServerConfiguration ToServerConfiguration()
    {
        return new ServerConfiguration
        {
            WorldName = WorldName,
            ServerName = ServerName,
            ServerPort = ServerPort,
            ServerPassword = ServerPassword,
            ServerArgs = ServerArgs,
            ServerPublic = ServerPublic,
            Crossplay = Crossplay,
            PublicTest = PublicTest,
            SteamCmdArgs = SteamCmdArgs,
            Puid = Puid,
            Pgid = Pgid,
            TimeZone = TimeZone
        };
    }
}

/// <summary>
/// Mutable backup configuration for dependency injection binding
/// </summary>
public class BackupOptions
{
    public bool Enabled { get; set; } = false;
    public int IntervalSeconds { get; set; } = 3600;
    public string BackupDirectory { get; set; } = "/opt/valheim/backups";
    public int MaxAgeDays { get; set; } = 3;
    public int MaxCount { get; set; } = 0;
    public bool OnlyWhenIdle { get; set; } = true;
    public int IdleGracePeriodSeconds { get; set; } = 3600;
    public bool UseCompression { get; set; } = true;
    public string? CronSchedule { get; set; }

    /// <summary>
    /// Convert to immutable BackupConfiguration
    /// </summary>
    public BackupConfiguration ToBackupConfiguration()
    {
        return new BackupConfiguration
        {
            Enabled = Enabled,
            IntervalSeconds = IntervalSeconds,
            BackupDirectory = BackupDirectory,
            MaxAgeDays = MaxAgeDays,
            MaxCount = MaxCount,
            OnlyWhenIdle = OnlyWhenIdle,
            IdleGracePeriodSeconds = IdleGracePeriodSeconds,
            UseCompression = UseCompression,
            CronSchedule = CronSchedule
        };
    }
}