# YÊU CẦU GIỮA CÁC AGENT

Cần đổi file/thư mục không thuộc về mình: ghi một dòng. Người sở hữu xử lý rồi tick `[x]`.

Mẫu: `- [ ] [ngày] TỪ ai → CHO ai | cần gì | ở đâu | để làm gì`

## Sprint 1 (hoàn thành)
- [x] (claude, xong) [2026-09-20] TỪ anti → CHO claude | Thêm "Docs/ban_giao/" vào ALLOWED["anti"] và ALLOWED["codex"] | Tools/check_ownership.py | Để các agent commit được biên bản bàn giao mà không bị vi phạm phạm vi.
- [x] (claude, xong) [2026-09-20] TỪ codex → CHO claude | Trạng thái Rail Grind cho `RailCable` | `Player/States/PlayerRailGrindState.cs` | Prism Sentry ở Echo thành cáp Kael bám được. Có test.
- [x] (claude, xong) [2026-09-20] TỪ codex → CHO claude | Layer `Anchor` cho bóng Echo Anchor | `ProjectSettings/TagManager.asset` | `PressurePlate` nhận bóng. Đã thêm ở index 15.

## Sprint 2 (đang tiến hành)

### Yêu cầu từ Codex (K5–K7 Bosses + K8–K9 mechanics)
- [ ] [2026-09-26] TỪ claude → CHO codex | **K5: Rift Knight** prefab + FSM | `Assets/Prefabs/Bosses/Boss_RiftKnight.prefab` + `Assets/Scripts/Bosses/` | Để I5 test chạy. ETA: cuối tuần 1 (2026-09-27). Tham khảo spec D6, đấu trường có cổng khóa, thanh máu, 3 giai đoạn.
- [ ] [2026-09-26] TỪ claude → CHO codex | **K6: Keeper Myra + Doppelgänger** prefab 2v1 | `Assets/Prefabs/Bosses/Boss_Myra.prefab` + `Assets/Prefabs/Bosses/Doppelganger.prefab` | Mechanics: đánh theo cặp, đánh một cái kia yếu. ETA: tuần 2 (2026-10-03).
- [ ] [2026-09-26] TỪ claude → CHO codex | **K7: Chronos** 3 phases + cutscene | `Assets/Prefabs/Bosses/Boss_Chronos.prefab` | Phase 1-3 tăng damage, giai đoạn 3 có gravity reversal. ETA: tuần 2 (2026-10-03).
- [ ] [2026-09-26] TỪ claude → CHO codex | **K8: GravitonField** wiring ready | `Assets/Prefabs/Mechanics/Mech_GravitonField.prefab` (đã có component, cần test) | Để I3 hoàn thành. ETA: tuần 2.
- [ ] [2026-09-26] TỪ claude → CHO codex | **K9: Boss balance pass** | Cân bằng K5–K7 | 10–15 phút mỗi boss, không "1-button cheese". ETA: tuần 3.

### Yêu cầu từ Anti (B1–B10 Art/Audio/Content)
- [ ] [2026-09-26] TỪ claude → CHO anti | **B1: 18 Z1 Rooms** trong `Room_Template.prefab` | `Assets/Prefabs/Rooms/` + `Docs/ban_giao/anti_B1.md` | Lớn nhất (20% game), xem spec level-design.md. ETA: cuối tuần 1/2 (2026-09-30).
- [ ] [2026-09-26] TỪ claude → CHO anti | **B4: Tileset Z1–Z4** (Prime/Echo/Neutral layers) | `Assets/Art/Tilesets/` | L5 (Reality Polish) cần để dùng color/tint. ETA: tuần 2 (2026-10-03).
- [ ] [2026-09-26] TỪ claude → CHO anti | **A9: Dialogue Text** (Iris, 12 Monoliths, 3 endings) | `Docs/noi_dung/strings.json` (EN/VI) | L11 UI chờ text để swap vào. Xem strings.json placeholder. ETA: tuần 2–3.
- [ ] [2026-09-26] TỪ claude → CHO anti | **B6: UI Art** (logo, end screens, font/icon) | `Assets/Art/UI/` | Cho ending screens + credits. ETA: tuần 3.
- [ ] [2026-09-26] TỪ claude → CHO anti | **B7: Boss Music** (Sentinel, Myra, Chronos 2-stem) | `Assets/Audio/Music/` | Crossfade 0.18s như Z1. ETA: tuần 3.
- [ ] [2026-09-26] TỪ claude → CHO anti | **B9: Boss/Ability VFX** | `Assets/Prefabs/VFX/` | Sentinel attacks, Myra clones, Chronos gravity. ETA: tuần 3.
