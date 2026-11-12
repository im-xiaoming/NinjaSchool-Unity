# Project Overview
- Game Title: NSO / NinjaGame (tên chính thức chưa xác nhận; giữ nguyên tên project).
- High-Level Concept: Game ninja port từ Java, giữ lõi gameplay/network và renderer cũ. Thêm cầu nối để prefab và đối tượng Unity được đặt theo tọa độ map, giúp người dùng và agent chỉnh scene mà không sửa lõi game.
- Players: Client/server hiện có; giữ nguyên mô hình người chơi, không thêm networking mới.
- Inspiration / Reference Games: Game Java NSO gốc.
- Tone / Art Direction: Giữ pixel art và giao diện hiện tại; không tạo art mới.
- Target Platform: StandaloneWindows64 hiện tại.
- Screen Orientation / Resolution: Giữ kích thước Game View/window và zoom hiện có; không áp đặt resolution mới.
- Render Pipeline: UniversalRP, Unity 6000.6.8f1.

# Game Mechanics
## Core Gameplay Loop
Giữ toàn bộ di chuyển, chiến đấu, chuyển map, cập nhật server và va chạm cũ. Overlay giai đoạn này chỉ là đối tượng hiển thị Unity, không trở thành vật cản, trigger gameplay hoặc nguồn trạng thái mới. Không tuyên bố tối ưu FPS khi chưa đo.
## Controls and Input Methods
Project đang dùng Both; giữ input legacy hiện có. Không thêm điều khiển mới. Scene editing qua Transform và Inspector của component cầu nối; không gửi teleport hoặc ghi TileMap qua Transform.

# UI
Không thay menu/HUD. Thêm Inspector rõ ràng và gizmo tọa độ map:
```
NSO Map Overlay Bridge
  Game Layer / Camera references
  Current Map ID, Zone, Ready (read-only)
  Debug grid / bounds
NSO Map Object
  Map ID / optional zone filter
  Logical position X,Y (pixel game, Y hướng xuống)
  Pivot / depth offset / sorting order
  Child visual prefab
```
Nút editor 'Capture Scene Position' và 'Apply Logical Position' chỉ đổi dữ liệu component overlay, không đổi trạng thái gameplay. Map root trong Scene View giúp placement ổn định, không phụ thuộc camera đang cuộn.

# Key Asset & Context
## Đã kiểm tra
- Assets/Scripts/Gameplay/World/TileMap.cs: mapID, zoneID, pxw/pxh, tmw/tmh, maps/types, size=24 trong khởi tạo.
- Assets/Scripts/Application/Interp.cs: tạm blend GameScr.cmx/cmy và vị trí entity trong BeginPaint, khôi phục trong EndPaint. Không đọc nhầm tọa độ blend như trạng thái authoritative.
- Assets/Scripts/Application/Main.cs: paint trong OnGUI Repaint; FixedUpdate cập nhật logic.
- Assets/Scripts/Rendering/NsoGameLayer.cs: fullscreen quad, RenderTexture theo Screen.width/height, camera fallback Camera.main, sortingOrder; tạo mesh runtime nhưng chưa hủy riêng.
- Assets/Scripts/Custom/NsoPlayerInfo.cs và Diagnostics/NsoGameControl.cs: cầu nối Inspector hiện có nhưng chưa map-space overlay.
- Assets/Scenes/NinjaGame.unity, Assets/Prefabs/NSO_Game.prefab: scene có NsoGameLayer, camera orthographic size=5, position=(0,1,-10), layer distance=10, unit scale.
- AssetDatabase.GetDependencies cho các mục đã thử không trả dependency; trước khi triển khai cần kiểm tra prefab/scene serialization và textual references bổ sung, không coi kết quả rỗng là không có dependents.

## Tạo dự kiến
- Assets/Scripts/Bridge/NsoMapSnapshot.cs: snapshot map/camera và trạng thái readiness.
- Assets/Scripts/Bridge/NsoMapCoordinates.cs: phép chuyển logical map pixel ↔ presentation, và phép nghịch đảo.
- Assets/Scripts/Bridge/NsoMapOverlayBridge.cs: registry overlay, lifecycle, map filter, render synchronization.
- Assets/Scripts/Bridge/NsoMapObject.cs: placement component cho prefab/scene object, không tự truy cập logic toàn cục.
- Assets/Editor/NsoMapOverlayEditors.cs: Inspector, placement actions, gizmos có Undo.
- Assets/Tests/EditMode/NsoMapCoordinatesTests.cs và test assembly tối thiểu nếu chưa có framework phù hợp.
- Assets/Documentation/UnityMapOverlayBridge.md: hướng dẫn sử dụng cho người và agent, ownership và giới hạn.
## Sửa dự kiến
- NsoGameLayer.cs: expose mapping dựa trên hình học quad/camera thực, quản lý mesh runtime và render synchronization hook tối thiểu.
- Main.cs: nếu cần, thêm hook render quanh paint; đặt Interp.EndPaint trong finally để không giữ trạng thái blend khi paint lỗi. Không refactor input/network trong đợt này.
- NSO_Game.prefab: thêm map overlay bridge/root; không đổi camera, game speed, input module.
- NinjaGame.unity: wiring instance và một đối tượng kiểm chứng đơn giản nếu không phù hợp đặt trong prefab; tránh duplicate bridge.

