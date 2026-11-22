from __future__ import annotations

import asyncio
import ipaddress
import io
import secrets
import time
from dataclasses import dataclass, field
from pathlib import Path

from fastapi import FastAPI, HTTPException, Request, WebSocket, WebSocketDisconnect
from fastapi.responses import FileResponse, StreamingResponse
from fastapi.staticfiles import StaticFiles
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent
STATIC_DIR = ROOT / "static"
FRAME_WIDTH = 960
FRAME_HEIGHT = 540
FRAME_RATE = 15
MAX_SESSIONS = 2
SESSION_IDLE_SECONDS = 15 * 60
ALLOWED_KEYS = {"ArrowLeft", "ArrowRight", "ArrowUp", "ArrowDown"}

app = FastAPI(
    title="PIblox Cloud Prototype",
    version="0.1.0",
    description="Local-only server-rendered streaming demo for an original test scene.",
)
app.mount("/static", StaticFiles(directory=STATIC_DIR), name="static")


@dataclass
class GameSession:
    session_id: str
    player_x: float = 0.5
    player_y: float = 0.72
    keys: set[str] = field(default_factory=set)
    last_seen: float = field(default_factory=time.monotonic)
    frame_number: int = 0


_sessions: dict[str, GameSession] = {}


def _require_loopback(host: str | None) -> None:
    """Keep this first prototype local. Remote access needs a real auth/TLS layer."""
    try:
        is_loopback = ipaddress.ip_address(host or "").is_loopback
    except ValueError:
        is_loopback = False
    if not is_loopback:
        raise HTTPException(
            status_code=403,
            detail="This prototype only accepts local connections. Remote access is not enabled.",
        )


def _active_sessions() -> None:
    now = time.monotonic()
    expired = [
        session_id
        for session_id, session in _sessions.items()
        if now - session.last_seen > SESSION_IDLE_SECONDS
    ]
    for session_id in expired:
        _sessions.pop(session_id, None)


def _get_session(session_id: str) -> GameSession:
    session = _sessions.get(session_id)
    if session is None:
        raise HTTPException(status_code=404, detail="Session not found or expired.")
    session.last_seen = time.monotonic()
    return session


def step_game(session: GameSession, delta_seconds: float) -> None:
    """Advance the original demo scene using the latest directional input."""
    dt = max(0.0, min(delta_seconds, 0.1))
    speed = 0.58
    dx = float("ArrowRight" in session.keys) - float("ArrowLeft" in session.keys)
    dy = float("ArrowDown" in session.keys) - float("ArrowUp" in session.keys)
    session.player_x = min(0.94, max(0.06, session.player_x + dx * speed * dt))
    session.player_y = min(0.91, max(0.12, session.player_y + dy * speed * dt))
    session.frame_number += 1


def render_frame(session: GameSession) -> bytes:
    """Render a self-contained test scene to JPEG; no desktop or third-party app is captured."""
    image = Image.new("RGB", (FRAME_WIDTH, FRAME_HEIGHT), (13, 16, 34))
    draw = ImageDraw.Draw(image)

    for x in range(0, FRAME_WIDTH, 48):
        draw.line((x, 0, x, FRAME_HEIGHT), fill=(28, 33, 62), width=1)
    for y in range(0, FRAME_HEIGHT, 48):
        draw.line((0, y, FRAME_WIDTH, y), fill=(28, 33, 62), width=1)

    draw.rounded_rectangle(
        (34, 32, FRAME_WIDTH - 34, FRAME_HEIGHT - 32),
        radius=22,
        outline=(91, 83, 190),
        width=3,
    )
    draw.text((58, 52), "PIBLOX CLOUD  /  TEST ARENA", fill=(220, 218, 255))
    draw.text((58, 78), "Server-rendered demo scene  |  Arrow keys via client", fill=(145, 155, 190))
    draw.rounded_rectangle((180, 180, 330, 215), radius=12, fill=(49, 59, 105))
    draw.rounded_rectangle((610, 295, 790, 330), radius=12, fill=(49, 59, 105))
    draw.ellipse((445, 145, 505, 205), fill=(93, 69, 165), outline=(171, 150, 255), width=3)

    px = int(session.player_x * FRAME_WIDTH)
    py = int(session.player_y * FRAME_HEIGHT)
    draw.ellipse((px - 25, py - 25, px + 25, py + 25), fill=(105, 190, 255), outline=(222, 244, 255), width=4)
    draw.ellipse((px - 8, py - 8, px + 8, py + 8), fill=(245, 251, 255))

    draw.rounded_rectangle(
        (FRAME_WIDTH - 228, 48, FRAME_WIDTH - 58, 95),
        radius=12,
        fill=(25, 31, 57),
        outline=(77, 87, 133),
    )
    draw.text((FRAME_WIDTH - 210, 63), f"FRAME  {session.frame_number:06d}", fill=(191, 198, 255))

    output = io.BytesIO()
    image.save(output, format="JPEG", quality=76, optimize=True)
    return output.getvalue()


