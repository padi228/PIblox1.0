# Changelog

## 1.0.0-preview — expanded scaffold
- Redesigned the WPF interface with a dark purple-blue theme and navigation sections.
- Added Roblox Player and Studio detection and launch actions.
- Added official Roblox experience search by name, numeric ID, or approved Roblox HTTPS link.
- Added local favorites and recent link history.
- Added CPU and RAM monitoring and a network ping check.
- Added local settings, optional Windows startup registration, and diagnostics export.
- Added GitHub Releases version checking.
- Added Inno Setup installer definition and Windows GitHub Actions build pipeline.
- Windows compilation and installer execution remain to be verified by CI.

## 1.0.1-preview — PADI Call groundwork
- Added an embedded PADI Call window using WebView2.
- Added audio-only WebRTC client controls: room creation/join by code, microphone selection, mute, hangup, and compact mode.
- Added a Cloudflare Durable Objects signaling Worker with two-peer room limits and ephemeral signaling relay.
- Added a separate CI workflow for Worker bundle validation.
- Live calls still require deployment of the signaling Worker; TURN relay and two-device verification are not included yet.

## 1.0.0-scaffold
- Added initial WPF project structure and basic launch actions.