# Implementation Steps
1. **Xác minh coordinate pipeline và compatibility.** Đọc đầy đủ mGraphics/ScaleGUI, GameScr.paint và shader NsoGameLayer để xác định zoom, translate, viewport/camera rect, UV orientation và sorting. Kiểm tra Unity/URP package version và test framework. Kiểm tra dependency/dependents bằng công cụ có sẵn và scene/prefab serialization. Dependencies: không có. Không thêm PixelPerfectCamera hoặc đổi renderer vì có thể phá quad cũ; giữ point filtering hiện tại.
2. **Định nghĩa snapshot và phép chuyển tọa độ.** Tạo NsoMapSnapshot và NsoMapCoordinates. Logic map dùng pixel với Y xuống; Scene map root dùng hệ ổn định với Y lên. Presentation dùng (map position - camera scroll), zoom đã xác minh và quad/camera basis; không hardcode orthographic size=5 hoặc tile size=24 làm scale renderer. Cung cấp round-trip và kiểm tra tham số bất hợp lệ. Dependencies: Step 1.
3. **Render synchronization.** Capture camera scroll đã interpolation tại đúng paint hook; phục hồi trạng thái bằng finally, không cho bridge sửa TileMap/GameScr. Giữ presentation timing nhất quán với texture cũ; kiểm chứng một marker trước khi mở rộng. Dependencies: Step 2. Nếu ordering OnGUI/URP khiến marker lệch một frame, điều chỉnh hook trong cùng kiến trúc; nếu cần renderer rewrite phải xin duyệt lại.
4. **Component overlay và lifecycle.** Tạo NsoMapOverlayBridge/NsoMapObject trong namespace riêng. Đăng ký khi enable, hủy khi disable/destroy; không FindObjects mỗi frame. Chỉ thay transform khi placement/camera thay đổi, chỉ refresh map metadata khi đổi map/readiness. Lọc map/zone, xử lý loading/disconnect, không tắt GameObject chứa registration làm mất đăng ký; toggle visual child/renderers. Không cấp phát collection hàng loạt mỗi frame. Dependencies: Steps 2–3.
5. **Editor workflow.** Inspector dùng SerializedProperty, Undo và prefab overrides; hiển thị trạng thái runtime read-only. Gizmos vẽ map bounds/grid, hạn chế số đường cho map lớn. Cung cấp placement từ tọa độ và capture từ Scene Transform, không sửa logic gameplay. Không xây hierarchy một GameObject cho mỗi ô tile. Dependencies: Step 4.
6. **Sorting và wiring scene.** Wire bridge vào prefab/root. Visual dùng material tương thích URP hiện tại. Cho sorting order trước/sau toàn bộ layer, không hứa interleave với nhân vật/tile nằm bên trong texture. Kiểm tra shader/depth thực tế, không chỉ dựa vào sorting order của MeshRenderer. Dùng primitive visual kiểm chứng trước, không import art hoặc package mới. Dependencies: Steps 1, 4–5.
7. **Cleanup và tài liệu.** Giữ tham chiếu mesh runtime của NsoGameLayer và Destroy đúng lifecycle; giữ cleanup material/RenderTexture. Tài liệu mô tả thao tác agent, coordinate units, map filters, cách revert overlay và giới hạn visual-only. Dependencies: Steps 3–6.
8. **Kiểm thử và báo cáo.** Chạy tests, kiểm tra thủ công Game View/Scene View và build Windows nếu môi trường hỗ trợ. Ghi nhận trước/sau, không cam kết tăng FPS. Dependencies: Steps 1–7.

