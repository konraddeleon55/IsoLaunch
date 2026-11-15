using System.Collections.Generic;

namespace IsoLaunch.Core.Models;

/// <summary>
/// Describes default isolation flags and common Windows install paths for an app preset.
/// </summary>
public sealed record AppPresetDefinition(
    AppPresetKind Kind,
    string DisplayName,
    string Description,
    bool DefaultIsolateProfilePaths,
    bool DefaultPassUserDataDir,
    bool DefaultPassNewWindowFlag,
    bool DefaultBypassSharedKeyring,
    IReadOnlyList<string> CandidateExecutablePaths)
{
    public static IReadOnlyList<AppPresetDefinition> All { get; } =
    [
        new(
            AppPresetKind.Antigravity,
            "Antigravity",
            "Isolates .gemini config, Electron user-data-dir, and language_server.exe keyring.",
            DefaultIsolateProfilePaths: true,
            DefaultPassUserDataDir: true,
            DefaultPassNewWindowFlag: true,
            DefaultBypassSharedKeyring: true,
            CandidateExecutablePaths:
            [
                @"%LOCALAPPDATA%\Programs\Antigravity\Antigravity.exe",
                @"%ProgramFiles%\Antigravity\Antigravity.exe"
            ]),
        new(
            AppPresetKind.VsCode,
            "VS Code",
            "Isolates extensions, settings, and Electron window state via --user-data-dir.",
            DefaultIsolateProfilePaths: true,
            DefaultPassUserDataDir: true,
            DefaultPassNewWindowFlag: true,
            DefaultBypassSharedKeyring: false,
            CandidateExecutablePaths:
            [
                @"%LOCALAPPDATA%\Programs\Microsoft VS Code\Code.exe",
                @"%ProgramFiles%\Microsoft VS Code\Code.exe"
            ]),
        new(
            AppPresetKind.Chrome,
            "Google Chrome",
            "Runs a completely separate Chrome profile with its own cache and cookies.",
            DefaultIsolateProfilePaths: true,
            DefaultPassUserDataDir: true,
            DefaultPassNewWindowFlag: true,
            DefaultBypassSharedKeyring: false,
            CandidateExecutablePaths:
            [
                @"%ProgramFiles%\Google\Chrome\Application\chrome.exe",
                @"%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe",
                @"%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe"
            ]),
        new(
            AppPresetKind.Edge,
            "Microsoft Edge",
            "Runs a completely separate Edge profile with its own cache and cookies.",
            DefaultIsolateProfilePaths: true,
            DefaultPassUserDataDir: true,
            DefaultPassNewWindowFlag: true,
            DefaultBypassSharedKeyring: false,
            CandidateExecutablePaths:
            [
                @"%ProgramFiles(x86)%\Microsoft\Edge\Application\msedge.exe",
                @"%ProgramFiles%\Microsoft\Edge\Application\msedge.exe"
            ]),
        new(
            AppPresetKind.Slack,
            "Slack",
            "Runs an isolated Slack workspace session.",
            DefaultIsolateProfilePaths: true,
            DefaultPassUserDataDir: true,
            DefaultPassNewWindowFlag: false,
            DefaultBypassSharedKeyring: false,
            CandidateExecutablePaths:
            [
                @"%LOCALAPPDATA%\slack\slack.exe",
                @"%ProgramFiles%\Slack\slack.exe"
            ]),
        new(
            AppPresetKind.Discord,
            "Discord",
            "Runs an isolated Discord account session.",
            DefaultIsolateProfilePaths: true,
            DefaultPassUserDataDir: true,
            DefaultPassNewWindowFlag: false,
            DefaultBypassSharedKeyring: false,
            CandidateExecutablePaths:
            [
                @"%LOCALAPPDATA%\Discord\Update.exe"
            ]),
        new(
            AppPresetKind.Custom,
            "Custom Application",
            "Launch any Windows executable with isolated profile directories and custom flags.",
            DefaultIsolateProfilePaths: true,
            DefaultPassUserDataDir: false,
            DefaultPassNewWindowFlag: false,
            DefaultBypassSharedKeyring: false,
            CandidateExecutablePaths: [])
    ];

    public static AppPresetDefinition Get(AppPresetKind kind)
    {
        foreach (var preset in All)
        {
            if (preset.Kind == kind)
            {
                return preset;
            }
        }

        return All[^1];
    }
}
