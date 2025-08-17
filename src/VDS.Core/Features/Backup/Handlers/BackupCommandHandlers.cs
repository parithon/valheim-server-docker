using Microsoft.Extensions.Logging;
using VDS.Contracts.Backup;
using VDS.Contracts.CQRS;
using VDS.Contracts.CQRS.Backup;

namespace VDS.Core.Features.Backup.Handlers;

/// <summary>
/// Handles backup creation commands
/// </summary>
public class CreateBackupHandler : ICommandHandler<CreateBackupCommand, Result<BackupInfo>>
{
    private readonly BackupService _backupService;
    private readonly ILogger<CreateBackupHandler> _logger;

    public CreateBackupHandler(
        BackupService backupService,
        ILogger<CreateBackupHandler> logger)
    {
        _backupService = backupService;
        _logger = logger;
    }

    public async Task<Result<BackupInfo>> HandleAsync(CreateBackupCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling CreateBackupCommand (Force: {Force}, BackupName: {BackupName})", 
            command.Force, command.BackupName);
        
        return await _backupService.CreateBackupAsync(command.Force, command.BackupName, cancellationToken);
    }
}

/// <summary>
/// Handles backup cleanup commands
/// </summary>
public class CleanupBackupsHandler : ICommandHandler<CleanupBackupsCommand, Result<int>>
{
    private readonly BackupService _backupService;
    private readonly IBackupConfigurationService _configService;
    private readonly ILogger<CleanupBackupsHandler> _logger;

    public CleanupBackupsHandler(
        BackupService backupService,
        IBackupConfigurationService configService,
        ILogger<CleanupBackupsHandler> logger)
    {
        _backupService = backupService;
        _configService = configService;
        _logger = logger;
    }

    public async Task<Result<int>> HandleAsync(CleanupBackupsCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling CleanupBackupsCommand");
        
        var config = await _configService.GetConfigurationAsync(cancellationToken);
        return await _backupService.CleanupBackupsAsync(config, cancellationToken);
    }
}

/// <summary>
/// Handles backup deletion commands
/// </summary>
public class DeleteBackupHandler : ICommandHandler<DeleteBackupCommand, Result>
{
    private readonly BackupService _backupService;
    private readonly ILogger<DeleteBackupHandler> _logger;

    public DeleteBackupHandler(
        BackupService backupService,
        ILogger<DeleteBackupHandler> logger)
    {
        _backupService = backupService;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(DeleteBackupCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling DeleteBackupCommand for: {BackupPath}", command.BackupPath);
        
        return await _backupService.DeleteBackupAsync(command.BackupPath, cancellationToken);
    }
}

/// <summary>
/// Handles backup restore commands
/// </summary>
public class RestoreBackupHandler : ICommandHandler<RestoreBackupCommand, Result>
{
    private readonly BackupService _backupService;
    private readonly ILogger<RestoreBackupHandler> _logger;

    public RestoreBackupHandler(
        BackupService backupService,
        ILogger<RestoreBackupHandler> logger)
    {
        _backupService = backupService;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(RestoreBackupCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling RestoreBackupCommand from: {BackupPath} (StopServerFirst: {StopServerFirst})", 
            command.BackupPath, command.StopServerFirst);
        
        return await _backupService.RestoreBackupAsync(command.BackupPath, command.StopServerFirst, cancellationToken);
    }
}

/// <summary>
/// Handles queries for backup lists
/// </summary>
public class GetBackupsHandler : IQueryHandler<GetBackupsQuery, List<BackupInfo>>
{
    private readonly BackupService _backupService;
    private readonly ILogger<GetBackupsHandler> _logger;

    public GetBackupsHandler(
        BackupService backupService,
        ILogger<GetBackupsHandler> logger)
    {
        _backupService = backupService;
        _logger = logger;
    }

    public async Task<List<BackupInfo>> HandleAsync(GetBackupsQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Handling GetBackupsQuery");
        
        return await _backupService.GetBackupsAsync(cancellationToken);
    }
}

/// <summary>
/// Interface for backup configuration management
/// </summary>
public interface IBackupConfigurationService
{
    Task<BackupConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default);
    Task<Result> UpdateConfigurationAsync(BackupConfiguration configuration, CancellationToken cancellationToken = default);
}