# AGENTS.md: rules for every agent working on this project

**Project:** an offline, single-player conversion of "Ninja School Online" (NSO).
- **Active Unity project:** `C:\My project`. Older history is in `C:\NSO_OFFLINE`: its `NinjaOffline` project is the predecessor and is no longer edited.
- **Original source (never modify):** `C:\NSO`.
- **Read-only reference copies:** `C:\NSO_OFFLINE\reference`. It holds the Java server source (`server_java/src/main/java/com/nsoz`) and the SQL dumps.

**User language:** Vietnamese. Reply to the user in Vietnamese. Code, comments and these documents are in English, except where Vietnamese game text is quoted.

## Before starting work
1. Read the project documents in this order:
   - this file;
   - `PROJECT_STATUS.md`, `TASKS.md`, `DECISIONS.md`, `HANDOFF.md`;
   - `docs/` (architecture, testing);
   - `Assets/Documentation/` (server port table, Inspector configuration, UI reference, map overlay);
   - `README.md`.
2. Read the older history only when you need background. It is in `C:\NSO_OFFLINE`: `HANDOFF.md`, `DECISIONS.md`, `docs/PROTOCOL.md` and `docs/ONLINE_REMOVAL.md`.
   - Its user decisions are still binding.
   - Its status tables are stale: they predate combat, quests and shops.
3. Use the **dev wiki** instead of re-reading code to look up an id or check whether something is ported:
   - While Play mode runs: `http://127.0.0.1:8787/api`.
   - Otherwise: the file `NsoWiki/db.json`.
   - See `README.md`.
4. Inspect the real source before assuming anything. Documents can be stale; the code and the tests are the truth.

## While working
- Continue from the existing implementation. Do not restart finished work. Do not overwrite or discard earlier changes without a stated reason.
- Follow `docs/ARCHITECTURE.md` and `DECISIONS.md`.
- Port behaviour from the Java source; name the Java method in a comment.
  - Anything that differs from Java on purpose is an "offline deviation". Comment it in the code and record it in `DECISIONS.md` and `Assets/StreamingAssets/NsoWiki/notes.json` (`deviations`).
- User rules carried over from previous sessions:
  - Keep Unity compiling and check the Console.
  - A fix must not affect unrelated behaviour.
  - Comment the code.
  - Make tunable values configurable.
  - Keep the docs updated.
  - The user debugs with screenshots.
- Ask the user before:
  - large or irreversible changes;
  - downloads (state the name, source and size);
  - copying large asset folders from `C:\NSO`;
  - deleting gameplay code.
- Test every change. Say plainly what was only compiled, what was run in tests, and what was seen in Play mode.

## Before finishing or handing off
- Update `PROJECT_STATUS.md`, `TASKS.md`, `DECISIONS.md` and `HANDOFF.md`.
- Also update `docs/` if the architecture changed, `Assets/Documentation/ServerPort.md` for server work, and `Assets/StreamingAssets/NsoWiki/notes.json` (features, constraints, commands, config).
- The handoff must state what was completed and which files changed, the tests run and their results, known bugs, dependencies, the exact next steps and the commands needed.
- Never mark an undocumented change as completed.

## Pitfalls already hit (do not repeat)
- **Global client types:** the client defines global `Math`, `TileMap`, `Mob`, `Zone` and similar names.
  - In `Assets/Server`, add `using Math = System.Math;` (and `Mob`/`Zone` aliases) inside the namespace block.
  - Otherwise Unity fails with CS0117 or an ambiguity error, even though `dotnet` builds fine.
- **Unity is the final judge:** the `dotnet` test build does not see the client types. Always check Unity too:
  - run `unity command script eval --file refresh.cs`;
  - then `unity command console status`, and look at `compilationFailed` and `consoleErrors`.
- **Play mode does work** on the open Editor, through `unity command script eval` with `UnityEditor.EditorApplication.isPlaying = true/false`.
  - Only `-batchmode -nographics` cannot play.
  - Read runtime state with small eval scripts (see `docs/TESTING.md`).
- **Encodings:**
  - PowerShell 5.1 `Get-Content`/`Set-Content` without explicit UTF-8 corrupts Vietnamese text.
  - Client files mix CRLF and tabs.
  - Use the Edit/Write tools, or Python with explicit `encoding='utf-8'` that keeps the original line endings.
- **Escapes in C# source:** the Write tool turned `'\u2028'` in C# source into a raw line-separator character, which broke the compile. Write such characters as `(char)0x2028`.
- **Heredocs:** in Bash, heredocs that contain quotes break. Use `<<'EOF'` or write a file first.
- **Test transport:** the tests talk to the server through the in-memory pipe (`GameServer.Connect`), which has no read timeout. Never set `ReadTimeout` on it; `TestClient.HasMore` reads in the background instead.
- **Processes:** do not blanket-kill processes. Stop only the ones you started; for example, find a test web server by the port it listens on.
