"""No live model, Qdrant, gameplay, training or production save access."""

import asyncio
import copy
import hashlib
import json
import os
from pathlib import Path
import unittest
import uuid

import httpx

from nso_brain import api
from nso_brain.config import is_local
from nso_brain.knowledge import build_documents
from nso_brain.llm import InvalidReply, parse_decision
from nso_brain.memory import MemoryStore, point_id
from nso_brain.orchestration import decide
from nso_brain.prompts import build_prompt, short

FIXTURES = Path(__file__).with_name("fixtures")
GOLDEN = json.loads((FIXTURES / "prompts.json").read_text(encoding="utf-8"))


class PromptContracts(unittest.TestCase):
    def test_csharp_prompt_fixtures(self):
        for case in GOLDEN["cases"]:
            with self.subTest(recalled=case["recalled"], terrain=case["terrain"]):
                self.assertEqual(
                    case["expected"],
                    build_prompt(
                        case["snapshot"], GOLDEN["settings"], case["recalled"], case["terrain"]
                    ),
                )

    def test_utf16_limit(self):
        self.assertEqual("a😀", short("a😀", 3))
        self.assertEqual("a...", short("a😀", 1))

    def test_endpoints(self):
        for url in ("http://127.0.0.1:1", "http://localhost:1", "http://[::1]:1"):
            self.assertTrue(is_local(url))
        for url in ("http://localhost.example.com", "file:///tmp/data", "http://192.168.1.1"):
            self.assertFalse(is_local(url))

    def test_guid_compatible(self):
        # Captured from System.Guid(MD5.ComputeHash(UTF8(...))) on .NET, not Python's own conversion.
        expected = "340e2ff6-d0bd-5013-dcbd-6eb395501d13"
        self.assertEqual(expected, point_id("aisample", "fact:danger:5"))
        self.assertNotEqual(point_id("a", "b"), point_id("b", "a"))

    def test_reply_aliases_and_defaults(self):
        result = parse_decision(
            '<think>ignored</think>```json\n{"steps":[{"goal":"fight"}, '
            '{"goal":"goto","x":123}, {"goal":"unknown"}],'
            '"priorities":{"hunt":2}}\n```'
        )
        self.assertEqual(["hunt", "goto"], [s["goal"] for s in result["steps"]])
        self.assertEqual(5, result["steps"][0]["count"])
        self.assertEqual(-1, result["steps"][0]["map"])
        self.assertEqual(2, result["priorities"]["hunt"])
        self.assertEqual(1, result["priorities"]["shop"])

    def test_invalid_reply_preserves_raw(self):
        for raw in ("", "thinking only", '{"reason":"no plan"}', '{"steps":[{"goal":"unknown"}]}'):
            with self.subTest(raw=raw), self.assertRaises(InvalidReply) as caught:
                parse_decision(raw)
            self.assertEqual(raw, caught.exception.response)

    @unittest.skipUnless(
        os.environ.get("NSO_BRAIN_KNOWLEDGE_INPUT"), "run check_contracts.py for C# export parity"
    )
    def test_all_csharp_knowledge_documents(self):
        data = json.loads(
            Path(os.environ["NSO_BRAIN_KNOWLEDGE_INPUT"]).read_text(encoding="utf-8-sig")
        )
        expected = json.loads((FIXTURES / "knowledge_hashes.json").read_text(encoding="utf-8"))
        docs = build_documents(data["wiki"], data["itemUse"])
        actual = {d["key"]: hashlib.sha256(d["text"].encode("utf-8")).hexdigest() for d in docs}
        self.assertEqual(expected, actual)


