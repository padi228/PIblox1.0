import { DurableObject } from "cloudflare:workers";

interface Env {
  CALL_ROOMS: DurableObjectNamespace<CallRoom>;
}

const ROOM_CODE_PATTERN = /^[A-Z2-9]{8}$/;
const ALLOWED_ORIGIN = "https://piblox.local";
const MAX_SIGNAL_BYTES = 48 * 1024;
const ALLOWED_SIGNAL_TYPES = new Set(["offer", "answer", "candidate", "hangup"]);

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);

    if (url.pathname === "/health" && request.method === "GET") {
      return Response.json({ ok: true, service: "padi-call-signaling" });
    }

    if (request.method !== "GET" || request.headers.get("Upgrade")?.toLowerCase() !== "websocket") {
      return new Response("WebSocket upgrade required", { status: 426 });
    }

    const origin = request.headers.get("Origin");
    if (origin !== ALLOWED_ORIGIN) {
      return new Response("Origin not allowed", { status: 403 });
    }

    const match = url.pathname.match(/^\/signal\/([A-Z2-9]{8})$/);
    const roomCode = match?.[1];
    if (!roomCode || !ROOM_CODE_PATTERN.test(roomCode)) {
      return new Response("Invalid room code", { status: 400 });
    }

    const room = env.CALL_ROOMS.getByName(roomCode);
    return room.fetch(request);
  },
};

export class CallRoom extends DurableObject<Env> {
  async fetch(request: Request): Promise<Response> {
    const existingPeers = this.ctx.getWebSockets("call");
    if (existingPeers.length >= 2) {
      return new Response("Room full", { status: 409 });
    }

    const pair = new WebSocketPair();
    const [client, server] = Object.values(pair);
    this.ctx.acceptWebSocket(server, ["call"]);
    server.serializeAttachment({ joinedAt: Date.now() });

    const peers = this.ctx.getWebSockets("call");
    server.send(JSON.stringify({ type: "room-state", peers: peers.length }));

    if (peers.length === 2) {
      const firstPeer = peers.find(peer => peer !== server);
      firstPeer?.send(JSON.stringify({ type: "peer-joined" }));
    }

    return new Response(null, { status: 101, webSocket: client });
  }

  async webSocketMessage(ws: WebSocket, message: string | ArrayBuffer): Promise<void> {
    if (typeof message !== "string" || new TextEncoder().encode(message).byteLength > MAX_SIGNAL_BYTES) {
      ws.close(1009, "Signal message too large or binary");
      return;
    }

    let payload: { type?: unknown; data?: unknown };
    try {
      payload = JSON.parse(message) as { type?: unknown; data?: unknown };
    } catch {
      ws.close(1007, "Invalid JSON");
      return;
    }

    if (typeof payload.type !== "string" || !ALLOWED_SIGNAL_TYPES.has(payload.type)) {
      ws.close(1008, "Unsupported signal type");
      return;
    }

    if (payload.type !== "hangup" && (!payload.data || typeof payload.data !== "object")) {
      ws.close(1008, "Invalid signal payload");
      return;
    }

    const relayMessage = JSON.stringify({ type: payload.type, data: payload.data ?? null });
    for (const otherPeer of this.ctx.getWebSockets("call")) {
      if (otherPeer !== ws) {
        try {
          otherPeer.send(relayMessage);
        } catch {
          // A peer may close between enumeration and send.
        }
      }
    }
  }

  async webSocketClose(
    ws: WebSocket,
    code: number,
    reason: string,
    wasClean: boolean,
  ): Promise<void> {
    for (const otherPeer of this.ctx.getWebSockets("call")) {
      if (otherPeer !== ws) {
        try {
          otherPeer.send(JSON.stringify({ type: "peer-left" }));
        } catch {
          // Ignore a peer that is already closing.
        }
      }
    }
  }

  async webSocketError(ws: WebSocket, error: unknown): Promise<void> {
    console.warn("PADI Call WebSocket error", error);
  }
}