# Verification & Testing
- Coordinate unit tests: round-trip tại origin/corners, camera scroll, zoom 1–4, resize và aspect ratios; Y orientation, root translation/rotation/scale, invalid dimensions và null camera/layer.
- Marker đặt tại tọa độ tile đã biết phải khớp hình map khi đứng yên và cuộn; kiểm tra interpolation ở tick 24 và render 60, đổi tốc độ và teleport.
- Map/zone change, loading, reconnect: đúng marker visibility và không giữ snapshot cũ.
- Sorting trước/sau quad phải được xác minh trên shader/URP thực tế; ghi rõ không interleave bên trong texture.
- Enable/disable bridge/visual, scene reload và Play nhiều lần với Domain Reload bật/tắt: không duplicate registration, callback hoặc object runtime.
- Editor Undo/Redo, prefab save/reopen và scene save/reopen giữ placement đúng; editor actions không đổi Char/TileMap.
- Overlay disabled phải giữ gameplay/render cũ; input, network và va chạm không thay đổi.
- Theo dõi allocations bridge sau warm-up bằng Profiler nếu sẵn có: không scan scene hoặc tạo collection mỗi frame. Chưa làm tối ưu loading/atlas/UI.
- Unity compile và test framework hiện có; Windows build smoke test nếu có đủ tooling. Kết quả nào chưa chạy phải được ghi rõ.

## Phạm vi và rollback
Không chuyển sang Unity Tilemap, không thay nhân vật bằng SpriteRenderer, không thêm physics/collision. Phần overlay trên là baseline; đợt tiếp theo bên dưới chỉ mở rộng cấu hình Inspector, không triển khai lại overlay hoặc tự sửa các vấn đề review ngoài phạm vi config.

# Bổ sung: Cấu hình qua component Inspector
## Project Overview bổ sung
Giữ game, gameplay loop, nền tảng Windows, URP, art và input hiện tại. Người dùng chọn component Inspector (không ScriptableObject), tất cả nhóm config và Inspector ưu tiên khi bắt đầu Play.

## Game Mechanics / Controls bổ sung
Không expose mọi biến runtime. Phân loại config vs trạng thái, client vs server và startup-only vs runtime trước khi expose. Giữ ownership gameplay cũ; cấu hình client không được giả làm quyền sửa thông số server. Không thay backend input. Hotkey tốc độ được giữ nhưng chỉ đổi runtime, không tự ghi PlayerPrefs khi ở chế độ Inspector ưu tiên; component không được ghi đè hotkey mỗi Update.

## UI bổ sung
Không đổi UI game. Inspector chia Header: Display, Timing, Camera, Overlay, Local Server, Gameplay, Diagnostics. Dùng private [SerializeField], Tooltip tiếng Việt hoặc rõ nghĩa, Range/Min và validation thực tế. Field startup-only ghi rõ cần Play lại; trạng thái runtime hiển thị disabled trong custom Inspector. Hướng dẫn Hierarchy object nào quản lý từng nhóm. Không tạo control host/port vì transport hiện là in-memory.

## Key Asset & Context bổ sung
- Assets/Scripts/Diagnostics/NsoGameControl.cs: uiZoom, logicTicksPerSecond, targetFrameRate, runInBackground đã public và serialized. Start đang lấy Main.tickRate ghi ngược vào config; Update ghi PlayerPrefs. Cần sửa ownership thay vì chỉ thêm attribute.
- Assets/Scripts/Application/Main.cs: startup đọc nsoTickRate và gán targetFrameRate=60; hotkey F9/F10 ghi PlayerPrefs. Cần thống nhất Inspector ưu tiên.
- Assets/Scripts/Diagnostics/NsoInputControl.cs: holdSeconds/clickPixel là config thao tác diagnostic, không phải điều khiển gameplay mới.
- Assets/Scripts/Diagnostics/NsoServerControl.cs: status/address/dataFolder là trạng thái, chưa config. ServerBootstrap khởi động BeforeSceneLoad nên Awake scene config hiện không thể ảnh hưởng startup.
- Assets/Server/ServerBootstrap.cs: server in-process, thư mục NsoData/NsoStatic; giữ client-close-before-server-stop đã sửa, không làm mất shutdown guard.
- Assets/Scripts/Rendering/NsoGameLayer.cs; Assets/Scripts/Bridge/NsoMapOverlayBridge.cs; Assets/Scripts/Bridge/NsoMapObject.cs: config serialized hiện tại cần bảo toàn prefab và API.
- Assets/Scripts/Screens/GameScr.cs, gameplay entities/world, camera logic, reconnect, server/world/config: phạm vi audit các tham số gameplay có thể cấu hình, không cam kết mọi numeric literal là config.
- Assets/Editor/NsoControlEditors.cs, NsoMapOverlayEditors.cs: cập nhật SerializedProperty, Undo, multi-edit và trạng thái read-only.
- Assets/Prefabs/NSO_Game.prefab và Assets/Scenes/NinjaGame.unity: giữ override hiện có, wire component config đúng một nguồn.
- Tạo dự kiến Assets/Scripts/Diagnostics/NsoGameplayControl.cs nếu cần cho config gameplay đã xác minh, Assets/Documentation/InspectorConfiguration.md và test config trong test assembly phù hợp.

