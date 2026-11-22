# Contributing to PIblox

1. Keep changes small and describe user-visible behavior.
2. Do not add credentials, tokens, cookies, or private data.
3. Keep all game links HTTPS and restricted to roblox.com.
4. Avoid undocumented Roblox endpoints, scraping, exploit behavior, or anti-cheat bypasses.
5. Build on Windows x64 with the .NET 10 SDK:

       dotnet build src/PIblox.App/PIblox.App.csproj --configuration Release

6. For the installer, install Inno Setup 6 and run:

       .\scripts\build-installer.ps1

A successful GitHub Actions Windows build is required before describing a commit as build-verified.
