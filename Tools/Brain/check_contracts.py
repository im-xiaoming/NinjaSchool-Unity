"""Reproducible C#/Python parity and local HTTP integration using scratch-only fixtures."""

import http.server
import json
import os
from pathlib import Path
import socket
import subprocess
import sys
import tempfile
import threading
import time
import unittest

import httpx

ROOT = Path(__file__).resolve().parents[2]
os.chdir(ROOT)
os.environ["NSO_BRAIN_SKIP_DOTENV"] = "1"
for provider_key in ("NSO_LLM_ENDPOINT", "NSO_QDRANT_URL", "NSO_EMBED_ENDPOINT", "NSO_LLM_API_KEY"):
    os.environ.pop(provider_key, None)
HOST = ROOT / "Tools/Brain/ContractHost/ContractHost.csproj"


def port():
    with socket.socket() as listener:
        listener.bind(("127.0.0.1", 0))
        return listener.getsockname()[1]


class Llm(http.server.BaseHTTPRequestHandler):
    def log_message(self, *args):
        pass

    points = {}
    points_lock = threading.Lock()

    def reply(self, body):
        body = json.dumps(body).encode()
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        self.reply({"result": {}})

    def do_PUT(self):
        body = json.loads(self.rfile.read(int(self.headers["Content-Length"])))
        with self.points_lock:
            for point in body.get("points", []):
                self.points[(self.path.split("/points")[0], point["id"])] = point
        self.reply({"result": {}})

    def do_POST(self):
        body = json.loads(self.rfile.read(int(self.headers["Content-Length"])))
        if self.path.endswith("/api/embed"):
            self.reply({"embeddings": [[1.0, 0.0] for _ in body["input"]]})
        elif self.path.endswith("/points/search"):
            owner = body["filter"]["must"][0]["match"]["value"]
            collection = self.path.split("/points")[0]
            with self.points_lock:
                points = [
                    value
                    for (group, _), value in self.points.items()
                    if group == collection and value["payload"]["agent"] == owner
                ]
            self.reply({"result": points[: body["limit"]]})
        elif self.path.endswith("/points"):
            collection = self.path.split("/points")[0]
            with self.points_lock:
                points = [
                    self.points[(collection, ident)]
                    for ident in body["ids"]
                    if (collection, ident) in self.points
                ]
            self.reply({"result": points})
        else:
            self.reply(
                {
                    "choices": [
                        {
                            "message": {
                                "content": json.dumps(
                                    {"reason": "contract", "steps": [{"goal": "goto", "x": 123}]}
                                )
                            }
                        }
                    ]
                }
            )


def main():
    with tempfile.TemporaryDirectory(prefix="nso-brain-contracts-") as scratch:
        scratch = Path(scratch)
        output = scratch / "host"
        subprocess.run(
            [
                "dotnet",
                "build",
                str(HOST),
                "--nologo",
                "-v:q",
                f"-p:OutputPath={output}/",
                f"-p:IntermediateOutputPath={scratch}/obj/",
            ],
            check=True,
        )
        command = ["dotnet", str(output / "ContractHost.dll")]
        exported = scratch / "knowledge.json"
        subprocess.run(command + ["export", str(exported)], check=True)
        subprocess.run(command + ["config"], check=True)
        os.environ["NSO_BRAIN_KNOWLEDGE_INPUT"] = str(exported)
        suite = unittest.defaultTestLoader.discover(str(ROOT / "Tools/Brain/tests"))
        result = unittest.TextTestRunner(verbosity=2).run(suite)
        if not result.wasSuccessful():
            return 1

        llm = http.server.ThreadingHTTPServer(("127.0.0.1", 0), Llm)
        threading.Thread(target=llm.serve_forever, daemon=True).start()
        brain_port = port()
        brain_url = f"http://127.0.0.1:{brain_port}"
        llm_url = f"http://127.0.0.1:{llm.server_port}/v1/chat/completions"
        options = {"creationflags": subprocess.CREATE_NO_WINDOW} if os.name == "nt" else {}
        with (scratch / "brain.log").open("wb") as log:
            process = subprocess.Popen(
                [sys.executable, str(ROOT / "Tools/Brain/serve.py"), "--port", str(brain_port)],
                stdout=log,
                stderr=log,
                **options,
            )
            try:
                for _ in range(100):
                    try:
                        if (
                            httpx.get(
                                brain_url + "/health", timeout=0.2, trust_env=False
                            ).status_code
                            == 200
                        ):
                            break
                    except httpx.HTTPError:
                        time.sleep(0.1)
                subprocess.run(command + ["probe", brain_url, llm_url, "external"], check=True)
                subprocess.run(command + ["ragprobe", brain_url, llm_url], check=True)
                assert (
                    httpx.get(brain_url + "/health", trust_env=False).status_code == 200
                ), "external process was stopped"
            finally:
                process.terminate()
                process.wait(timeout=10)
            # Exercise the game-owned hidden process path on a fresh port; never stop by process name.
            owned_url = f"http://127.0.0.1:{port()}"
            subprocess.run(command + ["probe", owned_url, llm_url, "auto"], check=True)
            llm.shutdown()
            llm.server_close()
        print(
            "PASS Brain contracts; no real model, Qdrant, gameplay, training or character save used"
        )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
