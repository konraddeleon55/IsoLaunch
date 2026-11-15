using System;
using System.Collections.Generic;
using System.IO;
using IsoLaunch.Core.Models;
using IsoLaunch.Core.Services;
using Xunit;

namespace IsoLaunch.Tests;

public sealed class ProfileStoreTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsProfiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "IsoLaunchStoreTests", Guid.NewGuid().ToString("N"));
        var storePath = Path.Combine(tempDir, "profiles.json");

        try
        {
            var store = new ProfileStore(storePath);
            Assert.Empty(store.Load());

            var profiles = new List<AppProfile>
            {
                new()
                {
                    ProfileName = "Account1",
                    AppName = "Antigravity",
                    Preset = AppPresetKind.Antigravity,
                    ExecutablePath = @"C:\Apps\Antigravity\Antigravity.exe",
                    BypassSharedKeyring = true
                },
                new()
                {
                    ProfileName = "Account2",
                    AppName = "VS Code",
                    Preset = AppPresetKind.VsCode,
                    ExecutablePath = @"C:\Apps\VSCode\Code.exe",
                    BypassSharedKeyring = false
                }
            };

            store.Save(profiles);
            var loaded = store.Load();

            Assert.Equal(2, loaded.Count);
            Assert.Equal("Account1", loaded[0].ProfileName);
            Assert.Equal(AppPresetKind.Antigravity, loaded[0].Preset);
            Assert.True(loaded[0].BypassSharedKeyring);
            Assert.Equal("Account2", loaded[1].ProfileName);
            Assert.Equal(AppPresetKind.VsCode, loaded[1].Preset);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
