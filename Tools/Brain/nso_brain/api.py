"""Versioned, loopback-only Brain API. Never reads character saves or executes game commands."""

import asyncio
import time
from contextlib import asynccontextmanager

import httpx
from fastapi import FastAPI, HTTPException, Request

from . import CONTRACT_VERSION
from .config import load_settings
from .contracts import (
    SessionRequest,
    DecisionRequest,
    MemoryBatch,
    KnowledgeBatch,
    RecallRequest,
    RenderRequest,
)
from .knowledge import build_documents
from .llm import InvalidReply
from .memory import MemoryStore
from .orchestration import decide
from .prompts import build_prompt, short


class Session:
    def __init__(self, client, settings):
        self.settings = load_settings(settings)
        self.memory = MemoryStore(client, self.settings)
        self.decisions = asyncio.Lock()
        self.knowledge_count = 0
        self.touched = time.monotonic()
        self.pending = set()

    async def close(self):
        pending = list(self.pending)
        for task in pending:
            task.cancel()
        if pending:
            await asyncio.gather(*pending, return_exceptions=True)
        await self.memory.stop()


async def prune_sessions(now=None):
    now = time.monotonic() if now is None else now
    for ident, session in list(app.state.sessions.items()):
        timeout = max(60, session.settings.get("BrainSessionIdleSeconds", 600))
        if not session.pending and now - session.touched > timeout:
            app.state.sessions.pop(ident, None)
            await session.close()


async def session_leases():
    # A Unity domain reload can interrupt best-effort DELETE. Reap abandoned external-service sessions.
    while True:
        await asyncio.sleep(30)
        await prune_sessions()


@asynccontextmanager
async def lifespan(app):
    app.state.sessions = {}
    async with httpx.AsyncClient(trust_env=False, follow_redirects=False) as client:
        app.state.client = client
        leases = asyncio.create_task(session_leases())
        try:
            yield
        finally:
            leases.cancel()
            await asyncio.gather(leases, return_exceptions=True)
            for session in list(app.state.sessions.values()):
                await session.close()


app = FastAPI(title="NSO Brain", version="1", lifespan=lifespan)


@app.middleware("http")
async def local_requests(request: Request, call_next):
    # The game and standalone test client send no Origin. Browser writes remain same-machine only.
    if request.headers.get("origin"):
        from starlette.responses import JSONResponse

        return JSONResponse({"error": "browser origins are not accepted"}, status_code=403)
    return await call_next(request)


def session_for(ident):
    session = app.state.sessions.get(ident)
    if session is None:
        raise HTTPException(404, "unknown session")
    session.touched = time.monotonic()
    return session


@app.get("/health")
async def health():
    return {
        "service": "nso-brain",
        "contractVersion": CONTRACT_VERSION,
        "sessions": {key: value.memory.status() for key, value in app.state.sessions.items()},
    }


@app.post("/v1/sessions")
async def create_session(body: SessionRequest):
    if body.contractVersion != CONTRACT_VERSION:
        raise HTTPException(409, "incompatible Brain contract")
    ident = str(body.sessionId)
    if ident not in app.state.sessions:
        try:
            session = Session(app.state.client, body.settings)
        except (KeyError, ValueError) as error:
            raise HTTPException(422, str(error)) from error
        app.state.sessions[ident] = session
        await session.memory.start()
    return {"sessionId": ident, "contractVersion": CONTRACT_VERSION}


@app.delete("/v1/sessions/{ident}")
async def delete_session(ident: str):
    session = app.state.sessions.pop(ident, None)
    if session:
        await session.close()
    return {"closed": True}


@app.post("/v1/sessions/{ident}/memories/batch")
async def remember(ident: str, body: MemoryBatch):
    session = session_for(ident)
    items = [item.model_dump(exclude_none=True) for item in body.items]
    session.memory.remember(items)
    return {"accepted": len(items), "rag": session.memory.status()}


@app.post("/v1/sessions/{ident}/knowledge/batch")
async def knowledge(ident: str, body: KnowledgeBatch):
    session = session_for(ident)
    documents = build_documents(body.wiki, body.itemUse)
    if session.settings["RagGameKnowledge"]:
        session.memory.remember(documents)
    session.knowledge_count = len(documents)
    return {"documents": len(documents), "rag": session.memory.status()}


# Diagnostic endpoints keep the existing headless prompt/knowledge/recall commands useful.
@app.post("/v1/sessions/{ident}/render")
async def render(ident: str, body: RenderRequest):
    session = session_for(ident)
    if body.wiki is not None:
        return {"documents": build_documents(body.wiki, body.itemUse)}
    return {"prompt": build_prompt(body.snapshot, session.settings, body.recalled, body.terrain)}


@app.post("/v1/sessions/{ident}/recall")
async def recall(ident: str, body: RecallRequest):
    session = session_for(ident)
    hits = await session.memory.recall(body.agent, body.query, body.count)
    return {"memories": hits, "rag": session.memory.status()}


@app.post("/v1/sessions/{ident}/decide")
async def decision(ident: str, body: DecisionRequest):
    session = session_for(ident)
    result = {
        "sessionId": ident,
        "requestId": body.requestId,
        "contractVersion": CONTRACT_VERSION,
    }
    task = asyncio.current_task()
    session.pending.add(task)
    try:
        # Match the existing C# pending-request window; abandoned HTTP calls cannot queue endless LLM work.
        async with asyncio.timeout(max(1, session.settings["LlmTimeoutSeconds"]) + 15):
            async with session.decisions:
                try:
                    result.update(
                        await decide(
                            app.state.client, session.memory, session.settings, body.snapshot
                        )
                    )
                except InvalidReply as error:
                    result.update(
                        error=str(error), errorKind="format", raw=short(error.response, 16384)
                    )
                except Exception as error:
                    result.update(error=f"{type(error).__name__}: {error}", errorKind="service")
    except TimeoutError:
        result.update(error="Brain request exceeded the pending window", errorKind="service")
    finally:
        session.pending.discard(task)
    result["rag"] = session.memory.status()
    return result


@app.post("/v1/sessions/{ident}/status")
async def status(ident: str):
    session = session_for(ident)
    return {"rag": session.memory.status(), "knowledge": session.knowledge_count}
