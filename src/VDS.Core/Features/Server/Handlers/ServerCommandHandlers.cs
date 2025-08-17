using Microsoft.Extensions.Logging;
using VDS.Contracts.CQRS;
using VDS.Contracts.CQRS.Server;

namespace VDS.Core.Features.Server.Handlers;

/// <summary>
/// Handles server start commands
/// </summary>
public class StartServerHandler : ICommandHandler<StartServerCommand, Result>
{
    private readonly ServerLifecycleService _lifecycleService;
    private readonly ILogger<StartServerHandler> _logger;

    public StartServerHandler(
        ServerLifecycleService lifecycleService,
        ILogger<StartServerHandler> logger)
    {
        _lifecycleService = lifecycleService;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(StartServerCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling StartServerCommand");
        return await _lifecycleService.StartServerAsync(cancellationToken);
    }
}

/// <summary>
/// Handles server stop commands
/// </summary>
public class StopServerHandler : ICommandHandler<StopServerCommand, Result>
{
    private readonly ServerLifecycleService _lifecycleService;
    private readonly ILogger<StopServerHandler> _logger;

    public StopServerHandler(
        ServerLifecycleService lifecycleService,
        ILogger<StopServerHandler> logger)
    {
        _lifecycleService = lifecycleService;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(StopServerCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling StopServerCommand");
        return await _lifecycleService.StopServerAsync(cancellationToken);
    }
}

/// <summary>
/// Handles server restart commands
/// </summary>
public class RestartServerHandler : ICommandHandler<RestartServerCommand, Result>
{
    private readonly ServerLifecycleService _lifecycleService;
    private readonly ILogger<RestartServerHandler> _logger;

    public RestartServerHandler(
        ServerLifecycleService lifecycleService,
        ILogger<RestartServerHandler> logger)
    {
        _lifecycleService = lifecycleService;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(RestartServerCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling RestartServerCommand");
        return await _lifecycleService.RestartServerAsync(cancellationToken);
    }
}

/// <summary>
/// Handles server idle check queries
/// </summary>
public class IsServerIdleHandler : IQueryHandler<IsServerIdleQuery, bool>
{
    private readonly ServerLifecycleService _lifecycleService;
    private readonly ILogger<IsServerIdleHandler> _logger;

    public IsServerIdleHandler(
        ServerLifecycleService lifecycleService,
        ILogger<IsServerIdleHandler> logger)
    {
        _lifecycleService = lifecycleService;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(IsServerIdleQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Handling IsServerIdleQuery");
        return await _lifecycleService.IsServerIdleAsync(cancellationToken);
    }
}