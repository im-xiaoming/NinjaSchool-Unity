# Cân bằng chiến đấu qua web local

Trong Play mode (Editor hoặc development build), mở web wiki của game, chọn **Điều chỉnh game → Chiến đấu**. Thay giá trị rồi nhấn **Lưu & áp dụng**. Cấu hình có hiệu lực từ lần quái đánh tiếp theo và được lưu vào `combat-rules.json` trong thư mục save cấu hình của server. Release vẫn đọc file này dù không chạy web wiki.

| Trường | Mặc định | Phạm vi | Ý nghĩa |
|---|---:|---:|---|
| dodgeScalePercent | 50 | 0–500 | Hệ số điểm né gốc, trước offset 100 trong phép tính xác suất |
| mobAccuracyScalePercent | 100 | 0–500 | Hệ số điểm chính xác theo cấp quái, trước offset 100 |
| maxDodgeChancePercent | 40 | 0–100 | Giới hạn xác suất né cuối cùng |
| mobDamagePercent | 150 | 0–500 | Hệ số HP thực mất sau kháng và giảm sát thương trang bị |

Mặc định giữ xác suất né; từ 2026-11-12 sát thương quái mặc định là 100% theo yêu cầu tăng độ khó. `20` tương đương giảm 80%; `100` bỏ mức giảm này. Damage nổi trên client bằng số HP server trừ sau khi áp dụng hệ số. `0` damage cho phép bất tử trước sát thương quái; `0` cap né khiến quái luôn trúng khi đã chọn mục tiêu hợp lệ. Hệ số né 0 không tự làm xác suất né bằng 0 vì còn offset nền; dùng cap 0 để tắt né hoàn toàn.

Muốn thử khó hơn: hệ số né 50, chính xác 100, cap né 40, damage 100. Đây chỉ là gợi ý, không tự áp dụng. Thay từng tham số rồi quan sát HP; thức ăn vẫn có thể hồi HP giữa các đòn đánh. Trạng thái web hiển thị né gốc và né hiệu lực, không ghi đè stat của nhân vật hoặc trang bị.

Nút **Mặc định** chỉ đổi các ô nhập, cần nhấn **Lưu & áp dụng**. Khi lỗi lưu hoặc xung đột phiên bản, server giữ cấu hình active; nhấn Làm mới để nhận bản mới. Combat và loot có revision riêng, không đổi `game-rules.json`.

## API
- `GET /control/state`: lấy token cục bộ hiện có.
- `GET /control/combat`: trả `revision`, `combat`, `defaults`, `persistence`.
- `POST /control/combat`: JSON `{ "revision": 0, "combat": { "dodgeScalePercent": 50, "mobAccuracyScalePercent": 100, "maxDodgeChancePercent": 40, "mobDamagePercent": 100 } }`.
- POST phải có `Content-Type: application/json` và `X-Nso-Control` bằng token. Giữ bảo vệ Host/Origin và giới hạn body của web local; không dùng endpoint làm public backend.

File có dạng `{ "version": 1, "combat": { ...bốn trường phía trên... } }`. Khi thiếu file dùng default; file lỗi giữ default và ghi cảnh báo, không xóa file. Không sửa file trực tiếp khi game đang chạy: dùng web, hoặc sửa khi game tắt rồi khởi động lại.

Phạm vi chỉ quái đánh nhân vật; không đổi EXP, PvP, kỹ năng, xác suất đồ rơi, input hoặc save nhân vật.


## 2026-11-12 - Harder default combat
User requested stronger monsters and lower player dodge. Defaults are dodgeScalePercent=50, mobAccuracyScalePercent=100, maxDodgeChancePercent=40, mobDamagePercent=150. This supersedes the older suggested values above: 150% is now the default damage scale. Saved explicit overrides still take priority. Effective dodge points are halved (not the final probability), with a 40% probability cap. Food and equipment recovery are retained.
