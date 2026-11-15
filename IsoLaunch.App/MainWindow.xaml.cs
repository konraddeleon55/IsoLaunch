using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using IsoLaunch.Core.Models;
using IsoLaunch.Core.Services;
using Microsoft.Win32;

namespace IsoLaunch.App;

public partial class MainWindow : Window
{
    private readonly ProfileStore _store = new();
    private List<AppProfile> _profiles = [];
    private AppProfile _editingProfile = new();
    private bool _isLoadingEditor;

    public MainWindow()
    {
        InitializeComponent();

        PresetComboBox.ItemsSource = AppPresetDefinition.All;
        PresetComboBox.DisplayMemberPath = nameof(AppPresetDefinition.DisplayName);
        StorePathText.Text = _store.FilePath;

        LoadProfiles();
        StartNewProfile(AppPresetKind.Antigravity);
    }

    private void LoadProfiles()
    {
        _profiles = _store.Load();
        RefreshProfileList();
    }

    private void RefreshProfileList(Guid? selectId = null)
    {
        var query = SearchBox.Text?.Trim() ?? string.Empty;
        var filtered = string.IsNullOrEmpty(query)
            ? _profiles
            : _profiles.Where(p =>
                p.ProfileName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.AppName.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

        ProfilesListBox.ItemsSource = null;
        ProfilesListBox.ItemsSource = filtered;

        if (selectId.HasValue)
        {
            var match = filtered.FirstOrDefault(p => p.Id == selectId.Value);
            if (match is not null)
            {
                ProfilesListBox.SelectedItem = match;
            }
        }
    }

    private void StartNewProfile(AppPresetKind presetKind)
    {
        var preset = AppPresetDefinition.Get(presetKind);
        var discoveredExe = AppDiscoveryService.FindExecutable(presetKind) ?? string.Empty;
        var countForPreset = _profiles.Count(p => p.Preset == presetKind) + 1;

        var profile = new AppProfile
        {
            ProfileName = $"{preset.DisplayName} {countForPreset}",
            AppName = preset.DisplayName,
            Preset = presetKind,
            ExecutablePath = discoveredExe,
            IsolateProfilePaths = preset.DefaultIsolateProfilePaths,
            PassUserDataDir = preset.DefaultPassUserDataDir,
            PassNewWindowFlag = preset.DefaultPassNewWindowFlag,
            BypassSharedKeyring = preset.DefaultBypassSharedKeyring
        };

        ProfilesListBox.SelectedItem = null;
        LoadIntoEditor(profile, isNew: true);
        SetStatus(string.IsNullOrEmpty(discoveredExe)
            ? $"New {preset.DisplayName} profile. Browse to select the executable."
            : $"New {preset.DisplayName} profile (auto-detected executable).");
    }

    private void LoadIntoEditor(AppProfile profile, bool isNew)
    {
        _isLoadingEditor = true;
        try
        {
            _editingProfile = profile;
            EditorTitleText.Text = isNew ? "New Profile" : $"Edit Profile — {profile.ProfileName}";

            PresetComboBox.SelectedItem = AppPresetDefinition.Get(profile.Preset);
            ProfileNameBox.Text = profile.ProfileName;
            AppNameBox.Text = profile.AppName;
            ExecutablePathBox.Text = profile.ExecutablePath;
            IsolatePathsCheck.IsChecked = profile.IsolateProfilePaths;
            PassUserDataDirCheck.IsChecked = profile.PassUserDataDir;
            PassNewWindowCheck.IsChecked = profile.PassNewWindowFlag;
            BypassKeyringCheck.IsChecked = profile.BypassSharedKeyring;
            ExtraArgsBox.Text = profile.ExtraArguments;
            CustomEnvBox.Text = string.Join(
                Environment.NewLine,
                profile.CustomEnvironmentVariables.Select(kvp => $"{kvp.Key}={kvp.Value}"));

            UpdateSandboxPreview();
        }
        finally
        {
            _isLoadingEditor = false;
        }
    }

    private AppProfile CreatePreviewProfile()
    {
        return new AppProfile
        {
            Id = _editingProfile.Id,
            ProfileName = ProfileNameBox.Text,
            SandboxRootOverride = _editingProfile.SandboxRootOverride
        };
    }

    private void UpdateSandboxPreview()
    {
        SandboxPreviewBox.Text = InstanceLauncher.ResolveSandboxPaths(CreatePreviewProfile()).BaseDir;
    }

    private bool TrySaveEditorToStore(out AppProfile savedProfile)
    {
        var profileName = ProfileNameBox.Text.Trim();
        var exePath = ExecutablePathBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(profileName))
        {
            MessageBox.Show(this, "Please enter a Profile Name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            savedProfile = _editingProfile;
            return false;
        }

        if (string.IsNullOrWhiteSpace(exePath))
        {
            MessageBox.Show(this, "Please specify the Executable Path (.exe).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            savedProfile = _editingProfile;
            return false;
        }

        var selectedPreset = PresetComboBox.SelectedItem as AppPresetDefinition ?? AppPresetDefinition.Get(AppPresetKind.Custom);

        _editingProfile.Preset = selectedPreset.Kind;
        _editingProfile.ProfileName = profileName;
        _editingProfile.AppName = string.IsNullOrWhiteSpace(AppNameBox.Text) ? selectedPreset.DisplayName : AppNameBox.Text.Trim();
        _editingProfile.ExecutablePath = exePath;
        _editingProfile.IsolateProfilePaths = IsolatePathsCheck.IsChecked == true;
        _editingProfile.PassUserDataDir = PassUserDataDirCheck.IsChecked == true;
        _editingProfile.PassNewWindowFlag = PassNewWindowCheck.IsChecked == true;
        _editingProfile.BypassSharedKeyring = BypassKeyringCheck.IsChecked == true;
        _editingProfile.ExtraArguments = ExtraArgsBox.Text.Trim();
        _editingProfile.CustomEnvironmentVariables = ParseCustomEnv(CustomEnvBox.Text);

        var existingIndex = _profiles.FindIndex(p => p.Id == _editingProfile.Id);
        if (existingIndex >= 0)
        {
            _profiles[existingIndex] = _editingProfile;
        }
        else
        {
            _profiles.Add(_editingProfile);
        }

        _store.Save(_profiles);
        RefreshProfileList(_editingProfile.Id);
        EditorTitleText.Text = $"Edit Profile — {_editingProfile.ProfileName}";
        savedProfile = _editingProfile;
        SetStatus($"Saved profile \"{_editingProfile.ProfileName}\".");
        return true;
    }

    private static Dictionary<string, string> ParseCustomEnv(string text)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(text))
        {
            return dict;
        }

        var lines = text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var idx = line.IndexOf('=');
            if (idx <= 0)
            {
                continue;
            }

            var key = line[..idx].Trim();
            var value = line[(idx + 1)..].Trim();
            if (!string.IsNullOrEmpty(key))
            {
                dict[key] = value;
            }
        }

