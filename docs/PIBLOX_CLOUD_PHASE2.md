# PIblox Cloud — Phase 2 design

## Objective
Move from the local MJPEG test arena toward a secure, authenticated remote-app streaming prototype.

## Transport decision
Use WebRTC for media and data channels for input/control. MJPEG remains a fallback for local diagnostics only. A signaling service exchanges session descriptions and ICE candidates; media should remain peer-to-peer where feasible, with a TURN relay configured for restrictive networks.

## Security baseline before remote sessions
- Authentication required before creating a session.
- One short-lived, unguessable session token per session; never place tokens in URLs.
- Session owner authorization for control and stop actions.
- TLS for signaling and control.
- Allowlisted applications only; no arbitrary shell commands.
- Separate worker process per session with CPU/memory/time limits.
- Idle shutdown, rate limits, and sanitized logs.
- TURN credentials are short-lived and server-generated.
- Do not store passwords or third-party game credentials.

## Incremental implementation
1. Keep the existing loopback demo as a regression baseline.
2. Add a WebRTC signaling prototype for a test video source (synthetic frames).
3. Add data-channel input to the same synthetic scene.
4. Measure end-to-end latency, packet loss, and reconnect behavior.
5. Add authentication and session isolation before any remote exposure.
6. Add a supervised, allowlisted app worker only after security review.

## Scope and caveats
This document describes a generic remote-application streaming prototype. It is not a promise that any third-party application or game will work in a remote environment. Review the target application's terms and technical requirements before testing it.
