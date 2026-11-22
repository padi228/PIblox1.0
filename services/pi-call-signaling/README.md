# PI Call signaling Worker

This service coordinates two-person WebRTC audio rooms. Audio is not routed through the Worker.

## Requirements

- A Cloudflare account managed by an adult account owner.
- Node.js 22.12 or newer.
- npm.

## Local validation

```powershell
npm install
npm run check
```

## Local development

```powershell
npm run dev
```

PIblox loads its Call client from the virtual HTTPS origin `https://piblox.local`. When testing the Worker locally, use that same origin so the Worker origin check accepts the WebSocket handshake.

## Deploy

```powershell
npx wrangler login
npm run deploy
```

Wrangler creates the Worker and provisions the configured SQLite-backed Durable Object class on the first deployment. Copy the resulting `https://...workers.dev` URL into the signaling-server field in PIblox.

## Protocol and limits

- WebSocket endpoint: `/signal/{ROOM_CODE}`.
- Health endpoint: `/health`.
- Room codes are eight random characters.
- Maximum two active WebSocket peers per room.
- Signaling messages accepted: `offer`, `answer`, `candidate`, and `hangup`.
- No user accounts, audio storage, recording, or chat history.
- WebSocket hibernation is used so idle rooms can hibernate.
- Origin is restricted to `https://piblox.local`.

This is a preview service, not a finished public calling platform. There is no TURN relay, authentication, global rate limit, or moderation service yet. Do not publish room codes. Review current Cloudflare pricing and limits before deploying or sharing it broadly.