# PIblox Cloud WebRTC synthetic demo

This isolated prototype sends a synthetic animated video track over WebRTC. It does not capture a desktop or launch/capture third-party applications.

## Install
Use Python 3.10+ in a fresh virtual environment:
```
python -m pip install -r requirements.txt
python webrtc_demo.py
```
The service listens only on 127.0.0.1:8790. The health endpoint is /health.

## Browser test
A browser-side WebRTC offer/answer test page is not yet included. The endpoint accepts JSON containing an SDP offer (`type`, `sdp`) and returns an SDP answer. Keep it local.

## Safety and limitations
- Loopback-only access; no public hosting.
- Synthetic video track only; no app/desktop capture, remote input, or game integration.
- No authentication/TLS because this is intentionally local-only.
- Experimental dependency stack; run tests and validate package compatibility before use.
