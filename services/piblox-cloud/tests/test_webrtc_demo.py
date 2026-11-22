import unittest

from httpx import ASGITransport, AsyncClient

from webrtc_demo import app, peers


class WebRTCDemoTests(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self) -> None:
        self.transport = ASGITransport(
            app=app,
            client=("127.0.0.1", 43120),
        )
        self.client = AsyncClient(
            transport=self.transport,
            base_url="http://127.0.0.1",
        )

    async def asyncTearDown(self) -> None:
        await self.client.aclose()
        active = list(peers.values())
        peers.clear()
        for peer in active:
            await peer.close()

    async def test_health_reports_local_synthetic_transport(self) -> None:
        response = await self.client.get("/health")
        self.assertEqual(response.status_code, 200)
        data = response.json()
        self.assertTrue(data["ok"])
        self.assertEqual(data["transport"], "webrtc")
        self.assertFalse(data["remote_access"])
        self.assertFalse(data["capture"])

    async def test_browser_page_is_served(self) -> None:
        response = await self.client.get("/")
        self.assertEqual(response.status_code, 200)
        self.assertIn("RTCPeerConnection", response.text)
        self.assertIn("Запустить WebRTC", response.text)

    async def test_empty_sdp_is_rejected_before_peer_creation(self) -> None:
        response = await self.client.post(
            "/offer",
            json={"type": "offer", "sdp": ""},
        )
        self.assertEqual(response.status_code, 422)
        self.assertEqual(len(peers), 0)

    async def test_answer_type_is_validated(self) -> None:
        response = await self.client.post(
            "/offer",
            json={"type": "answer", "sdp": "v=0"},
        )
        self.assertEqual(response.status_code, 422)
        self.assertEqual(len(peers), 0)

    async def test_oversized_sdp_is_rejected_before_peer_creation(self) -> None:
        response = await self.client.post(
            "/offer",
            json={"type": "offer", "sdp": "x" * 200_001},
        )
        self.assertEqual(response.status_code, 422)
        self.assertEqual(len(peers), 0)

    async def test_non_loopback_client_is_rejected(self) -> None:
        remote_transport = ASGITransport(
            app=app,
            client=("192.0.2.10", 43120),
        )
        async with AsyncClient(
            transport=remote_transport,
            base_url="http://example.invalid",
        ) as remote_client:
            response = await remote_client.get("/health")
        self.assertEqual(response.status_code, 403)


if __name__ == "__main__":
    unittest.main()
