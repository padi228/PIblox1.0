# PIblox Roblox Studio MCP connection

## What this adds

PIblox includes a local read-only MCP client for the official Roblox Studio MCP server. It can:

- Start the official local connector from %LOCALAPPDATA%\Roblox\mcp.bat.
- Complete the MCP stdio initialization handshake.
- List the MCP tools exposed by the installed Studio version.
- Call list_roblox_studios to list open Studio instances.
- Call get_studio_state for a selected studio_id.

The current PIblox UI does not call tools that write scripts, execute arbitrary Luau, upload assets, change game settings, or publish a place. The UI shows a consent prompt before starting the local connector.

## Enable in Roblox Studio

Use a current Roblox Studio installation:

1. Open Roblox Studio.
2. Open Assistant.
3. Select the three-dot menu and choose Manage MCP Servers.
4. Enable Enable Studio as MCP server.
5. Return to PIblox and open Roblox Studio Link.
6. Select Подключить Roblox Studio.

Roblox Studio creates the local connector batch file at %LOCALAPPDATA%\Roblox\mcp.bat. PIblox launches that official file through cmd.exe and communicates over standard input/output. No Roblox password, browser cookie, OAuth token, or API key is used by this connector.

## Current PIblox scope

The current UI is deliberately read-only:

- List currently connected Studio windows.
- Read the state for a Studio instance after its ID is entered.

The official Studio MCP server itself exposes more capabilities, including script editing, Luau execution, playtesting, screen capture, asset operations, and place saving. PIblox does not invoke those capabilities in this preview. A later expansion must add separate confirmation and permission checks for any action that changes a project.

## Cloud streaming relationship

This connector establishes a local control/inspection channel to Roblox Studio. It does not yet stream the Studio viewport through PIblox Cloud. The WebRTC demo remains a generated synthetic test scene on loopback. Desktop capture, remote keyboard/mouse relay, public hosting, and remote session authorization are not implemented.

## Windows 7 note

Roblox's current PC requirements list 64-bit Windows 10 or Windows 11. PIblox does not attempt to bypass Roblox's operating-system requirements. The PIblox Cloud test page is a browser-based synthetic stream test; Windows 7 browser compatibility has not been validated.

## Troubleshooting

- If PIblox says mcp.bat is missing, open the latest Roblox Studio, enable its MCP server, and retry.
- If no Studio windows are listed, open a place in Studio and refresh the list.
- If a Studio ID no longer works, refresh the list and use the ID from the current result.
- If the MCP server stops responding, disconnect in PIblox, restart Roblox Studio, enable the MCP server again, and reconnect.
