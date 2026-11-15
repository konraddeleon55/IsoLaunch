using System;
using System.IO;
using IsoLaunch.Core.Models;

namespace IsoLaunch.Core.Services;

/// <summary>
/// Discovers installed executables for built-in application presets.
/// </summary>
public static class AppDiscoveryService
{
    public static string? FindExecutable(AppPresetKind presetKind)
    {
        var preset = AppPresetDefinition.Get(presetKind);
        foreach (var candidate in preset.CandidateExecutablePaths)
        {
            var expanded = Environment.ExpandEnvironmentVariables(candidate);
            if (!string.IsNullOrWhiteSpace(expanded) && File.Exists(expanded))
            {
                return expanded;
            }
        }

        return null;
    }
}
