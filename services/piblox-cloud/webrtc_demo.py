"""Loopback-only WebRTC synthetic video demo. Does not capture desktop or launch third-party apps."""
from __future__ import annotations
import asyncio, ipaddress, time
from fractions import Fraction
from fastapi import FastAPI, HTTPException, Request
from fastapi.responses import JSONResponse
from av import VideoFrame
from aiortc import RTCPeerConnection, RTCSessionDescription, VideoStreamTrack
from aiortc.sdp import candidate_from_sdp
from aiortc import RTCIceCandidate
import numpy as np

app = FastAPI(title="PIblox Cloud WebRTC Synthetic Demo")
pcs: set[RTCPeerConnection] = set()

def loopback(request: Request):
    host = request.client.host if request.client else ""
    try: valid = ipaddress.ip_address(host).is_loopback
    except ValueError: valid = False
    if not valid: raise HTTPException(403, "Local connections only")

class SyntheticTrack(VideoStreamTrack):
    kind = "video"
    def __init__(self):
        super().__init__(); self.start = time.monotonic()
    async def recv(self):
        pts, tb = await self.next_timestamp()
        t = time.monotonic() - self.start
        img = np.zeros((360, 640, 3), dtype=np.uint8)
        img[:, :, 0] = 18; img[:, :, 1] = 20; img[:, :, 2] = 42
        x = int(40 + (560 * ((t % 5) / 5)))
        img[145:215, x:x+70, :] = [160, 90, 255]
        frame = VideoFrame.from_ndarray(img, format="bgr24")
        frame.pts = pts; frame.time_base = tb
        return frame

@app.get("/health")
async def health(request: Request):
    loopback(request)
    return {"ok": True, "demo": "synthetic-video", "peer_count": len(pcs)}

@app.post("/offer")
async def offer(request: Request):
    loopback(request)
    payload = await request.json()
    pc = RTCPeerConnection(); pcs.add(pc)
    pc.addTrack(SyntheticTrack())
    @pc.on("connectionstatechange")
    async def state():
        if pc.connectionState in ("failed", "closed", "disconnected"):
            await pc.close(); pcs.discard(pc)
    await pc.setRemoteDescription(RTCSessionDescription(sdp=payload["sdp"], type=payload["type"]))
    answer = await pc.createAnswer(); await pc.setLocalDescription(answer)
    return JSONResponse({"sdp": pc.localDescription.sdp, "type": pc.localDescription.type})

@app.on_event("shutdown")
async def shutdown():
    await asyncio.gather(*(pc.close() for pc in list(pcs)), return_exceptions=True)
    pcs.clear()

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=8790)
