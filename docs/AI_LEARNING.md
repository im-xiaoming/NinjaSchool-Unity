# Learning AI players (2026-11-13)

This is an offline addition, not a Java port. It extends the real-player AI architecture; it does not replace server gameplay rules. The user explicitly requested online learning, GPU training with uv, and reuse of the existing rules. No production save migration is required.

## Operation

Open `/agents.html` while the game runs. The learning panel provides Rules / Guided / RL, learning, optional human-action recording, CPU/GPU, enabled tier, teacher probability, rewards, evaluation, apply and rollback. Writes use the existing `/control/state` token, `X-Nso-Control`, origin checks and body limit; `GET/POST /control/learning` is the new endpoint.

Initial settings: Guided, learning on, CUDA, teacher probability 0.5, tier 0, automatic application of passing releases on, human recording off, long evaluation off. With no accepted model, all bots use the existing teacher. LLM calls produce advice/speech only and do not block plan execution. Turning the LLM off still works.

Tiers: 0 teacher only; 1 strategic goals; 2 combat primitives; 3 movement primitives. A published release cannot operate above its evaluated tier even if the enabled-tier setting is larger. Each newly evaluated tier sets guidance to 0.5 / 0.25 / 0.1 respectively. Rules mode always executes the teacher and can still collect demonstrations. RL mode samples the policy without teacher mixing; it still falls back safely if no compatible model is available.

The strategic teacher is `RulePlanner`; tactical demonstrations come from actual `BotClient` commands. NPC menus, waypoint routing, admission, equipment and other existing macros are retained. Primitive decisions intercept one eligible movement/attack/potion/pick request per bot per 500 ms time window; they can select another valid primitive or wait. Other macro messages retain their existing behaviour. This is hierarchical control over existing skills, not a second implementation of the game. The human-input capture hook observes supported commands without consuming their readers or changing them. Human recording stores structured game observations and action labels, not credentials/chat text; it is opt-in and persisted until disabled.

## Python and GPU

Canonical dependency management: `Tools/Learning/pyproject.toml` and `uv.lock`, Python 3.11, project-local `.venv`. The bootstrap selects `.venv/Scripts/python.exe`; it never uses the global Python environment or downloads anything at runtime.

```powershell
uv sync --project Tools/Learning --locked
uv run --project Tools/Learning --locked python Tools/Learning/check_contracts.py
```

Installed on this machine with the user's download approval: PyTorch 2.9.1+cu128 (official PyTorch CUDA 12.8 index; Windows wheel about 2.7 GiB), SB3 / sb3-contrib 2.7.1, Gymnasium 1.2.2, NumPy 2.2.6 and locked transitive dependencies from PyPI. GPU detected: RTX 3060, 12 GB VRAM, driver 610.62. A CUDA tensor calculation passed. Missing dependencies, Python or trainer leave rules active and expose the error/status on the web.

The trainer is an owned, hidden, below-normal-priority Python process, one CPU thread for host work, CUDA for network optimisation when selected/available. C# inference remains CPU-only. Set CPU in the dashboard to avoid GPU contention with Ollama. Small MLPs do not imply high GPU utilisation. Learning requires a fresh 5-second average FPS >=60 and 30 stable seconds. Low/stale FPS disables updates and cancels the owned evaluator; game decisions never wait for IPC. This controls load but is not a certified 60 FPS guarantee.

## Data and policy contract

Code is under `Assets/Server/Ai/Rl`; Python tools are under `Tools/Learning`. Runtime data defaults to `NsoLearning`, a sibling of the configured save directory, outside Assets. `nso.db`, characters and the old `ai_players` memory document retain their existing formats.

- Schema 1: 424 float inputs = 40 state/personality/advice/history values + 48 candidates x 8 features. Stable bounded slots encode candidate kind, identity, map, relative position, skill and count. Booleans mask infeasible actions; the server remains authoritative. The teacher's exact demonstrated movement occupies its directional slot. Nearby monster windows are sorted by distance/id, loot by id.
- Each policy has separate actor/critic MLPs, 64/64 Tanh, 48 masked categorical outputs. Strategy and tactical models share parameters across the 13 personalities, not between the two domains. JSON tensor shapes, finiteness, schema, domain, version, stage, size and evaluation hashes are checked before loading.
- JSONL transitions include stream/id, domain, source (rules/RL/human), version, observations, masks, action, value/log-probability, reward components, next state, elapsed time and terminated/truncated flags. Source-free HP gains, respawn healing, speech and menu opening produce no reward. Exploration rewards a map only once per agent session. Quest/EXP/resource differences and failed moves are computed from authoritative state. Personality weights affect caution, greed and curiosity; aggression/social behaviour also comes from teacher demonstrations and policy input.
- Writer queue: 2048 detached records, drop counter on overflow, no disk writes per inference. Disk worker rotates approximately 2 MB chunks. Trainer reads complete lines only, 4096 samples/domain in memory, retains up to 512 MB / 20 days of experience. Checkpoints are retained for manual diagnosis/rollback and require separate disk management.
- Teacher/human data trains behavioural cloning only. PPO updates use exact active-policy versions, validated C#/Python value/log-probabilities and contiguous streams. Teacher interruptions, dropped samples and episode boundaries cut trajectories with bootstrap; demonstrations never enter the PPO rollout buffer. Gamma is per decision, not per wall-clock second; elapsed time remains recorded for diagnostics. The maintained MaskablePPO implementation performs optimisation (256 steps, batch 64, 4 epochs, gamma .99, GAE .95, entropy .02, KL .02).

