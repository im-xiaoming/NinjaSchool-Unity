# Cấu hình NSO qua Inspector

Chọn đối tượng dưới **NSO Game** trong Hierarchy. Các config instance dùng private SerializeField; API property giữ tương thích. FormerlySerializedAs giữ dữ liệu tên field cũ trên prefab/scene.

| Object / component | Config | Áp dụng |
|---|---|---|
| NSO Game Control | UI Zoom (0 auto, 1–4); Fullscreen On Start | Play tiếp theo. Fullscreen chỉ Windows build; -windowed vẫn ưu tiên |
| NSO Game Control | Logic Ticks Per Second (10–60), Target Frame Rate (15–144), Run In Background | Inspector ưu tiên khi khởi động; có thể đổi lúc Play |
| NSO Input Control | Hold Seconds, Click Pixel | Thao tác diagnostic. Pixel có gốc trái trên |
| NSO Game Layer | Draw Into Layer, Sorting Order/Layer, Opaque, Camera, Distance | Presentation runtime; Shader cần bật lại layer/Play lại |
| NSO Map Overlay | Camera/Layer references, Edit Pixels Per Unit, Preview Map Size, Draw Grid | Layout editor và presentation; trạng thái snapshot chỉ đọc |
| NsoMapObject | Map ID/Zone (-1 tất cả), Position, Depth Offset, Sorting Order, Scale With Zoom, Sprite PPU, Visual | Visual-only. Position là pixel map, Y xuống. Visual phải là child, không dùng parent/root |
| NSO Server Control | Save Subfolder, Static Subfolder | Play tiếp theo; đường dẫn tương đối nằm trong persistentDataPath/StreamingAssets |

## Quy tắc timing
- Không dùng PlayerPrefs `nsoTickRate` ghi đè Inspector nữa.
- F9/F10 chỉ đổi tốc độ runtime, không lưu vào PlayerPrefs và không sửa default scene.
- Giá trị default đọc từ scene/prefab; chỉnh khi Play sẽ được Unity phục hồi khi Stop.
- Muốn giữ config: Stop, chỉnh Inspector rồi save scene/prefab.
- Dùng một NSO Game Control và một NSO Server Control cho mỗi phiên. Không tự tạo config mới nếu đã có object.

## Server và save
Server khởi động AfterSceneLoad để config Awake có hiệu lực, trước vòng Start gameplay. Thiếu control dùng default NsoData/NsoStatic. Đường dẫn rỗng, absolute hoặc thoát khỏi data root bị từ chối; không có thao tác tự di chuyển/xóa save. Các nút xóa save có sẵn vẫn yêu cầu xác nhận; không bấm nếu muốn giữ tài khoản cũ.

## Những thứ không phải config
HP/MP hiện tại, level/XP, vị trí entity, map ID đang chạy, protocol IDs, tile geometry gắn với artwork, khóa kết nối và tài khoản không phải các field thiết kế để ghi tùy tiện. Ví dụ Char.readParam đọc cspeed/MaxHP/MaxMP từ server. Không thêm client override cho các thông số đó: cần thiết kế server riêng nếu muốn thay gameplay.

## Giới hạn
Overlay không thêm collision, không thay Tilemap hoặc renderer Java. Không hứa tăng FPS. Root scale và sorting khác renderer cũ cần kiểm chứng trên scene thực. Disable Domain Reload/Scene Reload đồng thời chưa được xác minh trong đợt này.

## Effect presentation (2026-11-11)

NSO Game Control > Gameplay > Instant Movement Stop (default true) stops grounded manual movement on the last horizontal button release (arrows, A/D mapped on PC, number-pad controls, pointer controls). Disable to restore the original client friction. Running speed, jump/fall and destination movement remain unchanged.
NSO Game Control > Effect presentation: Smooth Effects (default true), Mob Health Seconds (0-0.5, default 0.12), Menu Open Seconds (0-0.5, default 0.14). These apply during Play and only affect visible damage numbers, hit-splash motion, menu motion and monster HP bars. Zero HP seconds restores immediate HP-bar changes; disable Smooth Effects to use original snapped effect drawing. Menu selection waits for the configured opening so invisible buttons cannot be activated. No gameplay timing or actual HP is changed.

