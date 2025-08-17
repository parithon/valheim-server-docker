using Microsoft.Extensions.Options;
using VDS.Contracts.Server;
using VDS.Contracts.Backup;
using VDS.Worker.Configuration;

namespace VDS.Worker.Services;

/// <summary>
/// Adapter that provides ServerConfiguration from ServerOptions
/// </summary>
public class ServerConfigurationAdapter : IOptions<ServerConfiguration>
{
    private readonly IOptionsMonitor<ServerOptions> _serverOptions;

    public ServerConfigurationAdapter(IOptionsMonitor<ServerOptions> serverOptions)
    {
        _serverOptions = serverOptions;
    }

    public ServerConfiguration Value => _serverOptions.CurrentValue.ToServerConfiguration();
}

/// <summary>
/// Adapter that provides BackupConfiguration from BackupOptions
/// </summary>
public class BackupConfigurationAdapter : IOptions<BackupConfiguration>
{
    private readonly IOptionsMonitor<BackupOptions> _backupOptions;

    public BackupConfigurationAdapter(IOptionsMonitor<BackupOptions> backupOptions)
    {
        _backupOptions = backupOptions;
    }

    public BackupConfiguration Value => _backupOptions.CurrentValue.ToBackupConfiguration();
}