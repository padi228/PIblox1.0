# PIblox 1.0

PIblox is a Windows desktop launcher scaffold for Roblox Player and Roblox Studio, with a dark purple-blue interface.

## Current implementation

- Detects common local Roblox Player and Studio installation folders.
- Provides Player and Studio launch buttons (using Roblox URI handlers with a local executable fallback).
- Opens the official Roblox experience search for a game name, numeric place ID, or HTTPS roblox.com link.
- Stores favorites and recent links locally.
- Shows CPU and RAM usage, and can ping www.roblox.com.
- Saves settings under %LOCALAPPDATA%\PIblox.
- Supports an optional per-user Windows startup entry.
- Creates and exports a local diagnostics report.
- Checks the GitHub Releases endpoint for a newer published version.
- Includes a Windows installer definition, a portable build script, and a GitHub Actions Windows build.

## Build

Requirements: Windows x64 and the .NET 10 SDK.

For a portable self-contained build:

    dotnet publish src/PIblox.App/PIblox.App.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/publish

To build PIblox_Setup.exe, install Inno Setup 6 and run:

    .\scripts\build-installer.ps1

GitHub Actions builds both the installer and portable ZIP on pushes to main, pull requests, or manual runs. A version tag such as v1.0.1 also publishes the generated files as a GitHub Release.

## Important status

This is an actively developed preview. The source has been committed to GitHub, but a successful Windows compile, installer run, and Roblox launch test still need to be confirmed by the Windows build workflow and a real Windows computer. Do not treat planned items in the roadmap as implemented.

## Security

PIblox only opens HTTPS links on roblox.com from its in-app game-link field. It does not ask for Roblox passwords or session cookies. Never add credentials, tokens, private data, or .env files to this public repository.