## Broad world presentation (2026-11-12)
NSO Game Control > Smooth World Motion (default true): fractional world/camera/projectile placement at render rate, with gameplay positions restored after paint. Smooth Effects now also covers weather/list-content motion. Target Frame Rate accepts 60-144 (default 60); Logic Ticks Per Second stays 24 by default. A requested cap cannot guarantee measured FPS or create new sprite poses. Disable the world option to compare legacy integer placement.

## Double-click = Enter (2026-11-12)
NSO Game Control > Gameplay > Double Click Ms (0-1000, default 400). Two clicks on the same monster, NPC, player or ground item within this time press Enter (`keyPressedz[5]`: attack / talk / pick up). 0 disables it. Offline client addition in `GameScr.onTargetClicked`; no server change.

## Gamepad / PS5 DualSense (2026-11-12)
`NsoGamepadInput` (Assets/Scripts/Input) is created automatically at startup ("NSO Gamepad Input"). To change the mapping, add the component to a scene object; then that one is used. Fields: Enabled, Stick Deadzone (default 0.5), Stick Run Only In World (default on: in the world the left stick only runs left/right and never jumps; D-pad up still jumps, menus still use the stick up/down), and one client key code per button (0 = unused, codes from MyKeyMap).
Default layout (Elden Ring style): left stick/D-pad move (up = jump); Cross = jump in the world, F2 back in menus/dialogs/popups; Circle = Enter in the world (interact/attack) and in menus (select); menu navigation with D-pad/stick up/down; Triangle = Enter (talk/pick up); R1 or right-stick flick = change target (`TargetCycler`: N nearest on-screen targets by elliptical distance, step +1, wrap to 0; list refreshed continuously); R2/L2/L1 = skill shortcuts 7/8/9; Options = F1 menu in the world; pressed again while the menu or any GameScr panel is open, it closes everything (Menu.doCloseMenu + GameScr.resetButton, the close-all action 11021); Square, Create and R3 unused (no matching client key).

Target switching fields: Flick Threshold (0.6), Target Count N (5), Ellipse Radius X/Y (120/60 map px), Refresh Seconds (0.1). Refresh = one pass over the map lists keeping top-N by insertion, O(m*N), no allocation.

## Dialog input (code statics, 2026-11-12)
`Dialog.NumberStep` (default 1) and `Dialog.NumberMin` (default 1): Up/Down change a numeric input field (e.g. buy quantity) by this step, never below the minimum. Dialogs that use Up/Down themselves (Input2Dlg) opt out via `UpDownStepsNumber`.

## Autonomous agents (NSO Server Control, 2026-11-12)
Áp dụng ở lần Play kế tiếp. `_agentsEnabled`, `_agentCount` (0-6), `_agentDamagePercent` / `_agentHpPercent` (agent không có trang bị), `_agentMinDecisionSeconds` (throttle), `_agentReviewSeconds` (xem lại kế hoạch định kỳ, 0 = tắt). LLM local: `_llmEnabled`, `_llmEndpoint` (Ollama mặc định), `_llmModel`, `_llmLocalOnly`, `_llmJsonMode` (tắt nếu server từ chối response_format), `_llmTimeoutSeconds`, `_llmBackoffSeconds`, `_llmTemperature`. Các giá trị khác ở `NsoServer.Ai.AgentConfig` (code).

## SQLite (NSO Server Control, 2026-11-13)
`_useSqlite` (mặc định bật): lưu tài khoản, nhân vật, ninja AI vào `NsoData/nso.db`; tắt thì dùng users.tsv / players.tsv như cũ. Player log: `_playerLogToDatabase` (mặc định bật, `Logs/PlayerLog/player_log.db`), `_playerLogToFile` (file text, mặc định tắt); `_playerLogKeepFiles` là số session giữ lại.

## Người chơi AI (NSO Server Control, 2026-11-13)
`_agentsEnabled`, `_aiPlayerCount` (mặc định 13, tạo nhân vật mới cấp 1), `_botLogEnabled` + `_botLogKeepDays` (log bot trong `player_log.db`, bảng `bot_entries`, giữ 20 ngày), `_llmGuide` (hướng dẫn cơ bản gửi LLM lúc khởi tạo). Các mục `_agentDamagePercent` / `_agentHpPercent` đã bỏ: AI chơi theo luật người chơi.


