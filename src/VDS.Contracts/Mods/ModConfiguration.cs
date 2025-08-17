namespace VDS.Contracts.Mods;

/// <summary>
/// Configuration for mod support
/// </summary>
public record ModConfiguration
{
    /// <summary>
    /// Whether ValheimPlus mod is enabled
    /// </summary>
    public bool ValheimPlusEnabled { get; init; } = false;

    /// <summary>
    /// ValheimPlus repository (GitHub format: owner/repo)
    /// </summary>
    public string ValheimPlusRepository { get; init; } = "Grantapher/ValheimPlus";

    /// <summary>
    /// ValheimPlus release version (latest, specific version)
    /// </summary>
    public string ValheimPlusRelease { get; init; } = "latest";

    /// <summary>
    /// Environment variable prefix for ValheimPlus config
    /// </summary>
    public string ValheimPlusConfigPrefix { get; init; } = "VPCFG_";

    /// <summary>
    /// Whether BepInEx mod is enabled
    /// </summary>
    public bool BepInExEnabled { get; init; } = false;

    /// <summary>
    /// Environment variable prefix for BepInEx config
    /// </summary>
    public string BepInExConfigPrefix { get; init; } = "BEPINEXCFG_";

    /// <summary>
    /// Debug flag to reinstall ValheimPlus
    /// </summary>
    public bool DebugReinstallValheimPlus { get; init; } = false;

    /// <summary>
    /// Debug flag to reinstall BepInEx
    /// </summary>
    public bool DebugReinstallBepInEx { get; init; } = false;
}

/// <summary>
/// Information about an installed mod
/// </summary>
public record ModInfo
{
    /// <summary>
    /// Name of the mod
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Version of the mod
    /// </summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>
    /// Whether the mod is currently enabled
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// Whether the mod is installed
    /// </summary>
    public bool IsInstalled { get; init; }

    /// <summary>
    /// Installation path of the mod
    /// </summary>
    public string InstallPath { get; init; } = string.Empty;

    /// <summary>
    /// Last update check time
    /// </summary>
    public DateTimeOffset LastUpdateCheck { get; init; }

    /// <summary>
    /// Whether an update is available
    /// </summary>
    public bool UpdateAvailable { get; init; }
}