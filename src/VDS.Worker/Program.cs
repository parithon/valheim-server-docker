using VDS.Worker;
using VDS.Core.Features.Server;
using VDS.Core.Features.Backup;
using VDS.Infrastructure.Process;
using VDS.Infrastructure.Storage;
using VDS.Infrastructure.Network;
using VDS.Contracts.Server;
using VDS.Contracts.Backup;
using VDS.Worker.Configuration;
using VDS.Worker.Services;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

// Configure mutable options for binding
builder.Services.Configure<ServerOptions>(builder.Configuration.GetSection("Server"));
builder.Services.Configure<BackupOptions>(builder.Configuration.GetSection("Backup"));

// Register configuration providers using factory delegates
builder.Services.AddSingleton<IOptions<ServerConfiguration>>(provider =>
{
    var serverOptions = provider.GetRequiredService<IOptions<ServerOptions>>();
    return Options.Create(serverOptions.Value.ToServerConfiguration());
});

builder.Services.AddSingleton<IOptions<BackupConfiguration>>(provider =>
{
    var backupOptions = provider.GetRequiredService<IOptions<BackupOptions>>();
    return Options.Create(backupOptions.Value.ToBackupConfiguration());
});

// Register core services
builder.Services.AddSingleton<ServerLifecycleService>();
builder.Services.AddSingleton<BackupService>();

// Register infrastructure services
builder.Services.AddSingleton<IServerProcessManager, ValheimServerProcessManager>();
builder.Services.AddSingleton<IServerStatusService, SteamQueryServerStatusService>();
builder.Services.AddSingleton<IBackupStorage, FileSystemBackupStorage>();

// Register the worker service
builder.Services.AddHostedService<ValheimServerWorker>();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddSystemdConsole();
    
    var logLevel = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Information";
    if (Enum.TryParse<LogLevel>(logLevel, true, out var level))
    {
        logging.SetMinimumLevel(level);
    }
});

var host = builder.Build();

// Configure timezone if specified
var timeZone = Environment.GetEnvironmentVariable("TZ");
if (!string.IsNullOrEmpty(timeZone))
{
    try
    {
        TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        Environment.SetEnvironmentVariable("TZ", timeZone);
    }
    catch (Exception ex)
    {
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Failed to set timezone {TimeZone}, using default", timeZone);
    }
}

host.Run();
