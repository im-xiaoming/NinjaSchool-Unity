# Botbrain refactor and local Python Brain

Implemented on branch `botbrain`, starting from `eac5538a` (2026-11-13). This supersedes older instructions that put all configuration in the Inspector or describe the LLM as advisory-only.

## Ownership and module map

| Owner | Responsibility |
| --- | --- |
| C# gameplay server | Character state, quests/combat/items, permissions, binary protocol, authoritative saves and world locking |
| C# AI Runtime / Contracts / Planning | Bot lifecycle, detached observations, decisions, rules/RL fallback and authoritative plan validation |
| C# AI Skills / Navigation | Existing quest/shop/combat/foraging macros, pathfinding and movement frames through BotClient/Controller |
| C# AI Memory / Integration | Gameplay-derived knowledge, local memory/save ownership, bounded outbox and Brain HTTP/process adapters |
| C# AI Learning | Existing `NsoServer.Ai.Rl` types; folder moved from `Ai/Rl` to `Ai/Learning`, namespace unchanged |
| Python Brain | Prompt construction, context budget, summaries, LLM transport, embedding, Qdrant mirroring/retrieval and document rendering |
| Unity adapters | Startup/shutdown, frame/tick bridge, input, rendering/audio/assets, optional overlays and Inspector compatibility |

Client global types and public gameplay methods retain their names. `GameScr`, `Char`, client `Controller`/`Service`, `AgentManager` and `AgentTactics` now have partial files grouped by responsibility. Their original files retain fields, initializers, constructors/base types and script GUIDs. The original command-dispatch branches remain intact: this is organization, not an invented replacement protocol/UI framework.

The server is still embedded C# over the in-memory game pipe. Only Brain uses HTTP. C# captures map visibility, terrain/reachability, NPC/monster/item positions and gameplay facts. Python receives detached snapshots/text, never a live world reference, a character database path, or permission to execute game commands. `GameKnowledge.UseInfo`, `Food` and `UsableFromBag` remain in C#; Python builds the RAG prose from their exported descriptions. Existing Java references and offline deviations remain binding.

## Setup and operation

The separate Python 3.11 environment is `Tools/Brain/.venv`; `Tools/Learning` and its CUDA dependencies remain separate. Approved dependencies are locked in `Tools/Brain/uv.lock`.

```powershell
uv sync --project Tools/Brain --locked
uv run --project Tools/Brain --locked python Tools/Brain/serve.py --port 8797
```

The default game configuration uses `BrainAutoStart=true`. It launches the existing venv interpreter as a hidden owned process; it never installs dependencies at runtime. If a compatible Brain already exists at the configured endpoint, the game attaches and does not own or terminate that process. Set `agents.BrainAutoStart=false` to require an independently launched debugger/service. The API binds to `127.0.0.1`; default port `8797` avoids wiki's `8787..8796`. No port reclamation or process-name killing is used.

LLM model/endpoints and all behavioral defaults are retained. Provider environment keys are `NSO_LLM_ENDPOINT`, `NSO_QDRANT_URL`, `NSO_EMBED_ENDPOINT`, `NSO_LLM_API_KEY`. Brain reads only these supported keys from the root ignored `.env`; process environment wins. `Tools/Brain/.env.example` contains placeholders. Keys are never serialized from C# or logged. Qdrant/embedding remain loopback; the existing `LlmLocalOnly` rule applies to LLM. Existing Ollama models and Qdrant collections are reused; setup downloads no model.

Missing Python, unavailable service, incompatible version, transport/model error or malformed model output leaves rules/RL available. `LlmLeads=true` retains a running plan while waiting, installs a valid LLM response, and uses rules/RL when a plan/service is unavailable. Turning it off retains advisory mode. No reward/policy tensor, model release gate, movement revision or ground timing is changed. Synthetic/evaluation hosts explicitly disable RAG and cannot launch Brain.

## Version 1 API

| Route | Purpose |
| --- | --- |
| `GET /health` | Service identity, contract version and per-session RAG diagnostics |
| `POST /v1/sessions` | `{contractVersion:1, sessionId:UUID, settings}`; idempotent connection/reconnection |
| `DELETE /v1/sessions/{id}` | Close that session, leave an externally owned server running |
| `POST /v1/sessions/{id}/memories/batch` | `{items:[{agent,key,kind,text,updated}]}`; bounded nonblocking mirror |
| `POST /v1/sessions/{id}/knowledge/batch` | Wiki records plus authoritative `itemUse` descriptions |
| `POST /v1/sessions/{id}/decide` | `{requestId,snapshot}` -> correlated decision/raw reply/summary/summaryCut/RAG status or typed error |
| `POST /v1/sessions/{id}/status` | Outbox/indexing progress for existing diagnostics |
| `POST /v1/sessions/{id}/render` | Diagnostic-only prompt or knowledge rendering, no model call |
| `POST /v1/sessions/{id}/recall` | Diagnostic retrieval, preserving the explicit RAG test commands |

The LLM decision retains reason/say/fleeHp/priorities/steps. C# decodes the proposal and applies existing world validation under `SyncRoot`. In-process request sequence and Brain session/request IDs reject stale responses. Network/indexing operations run on workers outside `SyncRoot`; shutdown does not wait on HTTP under the lock. Owned process termination is initiated before Unity assembly unload; external-session HTTP cleanup is asynchronous.