## Implementation Steps bổ sung
1. **Audit và bảng cấu hình.** Đọc lại source và scene/prefab hiện tại, kiểm tra dependencies/dependents trước mọi thay đổi. Liệt kê tất cả tham số có ý nghĩa thiết kế trong phạm vi hiển thị/timing/camera/overlay/local server/gameplay/reconnect, cùng source, default thực tế, ownership, validation và apply timing. Không expose ID protocol, tile dimensions gắn với asset, HP/XP/position runtime hay bí mật tài khoản. Các mục cần đổi protocol/schema/server authority phải báo lại và xin duyệt, không giả lập hiệu lực. Dependencies: không có.
2. **Serialization an toàn.** Chuyển config phù hợp sang private SerializeField; giữ tên field nếu có thể. Nếu đổi tên dùng FormerlySerializedAs, giữ public accessor/API cho dependents, không làm mất dữ liệu prefab. Không thêm SerializeField vào static/const/property vì không được serialize như field instance. Cập nhật custom editors có SerializedProperty và Undo. Dependencies: Step 1.
3. **Display/timing ownership.** Áp dụng config trước khi Main khởi tạo game; loại bỏ ghi ngược PlayerPrefs vào serialized config và hardcode startup ghi đè. Phím runtime và Inspector edit có một đường Apply rõ ràng, không loop tranh chấp mỗi frame. Clamp tick/frame rate trong cả load và runtime. Zoom/fullscreen phân loại startup-only nếu renderer cũ chưa hỗ trợ đổi nóng an toàn; giữ behavior mặc định lấy từ scene hiện tại. Dependencies: Steps 1–2.
4. **Overlay config.** Giữ placement trong logical pixels, map/zone filter, sorting/depth/scale, camera/layer refs. Validation PPU dương và offset hợp lệ theo camera; áp dụng runtime khi field đổi, không silently ghi gameplay state. Không sửa renderer/collision hoặc toàn bộ lifecycle overlay trong đợt này. Dependencies: Steps 1–2.
5. **Local server/lưu dữ liệu.** Cho cấu hình đường dẫn tương đối dưới persistentDataPath và đường dẫn static data tương thích hiện tại; kiểm tra path không hợp lệ và không xóa/di chuyển save tự động. Điều chỉnh startup ordering để config scene sẵn sàng trước server init và trước client connect, có fallback cho scene thiếu component. Xác minh default và lifecycle/assembly reload. Không thêm port/host vô hiệu. Giữ shutdown client trước server. Dependencies: Steps 1–2.
6. **Gameplay config.** Theo bảng audit Step 1, đưa các tham số thiết kế an toàn ra NsoGameplayControl hoặc component phù hợp; mỗi field phải có call site tiêu thụ thực, validation, default tương đương và test. Giá trị do server quyết định chỉ configurable qua server path đã xác minh, không expose writable client mirror. Không tự chuyển damage/HP/physics sang nguồn mới. Camera/reconnect nếu được expose phải nối đúng logic hiện tại và tôn trọng shutdown. Dependencies: Steps 1–2, 5 khi liên quan server.
7. **Wiring và tài liệu.** Migrate prefab/scene, kiểm tra mỗi nhóm có một owner; ghi danh mục field, startup/runtime, phạm vi ảnh hưởng và cách revert. Ghi rõ các biến loại trừ khỏi config và lý do. Dependencies: Steps 3–6.
8. **Verification.** Compile, serialization round-trip, prefab overrides và test runtime theo phần dưới. Báo những test chưa chạy thay vì tuyên bố hoàn tất. Dependencies: Steps 1–7.

## Verification & Testing bổ sung
- Inspector value khác PlayerPrefs: Play phải dùng Inspector, không mutate saved preferences ngoài thao tác lưu được chọn rõ ràng.
- F9/F10 đổi runtime tốc độ không bị NsoGameControl reset frame sau; runtime Inspector change áp dụng đúng một lần.
- Save/reopen scene/prefab, Undo/Redo và multi-edit giữ field cũ; không mất override do rename.
- Tick/FPS/PPU/holdSeconds/đường dẫn invalid: clamp hoặc lỗi có hướng dẫn, không chia cho 0 và không xóa save.
- Startup-only không gây hiểu nhầm: edit lúc Play không âm thầm ghi đè server/zoom hiện tại.
- Config server phải có hiệu lực trước connect; default NsoData vẫn load đúng save cũ; stop/Play nhiều lần không có lỗi Mất kết nối giả hoặc reconnect sau shutdown.
- Gameplay default so với baseline giữ tốc độ/luật cũ; mỗi exposed field thử thay giá trị và quan sát call site/runtime tương ứng, không field trang trí.
- Domain Reload bật/tắt, component thiếu hoặc duplicate: fallback/detection rõ ràng; không stale static config.
- Windows build smoke test nếu tooling sẵn có. Không cần thêm UI/game assets hay packages ngoài test dependencies đã xác minh.

