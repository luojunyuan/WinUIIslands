# ModernUwp.CLI.BuildTools

NuGet build tools for building modern .NET UWP XAML applications with `dotnet build` and `dotnet publish`, without requiring Visual Studio.

## Update history

- `1.0.3` - Added support for `Exe` preojects vs builds.
- `1.0.2` - Added support for `Exe` projects cli builds alongside `WinExe`, including XAML compilation and PRI metadata generation.
- `1.0.1` - Updated the package to `netstandard2.1` and stopped exposing its Windows SDK build dependencies transitively.
- `1.0.0` - Made the build hooks transitive and enabled `dotnet build` and `dotnet publish` support for release UWP XAML applications.

```xml
<PackageReference Include="ModernUwp.CLI.BuildTools"
                  Version="1.0.2"
                  PrivateAssets="all" />
```

Build and publish `WinExe` projects for `win-x86`, `win-x64`, and `win-arm64`:

```powershell
dotnet build -c Debug -r win-x64
dotnet publish -c Release -r win-x64
dotnet run -v:minimal -r win-x64
```
