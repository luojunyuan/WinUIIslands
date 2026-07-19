# WinUIIslands 1.1.0

Build unpackaged .NET 10 desktop applications with UWP XAML, WinUI 2.8.7 controls and NativeAOT deployment.

Measured on Windows 11 ARM64, the generated NativeAOT starter uses 3 runtime files and 14.83 MiB excluding PDB symbols, versus 198 files and 129.45 MiB for self-contained JIT. Its idle private working set is about 9.2 MiB.

The fastest way to start is the project template:

```powershell
dotnet new install WinUIIslands.Templates::1.1.0
dotnet new winuiislands -n HelloIslands
```

Build the generated XAML project from a Visual Studio Developer PowerShell or Developer Command Prompt:

```powershell
msbuild .\HelloIslands\HelloIslands.csproj /restore /m /p:Configuration=Debug /p:Platform=x64
```

Publish NativeAOT:

```powershell
msbuild .\HelloIslands\HelloIslands.csproj /t:Publish /restore /m /p:Configuration=Release /p:Platform=x64 /p:RuntimeIdentifier=win-x64
```

Supported targets: Windows 10 version 1903 or newer, x86/x64/ARM64, .NET 10, UWP XAML with WinUI 2 controls.

Documentation and App1 screenshot: https://github.com/luojunyuan/WinUIIslands
