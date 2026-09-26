# YÊU CẦU GIỮA CÁC AGENT

Cần đổi file/thư mục không thuộc về mình: ghi một dòng. Người sở hữu xử lý rồi tick `[x]`.

Mẫu: `- [ ] [ngày] TỪ ai → CHO ai | cần gì | ở đâu | để làm gì`
- [x] (claude, xong) [2026-09-20] TỪ anti → CHO claude | Thêm "Docs/ban_giao/" vào ALLOWED["anti"] và ALLOWED["codex"] | Tools/check_ownership.py | Để các agent commit được biên bản bàn giao mà không bị vi phạm phạm vi.
- [x] (claude, xong) [2026-09-20] TỪ codex → CHO claude | Trạng thái Rail Grind cho `RailCable` | `Player/States/PlayerRailGrindState.cs` | Prism Sentry ở Echo thành cáp Kael bám được. Có test.
- [x] (claude, xong) [2026-09-20] TỪ codex → CHO claude | Layer `Anchor` cho bóng Echo Anchor | `ProjectSettings/TagManager.asset` | `PressurePlate` nhận bóng. Đã thêm ở index 15.

## Giai đoạn 2: Z2 (Tháp đồng hồ) & Z3 (Rừng pha lê)
- [x] (anti/claude, xong) [2026-09-26] TỪ codex → CHO claude | Cung cấp prefab `Pickup_EchoAnchor` | `Assets/Prefabs/Environment/Pickup_EchoAnchor.prefab` | Làm phần thưởng sau khi hạ boss Mirror Doppelganger (K6) tại Zone 3. Đã tạo đầy đủ prefab + meta (PersistentId, AbilityPickup=16, Visual icon, Aura & Light2D).
- [x] (claude, đã có sẵn) [2026-09-26] TỪ codex → CHO claude | Hook kiểm tra đòn Resonance Strike trong `IDamageable`/`DamageInfo` | `Assets/Scripts/Combat/IDamageable.cs` | Đã có trường `info.IsResonance` (bool) trong `DamageInfo` được `PlayerCombat.cs` truyền `true` khi dùng ResonanceStrike.

- [x] (anti, xong) [2026-09-26] TỪ claude/game_design → CHO anti | Bộ UI Asset Fantasy Gothic Aether-punk Theme (B6) | `Assets/Art/UI/` | Thay thế UGUI trắng/xám bằng giao diện phong cách neon void (Cyan `#00D9FF` + Orange `#FF6B35` + Dark `#0A0E1F`). Đã bàn giao 12 asset + meta + bảng màu tại `Docs/ban_giao/anti_B6.md`.
- [ ] [2026-09-26] TỪ codex → CHO anti | Kích thước và thông số Visual cho Rift Knight (K5) và Boss Z2 Myra & Z3 Doppelganger (K6) | `Assets/Art/Sprites/Enemies/` & `Bosses/` | Gắn vào child `Visual` của prefab không làm lệch root scale và collider.
- [ ] [2026-09-26] TỪ claude → CHO anti | Bổ sung danh sách Room ID và layout kết nối cho Z2/Z3 | `Docs/noi_dung/z2_z3_map.md` | Nạp vào `MapDefinitionData` để hoàn thiện bản đồ Blueprint Fullscreen/Minimap (L10).

