namespace VDS.Contracts.Backup;

/// <summary>
/// Configuration for backups
/// </summary>
public record BackupConfiguration
{
    /// <summary>
    /// Whether backups are enabled
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <summary>
    /// Backup interval in seconds
    /// </summary>
    public int IntervalSeconds { get; init; } = 3600;

    /// <summary>
    /// Directory where backups are stored
    /// </summary>
    public string BackupDirectory { get; init; } = "/opt/valheim/backups";

    /// <summary>
    /// Maximum age of backups in days (0 = disabled)
    /// </summary>
    public int MaxAgeDays { get; init; } = 3;

    /// <summary>
    /// Maximum number of backups to keep (0 = unlimited)
    /// </summary>
    public int MaxCount { get; init; } = 0;

    /// <summary>
    /// Only create backups when server is idle
    /// </summary>
    public bool OnlyWhenIdle { get; init; } = true;

    /// <summary>
    /// Grace period in seconds to consider server idle
    /// </summary>
    public int IdleGracePeriodSeconds { get; init; } = 3600;

    /// <summary>
    /// Whether to create zip compressed backups
    /// </summary>
    public bool UseCompression { get; init; } = true;

    /// <summary>
    /// Cron expression for scheduled backups (optional)
    /// </summary>
    public string? CronSchedule { get; init; }
}

/// <summary>
/// Information about a backup file
/// </summary>
public record BackupInfo
{
    /// <summary>
    /// Backup file path
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// File name without path
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// When the backup was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Whether the backup is compressed
    /// </summary>
    public bool IsCompressed { get; init; }

    /// <summary>
    /// World name that was backed up
    /// </summary>
    public string WorldName { get; init; } = string.Empty;
}