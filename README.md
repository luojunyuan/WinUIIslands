# WinUI Islands

[![NuGet](https://img.shields.io/nuget/v/WinUIIslands?label=WinUIIslands)](https://www.nuget.org/packages/WinUIIslands/)
[![Templates](https://img.shields.io/nuget/v/WinUIIslands.Templates?label=templates)](https://www.nuget.org/packages/WinUIIslands.Templates/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/license/mit)

WinUI Islands brings UWP XAML and WinUI 2 controls to unpackaged .NET 10 desktop applications. It provides a native Win32 window host, XAML Islands startup, custom title bars, backdrop support, AppWindow-style APIs and a NativeAOT-compatible build pipeline. Support Windows 10 version 1903 (build 18362) and later.

`WinUIIslands` includes `ModernUwp.CLI.BuildTools` transitively, so application projects build and publish with the .NET CLI without requiring Visual Studio.

![WinUIIslands App1](https://raw.githubusercontent.com/luojunyuan/WinUIIslands/master/docs/images/app1.png)

## Why WinUI Islands

- Deployment size: 14.83 MiB across 3 files with ARM64 NativeAOT, versus 129.45 MiB across 198 files with self-contained JIT.
- Runtime memory: About 9.2 MiB idle private working set.
- Cold start: Faster than WinUI 3.
- Fast fixes: Build our own framework for quickly fix issues (There is no window-resize flickering issue in WinUIIslands).
- WinUI 3-aligned APIs: Closely follows WinUI 3 API design for easy migration from UWP or WinUI 3.
- MISC: No MSIX required. Custom `TitleBar`, Mica/Acrylic, and anything you want.

## Quick start with the template

Install the template package:

```powershell
dotnet new install WinUIIslands.Templates
```

Create an application:

```powershell
dotnet new winuiislands -n HelloIslands
cd HelloIslands
```

Build the XAML project with the .NET CLI:

```powershell
dotnet build .\HelloIslands.csproj -c Debug -r win-x64
.\bin\x64\Debug\net10.0-windows10.0.26100.0\HelloIslands.exe
```

> Use `ARM64` or `x86` instead of `x64` when appropriate.

Publish a self-contained NativeAOT build:

```powershell
dotnet publish .\HelloIslands.csproj -c Release -r win-x64
```

The application is written to:

```text
bin\x64\Release\net10.0-windows10.0.26100.0\win-x64\publish\
```

## Build this repository

```powershell
dotnet build .\WinUIIslands\WinUIIslands.csproj
dotnet build .\App1\App1\App1.csproj -c Debug -r win-x64
```

## License

MIT
