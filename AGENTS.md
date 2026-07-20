# WinUIIslands - AI Agent Reference

XAML Islands host library for unpackaged .NET 10 desktop apps.

- **WinUIIslands** - library providing `WinUIIslands.Application` + `WinUIIslands.Window`.
- **App1** - sample/demo app.

## UWP XAML, NOT WinUI 3

All XAML types are `Windows.UI.Xaml.*`. Never use WinUI 3's `Microsoft.UI.Xaml.*`.

MUXC (WinUI 2, the `Microsoft.UI.Xaml` NuGet package) is an **extension controls library** that layers on top of UWP. It has its own `Microsoft.UI.Xaml.Controls` namespace - this is **MUXC's namespace, not WinUI 3's**, and it's the only `Microsoft.*` namespace used in this project. E.g. `<XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />` pulls Win11 control styles into an otherwise pure UWP app.

## Build

`WinUIIslands` itself is a class library. Building it directly with `dotnet build` is fine:

```powershell
dotnet build WinUIIslands\WinUIIslands.csproj
```

Projects that contain XAML files, such as `App1`, use `ModernUwp.CLI.BuildTools` with the .NET CLI. Always specify a concrete runtime (`win-x86`, `win-x64`, or `win-arm64`); `Any CPU` does not work with UWP SDK dependencies such as VCLibs/UCRT.

```powershell
dotnet build App1\App1\App1.csproj -c Debug -r win-x64
dotnet publish App1\App1\App1.csproj -c Release -r win-x64
```

## File Encoding

Always create or save files with **UTF-8 BOM** encoding and **CRLF** line endings. This is required for compatibility with MSBuild / Windows tooling in this repository.
