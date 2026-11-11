# Cấu hình NSO qua Inspector

Chọn đối tượng dưới **NSO Game** trong Hierarchy. Các config instance dùng private SerializeField; API property giữ tương thích. FormerlySerializedAs giữ dữ liệu tên field cũ trên prefab/scene.

| Object / component | Config | Áp dụng |
|---|---|---|
| NSO Game Control | UI Zoom (0 auto, 1–4); Fullscreen On Start | Play tiếp theo. Fullscreen chỉ Windows build; -windowed vẫn ưu tiên |
| NSO Game Control | Logic Ticks Per Second (10–60), Target Frame Rate (15–144), Run In Background | Inspector ưu tiên khi khởi động; có thể đổi lúc Play |
| NSO Game Control | Auto Reconnect | Tùy chọn client; không sửa gameplay server |
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
