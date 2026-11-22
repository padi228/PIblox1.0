# PIblox Network — foundation plan

Status: foundation preview. This document describes the first safe milestone; it is not a claim that a VPN tunnel is already available.

## Scope of the first milestone

- Add a separate PIblox Network window to the existing WPF app.
- Detect the official WireGuard for Windows installation.
- Inspect local network-adapter names and operational states for WireGuard adapters.
- Show a local diagnostic report.
- Open the official WireGuard installation page or the installed WireGuard client.
- Do not change Windows routes, DNS, proxy settings, firewall rules, or network adapters.
- Do not collect, transmit, or log VPN private keys, profile contents, IP addresses, or browsing activity.

## What is still required for a real tunnel

A functioning VPN needs a reachable server that is explicitly authorized for this use and a matching client profile. Cloudflare Workers are not a generic VPN exit-node fleet. Cloudflare can provide application/API infrastructure, while the VPN data plane must be supplied by an appropriate network service or a server the project owner controls.

## Planned next milestones

1. Confirm the diagnostics window builds and runs on a real Windows 10/11 device.
2. Add secure local profile handling, keeping private keys out of logs, Git, exports, and crash reports.
3. Add user-confirmed connection and disconnection through the official WireGuard for Windows service interface, with a visible Windows administrator-consent prompt.
4. Test DNS, route handling, disconnect recovery, and failure behavior before calling the product a VPN.
5. Only then evaluate a server endpoint and operating costs. Never use an unknown public config or an unapproved relay.

## Security rules

- Never request a Roblox, Microsoft, or other account password or browser cookie.
- Never commit VPN configuration files, private keys, preshared keys, access tokens, or server secrets.
- Do not silently install drivers or change network routing.
- Show the exact operation before any future elevated action.
- Do not claim "connected" unless the tunnel service and its network interface are actually verified.
