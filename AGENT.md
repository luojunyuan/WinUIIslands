# CoreIsland — AI Agent Reference

XAML Islands host library for unpackaged .NET 10 desktop apps.

- **CoreIsland** — library providing `CoreIsland.Application` + `CoreIsland.Window`.
- **App1** — sample/demo app.

## UWP XAML, NOT WinUI 3

All XAML types are `Windows.UI.Xaml.*`. Never use WinUI 3's `Microsoft.UI.Xaml.*`.

MUXC (WinUI 2, the `Microsoft.UI.Xaml` NuGet package) is an **extension controls library** that layers on top of UWP. It has its own `Microsoft.UI.Xaml.Controls` namespace — this is **MUXC's namespace, not WinUI 3's**, and it's the only `Microsoft.*` namespace used in this project. E.g. `<XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />` pulls Win11 control styles into an otherwise pure UWP app.

## Build

The **startup project** (App1) is a UWP app. CoreIsland's build targets now transparently
redirect `dotnet build` → Framework MSBuild (required for the UWP XAML compiler), so
both entry points work:

```powershell
# ✅ Correct — dotnet (auto-redirects to Framework MSBuild)
dotnet build App1\App1\App1.csproj -p:Platform=x64 -p:Configuration=Debug
dotnet run   --project App1\App1 -p:Platform=x64 -p:Configuration=Debug
dotnet publish App1\App1\App1.csproj -p:Platform=x64 -p:Configuration=Release -p:RuntimeIdentifier=win-x64

# ✅ Also fine — MSBuild directly
msbuild CoreIsland.slnx -p:Platform=x64 -p:Configuration=Debug
msbuild CoreIsland.slnx -p:Platform=ARM64 -p:Configuration=Release

# ✅ CoreIsland library builds with either
dotnet build CoreIsland\CoreIsland.csproj
```

If `msbuild.exe` is not found (e.g. no Visual Studio installed), `dotnet build` will
fail with a clear error telling the user to install VS.

## File Encoding

Always create or save files with **UTF-8 BOM** encoding and **CRLF** line endings. This is required for compatibility with MSBuild / Windows tooling in this repository.

