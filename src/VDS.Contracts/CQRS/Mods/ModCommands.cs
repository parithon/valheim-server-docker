using VDS.Contracts.Mods;

namespace VDS.Contracts.CQRS.Mods;

// Commands for mod management

/// <summary>
/// Install or update ValheimPlus mod
/// </summary>
public record InstallValheimPlusCommand : Command<Result>
{
    /// <summary>
    /// Force reinstallation even if already installed
    /// </summary>
    public bool Force { get; init; } = false;

    /// <summary>
    /// Specific version to install (or "latest")
    /// </summary>
    public string Version { get; init; } = "latest";
}

/// <summary>
/// Install or update BepInEx mod
/// </summary>
public record InstallBepInExCommand : Command<Result>
{
    /// <summary>
    /// Force reinstallation even if already installed
    /// </summary>
    public bool Force { get; init; } = false;
}

/// <summary>
/// Remove ValheimPlus mod
/// </summary>
public record RemoveValheimPlusCommand : Command<Result>;

/// <summary>
/// Remove BepInEx mod
/// </summary>
public record RemoveBepInExCommand : Command<Result>;

/// <summary>
/// Check for mod updates
/// </summary>
public record CheckModUpdatesCommand : Command<Result<List<ModUpdateInfo>>>;

/// <summary>
/// Update mod configuration
/// </summary>
public record UpdateModConfigurationCommand : Command<Result>
{
    /// <summary>
    /// New mod configuration
    /// </summary>
    public ModConfiguration Configuration { get; init; } = new();
}

/// <summary>
/// Apply environment variables to mod configuration files
/// </summary>
public record ApplyModConfigurationCommand : Command<Result>
{
    /// <summary>
    /// Environment variables to apply
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; init; } = new();
}

// Queries for mod information

/// <summary>
/// Get all installed mods
/// </summary>
public record GetInstalledModsQuery : Query<List<ModInfo>>;

/// <summary>
/// Get mod configuration
/// </summary>
public record GetModConfigurationQuery : Query<ModConfiguration>;

/// <summary>
/// Get information about a specific mod
/// </summary>
public record GetModInfoQuery : Query<ModInfo?>
{
    /// <summary>
    /// Name of the mod to get info for
    /// </summary>
    public string ModName { get; init; } = string.Empty;
}

/// <summary>
/// Information about a mod update
/// </summary>
public record ModUpdateInfo
{
    /// <summary>
    /// Name of the mod
    /// </summary>
    public string ModName { get; init; } = string.Empty;

    /// <summary>
    /// Current version
    /// </summary>
    public string CurrentVersion { get; init; } = string.Empty;

    /// <summary>
    /// Available version
    /// </summary>
    public string AvailableVersion { get; init; } = string.Empty;

    /// <summary>
    /// Whether an update is available
    /// </summary>
    public bool UpdateAvailable { get; init; }

    /// <summary>
    /// Release notes for the new version
    /// </summary>
    public string? ReleaseNotes { get; init; }
}