## Release and evaluation

No model is considered strong just because it trained. The trainer saves `.zip` checkpoints for Python and JSON weights for C#, verifies export and C#/Python parity, then creates a candidate. Long evaluation starts only when the user enables its separate checkbox. While disabled, the candidate waits and demonstrations continue accumulating.

Evaluator: `Tools/Learning/RlHost`, standalone .NET 8 host compiling the same server and shared basic-attack progression. The client-only `DisplayMaxPoint` method is Unity-conditional so no fake client types or duplicated combat formulas are needed. The evaluator never opens the real save: new temp directories, TSV backend, copied balance files only, LLM off, 13 new characters, normal real-time ticks. Three independent trials per arm, 600 seconds each, compare Rules to the candidate (about one hour). They use matching initial conditions/balance/time but are not deterministic RNG replays. Scratch output is retained for diagnosis. Cancellation terminates only the process the supervisor started.

Gate per personality: completion rate for quests 0-3 decreases no more than 5 percentage points; quest progress >=95%, EXP >=85%; deaths/stuck counts increase no more than 10% (zero baseline requires zero). At least one baseline character must complete quests 0-3 to reject meaningless idle-vs-idle approval. These are initial noisy regression gates, not statistical proof or a measure of human likeness. Later quests and class mechanics still require manual validation.

Passing releases include content hashes. The supervisor atomically updates `active.json`, preserves `previous.json`, and handles explicit apply/rollback. The game verifies the report and both model hashes before publishing the pair. Bots retain their old model while fighting and switch at safe subsequent decision boundaries. Stops use per-launch stop files, avoiding process-wide kills or shutdown signals leaking into a later Play session. No untrusted Python model is loaded by the game.

Evaluation snapshots the active static-data path and balance files; interrupted trials cannot be reused after that context changes. Promotion checks the balance fingerprint again. A subsequent balance change invalidates the active release and returns bots to their teacher until a matching release passes. The supervisor stays available for explicit apply/rollback even with optimisation paused; requests are acknowledged by durable IDs so restarting Play does not repeat an old rollback. Windows Editor tooling is the configured target; a standalone build needs the external Tools/Learning environment deployed beside it to train (inference/fallback do not require Python).

The dashboard exposes sample/drop counts, trainer/candidate/evaluation, per-bot source/version/fallback, cumulative reward, stuck count and action distribution. `inferenceTickP95Ms` measures policy-selection work summed across a tick over the last 128 ticks; observation building/server gameplay is outside that metric. The <5 ms p95 target and whole-game frame time need real 13-bot profiling.

## Validation and next steps

Short synthetic checks passed on CUDA: teacher imitation, C#/Python masked inference parity with nonzero inputs, rejection of all-masked inference, PPO exclusion of human samples, checkpoint reload followed by PPO update, regression-gate rejection, and partial-JSONL recovery. These checks instantiate no game server and do not touch player saves.

No Play mode, live browser, production training, long evaluator run, gameplay regression or 13-bot performance certification was performed. The first real Play must confirm unchanged rules behaviour and data collection, then inspect the trainer status and candidate. Enable long evaluation explicitly when convenient; inspect per-personality results before trusting later tiers. RL cannot complete quest mechanics that have no executor; known handmade quest limitations remain. Human likeness is judged by the user and optional human demonstrations, not inferred solely from reward.


Final validation (2026-11-13): Unity refresh and current Console passed at 2026-11-12T19:01:12Z: compilationFailed=false, compiling=false, consoleErrors=0, consoleWarnings=0. Release host build passed (only the pre-existing unused Controller.zoomLevel warning). Seven short synthetic CUDA contracts passed; JS syntax, Python byte-compilation, uv lock and changed-file whitespace checks passed. No Play/browser/live training/long evaluation run; production saves unchanged. The earlier dirty NSO Game.prefab has a pre-existing trailing-space warning and was not edited for this change.


