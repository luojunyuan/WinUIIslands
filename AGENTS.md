# CoreIsland - AI Agent Reference

XAML Islands host library for unpackaged .NET 10 desktop apps.

- **CoreIsland** - library providing `CoreIsland.Application` + `CoreIsland.Window`.
- **App1** - sample/demo app.

## UWP XAML, NOT WinUI 3

All XAML types are `Windows.UI.Xaml.*`. Never use WinUI 3's `Microsoft.UI.Xaml.*`.

MUXC (WinUI 2, the `Microsoft.UI.Xaml` NuGet package) is an **extension controls library** that layers on top of UWP. It has its own `Microsoft.UI.Xaml.Controls` namespace - this is **MUXC's namespace, not WinUI 3's**, and it's the only `Microsoft.*` namespace used in this project. E.g. `<XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />` pulls Win11 control styles into an otherwise pure UWP app.

## Build

`CoreIsland` itself is a class library. Building it directly with `dotnet build` is fine:

```powershell
dotnet build CoreIsland\CoreIsland.csproj
```

Projects that contain XAML files, such as `CoreIsland.TitleBar` and `App1`, must be built with the Visual Studio MSBuild toolchain. Their XAML compilation depends on Visual Studio's UWP XAML build tasks, so plain `dotnet build` is not the right validation path for those projects.

Build XAML projects from a Visual Studio Developer Command Prompt, or invoke the Visual Studio MSBuild executable directly. Always specify a concrete platform (`x86`, `x64`, `ARM`, or `ARM64`); `Any CPU` does not work with UWP SDK dependencies such as VCLibs/UCRT.

```powershell
# From a Visual Studio Developer Command Prompt
msbuild App1\App1\App1.csproj /restore /m /p:Configuration=Debug /p:Platform=x64

# Or invoke VS MSBuild directly
& "C:\Program Files\Microsoft Visual Studio\18\Professional\MSBuild\Current\Bin\MSBuild.exe" App1\App1\App1.csproj /restore /m /p:Configuration=Debug /p:Platform=x64
```

## File Encoding

Always create or save files with **UTF-8 BOM** encoding and **CRLF** line endings. This is required for compatibility with MSBuild / Windows tooling in this repository.
