# Sidebar Diagnostics

Sidebar Diagnostics is a lightweight Windows desktop sidebar for live system monitoring. This fork has moved well beyond the original upstream README and now focuses on a compact, always-visible diagnostics view with a faster process monitor, grouped app view, richer process details, and release installers.

## Download

Installers and packaged builds are published in this repository's releases:

- [Latest release](https://github.com/AbdullahAhmed/SidebarDiagnostics/releases/latest)
- [All releases](https://github.com/AbdullahAhmed/SidebarDiagnostics/releases)

Current release assets include:

- `Setup.exe`
- `Setup.msi`
- Squirrel package files for update/install distribution

## What It Monitors

- Clock and date
- CPU load and supported CPU sensors
- RAM usage
- GPU load, VRAM usage, and supported GPU sensors
- Logical drives, capacity, read, and write throughput
- Network throughput and local IP
- Top processes with app-style grouping

## Current Highlights

- Compact sidebar UI intended for always-on desktop use
- Grouped process monitor that rolls child processes into app-level rows
- Expandable process groups with per-PID actions
- Rich process tooltips with on-demand details such as full name, CPU, memory, disk, GPU, network, PID, and related process stats
- Performance-focused process list updates to reduce UI churn and allocations
- Faster startup by deferring non-critical work until after the window is visible
- Graph window for supported metrics
- Customization, alerts, and hotkeys
- DPI-aware WPF desktop UI
- Release installer packaging via Squirrel

## Process Monitor Behavior

The Processes section is intentionally closer to Task Manager than a raw PID list.

- App groups are shown as a single row with aggregated CPU and RAM
- Child processes can be expanded when you need per-process visibility
- Grouped rows are meant for app-level visibility and tree actions
- Detailed per-process information loads only when you hover a process tooltip to keep steady-state overhead low

## Requirements

- Windows
- .NET Framework 4.7.2
- Administrator privileges

The app currently requests elevation on launch. Running elevated is important for several monitor and process-management features.

## Notes On Hardware Sensors

Sensor availability depends on the machine, drivers, Windows security settings, and what LibreHardwareMonitor can access on that hardware.

- If a metric is unavailable, the sidebar hides it instead of showing a fake or empty value
- CPU and GPU sensor coverage can vary across vendors and chip generations
- Data is sourced primarily from LibreHardwareMonitor, with some process detail gathered through Windows APIs and WMI

## Build From Source

### Prerequisites

- Visual Studio with .NET desktop development support, or MSBuild for .NET Framework projects
- NuGet package restore enabled
- Git submodules available

### Clone

```powershell
git clone --recurse-submodules https://github.com/AbdullahAhmed/SidebarDiagnostics.git
cd SidebarDiagnostics
```

If you already cloned without submodules:

```powershell
git submodule update --init --recursive
```

### Build

From Visual Studio:

- Open `SidebarDiagnostics.sln`
- Build the `SidebarDiagnostics` project

From MSBuild:

```powershell
& "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" ".\SidebarDiagnostics\SidebarDiagnostics.csproj" /t:Build /p:Configuration=Release /p:Platform=AnyCPU /m
```

Primary output:

- `SidebarDiagnostics\bin\Release\SidebarDiagnostics.exe`

## Installer Packaging

This repo uses Squirrel for Windows packaging. Release builds can be wrapped into installer artifacts such as:

- `Setup.exe`
- `Setup.msi`
- `RELEASES`
- full `.nupkg` packages

Installer artifacts are intended for releases, not day-to-day local debugging.

## Technology

- C#
- WPF
- .NET Framework 4.7.2
- LibreHardwareMonitor
- OxyPlot
- Squirrel.Windows

## Upstream

This project began from the original Sidebar Diagnostics project and has since diverged with substantial UI, process-monitoring, packaging, and performance changes.

## License

GNU General Public License v3.0
