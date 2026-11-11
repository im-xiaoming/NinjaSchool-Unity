# NSO Offline (Unity 6)

Bản offline của game NSO chạy trên Unity 6 (`6000.6.8f1`). Client là code game gốc đã chuyển sang Unity. Server được
viết lại bằng C# từ server Java gốc và chạy ngay trong game (trao đổi qua bộ nhớ, không cần mạng hay MySQL).

## Chạy game
1. Mở project bằng Unity `6000.6.8f1`.
2. Mở scene `Assets/Scenes/NinjaGame.unity` rồi bấm **Play**.
3. Server tự khởi động cùng game và tự đăng nhập vào tài khoản cục bộ. Tạo nhân vật, vào **Làng Tone**, gặp **Tajima**
   để bắt đầu chuỗi nhiệm vụ.

Save nằm trong `persistentDataPath/NsoData`. Nút mở thư mục và xóa save có trên object **NSO Server Control**.

## Thư mục chính
| Thư mục | Nội dung |
|---|---|
| `Assets/Scripts` | Client: màn hình, gameplay, vẽ, âm thanh, input, cấu hình Inspector |
| `Assets/Server` | Server C# (port từ Java): đăng nhập, bản đồ, chiến đấu, kỹ năng, vật phẩm, cửa hàng, nhiệm vụ, NPC |
| `Assets/Server/DevWiki` | Website tra cứu và API (xem bên dưới) |
| `Assets/StreamingAssets/NsoStatic` | Dữ liệu tĩnh của server (`tables.txt`, `Data/`) |
| `Assets/StreamingAssets/NsoWiki` | Trang web (`index.html`) và ghi chú viết tay (`notes.json`) của dev wiki |
| `Assets/Documentation` | Tài liệu chi tiết |
| `Tools/ServerTests` | Test tự động cho server (chạy bằng `dotnet`, không cần Unity) |
| `NsoWiki/db.json` | Bản xuất toàn bộ cơ sở dữ liệu tra cứu (tự ghi mỗi lần Play trong Editor) |

## NSO Workbench: điều chỉnh, theo dõi và tra cứu

Mở **http://127.0.0.1:8787/#overview** khi game chạy. Các mục chính:
- **Tổng quan:** nhân vật, HP/MP thật, map/khu, nhiệm vụ, tiến độ và túi đồ; cập nhật mỗi 2 giây.
- **Điều chỉnh game:** bật/tắt chặn đồ từ bù nhìn, chặn quái quá thấp cấp và đổi ngưỡng 10 cấp. Luật đồ thường Java (mặc định chênh tối đa 7 cấp) có công tắc và ngưỡng riêng.
- **Kiểm tra rơi đồ:** nhập cấp, quái và nhiệm vụ/bước để xem nguyên nhân bị chặn, nguồn đồ nhiệm vụ và tỉ lệ đồ thường. Công cụ chỉ giải thích, không tạo đồ.
- **Tra cứu:** bộ lọc, chi tiết nhiệm vụ từng bước, liên kết NPC/quái/vật phẩm/map, ghim, xem gần đây, sao chép và xuất JSON. Link cũ như `#quests/2` vẫn hoạt động.
- **Nhật ký cấu hình:** các lần lưu trong phiên. **API & chẩn đoán:** thử truy vấn chỉ đọc.

Đổi luật tại **http://127.0.0.1:8787/#settings**, rồi nhấn **Lưu & áp dụng**. Sau xác nhận thành công, luật mới được dùng cho lần rơi đồ tiếp theo. Cấu hình lưu nguyên tử trong `game-rules.json` dưới thư mục save server (mặc định `persistentDataPath/NsoData`), và được đọc lại khi mở game, kể cả bản release không chạy web. Nút **Mặc định** chỉ điền biểu mẫu, chưa lưu.

Tắt chặn “thấp hơn 10 cấp” không tự tắt luật đồ thường 7 cấp. Bộ kiểm tra giúp phân biệt hai luật. Thay đổi trên web không làm đổi tỉ lệ rơi đồ, damage, cooldown hoặc yêu cầu nhiệm vụ.

Website tra cứu **chỉ dành cho debug**: chỉ có trong Editor và development build (bản release không chứa), chạy trên luồng riêng
(`NsoDevWiki`) nên không ảnh hưởng luồng chính của game. Khi game chạy, website khởi động cùng game tại
**http://127.0.0.1:8787/**. Website chỉ mở được trên máy này; nếu cổng bận, game thử 9 cổng tiếp theo. Console in ra
`[NsoWiki] Dev wiki: <địa chỉ>`, và object **NSO Server Control** có nút **Open dev wiki in browser**.

Dữ liệu được tổ chức dạng NoSQL: mỗi collection là một danh sách bản ghi, mỗi bản ghi có `id`.

| Nhóm | Collection | Nguồn |
|---|---|---|
| Dữ liệu game | `quests`, `npcs`, `maps`, `mobs`, `items`, `shops`, `skills`, `effects`, `itemOptions`, `classes` | Tự sinh từ `tables.txt` và code server mỗi lần chạy, luôn khớp với server |
| Dự án | `features`, `constraints`, `commands`, `config`, `deviations`, `issues`, `tests` | Viết tay trong `Assets/StreamingAssets/NsoWiki/notes.json` |

