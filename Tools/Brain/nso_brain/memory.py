"""Bounded, retryable Qdrant mirror; never called under the game's world lock."""

import asyncio
import hashlib
import time
import uuid
from collections import deque
from urllib.parse import quote


def point_id(agent, key):
    # System.Guid(byte[]) treats its first three fields as little endian.
    digest = hashlib.md5((agent + "|" + key).encode("utf-8")).digest()
    return str(uuid.UUID(bytes_le=digest))


class MemoryStore:
    def __init__(self, client, settings):
        self.client, self.settings = client, settings
        self.queue = deque(maxlen=20000)
        self.stored_text = {}
        self.ready = self.available = False
        self.error = ""
        self.stored = self.recalls = self.failures = 0
        self.wake = asyncio.Event()
        self.task = None

    @property
    def collection(self):
        return "/collections/" + quote(self.settings["RagCollection"], safe="")

    async def send(self, method, path, body=None):
        async with asyncio.timeout(15):
            response = await self.client.request(
                method, self.settings["QdrantUrl"].rstrip("/") + path, json=body, timeout=15
            )
        response.raise_for_status()
        return response.json()

    def remember(self, items):
        if not self.settings["RagEnabled"]:
            return
        for item in items:
            if item.get("text"):
                self.queue.append({**item, "updated": item.get("updated", int(time.time() * 1000))})
        self.wake.set()

    async def start(self):
        if self.settings["RagEnabled"]:
            self.task = asyncio.create_task(self.worker())
            self.remember(
                [
                    {
                        "agent": "*",
                        "key": f"guide:{i}",
                        "kind": "guide",
                        "text": line.strip().lstrip("- "),
                    }
                    for i, line in enumerate((self.settings["LlmGuide"] or "").split("\n"))
                    if len(line.strip()) > 10
                ]
            )

    async def stop(self):
        if self.task:
            self.task.cancel()
            try:
                await self.task
            except asyncio.CancelledError:
                pass

    def fail(self, error):
        self.available = False
        self.failures += 1
        self.error = f"{type(error).__name__}: {error}"

    def status(self):
        return {
            "available": self.available,
            "stored": self.stored,
            "recalls": self.recalls,
            "failures": self.failures,
            "error": self.error,
            "queued": len(self.queue),
        }

    async def embed(self, texts):
        async with asyncio.timeout(15):
            response = await self.client.post(
                self.settings["EmbedEndpoint"],
                json={"model": self.settings["EmbedModel"], "input": texts},
                timeout=15,
            )
        response.raise_for_status()
        vectors = response.json()["embeddings"]
        if len(vectors) != len(texts) or not vectors or not vectors[0]:
            raise ValueError("embedding count mismatch")
        return vectors

    async def search(self, vector, owner, count):
        if count <= 0:
            return []
        result = await self.send(
            "POST",
            self.collection + "/points/search",
            {
                "vector": vector,
                "limit": max(1, count),
                "with_payload": True,
                "filter": {"must": [{"key": "agent", "match": {"value": owner}}]},
            },
        )
        return [
            f'[{h["payload"].get("kind", "")}] {h["payload"].get("text", "")}'
            for h in result.get("result", [])
            if h.get("payload") is not None
        ]

    async def recall(self, owner, query, count):
        if not self.settings["RagEnabled"] or not self.ready or not query:
            return None
        try:
            vector = (await self.embed([query]))[0]
            own = await self.search(vector, owner, count)
            for hit in await self.search(vector, "*", max(0, self.settings["RagKnowledgeTopK"])):
                if hit not in own:
                    own.append(hit)
            self.recalls += 1
            self.available = True
            return own
        except Exception as error:
            self.fail(error)
            return None

    async def open_collection(self):
        await self.send("GET", self.collection)
        self.ready = True

    async def ensure_collection(self, dimensions):
        try:
            await self.open_collection()
        except Exception:
            await self.send(
                "PUT", self.collection, {"vectors": {"size": dimensions, "distance": "Cosine"}}
            )
            await self.send(
                "PUT",
                self.collection + "/index",
                {"field_name": "agent", "field_schema": "keyword"},
            )
        self.ready = True

    async def unchanged(self, batch):
        result = await self.send(
            "POST",
            self.collection + "/points",
            {
                "ids": [point_id(x["agent"], x["key"]) for x in batch],
                "with_payload": True,
                "with_vector": False,
            },
        )
        have = {p["id"]: (p.get("payload") or {}).get("text") for p in result.get("result", [])}
        remaining = []
        for item in batch:
            ident = point_id(item["agent"], item["key"])
            if have.get(ident) == item["text"]:
                self.stored_text[ident] = item["text"]
            else:
                remaining.append(item)
        return remaining

    async def worker(self):
        try:
            await self.open_collection()
        except Exception:
            pass
        while True:
            if not self.queue:
                self.wake.clear()
                await self.wake.wait()
            batch = []
            while self.queue and len(batch) < 32:
                item = self.queue.popleft()
                if self.stored_text.get(point_id(item["agent"], item["key"])) != item["text"]:
                    batch.append(item)
            if not batch:
                continue
            try:
                if self.ready:
                    batch = await self.unchanged(batch)
                if not batch:
                    continue
                vectors = await self.embed([x["text"] for x in batch])
                if not self.ready:
                    await self.ensure_collection(len(vectors[0]))
                points = [
                    {"id": point_id(x["agent"], x["key"]), "vector": vector, "payload": x}
                    for x, vector in zip(batch, vectors)
                ]
                await self.send("PUT", self.collection + "/points?wait=false", {"points": points})
                for item in batch:
                    self.stored_text[point_id(item["agent"], item["key"])] = item["text"]
                self.stored += len(batch)
                self.available = True
            except Exception as error:
                self.fail(error)
                # Keep the old failed-batch retry policy, with a bounded queue.
                self.queue.extend(batch[: max(0, 20000 - len(self.queue))])
                await asyncio.sleep(30)
