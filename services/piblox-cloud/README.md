# PIblox Cloud Prototype

A staged prototype for testing local server-to-browser streaming with an original synthetic scene.

## Included prototypes

### MJPEG input demo — port 8787

- FastAPI service with a health endpoint and short-lived in-memory sessions.
- Server-rendered frames generated with Pillow.
- MJPEG video stream delivered to the browser.
- Arrow-key/touch input sent back over WebSocket.
- Round-trip input-channel latency display.

Run with python server.py and open http://127.0.0.1:8787.

### WebRTC synthetic video demo — port 8790

- Separate FastAPI service in webrtc_demo.py.
- Animated synthetic video track sent through WebRTC.
- Browser test page at http://127.0.0.1:8790/.
- Live connection state, candidate-pair RTT, time to first decoded frame, decoded-frame count, and FPS metrics.
- SDP size/type validation, loopback-only request checks, two-peer limit, and explicit peer cleanup.

Run with python webrtc_demo.py and open http://127.0.0.1:8790. See WEBRTC_DEMO.md for details.

The two prototypes run independently and use different ports. The original MJPEG page remains unchanged.

## What this is not

- It does not capture the Windows desktop or launch a native application.
- It does not launch or capture Roblox or another third-party game.
- It is not a public cloud host or a production service.
- It does not include user accounts, remote session authorization, TURN, hardware encoding, or multi-user isolation.
- It does not establish compatibility with unsupported operating systems or third-party games.

Do not expose either prototype to the public internet. Both are intended for local testing only. A remote service needs a separate reviewed security design before any public deployment.

## Run the MJPEG demo

Python 3.10 or newer:

Windows:
    py -3 -m venv .venv
    .venv\Scripts\activate
    python -m pip install -r requirements.txt
    python server.py

Linux/macOS:
    python3 -m venv .venv
    source .venv/bin/activate
    python -m pip install -r requirements.txt
    python server.py

Open http://127.0.0.1:8787.

## Run tests

From this directory:

    python -m unittest discover -s tests -v

The WebRTC CI workflow runs its API regression tests on GitHub Actions. Automated API checks do not replace a real browser test of media playback.

## Engineering milestones

1. Validate local WebRTC media delivery and collect browser-side frame/connection metrics.
2. Add authenticated session provisioning only after a dedicated security design.
3. Add supervised, isolated workers for applications explicitly permitted in the remote environment.
4. Add resource quotas, idle shutdown, and auditable operational controls.
5. Integrate a PIblox desktop client after protocol behavior and security boundaries are stable.
