# IsoLaunch

> **Run isolated, multi-account instances of desktop apps on Windows — without profile collisions or shared credential bleed.**

![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11%20(x64)-0078D4?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square)
![UI](https://img.shields.io/badge/UI-WPF-68217A?style=flat-square)

Modern Electron, Chromium, and IDE apps (like **Antigravity**, **VS Code**, **Chrome**, **Edge**, **Slack**, and **Discord**) normally enforce a single shared profile and credential store. Logging into a second work or personal account often overwrites your active session or forces you to sign out.

**IsoLaunch** solves this by launching each profile inside its own sandboxed directory and isolated process environment.

---

## Key Features

- **Built-in App Presets** — Pre-configured isolation defaults and automatic executable discovery for:
  - **Antigravity**
  - **VS Code**
  - **Google Chrome**
  - **Microsoft Edge**
  - **Slack**
  - **Discord**
  - **Custom** (any Windows `.exe`)
- **True Environment Isolation** — Overrides `USERPROFILE`, `APPDATA`, and `LOCALAPPDATA` per child process so app state, caches, and configs stay strictly separated.
- **Electron & Chromium Multi-Instance Flags** — Optionally passes `--user-data-dir="<UserDataDir>"` and `--new-window` to prevent new launches from attaching to an existing window.
- **Shared Keyring Bypass** — Optionally sets `SSH_CONNECTION` so apps like Antigravity's `language_server.exe` store OAuth tokens inside the isolated profile instead of the shared Windows Credential Manager.
- **Custom Arguments & Environment Variables** — Attach per-profile CLI flags, custom environment variables, or a custom sandbox root path.
- **Profile Cloning & Persistence** — Duplicate existing configurations in one click; profiles are stored cleanly in JSON via `ProfileStore`.

---

## How Isolation Works

When you launch a profile, IsoLaunch creates an isolated sandbox directory (by default under `%LocalAppData%\IsoLaunch\Profiles\<ProfileName>_<Id>`) and configures the child process environment:

| Setting / Flag | Effect |
| :--- | :--- |
| `USERPROFILE` | Redirected to `<SandboxRoot>` |
| `APPDATA` | Redirected to `<SandboxRoot>\AppData\Roaming` |
| `LOCALAPPDATA` | Redirected to `<SandboxRoot>\AppData\Local` |
| `--user-data-dir` | Pointed to `<SandboxRoot>\UserData` (for Electron/Chromium apps) |
| `--new-window` | Forces a separate application window |
| `SSH_CONNECTION` | Bypasses shared Windows Credential Manager keyring when enabled |

---

## Installation

### Option 1: Windows Installer (Recommended)
1. Download `IsoLaunch-Setup-<version>-win-x64.exe` from the **Releases** page.
2. Run the installer (supports both per-user and system-wide installation, with optional Desktop shortcut and clean Windows uninstaller).

### Option 2: Build from Source

**Prerequisites:**
- Windows 10/11 (x64)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

```powershell
# Clone the repository
git clone https://github.com/konraddeleon55/IsoLaunch.git
cd IsoLaunch

# Run unit tests
dotnet test

# Run the app
dotnet run --project IsoLaunch.App\IsoLaunch.App.csproj
```

---

## Building the Windows Installer

The repository includes an [Inno Setup](https://jrsoftware.org/isinfo.php) script (`installer/IsoLaunch.iss`) and an automated build script (`scripts/build-installer.ps1`) that publishes a self-contained `win-x64` binary and compiles the installer.

1. Install Inno Setup 6:
   ```powershell
   winget install JRSoftware.InnoSetup
   ```
2. Build the installer (auto-increments the patch version in `Directory.Build.props`):
   ```powershell
   .\scripts\build-installer.ps1
   ```
   The generated installer will be placed in `dist\IsoLaunch-Setup-<version>-win-x64.exe`.

   *(Optional)* Pass `-Bump minor` / `-Bump major` / `-Bump none`, `-Version "1.2.0"` to set a custom version, or `-FrameworkDependent` to build a smaller installer that relies on an existing .NET 10 Desktop Runtime.

---

## Project Structure

```text
IsoLaunch/
├── IsoLaunch.App/          # WPF desktop application (UI, Splash & MainWindow)
├── IsoLaunch.Core/         # Core models, preset definitions, discovery, launcher & store
├── IsoLaunch.Tests/        # xUnit unit tests for launcher & profile persistence
├── installer/              # Inno Setup script (IsoLaunch.iss)
├── scripts/                # Installer build automation (build-installer.ps1)
└── IsoLaunch.slnx          # Solution file
```

---

## Contributing

Contributions, bug reports, and new app presets are welcome! Feel free to open an issue or submit a pull request.
