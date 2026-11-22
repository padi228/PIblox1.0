"""Loopback-only WebRTC synthetic video demo.

This service streams an original generated test scene. It does not capture a
desktop, launch third-party applications, or accept remote input.
"""
from __future__ import annotations

import asyncio
import ipaddress
import secrets
from pathlib import Path
from typing import Literal

from aiortc import RTCPeerConnection, RTCSessionDescription, VideoStreamTrack
from av import VideoFrame
from fastapi import FastAPI, HTTPException, Request
from fastapi.responses import FileResponse, JSONResponse
from pydantic import BaseModel, Field
import numpy as np

STATIC_DIR = Path(__file__).resolve().parent / "static"
MAX_PEERS = 2
MAX_SDP_LENGTH = 200_000

app = FastAPI(
    title="PIblox Cloud WebRTC Synthetic Demo",
    version="0.2.0",
    description="Local-only WebRTC transport test using generated video frames.",
)
peers: dict[str, RTCPeerConnection] = {}


class OfferPayload(BaseModel):
    type: Literal["offer"]
    sdp: str = Field(min_length=1, max_length=MAX_SDP_LENGTH)


def require_loopback(request: Request) -> None:
    host = request.client.host if request.client else ""
    try:
        is_loopback = ipaddress.ip_address(host).is_loopback
    except ValueError:
        is_loopback = False
    if not is_loopback:
        raise HTTPException(
            status_code=403,
            detail="This prototype only accepts local connections.",
        )


class SyntheticTrack(VideoStreamTrack):
    """Animated test video; no screen or application capture."""

    kind = "video"

    def __init__(self) -> None:
        super().__init__()
        self.elapsed = 0.0

    async def recv(self) -> VideoFrame:
        pts, time_base = await self.next_timestamp()
        self.elapsed += 1 / 30
        image = np.zeros((360, 640, 3), dtype=np.uint8)
        image[:, :, 0] = 18
        image[:, :, 1] = 20
        image[:, :, 2] = 42

        x = int(40 + 560 * ((self.elapsed % 5) / 5))
        image[145:215, x:min(x + 70, 640), :] = [160, 90, 255]

        frame = VideoFrame.from_ndarray(image, format="bgr24")
        frame.pts = pts
        frame.time_base = time_base
        return frame


@app.get("/", include_in_schema=False)
async def home() -> FileResponse:
    return FileResponse(STATIC_DIR / "webrtc.html")


@app.get("/health")
async def health(request: Request) -> dict[str, object]:
    require_loopback(request)
    return {
        "ok": True,
        "demo": "synthetic-video",
        "transport": "webrtc",
        "peer_count": len(peers),
        "peer_limit": MAX_PEERS,
        "remote_access": False,
        "capture": False,
    }


@app.post("/offer")
async def offer(payload: OfferPayload, request: Request) -> JSONResponse:
    require_loopback(request)
    if len(peers) >= MAX_PEERS:
        raise HTTPException(status_code=429, detail="Local demo peer limit reached.")

    peer_id = secrets.token_urlsafe(18)
    pc = RTCPeerConnection()
    peers[peer_id] = pc
    pc.addTrack(SyntheticTrack())

    @pc.on("connectionstatechange")
    async def on_connection_state_change() -> None:
        if pc.connectionState == "closed":
            peers.pop(peer_id, None)
        elif pc.connectionState == "failed":
            peers.pop(peer_id, None)
            await pc.close()

    try:
        await pc.setRemoteDescription(
            RTCSessionDescription(sdp=payload.sdp, type=payload.type)
        )
        answer = await pc.createAnswer()
        await pc.setLocalDescription(answer)
        if pc.localDescription is None:
            raise RuntimeError("Local SDP answer was not created.")
        return JSONResponse(
            {
                "peer_id": peer_id,
                "sdp": pc.localDescription.sdp,
                "type": pc.localDescription.type,
            },
            headers={"Cache-Control": "no-store"},
        )
    except Exception as exc:
        peers.pop(peer_id, None)
        await pc.close()
        if isinstance(exc, HTTPException):
            raise
        raise HTTPException(
            status_code=400,
            detail="WebRTC negotiation failed. Check the local browser and server versions.",
        ) from exc


@app.delete("/peer/{peer_id}")
async def close_peer(peer_id: str, request: Request) -> dict[str, str]:
    require_loopback(request)
    pc = peers.pop(peer_id, None)
    if pc is None:
        raise HTTPException(status_code=404, detail="Peer session not found.")
    await pc.close()
    return {"peer_id": peer_id, "status": "closed"}


@app.on_event("shutdown")
async def shutdown() -> None:
    active = list(peers.values())
    peers.clear()
    await asyncio.gather(*(pc.close() for pc in active), return_exceptions=True)


if __name__ == "__main__":
    import uvicorn

    uvicorn.run(app, host="127.0.0.1", port=8790)
