using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using IsoLaunch.Core.Models;

namespace IsoLaunch.Core.Services;

public sealed record SandboxPaths(
    string BaseDir,
    string UserDataDir,
    string AppDataDir,
    string LocalAppDataDir);

public sealed record LaunchResult(
    bool Success,
    int? ProcessId,
    SandboxPaths Paths,
    string? ErrorMessage = null);

/// <summary>
/// Builds isolated process configurations and launches independent application instances.
/// </summary>
public static class InstanceLauncher
{
    public const string DefaultSshConnectionValue = "127.0.0.1 1234 127.0.0.1 22";

    public static string GetDefaultSandboxRoot()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IsoLaunch",
            "Profiles");
    }

    public static SandboxPaths ResolveSandboxPaths(AppProfile profile, string? defaultRoot = null)
    {
        ArgumentNullException.ThrowIfNull(profile);

        string baseDir;
        if (!string.IsNullOrWhiteSpace(profile.SandboxRootOverride))
        {
            baseDir = Environment.ExpandEnvironmentVariables(profile.SandboxRootOverride.Trim());
        }
        else
        {
            var root = string.IsNullOrWhiteSpace(defaultRoot) ? GetDefaultSandboxRoot() : defaultRoot;
            var folderName = BuildSafeFolderName(profile.ProfileName, profile.Id);
            baseDir = Path.Combine(root, folderName);
        }

        return new SandboxPaths(
            BaseDir: baseDir,
            UserDataDir: Path.Combine(baseDir, "UserData"),
            AppDataDir: Path.Combine(baseDir, "AppData", "Roaming"),
            LocalAppDataDir: Path.Combine(baseDir, "AppData", "Local"));
    }

    public static void EnsureSandboxDirectories(SandboxPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        Directory.CreateDirectory(paths.BaseDir);
        Directory.CreateDirectory(paths.UserDataDir);
        Directory.CreateDirectory(paths.AppDataDir);
        Directory.CreateDirectory(paths.LocalAppDataDir);
    }

    public static ProcessStartInfo BuildStartInfo(AppProfile profile, string? defaultRoot = null)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var paths = ResolveSandboxPaths(profile, defaultRoot);
        var args = new List<string>();

        if (profile.PassUserDataDir)
        {
            args.Add($"--user-data-dir=\"{paths.UserDataDir}\"");
        }

        if (profile.PassNewWindowFlag)
        {
            args.Add("--new-window");
        }

        if (!string.IsNullOrWhiteSpace(profile.ExtraArguments))
        {
            args.Add(profile.ExtraArguments.Trim());
        }

        var workingDir = string.Empty;
        if (!string.IsNullOrWhiteSpace(profile.ExecutablePath))
        {
            workingDir = Path.GetDirectoryName(profile.ExecutablePath) ?? string.Empty;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = profile.ExecutablePath,
            Arguments = string.Join(" ", args),
            WorkingDirectory = workingDir,
            UseShellExecute = false
        };

        if (profile.IsolateProfilePaths)
        {
            startInfo.Environment["USERPROFILE"] = paths.BaseDir;
            startInfo.Environment["APPDATA"] = paths.AppDataDir;
            startInfo.Environment["LOCALAPPDATA"] = paths.LocalAppDataDir;
        }

        if (profile.BypassSharedKeyring)
        {
            startInfo.Environment["SSH_CONNECTION"] = DefaultSshConnectionValue;
        }

        foreach (var kvp in profile.CustomEnvironmentVariables)
        {
            if (!string.IsNullOrWhiteSpace(kvp.Key))
            {
                startInfo.Environment[kvp.Key.Trim()] = kvp.Value ?? string.Empty;
            }
        }

        return startInfo;
    }

    public static LaunchResult Launch(AppProfile profile, string? defaultRoot = null)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var paths = ResolveSandboxPaths(profile, defaultRoot);

        if (string.IsNullOrWhiteSpace(profile.ExecutablePath))
        {
            return new LaunchResult(false, null, paths, "Executable path is required.");
        }

        var expandedExe = Environment.ExpandEnvironmentVariables(profile.ExecutablePath.Trim());
        if (!File.Exists(expandedExe))
        {
            return new LaunchResult(false, null, paths, $"Executable not found: {expandedExe}");
        }

        try
        {
            EnsureSandboxDirectories(paths);

            var profileToLaunch = new AppProfile
            {
                Id = profile.Id,
                ProfileName = profile.ProfileName,
                AppName = profile.AppName,
                Preset = profile.Preset,
                ExecutablePath = expandedExe,
                ExtraArguments = profile.ExtraArguments,
                IsolateProfilePaths = profile.IsolateProfilePaths,
                PassUserDataDir = profile.PassUserDataDir,
                PassNewWindowFlag = profile.PassNewWindowFlag,
                BypassSharedKeyring = profile.BypassSharedKeyring,
                CustomEnvironmentVariables = profile.CustomEnvironmentVariables,
                SandboxRootOverride = profile.SandboxRootOverride
            };

            var startInfo = BuildStartInfo(profileToLaunch, defaultRoot);
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return new LaunchResult(false, null, paths, "Process.Start returned null.");
            }

            return new LaunchResult(true, process.Id, paths);
        }
        catch (Exception ex)
        {
            return new LaunchResult(false, null, paths, ex.Message);
        }
    }

    public static bool TryDeleteSandbox(AppProfile profile, out string? errorMessage, string? defaultRoot = null)
    {
        try
        {
            var paths = ResolveSandboxPaths(profile, defaultRoot);
            if (Directory.Exists(paths.BaseDir))
            {
                Directory.Delete(paths.BaseDir, recursive: true);
            }

            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    internal static string BuildSafeFolderName(string profileName, Guid id)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var raw = string.IsNullOrWhiteSpace(profileName) ? "Profile" : profileName.Trim();
        var chars = raw.ToCharArray();

        for (var i = 0; i < chars.Length; i++)
        {
            if (Array.IndexOf(invalidChars, chars[i]) >= 0 || char.IsWhiteSpace(chars[i]))
            {
                chars[i] = '_';
            }
        }

        var safeName = new string(chars).Trim('_');
        if (string.IsNullOrEmpty(safeName))
        {
            safeName = "Profile";
        }

        var shortId = id.ToString("N")[..6];
        return $"{safeName}_{shortId}";
    }
}
