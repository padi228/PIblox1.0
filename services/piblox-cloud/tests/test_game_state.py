import unittest

from server import GameSession, render_frame, step_game


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


if __name__ == "__main__":
    unittest.main()
