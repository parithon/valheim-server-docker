using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VDS.Core.Features.LogFiltering;

/// <summary>
/// Configuration for log filtering
/// </summary>
public record LogFilterConfiguration
{
    /// <summary>
    /// Environment variable prefix for filter configurations
    /// </summary>
    public string FilterPrefix { get; init; } = "VALHEIM_LOG_FILTER_";

    /// <summary>
    /// Environment variable prefix for event hook configurations
    /// </summary>
    public string HookPrefix { get; init; } = "VALHEIM_LOG_HOOK_";

    /// <summary>
    /// Whether to filter empty log lines
    /// </summary>
    public bool FilterEmpty { get; init; } = true;

    /// <summary>
    /// Whether to filter invalid UTF-8 characters
    /// </summary>
    public bool FilterUtf8 { get; init; } = true;

    /// <summary>
    /// List of filter patterns and their associated actions
    /// </summary>
    public List<LogFilterRule> FilterRules { get; init; } = new();

    /// <summary>
    /// List of event hook patterns and their associated commands
    /// </summary>
    public List<LogEventHook> EventHooks { get; init; } = new();
}

/// <summary>
/// A log filter rule that matches patterns and performs actions
/// </summary>
public record LogFilterRule
{
    /// <summary>
    /// Regular expression pattern to match
    /// </summary>
    public string Pattern { get; init; } = string.Empty;

    /// <summary>
    /// Action to take when pattern matches
    /// </summary>
    public LogFilterAction Action { get; init; } = LogFilterAction.Remove;
}

/// <summary>
/// An event hook that executes commands when patterns match
/// </summary>
public record LogEventHook
{
    /// <summary>
    /// Regular expression pattern to match
    /// </summary>
    public string Pattern { get; init; } = string.Empty;

    /// <summary>
    /// Command to execute when pattern matches
    /// </summary>
    public string Command { get; init; } = string.Empty;
}

/// <summary>
/// Actions that can be taken when a log filter matches
/// </summary>
public enum LogFilterAction
{
    /// <summary>
    /// Remove the log line (don't output it)
    /// </summary>
    Remove,

    /// <summary>
    /// Keep the log line (output it)
    /// </summary>
    Keep,

    /// <summary>
    /// Execute a command with the log line
    /// </summary>
    Execute
}

/// <summary>
/// Service for filtering and processing Valheim server log output
/// Ported from the Go valheim-logfilter implementation
/// </summary>
public class LogFilterService
{
    private readonly IOptions<LogFilterConfiguration> _config;
    private readonly ILogger<LogFilterService> _logger;
    private readonly List<CompiledFilterRule> _compiledRules = new();
    private readonly List<CompiledEventHook> _compiledHooks = new();

    public LogFilterService(
        IOptions<LogFilterConfiguration> config,
        ILogger<LogFilterService> logger)
    {
        _config = config;
        _logger = logger;
        CompileRules();
    }