Đợt cấu hình này chờ phê duyệt; chưa sửa code hoặc asset game trong quá trình lập kế hoạch.

# Bổ sung mới: Tự cân bằng né và sát thương quái qua web local

## Project Overview
- Game Title: NSO / NinjaGame.
- High-Level Concept: Giữ game ninja offline hiện có; thêm cấu hình chiến đấu vào web local để người dùng tự cân bằng độ khó.
- Players: Single player, server C# nhúng in-process.
- Inspiration / Reference Games: NSO gốc.
- Tone / Art Direction: Giữ nguyên pixel art và giao diện web hiện có.
- Target Platform: Windows64, Unity 6000.6.8f1, UniversalRP.
- Screen Orientation / Resolution: Giữ cửa sổ game; web responsive theo layout hiện có.
- Render Pipeline: UniversalRP; không sửa renderer.

## Game Mechanics
### Core Gameplay Loop
Giữ đánh quái, nhận EXP, nhiệm vụ và hồi sinh. Chỉ thêm tham số cho quái đánh nhân vật. Không sửa EXP, thức ăn, đồ rơi hoặc dữ liệu nhân vật đã lưu. Không khẳng định né hiện tại là lỗi công thức khi chưa đối chiếu dữ liệu gốc.
### Controls and Input Methods
Không thay input game (Both, luồng legacy hiện tại). Người dùng chỉnh trên web local rồi nhấn Lưu & áp dụng; không cần thêm Unity Services hoặc networking multiplayer.

## UI
Mở rộng mục Điều chỉnh game bằng form Chiến đấu riêng, giữ form rơi đồ:
```
Chiến đấu                       Revision / trạng thái đồng bộ
Hệ số điểm né nhân vật (%)       [100]
Hệ số chính xác quái (%)         [100]
Trần xác suất né (%)             [100]
Sát thương quái thực nhận (%)    [20]
[Khôi phục mặc định]             [Lưu & áp dụng]
Trạng thái: cấp / HP / điểm né gốc / điểm né hiệu lực
```
Giải thích: 20% sát thương tương đương giảm 80% hiện tại; 100% là bỏ mức giảm này, nhưng vẫn giữ kháng và giảm sát thương trang bị. Trần né chỉ giới hạn xác suất, không phải điểm né. Mặc định giữ hành vi hiện tại. Có hướng dẫn thử giảm hệ số né hoặc trần né và tăng sát thương; không áp dụng preset tự động. Nút mặc định chỉ đổi draft, cần Lưu để có hiệu lực. Hiển thị validation, đang lưu, lỗi, revision conflict và giá trị active.

## Key Asset & Context
- Assets/Server/World/LootRules.cs: mẫu immutable snapshot, JSON version, validation và ghi file thay thế; giữ nguyên schema/rules rơi đồ.
- Assets/Server/DevWiki/WikiControl.cs: GET /control/state và POST /control/rules; token, revision và nhật ký thay đổi. Thêm state và write route riêng cho combat, không phá client cũ.
- Assets/Server/DevWiki/WikiServer.cs: POST allowlist hiện chỉ cho /control/rules; thêm đúng /control/combat với cùng kiểm tra token, Host, Origin, Content-Type và body size. Không mở arbitrary write route.
- Assets/Server/Net/MobBrain.cs: Hit đang roll accuracy từ mob.Level*10+100 và dodge từ pl.Miss+100, rồi giảm 80% damage. Nối cấu hình tại đây.
- Assets/Server/Net/PlayerCombat.cs: Miss có nền Thân pháp*1.5 và bonus trang bị/kỹ năng. Giữ raw stat, áp dụng hệ số né tại Hit để không cần tái tính toàn bộ stats hoặc gửi lại protocol chỉ số.
- Assets/Server/World/World.cs: nơi giữ immutable CombatRules bên cạnh LootRules.
- Assets/Server/GameServer.cs: load combat config khi startup, kể cả release không có wiki.
- Assets/StreamingAssets/NsoWiki/app.js, app.css, index.html: web HTML/JS/CSS hiện có, không chuyển sang uGUI/UI Toolkit.
- Tạo Assets/Server/World/CombatRules.cs và Assets/Documentation/CombatBalanceConfiguration.md; test trong framework hiện có nếu có, nếu không dùng kiểm thử cô lập phù hợp, không thêm package tự động.
- File runtime mới: combat-rules.json dưới Config.DataDirectory. Không đổi game-rules.json hoặc save nhân vật.

