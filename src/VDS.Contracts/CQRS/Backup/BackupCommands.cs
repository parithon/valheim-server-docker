using VDS.Contracts.Backup;

namespace VDS.Contracts.CQRS.Backup;

// Commands for backup management

/// <summary>
/// Create a backup of the current world
/// </summary>
public record CreateBackupCommand : Command<Result<BackupInfo>>
{
    /// <summary>
    /// Force backup even if server is not idle
    /// </summary>
    public bool Force { get; init; } = false;

    /// <summary>
    /// Custom backup name (optional)
    /// </summary>
    public string? BackupName { get; init; }
}

/// <summary>
/// Delete old backups based on retention policy
/// </summary>
public record CleanupBackupsCommand : Command<Result<int>>; // Returns number of deleted backups

/// <summary>
/// Delete a specific backup
/// </summary>
public record DeleteBackupCommand : Command<Result>
{
    /// <summary>
    /// Full path to the backup file to delete
    /// </summary>
    public string BackupPath { get; init; } = string.Empty;
}

/// <summary>
/// Restore from a backup
/// </summary>
public record RestoreBackupCommand : Command<Result>
{
    /// <summary>
    /// Full path to the backup file to restore from
    /// </summary>
    public string BackupPath { get; init; } = string.Empty;

    /// <summary>
    /// Stop server before restore (recommended)
    /// </summary>
    public bool StopServerFirst { get; init; } = true;
}

/// <summary>
/// Update backup configuration
/// </summary>
public record UpdateBackupConfigurationCommand : Command<Result>
{
    /// <summary>
    /// New backup configuration
    /// </summary>
    public BackupConfiguration Configuration { get; init; } = new();
}

// Queries for backup information

/// <summary>
/// Get all available backups
/// </summary>
public record GetBackupsQuery : Query<List<BackupInfo>>;

/// <summary>
/// Get backup configuration
/// </summary>
public record GetBackupConfigurationQuery : Query<BackupConfiguration>;

/// <summary>
/// Get backup statistics
/// </summary>
public record GetBackupStatisticsQuery : Query<BackupStatistics>;

/// <summary>
/// Backup statistics
/// </summary>
public record BackupStatistics
{
    /// <summary>
    /// Total number of backups
    /// </summary>
    public int TotalBackups { get; init; }

    /// <summary>
    /// Total size of all backups in bytes
    /// </summary>
    public long TotalSizeBytes { get; init; }

    /// <summary>
    /// Oldest backup date
    /// </summary>
    public DateTimeOffset? OldestBackup { get; init; }

    /// <summary>
    /// Newest backup date
    /// </summary>
    public DateTimeOffset? NewestBackup { get; init; }

    /// <summary>
    /// Average backup size in bytes
    /// </summary>
    public long AverageBackupSize { get; init; }

    /// <summary>
    /// Next scheduled backup time
    /// </summary>
    public DateTimeOffset? NextScheduledBackup { get; init; }
}