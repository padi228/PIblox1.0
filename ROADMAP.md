# PIblox roadmap

## Implemented in the current preview
- [x] WPF desktop shell with dark purple-blue styling
- [x] Local Roblox Player and Studio detection
- [x] Player and Studio launch actions
- [x] Official Roblox experience search by title, ID, or Roblox link
- [x] Local favorites and recent links
- [x] CPU and RAM metrics, plus a manual ping check
- [x] Local preferences and optional per-user startup entry
- [x] Diagnostics report copy/export
- [x] GitHub Releases check
- [x] Inno Setup script and Windows CI workflow

## Next development stages
- [ ] Run and fix the Windows CI build; verify the installer on Windows 10/11
- [ ] Add a richer Discover experience using supported Roblox endpoints or official search pages
- [ ] Add configurable overlay after validating CPU and memory overhead
- [ ] Add Roblox Studio project shortcuts, safe backups, and diagnostics
- [ ] Add an update download flow with explicit user confirmation and signature/hash validation
- [ ] Add import/export of settings
- [x] Add embedded PI Call client UI, microphone selection, room codes, mute, compact mode, and WebRTC audio negotiation
- [ ] Deploy and verify the Cloudflare PI Call signaling Worker with two real devices
- [ ] Add a managed TURN relay option for networks where direct WebRTC connectivity fails

## Requires separate infrastructure or external integrations
- [x] PI Call client and two-peer signaling Worker source; deployment, real-device test, TURN fallback, and stronger abuse controls still required
- [ ] Cross-device friends and invites: authenticated backend
- [ ] Online presence and chat: authenticated backend
- [ ] Multi-region VPN: a legitimate third-party VPN client/provider; PIblox will not operate its own network
- [ ] PADI 3D tools: a separate generation service or local model

Items under "Next development stages" and "Requires separate infrastructure" are not yet implemented.
