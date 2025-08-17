using System.IO.Compression;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VDS.Contracts.Backup;
using VDS.Core.Features.Backup;

namespace VDS.Infrastructure.Storage;

/// <summary>
/// File system implementation of backup storage
/// </summary>
public class FileSystemBackupStorage : IBackupStorage
{
    private readonly IOptions<BackupConfiguration> _config;
    private readonly ILogger<FileSystemBackupStorage> _logger;
    
    private const string WorldsDirectory = "/config/worlds_local";
    private const string LegacyWorldsDirectory = "/home/valheim/.config/unity3d/IronGate/Valheim/worlds_local";

    public FileSystemBackupStorage(
        IOptions<BackupConfiguration> config,
        ILogger<FileSystemBackupStorage> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<BackupInfo> CreateBackupAsync(string backupName, CancellationToken cancellationToken = default)
    {
        var config = _config.Value;
        var backupDir = config.BackupDirectory;
        
        // Ensure backup directory exists
        Directory.CreateDirectory(backupDir);

        // Determine worlds directory
        var worldsDir = GetWorldsDirectory();
        if (!Directory.Exists(worldsDir))
        {
            throw new DirectoryNotFoundException($"Worlds directory not found: {worldsDir}");
        }

        var timestamp = DateTimeOffset.UtcNow;
        var backupFileName = config.UseCompression ? $"{backupName}.zip" : backupName;
        var backupPath = Path.Combine(backupDir, backupFileName);

        _logger.LogInformation("Creating backup: {BackupPath} from {WorldsDir}", backupPath, worldsDir);

        try
        {
            if (config.UseCompression)
            {
                // Create ZIP backup
                using var archive = ZipFile.Open(backupPath, ZipArchiveMode.Create);
                await AddDirectoryToZipAsync(archive, worldsDir, "worlds_local", cancellationToken);
            }
            else
            {
                // Create directory backup
                var backupFullPath = Path.Combine(backupDir, backupName);
                Directory.CreateDirectory(backupFullPath);
                await CopyDirectoryAsync(worldsDir, Path.Combine(backupFullPath, "worlds_local"), cancellationToken);
                backupPath = backupFullPath;
            }

            var fileInfo = new FileInfo(backupPath);
            var backupInfo = new BackupInfo
            {
                FilePath = backupPath,
                SizeBytes = config.UseCompression ? fileInfo.Length : GetDirectorySize(backupPath),
                CreatedAt = timestamp,
                IsCompressed = config.UseCompression,
                WorldName = "worlds_local" // This could be made dynamic based on WORLD_NAME
            };

            _logger.LogInformation("Backup created successfully: {BackupPath} ({SizeBytes} bytes)", 
                backupPath, backupInfo.SizeBytes);

            return backupInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup: {BackupPath}", backupPath);
            
            // Clean up partial backup
            try
            {
                if (File.Exists(backupPath))
                    File.Delete(backupPath);
                else if (Directory.Exists(backupPath))
                    Directory.Delete(backupPath, true);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogWarning(cleanupEx, "Failed to cleanup partial backup: {BackupPath}", backupPath);
            }
            
            throw;
        }
    }

    public async Task<List<BackupInfo>> GetBackupsAsync(CancellationToken cancellationToken = default)
    {
        var config = _config.Value;
        var backupDir = config.BackupDirectory;
        
        if (!Directory.Exists(backupDir))
        {
            return new List<BackupInfo>();
        }

        var backups = new List<BackupInfo>();

        await Task.Run(() =>
        {
            // Get compressed backups (ZIP files)
            var zipFiles = Directory.GetFiles(backupDir, "*.zip");
            foreach (var zipFile in zipFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(zipFile);
                    backups.Add(new BackupInfo
                    {
                        FilePath = zipFile,
                        SizeBytes = fileInfo.Length,
                        CreatedAt = fileInfo.CreationTimeUtc,
                        IsCompressed = true,
                        WorldName = ExtractWorldNameFromPath(zipFile)
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read backup info for: {BackupPath}", zipFile);
                }
            }

            // Get directory backups
            var directories = Directory.GetDirectories(backupDir);
            foreach (var directory in directories)
            {
                try
                {
                    var dirInfo = new DirectoryInfo(directory);
                    backups.Add(new BackupInfo
                    {
                        FilePath = directory,
                        SizeBytes = GetDirectorySize(directory),
                        CreatedAt = dirInfo.CreationTimeUtc,
                        IsCompressed = false,
                        WorldName = ExtractWorldNameFromPath(directory)
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read backup info for: {BackupPath}", directory);
                }
            }
        }, cancellationToken);

        return backups.OrderByDescending(b => b.CreatedAt).ToList();
    }

    public async Task DeleteBackupAsync(string backupPath, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _logger.LogInformation("Deleting backup: {BackupPath}", backupPath);

            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
                _logger.LogInformation("Deleted backup file: {BackupPath}", backupPath);
            }
            else if (Directory.Exists(backupPath))
            {
                Directory.Delete(backupPath, true);
                _logger.LogInformation("Deleted backup directory: {BackupPath}", backupPath);
            }
            else
            {
                _logger.LogWarning("Backup not found: {BackupPath}", backupPath);
                throw new FileNotFoundException($"Backup not found: {backupPath}");
            }
        }, cancellationToken);
    }

    public async Task RestoreBackupAsync(string backupPath, CancellationToken cancellationToken = default)
    {
        var worldsDir = GetWorldsDirectory();
        
        _logger.LogInformation("Restoring backup from: {BackupPath} to: {WorldsDir}", backupPath, worldsDir);

        await Task.Run(() =>
        {
            // Backup current worlds if they exist
            if (Directory.Exists(worldsDir))
            {
                var backupCurrentPath = $"{worldsDir}.backup.{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}";
                _logger.LogInformation("Backing up current worlds to: {BackupPath}", backupCurrentPath);
                Directory.Move(worldsDir, backupCurrentPath);
            }

            try
            {
                if (File.Exists(backupPath) && Path.GetExtension(backupPath) == ".zip")
                {
                    // Restore from ZIP
                    Directory.CreateDirectory(worldsDir);
                    ZipFile.ExtractToDirectory(backupPath, Path.GetDirectoryName(worldsDir)!);
                }
                else if (Directory.Exists(backupPath))
                {
                    // Restore from directory
                    var sourceWorldsDir = Path.Combine(backupPath, "worlds_local");
                    if (Directory.Exists(sourceWorldsDir))
                    {
                        CopyDirectorySync(sourceWorldsDir, worldsDir);
                    }
                    else
                    {
                        throw new DirectoryNotFoundException($"worlds_local directory not found in backup: {backupPath}");
                    }
                }
                else
                {
                    throw new FileNotFoundException($"Backup not found: {backupPath}");
                }

                _logger.LogInformation("Backup restored successfully from: {BackupPath}", backupPath);
            }
            catch (Exception)
            {
                // Try to restore the original worlds directory if restore failed
                var backupCurrentPath = $"{worldsDir}.backup.{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}";
                if (Directory.Exists(backupCurrentPath))
                {
                    try
                    {
                        if (Directory.Exists(worldsDir))
                            Directory.Delete(worldsDir, true);
                        Directory.Move(backupCurrentPath, worldsDir);
                        _logger.LogInformation("Restored original worlds directory after failed restore");
                    }
                    catch (Exception restoreEx)
                    {
                        _logger.LogError(restoreEx, "Failed to restore original worlds directory");
                    }
                }
                throw;
            }
        }, cancellationToken);
    }

    private static string GetWorldsDirectory()
    {
        // Try primary location first, then fallback to legacy
        return Directory.Exists(WorldsDirectory) ? WorldsDirectory : LegacyWorldsDirectory;
    }

    private static async Task AddDirectoryToZipAsync(ZipArchive archive, string sourceDir, string entryPrefix, CancellationToken cancellationToken)
    {
        var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
        
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var entryName = Path.Combine(entryPrefix, relativePath).Replace('\\', '/');
            
            var entry = archive.CreateEntry(entryName);
            using var entryStream = entry.Open();
            using var fileStream = File.OpenRead(file);
            await fileStream.CopyToAsync(entryStream, cancellationToken);
        }
    }

    private static async Task CopyDirectoryAsync(string sourceDir, string destDir, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destDir);
        
        var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
        
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var destFile = Path.Combine(destDir, relativePath);
            var destFileDir = Path.GetDirectoryName(destFile)!;
            
            Directory.CreateDirectory(destFileDir);
            
            using var sourceStream = File.OpenRead(file);
            using var destStream = File.Create(destFile);
            await sourceStream.CopyToAsync(destStream, cancellationToken);
        }
    }

    private static void CopyDirectorySync(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        
        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var destFile = Path.Combine(destDir, relativePath);
            var destFileDir = Path.GetDirectoryName(destFile)!;
            
            Directory.CreateDirectory(destFileDir);
            File.Copy(file, destFile, true);
        }
    }

    private static long GetDirectorySize(string directory)
    {
        return Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
            .Sum(file => new FileInfo(file).Length);
    }

    private static string ExtractWorldNameFromPath(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        // Try to extract world name from backup filename
        // Format is typically: worlds-20240101-120000 or similar
        return fileName.StartsWith("worlds-") ? "worlds_local" : fileName;
    }
}