using Microsoft.Extensions.Logging;
using VDS.Contracts.CQRS;
using VDS.Contracts.CQRS.Server;
using VDS.Contracts.Server;

namespace VDS.Core.Features.Server;

/// <summary>
/// Handles server lifecycle operations
/// </summary>
public class ServerLifecycleService
{
    private readonly IServerProcessManager _processManager;
    private readonly IServerStatusService _statusService;
    private readonly ILogger<ServerLifecycleService> _logger;

    public ServerLifecycleService(
        IServerProcessManager processManager,
        IServerStatusService statusService,
        ILogger<ServerLifecycleService> logger)
    {
        _processManager = processManager;
        _statusService = statusService;
        _logger = logger;
    }

    /// <summary>
    /// Start the Valheim server process
    /// </summary>
    public async Task<Result> StartServerAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting Valheim server...");
            await _statusService.UpdateStatusAsync(ServerStatus.Starting, cancellationToken);

            var result = await _processManager.StartAsync(cancellationToken);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Valheim server started successfully");
                await _statusService.UpdateStatusAsync(ServerStatus.Running, cancellationToken);
            }
            else
            {
                _logger.LogError("Failed to start Valheim server: {Error}", result.ErrorMessage);
                await _statusService.UpdateStatusAsync(ServerStatus.Error, cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while starting server");
            await _statusService.UpdateStatusAsync(ServerStatus.Error, cancellationToken);
            return Result.Failure(ex);
        }
    }

    /// <summary>
    /// Stop the Valheim server process
    /// </summary>
    public async Task<Result> StopServerAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Stopping Valheim server...");
            await _statusService.UpdateStatusAsync(ServerStatus.Stopping, cancellationToken);

            var result = await _processManager.StopAsync(cancellationToken);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Valheim server stopped successfully");
                await _statusService.UpdateStatusAsync(ServerStatus.Stopped, cancellationToken);
            }
            else
            {
                _logger.LogError("Failed to stop Valheim server: {Error}", result.ErrorMessage);
                await _statusService.UpdateStatusAsync(ServerStatus.Error, cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while stopping server");
            await _statusService.UpdateStatusAsync(ServerStatus.Error, cancellationToken);
            return Result.Failure(ex);
        }
    }

    /// <summary>
    /// Restart the Valheim server process
    /// </summary>
    public async Task<Result> RestartServerAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Restarting Valheim server...");
            
            if (await _processManager.IsRunningAsync(cancellationToken))
            {
                var stopResult = await StopServerAsync(cancellationToken);
                if (!stopResult.IsSuccess)
                {
                    return stopResult;
                }

                // Wait a moment for clean shutdown
                await Task.Delay(2000, cancellationToken);
            }

            return await StartServerAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while restarting server");
            await _statusService.UpdateStatusAsync(ServerStatus.Error, cancellationToken);
            return Result.Failure(ex);
        }
    }

    /// <summary>
    /// Check if the server is idle (no players connected)
    /// </summary>
    public async Task<bool> IsServerIdleAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await _statusService.GetStatusAsync(cancellationToken);
            return status?.PlayerCount == 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check if server is idle, assuming not idle");
            return false;
        }
    }
}

/// <summary>
/// Interface for managing the server process
/// </summary>
public interface IServerProcessManager
{
    Task<Result> StartAsync(CancellationToken cancellationToken = default);
    Task<Result> StopAsync(CancellationToken cancellationToken = default);
    Task<bool> IsRunningAsync(CancellationToken cancellationToken = default);
    Task<ServerProcessInfo> GetProcessInfoAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for server status management
/// </summary>
public interface IServerStatusService
{
    Task<ServerStatusInfo?> GetStatusAsync(CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(ServerStatus status, CancellationToken cancellationToken = default);
    Task<bool> IsServerListeningAsync(CancellationToken cancellationToken = default);
}