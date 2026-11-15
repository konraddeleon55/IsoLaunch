using IsoLaunch.Core.Models;
using IsoLaunch.Core.Services;

namespace IsoLaunch.Tests;

public sealed class InstanceLauncherTests
{
    [Fact]
    public void BuildStartInfo_IsolatesEnvironmentAndPassesFlags_ForAntigravityPreset()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "IsoLaunchTests", Guid.NewGuid().ToString("N"));
        var profile = new AppProfile
        {
            Id = Guid.Parse("11223344-5566-7788-99aa-bbccddeeff00"),
            ProfileName = "Work Account",
            AppName = "Antigravity",
            Preset = AppPresetKind.Antigravity,
            ExecutablePath = @"C:\Apps\Antigravity\Antigravity.exe",
            ExtraArguments = "--verbose",
            IsolateProfilePaths = true,
            PassUserDataDir = true,
            PassNewWindowFlag = true,
            BypassSharedKeyring = true
        };
        profile.CustomEnvironmentVariables["CUSTOM_FLAG"] = "1";

        var startInfo = InstanceLauncher.BuildStartInfo(profile, tempRoot);
        var expectedPaths = InstanceLauncher.ResolveSandboxPaths(profile, tempRoot);

        Assert.False(startInfo.UseShellExecute);
        Assert.Equal(profile.ExecutablePath, startInfo.FileName);
        Assert.Equal(@"C:\Apps\Antigravity", startInfo.WorkingDirectory);

        Assert.Equal(expectedPaths.BaseDir, startInfo.Environment["USERPROFILE"]);
        Assert.Equal(expectedPaths.AppDataDir, startInfo.Environment["APPDATA"]);
        Assert.Equal(expectedPaths.LocalAppDataDir, startInfo.Environment["LOCALAPPDATA"]);
        Assert.Equal(InstanceLauncher.DefaultSshConnectionValue, startInfo.Environment["SSH_CONNECTION"]);
        Assert.Equal("1", startInfo.Environment["CUSTOM_FLAG"]);

        Assert.Contains($"--user-data-dir=\"{expectedPaths.UserDataDir}\"", startInfo.Arguments);
        Assert.Contains("--new-window", startInfo.Arguments);
        Assert.Contains("--verbose", startInfo.Arguments);
    }

    [Fact]
    public void BuildStartInfo_RespectsDisabledIsolationAndKeyringFlags()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "IsoLaunchTests", Guid.NewGuid().ToString("N"));
        var profile = new AppProfile
        {
            ProfileName = "Custom",
            AppName = "Notepad",
            Preset = AppPresetKind.Custom,
            ExecutablePath = @"C:\Windows\System32\notepad.exe",
            IsolateProfilePaths = false,
            PassUserDataDir = false,
            PassNewWindowFlag = false,
            BypassSharedKeyring = false
        };

        var startInfo = InstanceLauncher.BuildStartInfo(profile, tempRoot);

        Assert.False(startInfo.UseShellExecute);
        Assert.Equal(string.Empty, startInfo.Arguments);
        Assert.False(startInfo.Environment.ContainsKey("SSH_CONNECTION") &&
                     startInfo.Environment["SSH_CONNECTION"] == InstanceLauncher.DefaultSshConnectionValue);
    }

    [Fact]
    public void ResolveSandboxPaths_SanitizesInvalidCharacters()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "IsoLaunchTests", Guid.NewGuid().ToString("N"));
        var profile = new AppProfile
        {
            Id = Guid.Parse("abcdef12-3456-7890-abcd-ef1234567890"),
            ProfileName = "Work: Account/1?"
        };

        var paths = InstanceLauncher.ResolveSandboxPaths(profile, tempRoot);

        Assert.StartsWith(tempRoot, paths.BaseDir);
        Assert.Contains("Work__Account_1_abcdef", paths.BaseDir);
        Assert.EndsWith("UserData", paths.UserDataDir);
        Assert.EndsWith(Path.Combine("AppData", "Roaming"), paths.AppDataDir);
        Assert.EndsWith(Path.Combine("AppData", "Local"), paths.LocalAppDataDir);
    }

    [Fact]
    public void Launch_ReturnsError_WhenExecutableDoesNotExist()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "IsoLaunchTests", Guid.NewGuid().ToString("N"));
        var profile = new AppProfile
        {
            ProfileName = "Missing",
            ExecutablePath = Path.Combine(tempRoot, "NonExistentApp.exe")
        };

        var result = InstanceLauncher.Launch(profile, tempRoot);

        Assert.False(result.Success);
        Assert.Null(result.ProcessId);
        Assert.Contains("Executable not found", result.ErrorMessage);
    }

    [Fact]
    public void BuildStartInfo_ExpandsEnvironmentVariablesInExecutablePath()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "IsoLaunchTests", Guid.NewGuid().ToString("N"));
        var profile = new AppProfile
        {
            ProfileName = "EnvExe",
            ExecutablePath = @"  %SystemRoot%\System32\notepad.exe  ",
            IsolateProfilePaths = false,
            PassUserDataDir = false,
            PassNewWindowFlag = false,
            BypassSharedKeyring = false
        };

        var startInfo = InstanceLauncher.BuildStartInfo(profile, tempRoot);
        var expectedExe = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\notepad.exe");

        Assert.Equal(expectedExe, startInfo.FileName);
        Assert.Equal(Path.GetDirectoryName(expectedExe), startInfo.WorkingDirectory);
    }
}