## Implementation Steps
1. Audit lại dependencies bằng AssetDatabase.GetDependencies và tìm textual references vì công cụ graph có thể không có; đọc đầy đủ Hit, web form/submit, server startup và security handler trước sửa. Xác minh test framework. Dependency: không có.
2. Tạo CombatRules immutable, version 1, các trường nguyên dodgeScalePercent 0..500 (default 100), mobAccuracyScalePercent 0..500 (100), maxDodgeChancePercent 0..100 (100), mobDamagePercent 0..500 (20). Validate thiếu/extra keys, sai kiểu và ngoài range; hỗ trợ default khi thiếu file. Ghi file an toàn; lỗi load giữ default và báo warning không xóa file. Dependency: 1.
3. Thêm snapshot vào GameWorld và load trong GameServer.Start. Config dùng được trong release dù web chỉ chạy Editor/development build. Dependency: 2.
4. Tách hàm tính xác suất né kiểm thử được. Dùng cùng phân phối hai roll uniform hiện tại để tính xác suất exactly < miss, áp dụng hệ số lên điểm trước offset +100; giới hạn bởi maxDodgeChancePercent, rồi roll một lần. Dùng long/double và kiểm tra overflow; không roll riêng cap làm sai xác suất mục tiêu. Default phải khớp xác suất công thức cũ (không yêu cầu cùng chuỗi RNG). Áp mobDamagePercent sau kháng/giảm damage tại vị trí mức giảm 80% cũ; bảo toàn rounding 20% tương đương ceil(damage/5), miss không gây damage, 0% cho phép không mất HP, giá trị dương giữ damage tối thiểu 1 khi hit. Không đổi damage hiển thị/protocol trong phạm vi này, ghi rõ web là sát thương HP thực nhận. Dependency: 2–3.
5. Thêm GET /control/combat trả combat, defaults, revision và persistence; POST /control/combat nhận revision + combat. Revision riêng hoặc shared được ghi rõ và test, ưu tiên riêng để không xung đột form loot. Serialize update bằng changes lock; validate trước, persist trước rồi swap dưới World.SyncRoot. Nhật ký before/after; báo applied next monster hit. Không gọi Unity API từ HTTP thread, không sửa player records. Thêm raw/effective dodge vào runtime status phục vụ cân bằng. Dependency: 2–4.
6. Mở allowlist đúng combat route trong WikiServer, giữ tất cả security checks hiện tại. Dependency: 5.
7. Mở rộng trang settings của app.js với form chiến đấu riêng, fetch/save/default và xử lý lỗi; giữ loot form và dirty draft khi polling. Theo style app.css, chỉ sửa index.html nếu cần cấu trúc thực sự. Dependency: 5–6.
8. Tài liệu phạm vi từng field, mặc định, ví dụ tăng độ khó, đường dẫn file, hiệu lực runtime, web chỉ local/dev và thao tác rollback. Dependency: 4–7.
9. Compile và chạy tests dưới đây; không sửa scene/prefab, đồ họa, input hoặc các đợt overlay cũ trong plan này. Dependency: 1–8.

## Verification & Testing
- Default né đối chiếu công thức xác suất hai uniform với nhiều cặp level/Miss, bao gồm 0, cao, âm đã chuẩn hóa và giới hạn số nguyên. Monte Carlo kiểm tra trong tolerance thống kê; maxDodgeChancePercent không bị vượt.
- Hệ số né 0/50/100/500 và chính xác 0/100/500 ảnh hưởng đúng chiều; cap 0/40/100; không overflow hoặc Random.Next bound lỗi.
- Damage: default20 với 1/4/5/6/100 phải khớp logic cũ; 0/100/500, miss và kháng cao không heal nhân vật; đồ kháng vẫn có tác dụng.
- JSON round-trip, thiếu file, malformed/version/type/range/extra keys; lỗi ghi file không thay live snapshot. Không thay game-rules.json hoặc nhân vật.
- HTTP: GET state/default, POST hợp lệ, 400 invalid, 403 thiếu token/Origin sai, 409 stale revision, 500 persist failure; loot endpoint vẫn hoạt động, không mở write route khác.
- Hai tab, reset draft, save/reload web và restart game giữ đúng combat settings; update áp dụng cho lần hit tiếp theo mà không tạo stale stats.
- Kiểm thử runtime quái đánh nhân vật, HP mất và tử vong, với/không thức ăn để phân biệt hồi HP và né. Ghi rõ test nào chưa chạy; không tuyên bố fix hoàn tất chỉ vì compile.
- Giữ các sửa lỗi EXP khi chết và lưu thức ăn hiện có; regression thoát/vào, chết/hồi sinh.

## Phạm vi duyệt trước đây
Phần combat web và các bước overlay/Inspector trên là bối cảnh lịch sử; không tự triển khai lại trong đợt input dưới đây.

# Input System Migration — Current Approval Scope

