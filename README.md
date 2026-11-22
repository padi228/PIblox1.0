# PIblox 1.0

A Windows desktop launcher scaffold for Roblox Player and Roblox Studio. The initial commit lays down a small WPF application with local app detection, safe launch buttons, a settings file, and a diagnostics panel.

## Status
This is an early scaffold. It has not yet been compiled or tested on a Windows machine.

## Requirements
- Windows 10/11
- .NET 10 SDK

## Build
Open `src/PIblox.App/PIblox.App.csproj` in Visual Studio or run:

```powershell
dotnet build src/PIblox.App/PIblox.App.csproj
dotnet run --project src/PIblox.App/PIblox.App.csproj
```

## Current scaffold features
- Detect Roblox Player and Studio using common install locations
- Launch detected apps
- Local settings JSON
- Basic diagnostic report

## Not yet implemented
Game discovery catalog, social, voice calls, overlay, VPN integration, update system, installer, automated tests.

Never commit tokens, passwords, or private data.