## Learning AI (2026-11-13)
Learning settings live in /agents.html rather than adding another prefab configuration system: Rules/Guided/RL, enabled tier, teacher probability, CUDA/CPU, rewards, learning, human recording, isolated long evaluation and release controls. Defaults: Guided/tier 0/learning on/CUDA, human recording and long evaluation off. Persisted in NsoLearning/settings.json beside the save directory. Existing AI/LLM Inspector values remain; LLM is now advisory. Bootstrap uses Tools/Learning/.venv/Scripts/python.exe (uv-managed). NsoServerControl forwards the 5-second average FPS without Unity calls on server threads. See docs/AI_LEARNING.md.


## Shared ground loot and AI hunting preferences (2026-11-13)
NSO Server Control > Ground drops: _groundLootOwnerSeconds=30, _groundLootLifetimeSeconds=60, applied before the tick starts for all characters. Lifetime is always greater than protection. Under AI players: _aiLootScanRadius=600, _aiLootVerticalRange=144, _aiLootStallSeconds=4, _aiLootRetrySeconds=8; _aiSpreadHuntingZones=true, _aiZoneCheckSeconds=10, _aiZoneChangeCooldownSeconds=45, _aiZoneApproachTimeoutSeconds=30, _aiZoneCrowdingThreshold=3, _aiZonePopulationImprovement=2. These are behaviour preferences, not permission/capacity/range exceptions. No prefab field was renamed; values apply next Play.


## 2026-11-13 - AI response and remote motion controls
NSO Server Control: Llm Structured Outputs (true) sends required steps JSON schema; Llm Format Backoff Seconds (5, 0..60) handles malformed replies separately from service errors.
NSO Game Control: Smooth Remote Characters (true, also requires Smooth World Motion); Remote Initial Move Seconds (0.5), Remote Maximum Move Seconds (0.75), Remote Teleport Distance (180 logical px). These affect rendering only. Expect presentation delay about a packet interval; no measured latency/FPS claim yet.


## 2026-11-13 - Bot movement budget
NSO Server Control > AI players > AI Move Step Px (default60, range12..120; next Play) configures destination distance per decision and an execution speed cap. Actual horizontal speed is also capped by PlayerState.Speed. Collision dimensions and base jump/gravity are ported gameplay constants, not additional bot power settings. No switch to re-enable obstacle bypass. RL evaluator fingerprints and receives the same setting.

## LLM dẫn dắt (2026-11-13)
`_llmLeads` (mặc định bật): kế hoạch của LLM là việc AI làm; rules/RL chỉ chạy tạm lúc chờ LLM hoặc khi LLM lỗi. `_llmReviewSeconds` (120): LLM xem lại kế hoạch đang chạy sau ngần ấy giây.
`_aiMoveSendMilliseconds` (250): bot báo vị trí tối đa mỗi ngần ấy ms khi di chuyển (Char.sendMove). `_aiMinPotionsToFight` (3): rules mua bình HP trước khi đánh nếu ít hơn số này. `_aiNoPotionFleeHpPercent` (55): hết bình mà còn tiền thì quay về mua ở mức HP này. `_aiFoodStock` (2) / `_aiFoodMoneyReserve` (300): số suất thức ăn bot mang (mua ở Tabemono sau bình, chỉ bằng tiền vượt mức giữ lại); bot tự ăn khi hết hiệu ứng. `_aiBagReserveSlots` (3): khi túi còn ít ô trống hơn số này, bot bán đá và trang bị sẽ không mặc (0 = không bán). `_aiFastTravelMinSavedHops` (2): bot chỉ đi xe kéo Umayaki hoặc dùng Khả di lệnh khi bớt được ít nhất ngần ấy lần chuyển map so với đi bộ (0 = không bao giờ). `_aiFastTravelScrollCostHops` (2): Khả di lệnh dùng 1 lần được tính thêm ngần ấy lần chuyển map (vì mất cuộn; Vô hạn khả di lệnh không tính). `_ragGameKnowledge` (bật): nạp toàn bộ kiến thức game vào RAG; `_ragKnowledgeTopK` (4): số tài liệu kiến thức game mỗi prompt; `_ragMemoryChars` (600): độ dài tối đa mỗi trí nhớ trong prompt. `_llmPriorityMinutes` (15): mỗi câu trả lời LLM đặt hệ số ưu tiên quest/hunt/shop/explore/collect/idle (0-3) cho rules dùng khi LLM bận; hết hạn sau số phút này (0 = tắt).
