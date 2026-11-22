# PIblox architecture

## Current desktop architecture

- WPF application: navigation, launcher controls, settings, diagnostics, and status display.
- Roblox locator: checks the current user's Roblox Versions directory; it does not scan the whole disk.
- Launcher service: uses the registered Roblox URI handlers first and the detected local executable as a fallback.
- Roblox links service: accepts HTTPS addresses on roblox.com, numeric experience IDs, or search text. Search is opened on the official Roblox website.
- Settings service: local JSON under the current Windows user's LocalAppData folder. Import is size-limited and filters saved links to the official Roblox domain.
- System metrics: Windows CPU time counters and global memory status. Network latency is a manual ping to www.roblox.com.
- Update service: reads the latest public GitHub Release. It does not silently download or install updates.

## Build and distribution

GitHub Actions publishes a self-contained win-x64 build, packages a portable ZIP, and compiles an Inno Setup installer. Tagged releases can receive both files automatically. The CI result must be checked before calling a build verified.

## Features that need a backend

PI Call needs an authenticated signaling service and possibly TURN for restrictive networks. Friends, presence, shared invites, and shared project metadata also need a backend with access controls, rate limits, abuse reporting, and data retention rules. No such backend is deployed by this repository.

The VPN concept is an integration with a legitimate installed VPN client/provider, not a PIblox VPN network. Any overlay should remain opt-in and be measured for CPU/GPU overhead before release.
