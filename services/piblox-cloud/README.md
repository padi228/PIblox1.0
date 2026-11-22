# PIblox Cloud Prototype

A first end-to-end prototype for PIblox Cloud using an original test scene.

## What works in this prototype

- FastAPI service with a health endpoint and short-lived in-memory sessions.
- Server-rendered frames generated with Pillow.
- MJPEG video stream delivered to the browser.
- Keyboard and touch input sent back to the server over WebSocket.
- Round-trip input-channel latency display.
- Local-only binding and loopback checks.

The blue marker moves around an original test arena. This confirms the basic loop:

**server simulation → rendered frames → browser video → input messages → server simulation**

## What this is not

- It does not capture the Windows desktop or launch a native application.
- It does not launch Roblox or any other third-party game.
- It is not a public cloud host or a production service.
- It does not include user accounts, payment, persistent sessions, TURN, WebRTC, hardware encoding, or multi-user isolation.
- MJPEG is a proof-of-concept transport and is not efficient enough for a real cloud-gaming product.

Do not expose this prototype to the public internet. It intentionally binds to 127.0.0.1 and rejects non-loopback clients. A remote deployment needs a separate security design (authentication, TLS, session authorization, abuse protection, isolation, and cost controls) before it is safe to enable.

## Run on Windows

Requires Python 3.10 or newer.

    py -3 -m venv .venv
    .venv\Scripts\activate
    python -m pip install -r requirements.txt
    python server.py

Open http://127.0.0.1:8787.

Use the arrow keys or WASD. On a touch device, use the arrow buttons. Click Stop session to close the demo session.

## Run on Linux or macOS

    python3 -m venv .venv
    source .venv/bin/activate
    python -m pip install -r requirements.txt
    python server.py

Then open http://127.0.0.1:8787.

## Tests

    python -m unittest discover -s tests -v

## Next engineering milestones

1. Replace MJPEG with a WebRTC video pipeline and measure real end-to-end frame latency.
2. Add authenticated session creation and short-lived client credentials.
3. Add a supervised worker process for an application explicitly permitted to run in the remote environment.
4. Add per-session resource limits, process isolation, idle shutdown, and audit-safe logs.
5. Add a PIblox desktop client only after the protocol and security model are stable.

A successful local test validates only this demo pipeline; it does not prove internet-scale hosting or compatibility with any third-party game.