class WorkflowContracts(unittest.IsolatedAsyncioTestCase):
    async def test_summary_budget_and_source_immutability(self):
        calls = []

        def respond(request):
            body = json.loads(request.content)
            calls.append(body)
            content = "I learned to survive." if len(calls) == 1 else '{"steps":[{"goal":"quest"}]}'
            return httpx.Response(200, json={"choices": [{"message": {"content": content}}]})

        settings = dict(GOLDEN["settings"], RagEnabled=False, LlmApiKey="", LlmContextTokens=1600)
        source = copy.deepcopy(GOLDEN["cases"][0]["snapshot"])
        source["Memory"] = [f"event {i}" for i in range(22)]
        before = copy.deepcopy(source)
        async with httpx.AsyncClient(transport=httpx.MockTransport(respond)) as client:
            result = await decide(client, MemoryStore(client, settings), settings, source)
        self.assertEqual(before, source)
        self.assertEqual(14, result["summaryCut"])
        self.assertEqual("I learned to survive.", result["summary"])
        self.assertNotIn("terrain (", calls[-1]["messages"][-1]["content"])
        self.assertNotIn("response_format", calls[0])
        self.assertEqual("json_schema", calls[1]["response_format"]["type"])

    async def test_llm_total_timeout_and_single_precision_temperature(self):
        from nso_brain.llm import chat

        observed = []

        async def slow(request):
            observed.append(json.loads(request.content)["temperature"])
            await asyncio.sleep(2)
            return httpx.Response(200, json={"message": {"content": "late"}})

        settings = dict(GOLDEN["settings"], LlmTimeoutSeconds=1, LlmApiKey="")
        async with httpx.AsyncClient(transport=httpx.MockTransport(slow)) as client:
            with self.assertRaises(TimeoutError):
                await chat(client, settings, None, "test", 10, False)
        # .NET (double)(float)0.7, as sent by the old C# chat transport.
        self.assertEqual([0.699999988079071], observed)

    async def test_failed_summary_keeps_memory(self):
        calls = 0

        def respond(request):
            nonlocal calls
            calls += 1
            if calls == 1:
                return httpx.Response(503)
            return httpx.Response(200, json={"message": {"content": '{"steps":[{"goal":"wait"}]}'}})

        settings = dict(GOLDEN["settings"], RagEnabled=False, LlmApiKey="")
        source = copy.deepcopy(GOLDEN["cases"][0]["snapshot"])
        source["Memory"] = [str(i) for i in range(22)]
        async with httpx.AsyncClient(transport=httpx.MockTransport(respond)) as client:
            result = await decide(client, MemoryStore(client, settings), settings, source)
        self.assertIsNone(result["summary"])
        self.assertEqual(0, result["summaryCut"])
        self.assertEqual(22, len(source["Memory"]))

    async def test_recall_separates_owners(self):
        owners = []

        def respond(request):
            if str(request.url).endswith("/api/embed"):
                return httpx.Response(200, json={"embeddings": [[1.0, 0.0]]})
            body = json.loads(request.content)
            owners.append(body["filter"]["must"][0]["match"]["value"])
            return httpx.Response(
                200, json={"result": [{"payload": {"kind": "fact", "text": "same"}}]}
            )

        async with httpx.AsyncClient(transport=httpx.MockTransport(respond)) as client:
            memory = MemoryStore(client, GOLDEN["settings"])
            memory.ready = True
            hits = await memory.recall("aisample", "quest", 8)
        self.assertEqual(["aisample", "*"], owners)
        self.assertEqual(["[fact] same"], hits)

    async def test_rag_failure_returns_context_fallback(self):
        def respond(request):
            return httpx.Response(503)

        async with httpx.AsyncClient(transport=httpx.MockTransport(respond)) as client:
            memory = MemoryStore(client, GOLDEN["settings"])
            memory.ready = True
            self.assertIsNone(await memory.recall("a", "query", 8))
            self.assertFalse(memory.available)
            self.assertEqual(1, memory.failures)

    async def test_deduplication_survives_restart(self):
        item = {"agent": "a", "key": "fact:1", "kind": "fact", "text": "unchanged"}
        ident = point_id("a", "fact:1")
        async with httpx.AsyncClient(
            transport=httpx.MockTransport(
                lambda r: httpx.Response(
                    200, json={"result": [{"id": ident, "payload": {"text": "unchanged"}}]}
                )
            )
        ) as client:
            memory = MemoryStore(client, GOLDEN["settings"])
            self.assertEqual([], await memory.unchanged([item]))
            self.assertEqual("unchanged", memory.stored_text[ident])

    async def test_worker_upserts_and_skips_existing_vectors(self):
        points = {}
        embeds = 0

        async def respond(request):
            nonlocal embeds
            body = json.loads(request.content) if request.content else {}
            if request.url.path.endswith("/api/embed"):
                embeds += 1
                return httpx.Response(200, json={"embeddings": [[1.0, 0.0] for _ in body["input"]]})
            if request.method == "GET":
                return httpx.Response(200, json={"result": {}})
            if request.method == "PUT":
                for point in body["points"]:
                    points[point["id"]] = point
                return httpx.Response(200, json={"result": {}})
            return httpx.Response(
                200, json={"result": [points[i] for i in body["ids"] if i in points]}
            )

        items = [
            {"agent": "a", "key": f"fact:{i}", "kind": "fact", "text": f"fact {i}"}
            for i in range(2)
        ]

        async def until(predicate):
            while not predicate():
                await asyncio.sleep(0)

        async with httpx.AsyncClient(transport=httpx.MockTransport(respond)) as client:
            settings = dict(GOLDEN["settings"], LlmGuide="")
            first = MemoryStore(client, settings)
            first.remember(items)
            await first.start()
            await asyncio.wait_for(until(lambda: first.stored == 2), timeout=2)
            await first.stop()
            self.assertTrue(first.available)
            self.assertEqual({point_id("a", x["key"]) for x in items}, set(points))
            self.assertEqual("a", next(iter(points.values()))["payload"]["agent"])
            second = MemoryStore(client, settings)
            second.remember(items)
            await second.start()
            await asyncio.wait_for(until(lambda: len(second.stored_text) == 2), timeout=2)
            await second.stop()
            self.assertEqual(1, embeds)

    async def test_failed_batch_remains_queued_during_backoff(self):
        async with httpx.AsyncClient(
            transport=httpx.MockTransport(lambda r: httpx.Response(503))
        ) as client:
            memory = MemoryStore(client, dict(GOLDEN["settings"], LlmGuide=""))
            memory.remember([{"agent": "a", "key": "fact:1", "kind": "fact", "text": "retry"}])
            await memory.start()

            async def failed():
                while memory.failures == 0:
                    await asyncio.sleep(0)

            await asyncio.wait_for(failed(), timeout=2)
            self.assertEqual(1, len(memory.queue))
            self.assertFalse(memory.available)
            await asyncio.wait_for(memory.stop(), timeout=1)

    async def test_bounded_outbox_and_cancel(self):
        async with httpx.AsyncClient() as client:
            memory = MemoryStore(client, GOLDEN["settings"])
            memory.remember(
                [{"agent": "a", "key": str(i), "kind": "fact", "text": "x"} for i in range(20001)]
            )
            self.assertEqual(20000, len(memory.queue))
            self.assertEqual("1", memory.queue[0]["key"])
            await memory.start()
            await memory.stop()
            self.assertTrue(memory.task.cancelled())


