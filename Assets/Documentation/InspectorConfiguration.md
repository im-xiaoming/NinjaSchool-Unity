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
