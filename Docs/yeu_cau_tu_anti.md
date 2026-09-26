# YÊU CẦU ĐỔI CODE TỪ ANTI

Mỗi dòng một yêu cầu: `[ngày] cần gì | ở đâu | để làm gì`. Claude xử lý theo thứ tự và đánh dấu `[x]`.

- [x] (Claude, đã gắn: `VfxLibrary`, slash/impact/dust/shockwave/enemy death; đã có test) [2026-09-20] Gắn VFX prefabs (`Assets/Prefabs/VFX/`) | `PlayerCombat.cs`, `EnemyControllerBase.cs` | Hiện Kael và quái đánh nhau chưa kích hoạt particle VFX (SlashArc Prime/Echo, ShiftWave, Impact Clean/Deflect, EnemyDeath).
- [x] (Claude, xong: `MusicLayerController`, crossfade equal-power 0.18 s; **đang im lặng vì `Assets/Audio/Music/` trống**, xem `task_antigravity.md` A5) [2026-09-20] Hỗ trợ phát nhạc 2 stem crossfade | `AudioManager.cs` | Đã tạo `Assets/Audio/Music/MUS_Z1_Prime.ogg` và `MUS_Z1_Echo.ogg` (110 BPM, đồng độ dài mẫu), cần 2 AudioSource song song crossfade theo `RealityEventBus.OnRealmSwitched`.
- [x] [2026-09-24] **Slice sprite HD sheets trong Sprite Editor** | `Assets/Art/Sprites/Kael/spr_kael_sheet_HD.png` + enemies/boss/tileset/HUD | Đã cấu hình sub-sprites 4x4 grid (256x256), cập nhật SceneGenerator.cs và Player.prefab sang HD sheet.
- [ ] [2026-09-20] Import TMP Essentials & bake TMP Font Assets | `Assets/Fonts/` | Orbitron (cho số, tiếng Anh) và Space Mono (cho tiếng Việt HUD) để HUD hiển thị chuẩn Aether-punk (tùy chọn, hiện dùng UGUI Text chuẩn).
- [x] (Claude, xong) [2026-09-20] Gắn icon kỹ năng vào HUD | `Assets/Art/UI/Icons/` | Đã nạp 4 icon kỹ năng vào PlayerHUD (SceneGenerator.cs) và 2 icon MapLock/ChronoStation vào MapIconProvider.