## Shared loot timing / teacher foraging and zone update (2026-11-13)
This supersedes the older references to 20-second ownership above: every character now shares GroundLootPolicy (30-second protection / 60-second default lifetime). Timing is Inspector-configurable, passed to the scratch evaluator and included in the balance fingerprint. Old candidates evaluated under the previous fingerprint require evaluation again. Teacher behaviours now include wider opportunistic foraging, bounded stuck-item retries, contested-target avoidance and population-based hunting-zone changes through the actual zone NPC/client requests. Courtesy reservations only affect AI decisions; they grant no ownership, slots or access rights. No new policy action/schema was added; these remain shared execution skills. Compile only for this update; user checks runtime.

## Confirmed item/NPC action rewards (2026-11-13)

These are learning rewards only, never additional character EXP, currency or permissions. Configure them in the existing reward panel; setting a weight to zero disables its contribution without erasing history.

| Setting | Default | Confirmation and repeat policy |
| --- | --- | --- |
| newItemUseReward | 0.2 | First observed successful use of a template: consumption, exact equipment instance, real skill change, confirmed timed-action start, movement/teleport or auto-play activation. Item-menu opening alone is not use. Once per template across normal saves/relogin. |
| newItemPurchaseReward | 0.1 | A normal buy command actually increases inventory quantity. Once per template, independent of quantity/stack/lock state. Failed purchases grant nothing. |
| timelyItemUseReward | 0.3 | HP potion starts a positive new recovery effect while HP <=50%; MP actually increases from <=25%; food starts a positive absent food effect while HP/MP is missing; forward quest-item progress; first skill learning or stat-improving equipment use. Default 30-second gap. Objective-item keys suppress reset/replay rewards. |
| npcDiscoveryReward | 0.1 | First nearby NPC interaction with a real server menu, valid zone list, or needed successful purchase. Once per NPC template. |
| usefulNpcFeatureReward | 0.3 | Forward quest/quiz progress, school admission, purchasing missing needed supplies, opening/depositing into storage when free slots <=2, withdrawing needed supplies, actual upgrade growth, or moving to a less populated zone. Once per NPC/feature/task-step; quiz index distinguishes answers. Merely reopening/cancelling menus does not count. |

Weights allow 0..5. `usefulHpPercent` / `usefulMpPercent` are integer 1..100; `usefulItemCooldownSeconds` 1..3600; `usefulBagFreeSlots` 0..200. Future confirmed events use the current weights. Weighted cumulative ledgers are snapshotted at each policy transition boundary; `components.actions` contains each category's interval delta. Strategic and tactical policies receive the events in their respective intervals. Counters/totals in a bot's Plan tab are session-local, while novelty keys survive sessions.

The receipt observer wraps normal Controller commands and uses a separate reader, so it does not consume or alter the gameplay payload. Observation/persistence errors are isolated from gameplay commands and logout. Optional `ai_players[].rewardHistory` stores bot novelty sets and cooldown; opted-in human demonstration history uses `rl_reward_history_<characterId>` documents. Old documents/settings without these fields retain defaults. The same classification is used for opted-in humans, but NPC actions without a supported pending policy transition are not fabricated into PPO records. Existing executors still determine which services an AI can operate; adding a reward does not implement missing quest mechanics or make an untrained policy immediately understand them.

This change was compiled and statically checked only. Manual checks: first/repeated and rejected item use/purchase; thresholds and active effects; menu confirmations; useful NPC services versus spam/cancel/reset; normal relog retaining novelty; settings and per-category counters/transition components. No live training/gameplay acceptance was run by the agent.


## Readiness snapshot and LLM input reliability (2026-11-13)
The inspected local installation is Guided/stage0: one unevaluated candidate v1794510524833, no active release and long evaluation disabled. This is teacher collection, not evidence of a qualified primary RL policy. Enable isolated evaluation separately and review the existing gates before increasing tiers. No live evaluation was run for this assessment.
LLM structured output now requires reason/say/fleeHp/steps and known goal fields. json_object alone guaranteed JSON syntax but allowed reason-only objects. Schema support follows https://docs.ollama.com/capabilities/structured-outputs . The Inspector can disable structured output for incompatible services. Invalid reply diagnostics retain at most 16384 characters and use a separate 5-second format backoff. This improves advice transport, not policy skill; runtime verification remains pending.


## Movement intent execution (2026-11-13)
Movement candidates now request destinations that BotMovement attempts using swept walking and grounded jumping; they do not set arbitrary endpoint coordinates. Rules and RL share the same executor. Policy decisions/transition boundaries remain 500 ms; ~42 ms client frames are execution, not additional PPO samples. Normal server handlers remain authoritative. The evaluation fingerprint includes |motion:tile-motion-1:<moveStepPx>; environment.json and both isolated arms pass this setting. Earlier evaluation reports must be regenerated. No new tensor/action slot or save schema is added. Tall/unreachable terrain can cause a plan to stall; route planning and gameplay acceptance remain separate work.
