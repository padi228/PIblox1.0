import unittest

from fastapi.testclient import TestClient

from server import GameSession, app, render_frame, step_game


class GameStateTests(unittest.TestCase):
    def test_player_moves_right(self):
        session = GameSession(session_id="test")
        start_x = session.player_x
        session.keys.add("ArrowRight")
        step_game(session, 0.1)
        self.assertGreater(session.player_x, start_x)

    def test_player_stays_inside_playfield(self):
        session = GameSession(session_id="test", player_x=0.93)
        session.keys.add("ArrowRight")
        step_game(session, 1.0)
        self.assertLessEqual(session.player_x, 0.94)
        self.assertGreaterEqual(session.player_x, 0.06)

    def test_unknown_key_does_not_move_player(self):
        session = GameSession(session_id="test")
        start_x = session.player_x
        session.keys.add("Space")
        step_game(session, 0.1)
        self.assertEqual(session.player_x, start_x)

    def test_frame_is_jpeg(self):
        frame = render_frame(GameSession(session_id="test"))
        self.assertEqual(frame[:2], bytes([0xFF, 0xD8]))
        self.assertEqual(frame[-2:], bytes([0xFF, 0xD9]))

    def test_local_session_and_websocket_lifecycle(self):
        with TestClient(app, client=("127.0.0.1", 50000)) as client:
            health = client.get("/api/health")
            self.assertEqual(health.status_code, 200)
            self.assertTrue(health.json()["ok"])

            created = client.post("/api/sessions")
            self.assertEqual(created.status_code, 200)
            session_id = created.json()["session_id"]

            with client.websocket_connect(f"/ws/sessions/{session_id}") as socket:
                socket.send_json({"type": "keys", "keys": ["ArrowRight", "Space"]})
                self.assertEqual(socket.receive_json(), {"type": "input_ack"})
                socket.send_json({"type": "ping", "sent_at": 123.0})
                self.assertEqual(
                    socket.receive_json(),
                    {"type": "pong", "sent_at": 123.0},
                )

            closed = client.delete(f"/api/sessions/{session_id}")
            self.assertEqual(closed.status_code, 200)
            self.assertEqual(closed.json()["status"], "closed")

    def test_remote_session_creation_is_rejected(self):
        with TestClient(app, client=("192.0.2.10", 50000)) as client:
            response = client.post("/api/sessions")
            self.assertEqual(response.status_code, 403)


if __name__ == "__main__":
    unittest.main()
