# KỸ THUẬT: QUY CHUẨN SPRITE SHEET HD & ANIMATION PIPELINE

## 1. Mục tiêu kỹ thuật
- Chuẩn hóa các Sprite Sheet độ nét cao (HD Sprite Sheets) vừa được tạo:
  1. `spr_kael_sheet_HD.png` (4x4 frames: Idle, Run, Jump/Fall, Attack)
  2. `spr_enemies_sheet_HD.png` (Chrono Crawler, Void Weaver, Void Strider)
  3. `spr_sentinel01_sheet_HD.png` (Sentinel-01 Boss 9 frames)
  4. `tileset_z1_HD.png` (Tileset sàn, tường, cơ chế)
  5. `hud_elements_HD.png` (Thanh máu, năng lượng, icon kỹ năng)
- Đảm bảo **Pixels Per Unit (PPU)** đồng nhất, Pivot chính xác ở chân (bottom-center) hoặc tâm (center), không bị rung giật (jittering) khi chuyển trạng thái animation.

---

## 2. Thông số kỹ thuật Sprite Import & Slicing

| Asset | Kích thước Texture | Cell Size | Sliced Frames | PPU | Pivot | Filter Mode |
|---|---|---|---|---|---|---|
| `spr_kael_sheet_HD.png` | 1024 × 1024 | 256 × 256 | 16 frames (4x4) | **64** | Bottom-Center `(0.5, 0.0)` | Point (no filter) |
| `spr_enemies_sheet_HD.png` | 1024 × 1024 | 256 × 256 | 16 frames | **64** | Bottom-Center `(0.5, 0.0)` | Point |
| `spr_sentinel01_sheet_HD.png` | 1536 × 1536 | 512 × 512 | 9 frames (3x3) | **64** | Bottom-Center `(0.5, 0.0)` | Point |
| `tileset_z1_HD.png` | 1024 × 1024 | 128 × 128 | 64 tiles (8x8) | **64** | Center `(0.5, 0.5)` | Point |
| `hud_elements_HD.png` | 1024 × 1024 | Tùy biến (Rect) | UI Icons & Bars | **100** | Center `(0.5, 0.5)` | Bilinear |

> [!NOTE]
> **PPU = 64:** Do cell size của Kael là 256x256 px, với PPU = 64, kích thước nhân vật trong Unity world sẽ là chính xác `256 / 64 = 4 units` (hoặc scale về 1.8 - 2.0 units bằng `Visual.localScale` hoặc tinh chỉnh PPU = 128 tùy theo visual mong muốn).

---

## 3. Mapping Frame Kael (`spr_kael_sheet_HD`) vào Animation Clips

Lưới 4 hàng × 4 cột (chỉ mục frame từ `0` đến `15`):

```
Row 0 (Frames 0..3)   : [Idle 0]     [Idle 1]     [Idle 2]     [Idle 3]
Row 1 (Frames 4..7)   : [Run 0]      [Run 1]      [Run 2]      [Run 3]
Row 2 (Frames 8..11)  : [Jump Start] [Jump Apex]  [Fall 0]     [Land 0]
Row 3 (Frames 12..15) : [Attack1]    [Attack2]    [Attack3]    [Dash/Cast]
```

### Chi tiết gán vào AnimationClip:
- `Kael_Idle.anim`: Frame 0 -> 1 -> 2 -> 3 (tốc độ: 6 fps, loop)
- `Kael_Run.anim`: Frame 4 -> 5 -> 6 -> 7 (tốc độ: 10 fps, loop)
- `Kael_Jump.anim`: Frame 8 -> 9 (tốc độ: 12 fps, clamp)
- `Kael_Fall.anim`: Frame 10 (1 frame giữ nguyên khi rơi)
- `Kael_Land.anim`: Frame 11 (thời lượng 0.08s)
- `Kael_Attack1.anim`: Frame 12 (thời lượng 0.15s)
- `Kael_Attack2.anim`: Frame 13 (thời lượng 0.15s)
- `Kael_Attack3.anim`: Frame 14 (thời lượng 0.20s)
- `Kael_Dash.anim`: Frame 15 (thời lượng 0.20s)

---

## 4. Pipeline Tự động hóa qua Editor Script (`SpriteSheetImporterUtility.cs`)

Để tránh việc phải kéo thả bằng tay từng frame trong Unity Editor gây sai sót:
1. Script đọc TextureImporter của `spr_kael_sheet_HD.png`.
2. Tạo SpriteMetaData cho lưới 4x4, đặt PPU = 64/128, pivot = BottomCenter.
3. Sinh hoặc cập nhật các `AnimationClip` trong `Assets/Art/Animations/Kael/` với đúng GUID/Sprite references.
4. Gán sprite mặc định frame 0 vào `SpriteRenderer` của prefab `Player.prefab`.

---

## 5. Quy tắc kiểm thử
- `PlayerAnimationTests.cs`:
  - Verify Animator Controller không bị missing clip nào.
  - Verify `Player.prefab` child `Visual` có `SpriteRenderer` gắn sprite hợp lệ (không null).
  - Verify Collider của Kael không bị lệch tâm so với sprite ở mọi trạng thái Idle/Run/Jump.