class ApiContracts(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.lifespan = api.lifespan(api.app)
        await self.lifespan.__aenter__()
        self.client = httpx.AsyncClient(
            transport=httpx.ASGITransport(app=api.app), base_url="http://brain-test"
        )

    async def asyncTearDown(self):
        await self.client.aclose()
        await self.lifespan.__aexit__(None, None, None)

    async def test_session_lifecycle_version_and_origin(self):
        client = self.client
        ident = str(uuid.uuid4())
        settings = dict(GOLDEN["settings"], RagEnabled=False)
        body = {"sessionId": ident, "contractVersion": 1, "settings": settings}
        self.assertEqual(
            409,
            (await client.post("/v1/sessions", json={**body, "contractVersion": 2})).status_code,
        )
        self.assertEqual(200, (await client.post("/v1/sessions", json=body)).status_code)
        self.assertEqual(ident, (await client.post("/v1/sessions", json=body)).json()["sessionId"])
        response = await client.post(
            f"/v1/sessions/{ident}/render", json={"snapshot": GOLDEN["cases"][0]["snapshot"]}
        )
        self.assertEqual(GOLDEN["cases"][0]["expected"], response.json()["prompt"])
        self.assertEqual(
            403,
            (
                await client.post(
                    "/v1/sessions", json=body, headers={"Origin": "http://example.com"}
                )
            ).status_code,
        )
        self.assertEqual(
            422,
            (
                await client.post(f"/v1/sessions/{ident}/memories/batch", json={"items": [{}]})
            ).status_code,
        )
        self.assertEqual(200, (await client.delete(f"/v1/sessions/{ident}")).status_code)
        self.assertEqual(
            404, (await client.post(f"/v1/sessions/{ident}/status", json={})).status_code
        )

    async def test_delete_cancels_pending_decision(self):
        from nso_brain.contracts import DecisionRequest

        ident = str(uuid.uuid4())
        await self.client.post(
            "/v1/sessions",
            json={
                "sessionId": ident,
                "contractVersion": 1,
                "settings": dict(GOLDEN["settings"], RagEnabled=False),
            },
        )
        started = asyncio.Event()
        old = api.decide

        async def blocked(*args):
            started.set()
            await asyncio.Event().wait()

        api.decide = blocked
        try:
            task = asyncio.create_task(
                api.decision(ident, DecisionRequest(requestId=1, snapshot={}))
            )
            await started.wait()
            await self.client.delete(f"/v1/sessions/{ident}")
            self.assertTrue(task.cancelled())
        finally:
            api.decide = old

    async def test_abandoned_external_session_expires(self):
        ident = str(uuid.uuid4())
        await self.client.post(
            "/v1/sessions",
            json={
                "sessionId": ident,
                "contractVersion": 1,
                "settings": dict(GOLDEN["settings"], RagEnabled=False),
            },
        )
        api.app.state.sessions[ident].touched = 0
        await api.prune_sessions(now=601)
        self.assertNotIn(ident, api.app.state.sessions)

    async def test_bad_model_reply_is_correlated_format_failure(self):
        client = self.client
        ident = str(uuid.uuid4())
        await client.post(
            "/v1/sessions",
            json={
                "sessionId": ident,
                "contractVersion": 1,
                "settings": dict(GOLDEN["settings"], RagEnabled=False),
            },
        )
        old = api.decide

        async def bad(*args):
            raise InvalidReply("no steps", "bad reply")

        api.decide = bad
        try:
            result = (
                await client.post(
                    f"/v1/sessions/{ident}/decide", json={"requestId": 42, "snapshot": {}}
                )
            ).json()
        finally:
            api.decide = old
        self.assertEqual(42, result["requestId"])
        self.assertEqual(ident, result["sessionId"])
        self.assertEqual("format", result["errorKind"])
        self.assertEqual("bad reply", result["raw"])


if __name__ == "__main__":
    unittest.main()
