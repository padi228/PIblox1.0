# PIblox Cloud

## Current stage: local streaming prototype

The first prototype lives in services/piblox-cloud. It renders an original test arena on the server, streams JPEG frames to a browser using MJPEG, and sends directional input back using WebSocket messages.

This proves the initial client/server interaction loop without depending on a third-party application.

## Security boundary

The prototype is local-only:
- Uvicorn binds to 127.0.0.1.
- HTTP streaming and session endpoints reject non-loopback clients.
- It does not capture the desktop, launch third-party games, or accept executable paths from clients.
- Sessions are short-lived and held only in memory.

Do not deploy it as a public endpoint. Remote hosting needs authenticated session provisioning, TLS, authorization, rate limits, process isolation, resource quotas, and a reviewed threat model.

## Planned stages

1. **WebRTC transport:** replace MJPEG after the local loop is verified.
2. **Secure session service:** short-lived tokens, authenticated user ownership, and session expiry.
3. **Worker isolation:** supervised, disposable workers for applications permitted in the remote environment.
4. **Desktop client:** integrate the protocol into PIblox after server behavior is stable.
5. **Performance:** measure input latency, frame latency, bitrate, packet loss, and CPU/GPU utilization.

No release or third-party game compatibility is claimed at this stage.