@app.get("/", include_in_schema=False)
async def home() -> FileResponse:
    return FileResponse(STATIC_DIR / "index.html")


@app.get("/api/health")
async def health() -> dict[str, object]:
    _active_sessions()
    return {
        "ok": True,
        "service": "PIblox Cloud Prototype",
        "version": "0.1.0",
        "mode": "local-demo",
        "active_sessions": len(_sessions),
        "remote_access": False,
    }


@app.post("/api/sessions")
async def create_session(request: Request) -> dict[str, str]:
    _require_loopback(request.client.host if request.client else None)
    _active_sessions()
    if len(_sessions) >= MAX_SESSIONS:
        raise HTTPException(status_code=429, detail="Demo session limit reached.")
    session_id = secrets.token_urlsafe(24)
    _sessions[session_id] = GameSession(session_id=session_id)
    return {"session_id": session_id, "status": "created"}


@app.delete("/api/sessions/{session_id}")
async def close_session(session_id: str, request: Request) -> dict[str, str]:
    _require_loopback(request.client.host if request.client else None)
    if _sessions.pop(session_id, None) is None:
        raise HTTPException(status_code=404, detail="Session not found.")
    return {"session_id": session_id, "status": "closed"}


@app.get("/api/sessions/{session_id}/stream")
async def stream_session(session_id: str, request: Request) -> StreamingResponse:
    _require_loopback(request.client.host if request.client else None)
    session = _get_session(session_id)

    async def frames():
        boundary = b"pibloxframe"
        frame_interval = 1.0 / FRAME_RATE
        previous = time.monotonic()
        try:
            while session_id in _sessions:
                if await request.is_disconnected():
                    break
                now = time.monotonic()
                step_game(session, now - previous)
                previous = now
                jpeg = render_frame(session)
                yield (
                    b"--" + boundary + b"\r\n"
                    b"Content-Type: image/jpeg\r\n"
                    b"Content-Length: " + str(len(jpeg)).encode("ascii") + b"\r\n\r\n"
                    + jpeg + b"\r\n"
                )
                await asyncio.sleep(frame_interval)
        finally:
            session.keys.clear()

    return StreamingResponse(
        frames(),
        media_type="multipart/x-mixed-replace; boundary=pibloxframe",
        headers={"Cache-Control": "no-store, no-cache, must-revalidate", "Pragma": "no-cache"},
    )


@app.websocket("/ws/sessions/{session_id}")
async def session_input(websocket: WebSocket, session_id: str) -> None:
    host = websocket.client.host if websocket.client else None
    try:
        ip = ipaddress.ip_address(host or "")
    except ValueError:
        ip = None
    if ip is None or not ip.is_loopback:
        await websocket.close(code=1008, reason="Local connections only")
        return

    session = _sessions.get(session_id)
    if session is None:
        await websocket.close(code=1008, reason="Session not found or expired")
        return

    await websocket.accept()
    session.last_seen = time.monotonic()
    try:
        while True:
            message = await websocket.receive_json()
            message_type = message.get("type")
            if message_type == "keys":
                keys = message.get("keys", [])
                if not isinstance(keys, list):
                    keys = []
                session.keys = {key for key in keys if isinstance(key, str) and key in ALLOWED_KEYS}
                session.last_seen = time.monotonic()
                await websocket.send_json({"type": "input_ack"})
            elif message_type == "ping":
                await websocket.send_json({"type": "pong", "sent_at": message.get("sent_at")})
            elif message_type == "reset":
                session.keys.clear()
                await websocket.send_json({"type": "reset_ack"})
    except WebSocketDisconnect:
        session.keys.clear()
    except (TypeError, ValueError):
        session.keys.clear()
        await websocket.close(code=1003, reason="Invalid message")


if __name__ == "__main__":
    import uvicorn

    uvicorn.run("server:app", host="127.0.0.1", port=8787, reload=False)
