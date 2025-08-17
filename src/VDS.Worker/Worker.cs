using VDS.Core.Features.Server;
using VDS.Core.Features.Backup;
using VDS.Contracts.CQRS.Server;
using VDS.Contracts.CQRS.Backup;
using VDS.Contracts.Server;
using VDS.Contracts.Backup;
using Microsoft.Extensions.Options;

namespace VDS.Worker;

/// <summary>
/// Main worker service that manages Valheim server lifecycle and maintenance tasks
/// Replaces supervisord functionality
/// </summary>
public class ValheimServerWorker : BackgroundService
{
    private readonly ILogger<ValheimServerWorker> _logger;
    private readonly ServerLifecycleService _serverLifecycle;
    private readonly BackupService _backupService;
    private readonly IOptions<ServerConfiguration> _serverConfig;
    private readonly IOptions<BackupConfiguration> _backupConfig;
    private readonly Timer _updateTimer;
    private readonly Timer _backupTimer;

    public ValheimServerWorker(
        ILogger<ValheimServerWorker> logger,
        ServerLifecycleService serverLifecycle,
        BackupService backupService,
        IOptions<ServerConfiguration> serverConfig,
        IOptions<BackupConfiguration> backupConfig)
    {
        _logger = logger;
        _serverLifecycle = serverLifecycle;
        _backupService = backupService;
        _serverConfig = serverConfig;
        _backupConfig = backupConfig;

        // Initialize timers for periodic tasks
        _updateTimer = new Timer(CheckForUpdates, null, Timeout.Infinite, Timeout.Infinite);
        _backupTimer = new Timer(PerformBackup, null, Timeout.Infinite, Timeout.Infinite);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Valheim Server Worker starting up...");

        try
        {
            // Bootstrap the server environment
            await BootstrapServerAsync(stoppingToken);

            // Start the server
            await StartServerAsync(stoppingToken);

            // Start periodic tasks
            StartPeriodicTasks();

            // Main worker loop - monitor server status
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    MonitorServerHealth(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Check every 30 seconds
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in worker monitoring loop");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Brief delay before retrying
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error in Valheim Server Worker");
            throw;
        }
        finally
        {
            _logger.LogInformation("Valheim Server Worker shutting down...");
            await ShutdownAsync(stoppingToken);
        }
    }

    private async Task BootstrapServerAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Bootstrapping Valheim server environment...");

        try
        {
            // This would include tasks like:
            // - Setting up file permissions
            // - Creating necessary directories
            // - Downloading/updating server files
            // - Installing/updating mods
            // - Setting up configuration files

            await Task.CompletedTask; // Placeholder for actual bootstrap work

            _logger.LogInformation("Server environment bootstrapped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bootstrap server environment");
            throw;
        }
    }

    private async Task StartServerAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Valheim server...");

        var result = await _serverLifecycle.StartServerAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            _logger.LogError("Failed to start Valheim server: {Error}", result.ErrorMessage);
            throw new InvalidOperationException($"Failed to start server: {result.ErrorMessage}");
        }

        _logger.LogInformation("Valheim server started successfully");
    }

    private void StartPeriodicTasks()
    {
        var backupConfig = _backupConfig.Value;

        // Start backup timer if backups are enabled
        if (backupConfig.Enabled && backupConfig.IntervalSeconds > 0)
        {
            var backupInterval = TimeSpan.FromSeconds(backupConfig.IntervalSeconds);
            _backupTimer.Change(backupInterval, backupInterval);
            _logger.LogInformation("Backup scheduler started with interval: {Interval}", backupInterval);
        }

        // Start update check timer (every hour by default)
        var updateInterval = TimeSpan.FromHours(1);
        _updateTimer.Change(updateInterval, updateInterval);
        _logger.LogInformation("Update check scheduler started with interval: {Interval}", updateInterval);
    }

    private void MonitorServerHealth(CancellationToken cancellationToken)
    {
        try
        {
            // Add server health monitoring logic here
            // - Memory usage monitoring
            // - CPU usage monitoring  
            // - Network connectivity checks
            // - Log analysis for errors
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error monitoring server health");
        }
    }

    private async void CheckForUpdates(object? state)
    {
        try
        {
            _logger.LogDebug("Checking for server updates...");

            // Check if server is idle before updating
            var isIdle = await _serverLifecycle.IsServerIdleAsync();
            if (!isIdle)
            {
                _logger.LogDebug("Server has active players, skipping update check");
                return;
            }

            // TODO: Implement update checking logic
            // This would include:
            // - Checking Steam for server updates
            // - Checking for mod updates
            // - Performing updates if available
            // - Restarting server if updated

            _logger.LogDebug("Update check completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during update check");
        }
    }

    private async void PerformBackup(object? state)
    {
        try
        {
            _logger.LogDebug("Starting scheduled backup...");

            var backupConfig = _backupConfig.Value;
            var result = await _backupService.CreateBackupAsync(force: false, cancellationToken: CancellationToken.None);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Scheduled backup completed: {BackupPath}", result.Value?.FilePath);

                // Clean up old backups
                var cleanupResult = await _backupService.CleanupBackupsAsync(backupConfig, CancellationToken.None);
                if (cleanupResult.IsSuccess && cleanupResult.Value > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} old backups", cleanupResult.Value);
                }
            }
            else
            {
                _logger.LogWarning("Scheduled backup failed: {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled backup");
        }
    }

    private async Task ShutdownAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Stop timers
            await _updateTimer.DisposeAsync();
            await _backupTimer.DisposeAsync();

            // Stop the server gracefully
            var result = await _serverLifecycle.StopServerAsync(cancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to stop server gracefully: {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during shutdown");
        }
    }

    public override void Dispose()
    {
        _updateTimer?.Dispose();
        _backupTimer?.Dispose();
        base.Dispose();
    }
}
