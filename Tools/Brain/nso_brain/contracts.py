"""API envelopes. Snapshot field names deliberately match the detached C# AgentSnapshot."""

from typing import Any
from uuid import UUID

from pydantic import BaseModel, Field


class SessionRequest(BaseModel):
    contractVersion: int
    sessionId: UUID
    settings: dict[str, Any]


class DecisionRequest(BaseModel):
    requestId: int
    snapshot: dict[str, Any]


class MemoryRecord(BaseModel):
    agent: str
    key: str
    kind: str
    text: str
    updated: int | None = None


class MemoryBatch(BaseModel):
    items: list[MemoryRecord] = Field(default_factory=list, max_length=256)


class KnowledgeBatch(BaseModel):
    wiki: dict[str, Any]
    itemUse: dict[str, Any]


class RecallRequest(BaseModel):
    agent: str
    query: str
    count: int


class RenderRequest(BaseModel):
    snapshot: dict[str, Any] | None = None
    wiki: dict[str, Any] | None = None
    itemUse: dict[str, Any] = Field(default_factory=dict)
    recalled: list[str] | None = None
    terrain: bool = True
