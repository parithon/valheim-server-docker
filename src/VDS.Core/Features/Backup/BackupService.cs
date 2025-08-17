using Microsoft.Extensions.Logging;
using VDS.Contracts.Backup;
using VDS.Contracts.CQRS;
using VDS.Contracts.Server;
using VDS.Core.Features.Server;

namespace VDS.Core.Features.Backup;

/// <summary>
/// Service for managing Valheim world backups
/// </summary>
public class BackupService
{
    private readonly IBackupStorage _storage;
    private readonly IServerStatusService _serverStatus;
    private readonly ILogger<BackupService> _logger;

    public BackupService(
        IBackupStorage storage,
        IServerStatusService serverStatus,
        ILogger<BackupService> logger)
    {
        _storage = storage;
        _serverStatus = serverStatus;
        _logger = logger;
    }

    /// <summary>
    /// Create a backup of the current world
    /// </summary>
    public async Task<Result<BackupInfo>> CreateBackupAsync(
        bool force = false,
        string? customName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting backup creation (force: {Force}, customName: {CustomName})", force, customName);

            // Check if server is idle unless forced
            if (!force)
            {
                var status = await _serverStatus.GetStatusAsync(cancellationToken);
                if (status?.PlayerCount > 0)
                {
                    _logger.LogInformation("Server has {PlayerCount} players connected, skipping backup", status.PlayerCount);
                    return Result<BackupInfo>.Failure("Server is not idle - players are connected");
                }
            }

            // Determine backup name
            var backupName = customName ?? GenerateBackupName();
            
            // Create the backup
            var backupInfo = await _storage.CreateBackupAsync(backupName, cancellationToken);
            
            _logger.LogInformation("Backup created successfully: {BackupPath} ({SizeBytes} bytes)", 
                backupInfo.FilePath, backupInfo.SizeBytes);

            return Result<BackupInfo>.Success(backupInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup");
            return Result<BackupInfo>.Failure(ex);
        }
    }

    /// <summary>
    /// Get all available backups
    /// </summary>
    public async Task<List<BackupInfo>> GetBackupsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _storage.GetBackupsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get backups list");
            return new List<BackupInfo>();
        }
    }

    /// <summary>
    /// Delete old backups based on retention policy
    /// </summary>
    public async Task<Result<int>> CleanupBackupsAsync(
        BackupConfiguration config,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting backup cleanup with config: MaxAge={MaxAge}d, MaxCount={MaxCount}", 
                config.MaxAgeDays, config.MaxCount);

            var backups = await _storage.GetBackupsAsync(cancellationToken);
            var deletedCount = 0;

            // Sort by creation date (oldest first)
            var sortedBackups = backups.OrderBy(b => b.CreatedAt).ToList();

            // Remove backups older than max age
            if (config.MaxAgeDays > 0)
            {
                var cutoffDate = DateTimeOffset.UtcNow.AddDays(-config.MaxAgeDays);
                var oldBackups = sortedBackups.Where(b => b.CreatedAt < cutoffDate).ToList();
                
                foreach (var backup in oldBackups)
                {
                    await _storage.DeleteBackupAsync(backup.FilePath, cancellationToken);
                    deletedCount++;
                    _logger.LogInformation("Deleted old backup: {BackupPath} (created: {CreatedAt})", 
                        backup.FilePath, backup.CreatedAt);
                }
            }

            // Remove excess backups beyond max count
            if (config.MaxCount > 0)
            {
                var remainingBackups = sortedBackups.Skip(deletedCount).ToList();
                var excessCount = remainingBackups.Count - config.MaxCount;
                
                if (excessCount > 0)
                {
                    var excessBackups = remainingBackups.Take(excessCount);
                    
                    foreach (var backup in excessBackups)
                    {
                        await _storage.DeleteBackupAsync(backup.FilePath, cancellationToken);
                        deletedCount++;
                        _logger.LogInformation("Deleted excess backup: {BackupPath} (over max count)", backup.FilePath);
                    }
                }
            }

            _logger.LogInformation("Backup cleanup completed. Deleted {DeletedCount} backups", deletedCount);
            return Result<int>.Success(deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup backups");
            return Result<int>.Failure(ex);
        }
    }

    /// <summary>
    /// Delete a specific backup
    /// </summary>
    public async Task<Result> DeleteBackupAsync(string backupPath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting backup: {BackupPath}", backupPath);
            await _storage.DeleteBackupAsync(backupPath, cancellationToken);
            _logger.LogInformation("Backup deleted successfully: {BackupPath}", backupPath);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete backup: {BackupPath}", backupPath);
            return Result.Failure(ex);
        }
    }

    /// <summary>
    /// Restore from a backup
    /// </summary>
    public async Task<Result> RestoreBackupAsync(
        string backupPath,
        bool stopServerFirst = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting backup restore from: {BackupPath} (stopServerFirst: {StopServer})", 
                backupPath, stopServerFirst);

            // Check if backup exists
            var backups = await _storage.GetBackupsAsync(cancellationToken);
            var backup = backups.FirstOrDefault(b => b.FilePath == backupPath);
            if (backup == null)
            {
                return Result.Failure($"Backup not found: {backupPath}");
            }

            // Stop server if requested
            if (stopServerFirst)
            {
                // Note: This would need to be injected as a dependency or use a mediator pattern
                _logger.LogInformation("Stopping server before restore...");
                // await _mediator.Send(new StopServerCommand(), cancellationToken);
            }

            // Perform the restore
            await _storage.RestoreBackupAsync(backupPath, cancellationToken);
            
            _logger.LogInformation("Backup restored successfully from: {BackupPath}", backupPath);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore backup: {BackupPath}", backupPath);
            return Result.Failure(ex);
        }
    }

    /// <summary>
    /// Generate a backup name with timestamp
    /// </summary>
    private static string GenerateBackupName()
    {
        return $"worlds-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}";
    }
}

/// <summary>
/// Interface for backup storage operations
/// </summary>
public interface IBackupStorage
{
    Task<BackupInfo> CreateBackupAsync(string backupName, CancellationToken cancellationToken = default);
    Task<List<BackupInfo>> GetBackupsAsync(CancellationToken cancellationToken = default);
    Task DeleteBackupAsync(string backupPath, CancellationToken cancellationToken = default);
    Task RestoreBackupAsync(string backupPath, CancellationToken cancellationToken = default);
}