# Project Overview
- Game Title: NSO / NinjaGame (retain existing title).
- High-Level Concept: Preserve the existing Java-port ninja client and embedded server while replacing deprecated gameplay input acquisition with Unity Input System.
- Players: Existing offline client with in-process server; no networking changes.
- Inspiration / Reference Games: Original NSO; existing controls are the baseline.
- Tone / Art Direction: Existing pixel art; no art or renderer changes.
- Target Platform: StandaloneWindows64.
- Screen Orientation / Resolution: Preserve existing window/fullscreen and zoom settings.
- Render Pipeline: URP, Unity 6000.6.8f1; installed Input System 1.19.0, URP 17.6.0, Test Framework 1.8.0.

# Game Mechanics
## Core Gameplay Loop
Keep movement, combat, target selection, menus, chat, terminal commands and server updates unchanged. Translate Input System events into the existing GameCanvas keyPressed/keyReleased and pointer methods; do not rewrite gameplay.
## Controls and Input Methods
Preserve MyKeyMap mappings, including Space/up jump, arrows, letter/digit shortcuts, F1–F3, Tab, Enter, Backspace, slash/keypad divide and Shift+2/minus special mappings. Preserve F6 settings, F9/F10 speed, F11/Alt+Enter fullscreen. Retain current gamepad Inspector mappings and world/menu behavior.
Use event-driven InputActions for keyboard bindings, pointer press/position/scroll and global hotkeys; use Keyboard.onTextInput and onIMECompositionChange for committed text and composition. Queue events and dispatch once per dynamic frame rather than in repeated OnGUI calls. Track the code sent on press so release remains correct after modifiers or context change. Release held controls on focus loss, disable and device removal; prevent keyboard/gamepad sources releasing a code still held by another source where shared ownership is required.

# UI
No menu/HUD redesign. Keep the existing IMGUI diagnostics and custom game renderer. Route diagnostic input ahead of gameplay when the F6 window is open, with one owner per event. Preserve chat printable-key suppression, Enter submission, navigation and Escape behavior. Composition remains visible in NsoTerminal without duplicate committed characters.
```
Keyboard / Mouse actions + text / IME events
              -> ordered input bridge
              -> diagnostics OR chat / GameCanvas
Existing gamepad -> preserved mappings / shared held-key ownership
EventSystem     -> InputSystemUIInputModule
```

# Key Asset & Context
- Assets/Scripts/Application/Main.cs: currently polls legacy mouse/text in Update and processes keys/pointer inside OnGUI. Remove checkInput acquisition from paint; retain Session_ME.update and render hooks unchanged.
- Assets/Scripts/Input/MyKeyMap.cs: explicit client key translation; retain existing API if referenced and add Input System mapping support rather than changing client key codes.
- Assets/Scripts/Input/NsoGamepadInput.cs: already Input System-based, runtime bootstrapped. Do not remap controller or add duplicate actions; minimally integrate shared release ownership if needed.
- Create Assets/Scripts/Input/NsoInputSystemBridge.cs and Assets/Input/NsoControls.inputactions (or equivalent action asset path following existing conventions after checking assets). One lifecycle owner; callbacks register/unregister safely and actions dispose/disable appropriately.
- Assets/Scripts/Rendering/mGraphics.cs and Assets/Scripts/Diagnostics/TextAlignPanel.cs: replace legacy mouse-position reads with bridge/Input System pointer state and null-device handling.
- Assets/Scripts/Diagnostics/TextCalib.cs, NsoSettingsMenu.cs, TextAlignPanel.cs: audit full diagnostic event handling and adapt key routing without redesigning diagnostics.
- Assets/Scripts/UI/Components/NsoTerminal.cs: replace Input.compositionString with bridge composition state.
- Assets/Scripts/UI/Components/Scroll.cs: preserve game-coordinate wheel behavior and update outdated comments. Verify new scroll-unit scaling against current list behavior.
- Assets/Prefabs/NSO Game.prefab (actual asset name includes a space) and Assets/Scenes/NinjaGame.unity: migrate StandaloneInputModule to InputSystemUIInputModule, wire action references and bridge once, preserve overrides.
- ProjectSettings/ProjectSettings.asset: activeInputHandler verified as 2 (Both); change to Input System only after migration tests. Input Manager axes may remain as unused rollback data.
- Assets/Scripts/Platform/GPS.cs: Input.location is a separate location service with no direct Input System equivalent; retain it, do not invent a replacement.
- Dependencies inspected with AssetDatabase.GetDependencies. Game prefab owns Main and StandaloneInputModule; dependents include NinjaGame.unity, testNj.unity and _Recovery/0.unity. Inspect test scene for active legacy modules before final switch; do not rewrite recovery assets automatically. Script dependency results are empty and do not prove absence of references: supplement with text/reference scans.
- Add migration documentation and focused tests within compatible existing test assemblies; inspect assembly definitions before adding tests, do not blindly reference Assembly-CSharp from a new asmdef.
- Reference: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/Migration.html (text events, IME, mouse APIs, location limitations).