RAG preserves the `nso_agent_memory` collection, `{agent,key,kind,text,updated}` payload, shared owner `*`, separate personal/shared searches, unchanged-document deduplication, 32-document embedding batches, 20,000-entry bounded queues and 30-second retry. Point IDs reproduce `new Guid(MD5(UTF8(agent + "|" + key)))`, including its little-endian GUID fields. Existing Qdrant memory is not reset. Character knowledge/events and optional summary remain in `ai_players`; no character-save migration occurs.

## Runtime configuration

Optional file: `Application.persistentDataPath/NsoConfig/runtime-config.json`, version 1. Sections are `server`, `game`, `ui` and `agents`. `Tools/Brain/runtime-config.example.json` is a minimal example.

Precedence is explicit JSON override, then captured scene/Inspector value, then the existing code default. `agents` overrides apply after the legacy server-to-AgentConfig mapping; they are explicit overrides, not duplicated defaults. Existing balance, monster, loot and learning files retain their locations and ownership. There is no automatic save move or automatic configuration write.

Unity components keep their names, serialized fields, clamps and accessors. `NsoConfigurationBridge` captures their values into the pure `RuntimeConfiguration` model. Existing in-game setters update session configuration. The old Inspector is a fallback/editor adapter; JSON overrides take priority. UI layout objects remain mutable so the existing F6 sliders still work.

On NSO Server Control, **Export runtime configuration (never overwrite)** exports the active profile, including scene overrides, or current session UI preferences during Play. Export refuses an existing destination. Export outside Play does not start the game, alter a prefab/scene or touch character saves. Export first, edit JSON, then restart Play to apply file changes. The exporter does not replace current tuning with new defaults.

## Verification and maintenance

```powershell
uv run --project Tools/Brain --locked python Tools/Brain/check_contracts.py
Tools/Learning/.venv/Scripts/python.exe Tools/Learning/check_contracts.py
dotnet run --project Tools/CodeMaintenance/CodeMaintenance.csproj -- audit .
dotnet run --project Tools/CodeMaintenance/CodeMaintenance.csproj -- check .
```

The maintenance tool reuses Roslyn from the installed .NET SDK (this machine: SDK 10). `format` verifies syntax tokens before writing whitespace; `audit` compares original gameplay members with Git HEAD across partial files, excluding the documented integration/configuration changes. Python formatting uses the preinstalled Black 25.1.0 with its AST-equivalence check. No formatter package was downloaded. `AutoTaThu.cs` is deliberately byte-preserved because it is not UTF-8. Vendor/generated files, original/reference projects and assets are not reformatted.

Brain tests use mock upstreams and scratch directories only. They compare six pre-refactor prompt fixtures and hashes of all 1,885 pre-refactor knowledge documents, exercise summary/context fallback, memory dedupe/ownership/queue behavior, configuration precedence/export, actual C# HTTP round trips and automatic/external process ownership. The RL synthetic runner builds its own scratch host so a live evaluator cannot lock its DLL. Neither suite starts a real game or trains against player data.

Gameplay regression commands run from `Tools/ServerTests`: combatcheck, questcheck, progresscheck, lootcheck, regencheck, rulecheck, navcheck, webcheck, bagcheck, upgradecheck, ledgercheck. Ordinary fixtures disable RAG; explicit ragknowledge/promptcheck rag still opt in. Build outputs for this refactor are isolated under `.utmp`, apart from an unsuccessful attempt to rebuild a DLL held by an existing evaluator; no existing process was stopped for that attempt.

Manual acceptance remains with the user: compare the same title/world/inventory/menu, F6 settings, movement/combat and bots; inspect LLM plans and RAG diagnostics; disconnect/restart Brain; verify the old configuration fallback and an exported JSON profile. Real Ollama/Qdrant gameplay, 13-bot performance, long evaluation and standalone Windows distribution are not certified by these isolated tests. The dev environment is the delivery target; release-runtime packaging remains future work.

## Final validation record

Actual Unity Editor refresh/compilation passed after authorized normal Stop; the final Console has zero current errors/warnings. The Editor is left stopped. Before clearing the recorded transition logs, RMS and Main.FixedUpdate emitted two runtime error groups during reload of the existing Play. Their source methods retain original tokens; no fresh Play was started to certify lifecycle behavior. The preserved stack traces are in .utmp/reload-errors.log and the handoff records the limitation.

Brain has 20 tests plus actual HTTP probes, including mock embedding/Qdrant outbox/recall without wiki data, an owned-service crash/restart and external ownership. Per-request deadlines and the float-to-double LLM temperature conversion match the previous C# transport. The existing pending window bounds work after HTTP disconnect. An external session has a configurable idle lease (agents.BrainSessionIdleSeconds, default 600 seconds) so interrupted Unity reloads cannot accumulate session workers.

Eleven gameplay regression groups and pendingcheck/knowledgecheck/promptcheck passed. Both original and refactored default suites have 18 PASS / 2 FAIL (obsolete login assertions), exit 2; these are preserved failures, not a completely green default suite. Seven CUDA RL synthetic contracts passed. Final source audit verifies 8,778 original fields/properties/methods; the formatter is idempotent with the actual Unity preprocessor symbols and strips trailing whitespace only in syntax trivia, preserving literal contents.

Additional compile check without entering Play:

```powershell
python Tools/CodeMaintenance/check_unity_sources.py
```

It compiles runtime and Editor sources with Unity's own compiler/defines/references into .utmp; it does not replace the actual Editor refresh/Console check. Machine-readable evidence and exact changed-file/moved-GUID inventories: refactor-validation.json and refactor-changes.json.
