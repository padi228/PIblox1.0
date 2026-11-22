# PIblox Network — engine roadmap

## Preview engine (current milestone)
- A UI-facing state machine is provided by `NetworkEnginePreviewService`.
- It demonstrates disconnected/prepare/preview-connected/disconnecting states.
- **It is a simulation only**: it does not create a tunnel, route traffic, change DNS/routes, install drivers, or contact servers.

## Planned engineering milestones
1. Integrate preview states into the Network window and add clear simulation labels.
2. Add local diagnostics, report export, and error handling.
3. Define a provider abstraction and configuration validation for networks the user owns or is explicitly authorized to operate.
4. Add test coverage and Windows CI checks before any production networking components are considered.

## Safety and transparency
A simulated connected state is not an active VPN connection. Real networking requires a properly authorized network operator, infrastructure, and carefully reviewed configuration. PIblox must never claim that traffic is routed when it is not.
