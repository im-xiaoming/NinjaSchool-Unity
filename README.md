# NSO Offline (Unity 6)

Bản offline của NSO trên Unity `6000.6.8f1`. Server C# (port từ Java) chạy ngay trong game, không cần mạng.

## Chạy
Mở scene `Assets/Scenes/NinjaGame.unity` → **Play**. Mọi cấu hình nằm trên object **NSO Server Control**.

## Dữ liệu
| | Vị trí |
|---|---|
| Save (SQLite) | `persistentDataPath/NsoData/nso.db` (tắt `_useSqlite` để dùng TSV) |
| Log mỗi phiên Play (SQLite) | `Logs/PlayerLog/player_log.db` |
| Dữ liệu tĩnh | `Assets/StreamingAssets/NsoStatic` |

## Người chơi AI
13 nhân vật AI chơi như người (tài khoản riêng, theo đúng luật, tự làm nhiệm vụ). LLM tùy chọn: cài [Ollama](https://ollama.com),
`ollama pull qwen3.5:4b`; không có LLM thì dùng luật có sẵn. Xem: `http://127.0.0.1:8787/agents.html`. Log: bảng `bot_entries`
trong `Logs/PlayerLog/player_log.db` (giữ 20 ngày).

## Dev wiki (chỉ Editor / dev build)
`http://127.0.0.1:8787/` khi game chạy: trạng thái nhân vật, chỉnh luật rơi đồ, tra cứu item/quest/NPC/map.
API: `GET /api`, `/api/search?q=...`, `/api/<collection>/<id>`. Không mở Unity thì đọc `NsoWiki/db.json`.

## Test
```bash
dotnet run --project "C:/My project/Tools/ServerTests/ServerTests.csproj" -- combatcheck   # hoặc questcheck, progresscheck, wiki
```

## Tài liệu
Agent đọc `AGENTS.md` trước. Chi tiết: `PROJECT_STATUS.md`, `HANDOFF.md`, `DECISIONS.md`, `docs/`, `Assets/Documentation/`.


## RL learning (development tools)
Open `/agents.html` during Play. Rules initially teach the two RL policies; the trainer uses the uv-managed CUDA environment. Human-action capture and long isolated evaluation are opt-in. Models only take control after evaluation; no pre-trained strong model is bundled. Setup, defaults, commands and limitations: [docs/AI_LEARNING.md](docs/AI_LEARNING.md).
