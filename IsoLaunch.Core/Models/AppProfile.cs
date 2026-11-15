namespace IsoLaunch.Core.Models;

/// <summary>
/// Configuration for an isolated application instance profile.
/// </summary>
public sealed class AppProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ProfileName { get; set; } = string.Empty;

    public string AppName { get; set; } = string.Empty;

    public AppPresetKind Preset { get; set; } = AppPresetKind.Antigravity;

    public string ExecutablePath { get; set; } = string.Empty;

    public string ExtraArguments { get; set; } = string.Empty;

    /// <summary>
    /// Overrides USERPROFILE, APPDATA, and LOCALAPPDATA in the child process.
    /// </summary>
    public bool IsolateProfilePaths { get; set; } = true;

    /// <summary>
    /// Passes --user-data-dir="&lt;UserDataDir&gt;" for Electron / Chromium apps.
    /// </summary>
    public bool PassUserDataDir { get; set; } = true;

    /// <summary>
    /// Passes --new-window so Electron / VS Code-based apps open a separate window.
    /// </summary>
    public bool PassNewWindowFlag { get; set; } = true;

    /// <summary>
    /// Sets SSH_CONNECTION so Antigravity's language_server.exe stores OAuth tokens
    /// inside the isolated profile instead of the shared Windows Credential Manager.
    /// </summary>
    public bool BypassSharedKeyring { get; set; } = true;

    public Dictionary<string, string> CustomEnvironmentVariables { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string? SandboxRootOverride { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastLaunchedAt { get; set; }

    public AppProfile CloneWithNewId(string newProfileName)
    {
        return new AppProfile
        {
            Id = Guid.NewGuid(),
            ProfileName = newProfileName,
            AppName = AppName,
            Preset = Preset,
            ExecutablePath = ExecutablePath,
            ExtraArguments = ExtraArguments,
            IsolateProfilePaths = IsolateProfilePaths,
            PassUserDataDir = PassUserDataDir,
            PassNewWindowFlag = PassNewWindowFlag,
            BypassSharedKeyring = BypassSharedKeyring,
            CustomEnvironmentVariables = new Dictionary<string, string>(CustomEnvironmentVariables, StringComparer.OrdinalIgnoreCase),
            SandboxRootOverride = SandboxRootOverride,
            CreatedAt = DateTimeOffset.UtcNow,
            LastLaunchedAt = null
        };
    }
}
