"""Headless Chromium end-to-end test for the local WebRTC demo.

Starts the service on loopback, negotiates a real browser peer connection,
waits for decoded video frames, and confirms that Stop releases the peer.
"""
from __future__ import annotations

import json
import socket
import subprocess
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path

from playwright.sync_api import sync_playwright

SERVICE_DIR = Path(__file__).resolve().parents[1]
HOST = "127.0.0.1"


def free_port() -> int:
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
        sock.bind((HOST, 0))
        return int(sock.getsockname()[1])


def get_health(base_url: str) -> dict:
    request = urllib.request.Request(
        f"{base_url}/health",
        headers={"Cache-Control": "no-cache"},
    )
    with urllib.request.urlopen(request, timeout=2) as response:
        return json.loads(response.read().decode("utf-8"))


def wait_for_server(base_url: str, process: subprocess.Popen) -> None:
    deadline = time.monotonic() + 20
    last_error: Exception | None = None
    while time.monotonic() < deadline:
        if process.poll() is not None:
            raise RuntimeError("WebRTC server exited before becoming ready.")
        try:
            health = get_health(base_url)
            if health.get("ok") and health.get("transport") == "webrtc":
                return
        except (OSError, urllib.error.URLError, TimeoutError) as exc:
            last_error = exc
        time.sleep(0.25)
    raise TimeoutError(f"WebRTC server did not become ready: {last_error}")


def wait_for_peer_count(base_url: str, expected: int, timeout: float = 8) -> None:
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        try:
            if get_health(base_url).get("peer_count") == expected:
                return
        except (OSError, urllib.error.URLError, TimeoutError):
            pass
        time.sleep(0.2)
    raise AssertionError(f"Expected {expected} active peers before timeout.")


def main() -> int:
    port = free_port()
    base_url = f"http://{HOST}:{port}"
    command = [
        sys.executable,
        "-m",
        "uvicorn",
        "webrtc_demo:app",
        "--host",
        HOST,
        "--port",
        str(port),
        "--log-level",
        "warning",
    ]
    server = subprocess.Popen(
        command,
        cwd=SERVICE_DIR,
        stdout=subprocess.DEVNULL,
        stderr=subprocess.STDOUT,
    )

    browser = None
    page = None
    browser_errors: list[str] = []
    try:
        wait_for_server(base_url, server)

        with sync_playwright() as playwright:
            browser = playwright.chromium.launch(
                headless=True,
                args=[
                    "--no-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-features=WebRtcHideLocalIpsWithMdns",
                ],
            )
            page = browser.new_page()
            page.on("pageerror", lambda error: browser_errors.append(str(error)))
            page.goto(base_url, wait_until="domcontentloaded", timeout=15000)

            if page.locator("#health").inner_text() != "Доступен":
                page.wait_for_function(
                    "() => document.querySelector('#health').textContent === 'Доступен'",
                    timeout=5000,
                )

            page.locator("#start").click()
            page.wait_for_function(
                """() => {
                    const video = document.querySelector('#video');
                    const connection = document.querySelector('#connection');
                    const decoded = Number(document.querySelector('#frames').textContent);
                    return connection.textContent === 'connected'
                        && video.readyState >= 2
                        && video.videoWidth > 0
                        && video.videoHeight > 0
                        && decoded > 0;
                }""",
                timeout=30000,
                polling=250,
            )

            metrics = page.evaluate(
                """() => ({
                    connection: document.querySelector('#connection').textContent,
                    width: document.querySelector('#video').videoWidth,
                    height: document.querySelector('#video').videoHeight,
                    decodedFrames: document.querySelector('#frames').textContent,
                    fps: document.querySelector('#fps').textContent
                })"""
            )
            print("BROWSER_STREAM_OK " + json.dumps(metrics, ensure_ascii=False))
            wait_for_peer_count(base_url, 1)

            page.locator("#stop").click()
            wait_for_peer_count(base_url, 0)
            print("BROWSER_STOP_OK " + json.dumps(get_health(base_url)))

            if browser_errors:
                raise AssertionError("Browser JavaScript errors: " + "; ".join(browser_errors))
            return 0
            # Browser and page are closed before the Playwright context exits.
        finally:
            if page is not None:
                try:
                    page.close()
                except Exception:
                    pass
            if browser is not None:
                browser.close()
                browser = None
    finally:
        if server.poll() is None:
            server.terminate()
            try:
                server.wait(timeout=5)
            except subprocess.TimeoutExpired:
                server.kill()
                server.wait(timeout=5)


if __name__ == "__main__":
    raise SystemExit(main())
