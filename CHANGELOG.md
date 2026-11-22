# Changelog

## PIblox Network foundation (feature branch)
- Added a separate PIblox Network window to the existing WPF app.
- Added local detection of the official WireGuard for Windows executable and WireGuard network adapters.
- Added local diagnostics and shortcuts to the installed client / official installation page.
- This is a foundation preview only: it does not create a VPN tunnel, install drivers, or modify Windows routes and DNS.

## 1.0.5-preview / Build 85 — visual refresh
- Updated the PIblox home screen branding and simplified labels for search, account sign-in and Player/Studio cards.
- Polished shared button/control styling.
- Clarified that Roblox sign-in opens the official website only; account OAuth connection is not yet configured.
- PI Call remains experimental and requires a deployed signaling Worker URL; no TURN relay is included.

## 1.0.4-preview — Roblox connection shortcut and desktop icon
- Added a home-screen button that opens the official Roblox login page in the browser. PIblox does not collect or store account credentials.
- Desktop icon is enabled by default during installation (can be unchecked).
- Bumped application and installer version to 1.0.4.

## 1.0.3-preview — Roblox account shortcut
- Added a home-screen Roblox login shortcut opening the official login page in the browser. PIblox does not collect or store passwords.
- Desktop shortcut creation is enabled by default in the installer.

## 1.0.3-preview — Roblox account shortcut\n- Added a dedicated home-screen Roblox sign-in shortcut that opens the official login page in the browser. PIblox does not collect or store passwords.\n- Desktop shortcut task is selected by default during installation.\n\n## 1.0.2-preview — PI Call branding
- Renamed the embedded calling module from PADI Call to PI Call throughout the app and documentation.
- Renamed the signaling Worker package and source directory to `pi-call-signaling`.
- Migrated saved signaling-server and display-name settings to the new `piCall` keys while preserving existing values.



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

## 1.0.1-preview — PI Call groundwork
- Added an embedded PI Call window using WebView2.
- Added audio-only WebRTC client controls: room creation/join by code, microphone selection, mute, hangup, and compact mode.
- Added a Cloudflare Durable Objects signaling Worker with two-peer room limits and ephemeral signaling relay.
- Added a separate CI workflow for Worker bundle validation.
- Live calls still require deployment of the signaling Worker; TURN relay and two-device verification are not included yet.

## 1.0.0-scaffold
- Added initial WPF project structure and basic launch actions.
