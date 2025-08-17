using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VDS.Contracts.CQRS;
using VDS.Contracts.CQRS.Server;
using VDS.Contracts.Server;
using VDS.Core.Features.Server;

namespace VDS.Infrastructure.Process;

/// <summary>
/// Implementation of server process management using System.Diagnostics.Process
/// </summary>
public class ValheimServerProcessManager : IServerProcessManager
{
    private readonly IOptions<ServerConfiguration> _serverConfig;
    private readonly ILogger<ValheimServerProcessManager> _logger;
    private System.Diagnostics.Process? _serverProcess;
    private readonly object _processLock = new();

    public ValheimServerProcessManager(
        IOptions<ServerConfiguration> serverConfig,
        ILogger<ValheimServerProcessManager> logger)
    {
        _serverConfig = serverConfig;
        _logger = logger;
    }

    public async Task<Result> StartAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            lock (_processLock)
            {
                try
                {
                    if (IsProcessRunning())
                    {
                        _logger.LogWarning("Valheim server process is already running");
                        return Result.Success();
                    }

                    var config = _serverConfig.Value;
                    var executablePath = GetServerExecutablePath(config);
                    var arguments = BuildServerArguments(config);

                    _logger.LogInformation("Starting Valheim server: {Path} {Args}", executablePath, arguments);

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = executablePath,
                        Arguments = arguments,
                        WorkingDirectory = GetWorkingDirectory(config),
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        CreateNoWindow = true
                    };

                    // Set environment variables
                    startInfo.EnvironmentVariables["SteamAppId"] = "892970";
                    SetModEnvironmentVariables(startInfo, config);

                    _serverProcess = new System.Diagnostics.Process
                    {
                        StartInfo = startInfo,
                        EnableRaisingEvents = true
                    };

                    // Hook up event handlers
                    _serverProcess.OutputDataReceived += OnOutputDataReceived;
                    _serverProcess.ErrorDataReceived += OnErrorDataReceived;
                    _serverProcess.Exited += OnProcessExited;

                    var started = _serverProcess.Start();
                    if (!started)
                    {
                        return Result.Failure("Failed to start Valheim server process");
                    }

                    _serverProcess.BeginOutputReadLine();
                    _serverProcess.BeginErrorReadLine();

                    _logger.LogInformation("Valheim server process started with PID: {ProcessId}", _serverProcess.Id);
                    return Result.Success();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to start Valheim server process");
                    return Result.Failure(ex);
                }
            }
        }, cancellationToken);
    }

    public async Task<Result> StopAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            lock (_processLock)
            {
                try
                {
                    if (!IsProcessRunning())
                    {
                        _logger.LogWarning("Valheim server process is not running");
                        return Result.Success();
                    }

                    _logger.LogInformation("Stopping Valheim server process (PID: {ProcessId})", _serverProcess!.Id);

                    // Try graceful shutdown first
                    if (!_serverProcess.HasExited)
                    {
                        _serverProcess.CloseMainWindow();
                        
                        // Wait for graceful shutdown
                        if (!_serverProcess.WaitForExit(30000)) // 30 seconds timeout
                        {
                            _logger.LogWarning("Graceful shutdown timeout, forcing process termination");
                            _serverProcess.Kill(true); // Kill process tree
                        }
                    }

                    _serverProcess?.Dispose();
                    _serverProcess = null;

                    _logger.LogInformation("Valheim server process stopped");
                    return Result.Success();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to stop Valheim server process");
                    return Result.Failure(ex);
                }
            }
        }, cancellationToken);
    }

    public async Task<bool> IsRunningAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            lock (_processLock)
            {
                return IsProcessRunning();
            }
        }, cancellationToken);
    }

    public async Task<ServerProcessInfo> GetProcessInfoAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            lock (_processLock)
            {
                if (!IsProcessRunning())
                {
                    return new ServerProcessInfo
                    {
                        ProcessId = 0,
                        IsRunning = false,
                        Uptime = TimeSpan.Zero,
                        MemoryUsageBytes = 0,
                        CpuUsagePercent = 0,
                        LastActivity = DateTimeOffset.UtcNow
                    };
                }

                try
                {
                    var process = _serverProcess!;
                    process.Refresh();

                    return new ServerProcessInfo
                    {
                        ProcessId = process.Id,
                        IsRunning = true,
                        Uptime = DateTimeOffset.UtcNow - process.StartTime,
                        MemoryUsageBytes = process.WorkingSet64,
                        CpuUsagePercent = GetCpuUsage(process),
                        LastActivity = DateTimeOffset.UtcNow
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get process info");
                    return new ServerProcessInfo
                    {
                        ProcessId = _serverProcess?.Id ?? 0,
                        IsRunning = false,
                        Uptime = TimeSpan.Zero,
                        MemoryUsageBytes = 0,
                        CpuUsagePercent = 0,
                        LastActivity = DateTimeOffset.UtcNow
                    };
                }
            }
        }, cancellationToken);
    }

    private bool IsProcessRunning()
    {
        return _serverProcess != null && !_serverProcess.HasExited;
    }

    private static string GetServerExecutablePath(ServerConfiguration config)
    {
        // This mirrors the logic from valheim-server script
        var installPath = "/opt/valheim/server";
        return Path.Combine(installPath, "valheim_server.x86_64");
    }

    private static string GetWorkingDirectory(ServerConfiguration config)
    {
        return "/opt/valheim/server";
    }

    private static string BuildServerArguments(ServerConfiguration config)
    {
        var args = new List<string>
        {
            "-nographics",
            "-batchmode",
            "-name", $"\"{config.ServerName}\"",
            "-port", config.ServerPort.ToString(),
            "-world", $"\"{config.WorldName}\"",
            "-public", config.ServerPublic.ToString()
        };

        if (!string.IsNullOrEmpty(config.ServerPassword))
        {
            args.AddRange(["-password", $"\"{config.ServerPassword}\""]);
        }

        if (config.Crossplay)
        {
            args.Add("-crossplay");
        }

        if (!string.IsNullOrEmpty(config.ServerArgs))
        {
            args.Add(config.ServerArgs);
        }

        return string.Join(" ", args);
    }

    private static void SetModEnvironmentVariables(ProcessStartInfo startInfo, ServerConfiguration config)
    {
        // Set LD_LIBRARY_PATH for the server
        var libraryPath = "/opt/valheim/server/linux64/";
        startInfo.EnvironmentVariables["LD_LIBRARY_PATH"] = libraryPath;
    }

    private double GetCpuUsage(System.Diagnostics.Process process)
    {
        try
        {
            // Simplified CPU usage calculation
            // In a real implementation, you'd want to track this over time
            return process.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount / 1000.0 * 100.0;
        }
        catch
        {
            return 0.0;
        }
    }

    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            _logger.LogInformation("[VALHEIM] {Output}", e.Data);
        }
    }

    private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            _logger.LogWarning("[VALHEIM] {Error}", e.Data);
        }
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        lock (_processLock)
        {
            if (_serverProcess != null)
            {
                _logger.LogInformation("Valheim server process exited with code: {ExitCode}", _serverProcess.ExitCode);
                _serverProcess.Dispose();
                _serverProcess = null;
            }
        }
    }

    public void Dispose()
    {
        lock (_processLock)
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                try
                {
                    _serverProcess.Kill(true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error killing server process during disposal");
                }
            }
            _serverProcess?.Dispose();
        }
    }
}