        return dict;
    }

    private void LaunchProfile(AppProfile profile)
    {
        var result = InstanceLauncher.Launch(profile);
        if (!result.Success)
        {
            SetStatus($"Failed to launch \"{profile.ProfileName}\": {result.ErrorMessage}");
            MessageBox.Show(this, result.ErrorMessage ?? "Unknown launch error.", "Launch Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        profile.LastLaunchedAt = DateTimeOffset.UtcNow;
        _store.Save(_profiles);
        SetStatus($"Launched \"{profile.ProfileName}\" ({profile.AppName}) — PID {result.ProcessId}.");
    }

    private void OpenSandboxFolder(AppProfile profile)
    {
        var paths = InstanceLauncher.ResolveSandboxPaths(profile);
        InstanceLauncher.EnsureSandboxDirectories(paths);
        using var _ = Process.Start(new ProcessStartInfo
        {
            FileName = paths.BaseDir,
            UseShellExecute = true
        });
    }

    private void SetStatus(string message)
    {
        StatusText.Text = message;
    }

    private void QuickAddAntigravity_Click(object sender, RoutedEventArgs e) => StartNewProfile(AppPresetKind.Antigravity);
    private void QuickAddVsCode_Click(object sender, RoutedEventArgs e) => StartNewProfile(AppPresetKind.VsCode);
    private void QuickAddChrome_Click(object sender, RoutedEventArgs e) => StartNewProfile(AppPresetKind.Chrome);
    private void NewProfile_Click(object sender, RoutedEventArgs e) => StartNewProfile(AppPresetKind.Custom);

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshProfileList(_editingProfile.Id);

    private void ProfilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProfilesListBox.SelectedItem is AppProfile selected)
        {
            LoadIntoEditor(selected, isNew: false);
        }
    }

    private void PresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingEditor || PresetComboBox.SelectedItem is not AppPresetDefinition preset)
        {
            return;
        }

        AppNameBox.Text = preset.DisplayName;
        IsolatePathsCheck.IsChecked = preset.DefaultIsolateProfilePaths;
        PassUserDataDirCheck.IsChecked = preset.DefaultPassUserDataDir;
        PassNewWindowCheck.IsChecked = preset.DefaultPassNewWindowFlag;
        BypassKeyringCheck.IsChecked = preset.DefaultBypassSharedKeyring;

        if (string.IsNullOrWhiteSpace(ExecutablePathBox.Text))
        {
            var found = AppDiscoveryService.FindExecutable(preset.Kind);
            if (!string.IsNullOrEmpty(found))
            {
                ExecutablePathBox.Text = found;
            }
        }
    }

    private void EditorField_Changed(object sender, TextChangedEventArgs e)
    {
        if (!_isLoadingEditor)
        {
            UpdateSandboxPreview();
        }
    }

    private void BrowseExecutable_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Application Executable",
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            ExecutablePathBox.Text = dialog.FileName;
            if (string.IsNullOrWhiteSpace(AppNameBox.Text))
            {
                AppNameBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
            }
        }
    }

    private void SaveProfile_Click(object sender, RoutedEventArgs e)
    {
        TrySaveEditorToStore(out _);
    }

    private void SaveAndLaunch_Click(object sender, RoutedEventArgs e)
    {
        if (TrySaveEditorToStore(out var saved))
        {
            LaunchProfile(saved);
        }
    }

    private void OpenSelectedSandbox_Click(object sender, RoutedEventArgs e)
    {
        OpenSandboxFolder(CreatePreviewProfile());
    }

    private void WipeSelectedSandbox_Click(object sender, RoutedEventArgs e)
    {
        var previewProfile = CreatePreviewProfile();
        var paths = InstanceLauncher.ResolveSandboxPaths(previewProfile);
        var confirm = MessageBox.Show(
            this,
            $"Delete all isolated data in:\n{paths.BaseDir}\n\nThis cannot be undone.",
            "Wipe Sandbox Data",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        if (InstanceLauncher.TryDeleteSandbox(previewProfile, out var error))
        {
            SetStatus($"Wiped sandbox directory for \"{previewProfile.ProfileName}\".");
        }
        else
        {
            MessageBox.Show(this, $"Could not wipe sandbox: {error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LaunchProfileCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: AppProfile profile })
        {
            LaunchProfile(profile);
        }
    }

    private void OpenFolderCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: AppProfile profile })
        {
            OpenSandboxFolder(profile);
        }
    }

    private void CloneProfileCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: AppProfile profile })
        {
            var cloned = profile.CloneWithNewId($"{profile.ProfileName} (Copy)");
            _profiles.Add(cloned);
            _store.Save(_profiles);
            RefreshProfileList(cloned.Id);
            SetStatus($"Cloned \"{profile.ProfileName}\" -> \"{cloned.ProfileName}\".");
        }
    }

    private void DeleteProfileCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: AppProfile profile })
        {
            var confirm = MessageBox.Show(
                this,
                $"Remove profile \"{profile.ProfileName}\" from IsoLaunch?\n(Sandbox files on disk are kept unless wiped.)",
                "Delete Profile",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            _profiles.RemoveAll(p => p.Id == profile.Id);
            _store.Save(_profiles);
            RefreshProfileList();
            if (_editingProfile.Id == profile.Id)
            {
                StartNewProfile(AppPresetKind.Antigravity);
            }

            SetStatus($"Deleted profile \"{profile.ProfileName}\".");
        }
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        => App.OpenExternalUri(e);
}