    /// <summary>
    /// Process a log line through all filters and hooks
    /// </summary>
    /// <param name="logLine">The log line to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The processed log line, or null if it should be filtered out</returns>
    public string? ProcessLogLine(string logLine, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = _config.Value;

            // Filter empty lines if configured
            if (config.FilterEmpty && string.IsNullOrEmpty(logLine))
            {
                _logger.LogTrace("Filtered empty line");
                return null;
            }

            // Filter invalid UTF-8 if configured
            if (config.FilterUtf8 && !IsValidUtf8(logLine))
            {
                _logger.LogTrace("Filtered invalid UTF-8 line");
                return null;
            }

            // Process removal filters first
            foreach (var rule in _compiledRules)
            {
                if (rule.Regex.IsMatch(logLine))
                {
                    _logger.LogTrace("Line matched removal filter: {Pattern}", rule.Pattern);
                    
                    if (rule.Action == LogFilterAction.Remove)
                    {
                        return null; // Filter out this line
                    }
                    else if (rule.Action == LogFilterAction.Execute && !string.IsNullOrEmpty(rule.Command))
                    {
                        // Execute command but continue processing
                        _ = Task.Run(() => ExecuteHookCommand(rule.Command, logLine, cancellationToken), cancellationToken);
                    }
                }
            }

            // Process event hooks
            foreach (var hook in _compiledHooks)
            {
                if (hook.Regex.IsMatch(logLine))
                {
                    _logger.LogTrace("Line matched event hook: {Pattern}", hook.Pattern);
                    // Execute hook command asynchronously
                    _ = Task.Run(() => ExecuteHookCommand(hook.Command, logLine, cancellationToken), cancellationToken);
                }
            }

            return logLine; // Line passed all filters
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing log line: {LogLine}", logLine);
            return logLine; // Return original line on error
        }
    }

    /// <summary>
    /// Stream log lines from input to output, applying filters
    /// </summary>
    public async Task FilterLogStreamAsync(
        TextReader input, 
        TextWriter output, 
        CancellationToken cancellationToken = default)
    {
        string? line;
        while ((line = await input.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
        {
            var processedLine = ProcessLogLine(line, cancellationToken);
            if (processedLine != null)
            {
                await output.WriteLineAsync(processedLine);
                await output.FlushAsync();
            }
        }
    }

    private void CompileRules()
    {
        var config = _config.Value;

        // Compile filter rules
        foreach (var rule in config.FilterRules)
        {
            try
            {
                var regex = new Regex(rule.Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
                _compiledRules.Add(new CompiledFilterRule
                {
                    Pattern = rule.Pattern,
                    Regex = regex,
                    Action = rule.Action,
                    Command = string.Empty // Commands are only for hooks
                });
                _logger.LogDebug("Compiled filter rule: {Pattern} -> {Action}", rule.Pattern, rule.Action);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compile filter pattern: {Pattern}", rule.Pattern);
            }
        }

        // Compile event hooks
        foreach (var hook in config.EventHooks)
        {
            try
            {
                var regex = new Regex(hook.Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
                _compiledHooks.Add(new CompiledEventHook
                {
                    Pattern = hook.Pattern,
                    Regex = regex,
                    Command = hook.Command
                });
                _logger.LogDebug("Compiled event hook: {Pattern} -> {Command}", hook.Pattern, hook.Command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compile hook pattern: {Pattern}", hook.Pattern);
            }
        }

        _logger.LogInformation("Compiled {FilterCount} filter rules and {HookCount} event hooks", 
            _compiledRules.Count, _compiledHooks.Count);
    }

    private async Task ExecuteHookCommand(string command, string logLine, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Executing hook command: {Command}", command);

            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new System.Diagnostics.Process { StartInfo = processStartInfo };
            
            if (!process.Start())
            {
                _logger.LogError("Failed to start hook command: {Command}", command);
                return;
            }

            // Write log line to command's stdin
            await process.StandardInput.WriteLineAsync(logLine);
            await process.StandardInput.FlushAsync();
            process.StandardInput.Close();

            // Wait for completion with timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(30)); // 30 second timeout

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
                
                if (process.ExitCode != 0)
                {
                    var stderr = await process.StandardError.ReadToEndAsync();
                    _logger.LogWarning("Hook command exited with code {ExitCode}: {Command}. Error: {Error}", 
                        process.ExitCode, command, stderr);
                }
                else
                {
                    _logger.LogTrace("Hook command completed successfully: {Command}", command);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Hook command timed out and was terminated: {Command}", command);
                try
                {
                    process.Kill(true);
                }
                catch (Exception killEx)
                {
                    _logger.LogError(killEx, "Failed to kill timed out hook command: {Command}", command);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute hook command: {Command}", command);
        }
    }

    private static bool IsValidUtf8(string text)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var decoded = Encoding.UTF8.GetString(bytes);
            return decoded.Equals(text);
        }
        catch
        {
            return false;
        }
    }

    private record CompiledFilterRule
    {
        public string Pattern { get; init; } = string.Empty;
        public Regex Regex { get; init; } = null!;
        public LogFilterAction Action { get; init; } = LogFilterAction.Remove;
        public string Command { get; init; } = string.Empty;
    }

    private record CompiledEventHook
    {
        public string Pattern { get; init; } = string.Empty;
        public Regex Regex { get; init; } = null!;
        public string Command { get; init; } = string.Empty;
    }
}