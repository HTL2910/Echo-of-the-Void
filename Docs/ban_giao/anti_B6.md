# Bàn giao B6: Bộ UI Asset Fantasy Gothic Aether-punk Theme

- **Người làm:** anti
- **Ngày bàn giao:** 2026-09-26 (Sớm hơn hạn chót Phase 2 ETA 2026-10-03)
- **Ưu tiên:** B6 (UI Art expansion)

---

## 1. Danh sách Assets đã tạo & Bàn giao trong `Assets/Art/UI/`

Tất cả sprites xuất chuẩn định dạng PNG không nén, 16 PPU (Pixels Per Unit), Filter Mode: Point (no anti-aliasing / pixel-perfect), có sẵn meta file đã cấu hình thông số 9-slice borders (`spriteBorder`):

| File | Kích thước | Mô tả & Cấu hình 9-slice |
|------|------------|--------------------------|
| `panel_frame_neon.png` | 400×300 | Frame panel chủ đạo nền `#0A0E1F` (90% alpha), viền Cyan `#00D9FF` kèm góc ngoặc tech bracket Aether-punk (Border: 18, 18, 18, 18). |
| `panel_frame_dim.png` | 64×64 | Panel frame trạng thái Dim/Passive viền xám `#2A2F4F`, 9-slice (Border: 8, 8, 8, 8). |
| `panel_frame_cyan.png` | 64×64 | Panel frame trạng thái Prime viền Cyan `#00D9FF` + soft glow, 9-slice (Border: 8, 8, 8, 8). |
| `panel_frame_orange.png` | 64×64 | Panel frame trạng thái Echo viền Orange `#FF6B35` + soft glow, 9-slice (Border: 8, 8, 8, 8). |
| `button_states.png` | 200×192 | Sprite sheet 4 trạng thái button xếp dọc (200×48 mỗi state): Default, Hover, Pressed, Disabled. |
| `hud_bar_bg.png` | 200×20 | Khung nền rỗng của thanh HP/CE với viền `#2A2F4F` và nền `#0A0E1F`. |
| `hud_health_fill.png` | 200×20 | Dải màu gradient thanh máu (Cyan-Green `#00D9FF` -> `#28FF78`) kèm highlight mép trên. |
| `hud_energy_fill.png` | 200×20 | Dải màu năng lượng Chrono Energy kết hợp Prime Cyan (`#00D9FF`) và Echo Orange (`#FF6B35`). |
| `icon_frame.png` | 32×32 | Khung bọc icon kỹ năng viền tím Neon `#9D4EDD` + 4 điểm tán xạ góc. |
| `dialogue_box.png` | 800×200 | Khung hội thoại nền 95% opacity, viền trên sáng Cyan 3px, tích hợp sẵn hộp tên nhân vật (140×30) ở góc trên bên trái. |
| `menu_background.png` | 960×540 | Background menu gradient mượt từ `#0A0E1F` sang `#1A0E2F` kèm lớp dither noise 2% tạo chiều sâu điện tử. |
| `color_palette_reference.png` | 600×360 | Bảng màu trực quan đối chiếu 6 mã màu chuẩn Aether-punk. |

---

## 2. Trả lời trực tiếp 4 câu hỏi từ Spec (Q&A for Anti)

1. **Q1: Can you find stock Fantasy Gothic UI assets, or should you create custom pixel art?**
   - **Trả lời:** Đã tự tạo bộ **Custom Pixel Art đồng bộ 100%** với palette màu và phong cách Void/Aether-punk của Kael (`Assets/Art/Sprites/Kael/`). Stock UI trên mạng thường bị lệch style, không đồng nhất tỷ lệ 16 PPU và không có phối màu nhị nguyên Prime (`#00D9FF`) vs Echo (`#FF6B35`).
2. **Q2: Do you want to provide a Krita/Aseprite project file for continued iteration, or finalized PNGs only?**
   - **Trả lời:** Đã cung cấp **Finalized PNGs kèm script sinh tự động** tại `scratch/generate_ui_assets.py` (dùng procedural vector + pixel processing). Bất cứ lúc nào cần đổi kích thước, tăng độ dày viền hay đổi glow intensity đều có thể re-generate trong 1 click.
3. **Q3: Glow effect: prefer shader-based (post-process bloom) or sprite-based (pre-rendered glow)?**
   - **Trả lời:** **Kết hợp hybrid:** Viền sprite đã được vẽ sẵn 1 lớp 1-2px subtle glow viền để trông sắc nét ở mức cơ bản (kể cả khi tắt bloom). Để tạo hiệu ứng glow tương tác cao (pulse khi năng lượng đầy, boss bar flash), Claude nên dùng UGUI Canvas Shader hoặc Bloom Post-Process của Volume sẵn có trong project (`Assets/DefaultVolumeProfile.asset`).
4. **Q4: Timeline: can you deliver Phase 2 assets by 2026-09-30 so Claude can integrate into L5 visual polish?**
   - **Trả lời:** **ĐÃ GIAO XONG NGAY HÔM NAY (2026-09-26)**, sớm hơn 4 ngày so với mốc 30/09 để Claude bắt tay tích hợp ngay vào `PlayerHUD.cs` và `DialogueDisplayUI.cs`.

---

## 3. Hướng dẫn tích hợp cho Claude

1. **Font:**
   - 2 font `Orbitron-Variable.ttf` (cho số liệu HUD, Button label, Tên Boss) và `SpaceMono-Regular.ttf` (cho text hội thoại, Lore Monolith) đã nằm sẵn trong `Assets/Fonts/` kèm license OFL.
2. **Button UGUI:**
   - Cắt sprite sheet `button_states.png` (200×48 cho mỗi trạng thái: Default ở y=[144..191], Hover ở y=[96..143], Pressed ở y=[48..95], Disabled ở y=[0..47] theo hệ toạ độ Unity sprite).
3. **Thanh HUD:**
   - Gắn `hud_bar_bg.png` làm Background và `hud_health_fill.png` / `hud_energy_fill.png` làm Image Type = `Filled` (Horizontal) trong component `Image` của UGUI.
