# ModernUwp.CLI.BuildTools

NuGet build tools for building modern .NET UWP XAML applications with `dotnet build` and `dotnet publish`, without requiring Visual Studio.

```xml
<PackageReference Include="ModernUwp.CLI.BuildTools"
                  Version="1.0.0"
                  PrivateAssets="all" />
```

Build and publish `WinExe` projects for `win-x86`, `win-x64`, and `win-arm64`:

```powershell
dotnet build -c Debug -r win-x64
dotnet publish -c Release -r win-x64
```