# Implementation Steps
1. **Complete baseline and impact audit.** Read full diagnostic routing, chat/terminal field editing, GameCanvas key/pointer handling, existing action assets/settings and test assemblies. Inspect prefab/scene serialization and all runtime legacy APIs, including non-Scripts folders and aliases. Check dependencies/dependents before each edited asset. Record mappings, text order/repeat behavior and mouse wheel baseline. Dependencies: none.
2. **Implement action bridge and translation.** Create action asset and bridge; map all existing keyboard codes explicitly and capture modifiers at press time. Add pointer conversion preserving Screen.height Y inversion and mGraphics.zoomLevel, guarded against invalid zoom/no devices. Dispatch edge events once; continuous drag at most once per frame. Actions use dynamic update compatible with the current FixedUpdate gameplay loop; short taps remain visible to legacy logic. Dependencies: 1.
3. **Migrate text and diagnostic routing.** Subscribe to keyboard text/composition events with device-change cleanup. Preserve Unicode/Vietnamese, ordered Backspace replacement, repeat editing, slash-open behavior, Enter/escape/navigation; avoid action/text double delivery and accidental immediate submission. Verify actual Backspace behavior on Windows rather than assuming onTextInput includes it. Move diagnostic/global hotkey handling into a deterministic route; retain OnGUI for drawing. Dependencies: 2.
4. **Replace remaining reads and integrate sources.** Remove Main legacy acquisition and update hover/diagnostic pointer reads and terminal composition. Preserve gamepad mappings; coordinate shared held codes where necessary so source releases do not cause stuck/dropped movement. Release/cancel input on focus loss, device disconnect, scene teardown, chat/diagnostic context changes and component disable. Dependencies: 2–3.
5. **Wire prefab and UI module.** Update actual NSO Game prefab and main scene without duplicate bridge/EventSystem. Configure InputSystemUIInputModule actions explicitly; ensure UI navigation does not also dispatch duplicate game commands. Audit testNj scene and other active scene modules; any additional changes require verified relevance. Do not edit recovery copies, package source, renderer, server or combat settings. Dependencies: 4.
6. **Test while Both remains enabled, then retire legacy backend.** Compile and run focused tests first, scan for remaining legacy gameplay APIs, distinguish retained location API from backend-dependent controls. Switch Active Input Handling to Input System only via supported settings, allowing Editor restart if requested. Re-run tests after restart; no generated scripting defines or compatibility guard changes. Dependencies: 5 and successful preliminary regression checks.
7. **Documentation and verification report.** Document controls, ownership, text/IME caveats, action editing, restart requirement and rollback. Report test outcomes and tests not executed. Dependencies: 6.

# Verification & Testing
- Mapping tests cover every MyKeyMap key, modifier exceptions, matched release codes and aliases (Space/up). Simultaneous keyboard/gamepad holds must not release each other prematurely.
- Simulated action presses/releases: single delivery, tap between logic ticks, sustained movement, pointer drag/release, no duplicate callbacks across OnGUI Layout/Repaint, scroll sign/magnitude and hover coordinates across zoom/resize/fullscreen.
- Runtime controls: movement/jump, attack/interact, skills 7/8/9, target switching, menus/back, F6 diagnostic navigation/text drag, F9/F10 tick-rate ownership, F11 and Alt+Enter once per press.
- Text: chat/terminal open/close, slash command entry, Enter once, Backspace press/hold, cursor navigation, paste if currently supported, Unicode accented Vietnamese and real Unikey/Windows IME composition/commit/cancel. Confirm no duplicated deletion or committed character and no gameplay keys while typing.
- No keyboard/mouse/gamepad, hot-plug, focus loss/return, disable/re-enable, scene reload, repeated Play and Domain Reload on/off: no exceptions, stale composition, stuck keys or duplicate subscriptions.
- EventSystem/module serialization, prefab overrides, save/reopen scene and test scene compatibility; exactly one active input route per context.
- Verify activeInputHandler is Input System only after restart, compile cleanly and scan remaining Input references. Retained GPS location calls are documented exceptions, not unfinished gameplay migration.
- Windows development build smoke test if tooling available; manual hardware/IME checks are required and cannot be claimed solely from simulated tests.
- Rollback: revert migration scripts/action assets/prefab changes and restore Both in settings; preserve saves, Inspector configuration, gamepad mappings and server data.

## Current Approval Boundary
Only implement the Input System migration above. Earlier overlay, Inspector and combat-web plans are historical context. No new controls, art/UI redesign, package upgrades, server changes or performance claims.