Trạng thái (`status`): `done` đã port, `partial` một phần, `todo` chưa làm, `skip` bước nhiệm vụ được bỏ qua ở bản offline.

**Tìm trên trang:**
- Gõ không cần dấu: `kiem go`.
- Lọc theo trường: `id:194`, `status:todo`, `giverNpc:12`, `area:"Vật phẩm"`, `maps:"lang tone"` (tìm cả trong danh sách con).
- Mỗi bản ghi có link riêng, ví dụ `#items/194` hoặc `#quests/12`, dán vào ghi chú bug được.
- Sửa `notes.json` xong thì bấm **Tải lại dữ liệu**.

**API JSON cho script và AI agent:**
| Request | Kết quả |
|---|---|
| `GET /api` | Danh sách collection, số bản ghi, tên trường, cú pháp truy vấn |
| `GET /api/search?q=tajima` | Kết quả trong mọi collection |
| `GET /api/npcs?status=todo&fields=id,name` | Lọc (`q=`, `<trường>=<giá trị>`), chọn trường, phân trang (`limit`, `offset`) |
| `GET /api/quests/2` | Một bản ghi |

**Khi không mở Unity:**
- Đọc thẳng `NsoWiki/db.json`.
- Dựng lại file đó: `dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- wiki`
- Dựng lại và chạy website ở cổng 8790: `dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- wiki serve 8790`

Cấu hình trên **NSO Server Control**: `_wikiEnabled` (bật/tắt), `_wikiPort` (mặc định 8787), `_wikiOpenBrowser` (tự mở
trình duyệt). Nếu wiki lỗi, game chỉ hiện cảnh báo và vẫn chạy bình thường.

Khi port thêm tính năng: cập nhật `notes.json` (features, constraints, commands...). Trạng thái tự sinh (nhiệm vụ, NPC,
kỹ năng, bản đồ) được tính trong `Assets/Server/DevWiki/WikiData.cs`.

## Server: đã làm và chưa làm
Đã port:
- Đăng nhập, tạo và chọn nhân vật; vào map, di chuyển, đổi map và khu; chết và hồi sinh.
- Quái tấn công; đánh quái, chí mạng, né, exp, lên cấp.
- Kỹ năng: học sách, cộng điểm, chọn chiêu. Hiệu ứng lên quái: bỏng, đóng băng, choáng, trói.
- Túi đồ, trang bị, thuốc, thức ăn, cửa hàng (xu / yên), rơi đồ và nhặt đồ.
- Hồi HP/MP theo thời gian.
- Nhiệm vụ chính 0–42 và menu NPC làng, trường.

Chưa làm: kỹ năng buff, nâng cấp / khảm / tách đồ, rương đồ, thú cưỡi, thú nuôi, map đặc biệt (hang động, chiến
trường, sự kiện), bạn bè / nhóm / giao dịch / gia tộc, PK.

Khác bản gốc (có chủ đích):
- Không tự hồi HP/MP khi không có nguồn hồi phục (`PlayerEffects.NaturalRegenPercent = 0`). Thức ăn, thuốc và option trang bị vẫn hồi như bình thường.
- Bước nhiệm vụ chưa port được cơ chế có mục **"Bỏ qua: <bước>"** ở NPC giao nhiệm vụ.
- Không có lượng.

Chi tiết từng phần nằm trong dev wiki (collection `features`) và `Assets/Documentation/ServerPort.md`.

## Âm thanh menu
Nhạc màn hình menu (`res/music/0.ogg`, dài khoảng 3,7 giây) phát **một lần** mỗi khi vào menu. Chuyển giữa các màn
menu thì không phát lại; từ game quay về menu thì phát lại. Đặt `MenuAudio.menuMusicLoop = true`
(`Assets/Scripts/Audio/MenuAudio.cs`) nếu muốn lặp lại. Tắt nhạc menu và âm nút bấm trong Cài đặt (F6) > Âm thanh.

## Test server
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- questcheck
```
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- progresscheck
```
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- combatcheck
```
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj"
```
Bộ test đầy đủ (lệnh cuối) có 2 test đăng nhập luôn fail, đây là dự kiến vì bản offline luôn đăng nhập vào một tài khoản
cục bộ.

## Tài liệu khác
- **Bàn giao giữa các agent:** đọc `AGENTS.md` trước, rồi `PROJECT_STATUS.md`, `TASKS.md`, `DECISIONS.md`, `HANDOFF.md`, `docs/ARCHITECTURE.md`, `docs/TESTING.md`.
- `Assets/Documentation/ServerPort.md`: bảng port Java → C#, NPC đã port, test.
- `Assets/Documentation/InspectorConfiguration.md`: cấu hình qua Inspector.
- `Assets/Documentation/UiReference.md`: tên nút, biến và giá trị chỉnh giao diện.
- `Assets/Documentation/UnityMapOverlayBridge.md`: đặt object Unity lên map của game.
