# PIblox 1.0

PIblox is a Windows desktop launcher scaffold for Roblox Player and Roblox Studio, with a dark purple-blue interface.

## Current implementation

- Provides a home-screen shortcut to the official Roblox login page; credentials remain in the browser.
- Detects common local Roblox Player and Studio installation folders.
- Provides Player and Studio launch buttons (using Roblox URI handlers with a local executable fallback).
- Opens the official Roblox experience search for a game name, numeric place ID, or HTTPS roblox.com link.
- Stores favorites and recent links locally.
- Shows CPU and RAM usage, and can ping www.roblox.com.
- Includes an embedded PI Call window with microphone selection, two-person room codes, WebRTC audio peer connection, mute, and compact mode.
- Includes the Cloudflare Durable Objects signaling Worker source in `services/pi-call-signaling`; calls remain unavailable until this Worker is deployed and its HTTPS URL is entered in PI Call.
- Saves settings under %LOCALAPPDATA%\PIblox.
- Supports an optional per-user Windows startup entry.
- Creates and exports a local diagnostics report.
- Checks the GitHub Releases endpoint for a newer published version.
- Includes a Windows installer definition, a portable build script, and a GitHub Actions Windows build.

## Build

Requirements: Windows x64, the .NET 10 SDK, and the Microsoft Edge WebView2 Runtime for PI Call.

For a portable self-contained build:

    dotnet publish src/PIblox.App/PIblox.App.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/publish

To build PIblox_Setup.exe, install Inno Setup 6 and run:

    .\scripts\build-installer.ps1

GitHub Actions builds both the installer and portable ZIP on pushes to main, pull requests, or manual runs. A version tag such as v1.0.1 also publishes the generated files as a GitHub Release.

## PI Call setup

PI Call is an audio-only WebRTC client. The media stream is peer-to-peer; the signaling Worker relays only room and WebRTC negotiation messages. The Worker source uses one Durable Object per room, limits rooms to two connected peers, and does not save audio or chat messages.

To deploy the signaling service (an adult account owner should manage the Cloudflare account and review usage):

1. Install Node.js 22.12 or newer.
2. Open a terminal in `services/pi-call-signaling`.
3. Run `npm install`, then `npx wrangler login`, then `npm run deploy`.
4. Copy the deployed HTTPS Worker URL (for example, `https://your-worker.workers.dev`).
5. Open PI Call in PIblox and paste that URL into the signaling-server field. PIblox converts it to a secure WebSocket connection automatically.

The client uses a public STUN server for direct connection discovery. A TURN relay is not included, so some firewalls or network configurations may prevent a peer-to-peer call. A successful code build does not prove that two real devices can connect; test with two devices and review the Cloudflare usage limits before wider use.

## Important status

This is an actively developed preview. The PI Call client and signaling Worker source are included, but the Worker has not been deployed and real two-device audio calls have not yet been verified. The Windows installer, WebView2 runtime availability, and Roblox launch still need validation on a real Windows computer. Do not treat unverified items as production-ready.

## Security

PIblox only opens HTTPS links on roblox.com from its in-app game-link field. It does not ask for Roblox passwords or session cookies. Never add credentials, tokens, private data, or .env files to this public repository.
