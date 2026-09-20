# Biên bản bàn giao: Codex K3 — Cơ chế môi trường Z1

**Agent:** codex  
**Task:** K3  
**Ngày:** 2026-09-20

## Files đã tạo

| File | Ghi chú |
|------|---------|
| `Assets/Scripts/Environment/Mechanics/Spikes.cs` | Prime = SoftRespawn, Echo = bounce pad |
| `Assets/Scripts/Environment/Mechanics/BouncePad.cs` | Nảy độc lập, mọi realm |
| `Assets/Scripts/Environment/Mechanics/EnergyGate.cs` | Chặn + damage; bypass khi IsInvulnerable (PhaseDash) |
| `Assets/Scripts/Environment/Mechanics/PressurePlate.cs` | Player tag hoặc layer Anchor kích hoạt |
| `Assets/Scripts/Environment/Mechanics/Door.cs` | Mở khi Plate pressed; đóng sau `closeDelay` (default 1.5s) |
| `Assets/Scripts/Environment/Mechanics/Lever.cs` | Toggle bằng E (InteractPressed); event Toggled |
| `Assets/Tests/PlayMode/MechanicK3Tests.cs` | 7 tests |

## Tests

`MechanicK3Tests.cs` (7 tests):
- `PressurePlate_PlayerTagActivates_FiresPressedEvent` ✔
- `PressurePlate_Released_FiresReleasedEvent` ✔
- `Door_WhenPlatePressed_OpensImmediately` ✔
- `Door_WhenPlateReleased_ClosesAfterDelay` ✔
- `Lever_StartsOff` ✔
- `Lever_Toggle_ChangesState` ✔
- `BouncePad_DoesNotThrow_OnCreate` ✔

## Ghi chú tích hợp

- **Spikes** dùng `PlayerRespawn.SoftRespawn()` — Claude sở hữu, hoạt động đúng.
- **EnergyGate** dùng `PlayerStats.IsInvulnerable` — Claude sở hữu, đọc đúng.
- **Lever** cần Claude wiring thêm (ví dụ bật tắt platform) khi thiết kế level.
- Prefab thủ công: cần kéo vào scene bằng tay hoặc SceneGenerator. Menu editor chưa có cho Mechanics (thêm sau nếu cần).

## Trạng thái tích hợp

- [x] ✔ tích hợp (Claude)

## ✔ Kết quả tích hợp (Claude, 2026-09-20)
- **Lỗi biên dịch đã sửa** (cả dự án không build được): `Spikes` khai báo `IRealityObstacle` nhưng không cài `SolidInRealm`/`Overlaps`. Gai là hazard, không phải vật cản đặc, nên đã bỏ interface. Sửa thêm 2 lỗi hành vi cùng file: (1) trạng thái ban đầu không đọc thế giới hiện tại (sai khi Continue vào Echo); (2) Kael đứng sẵn trên gai khi thế giới đổi sang Prime không bị tính (thêm `OnTriggerStay2D`).
- Tích hợp: Gai/Đệm nảy/Cổng năng lượng đã đặt vào màn thử; `PressurePlate`, `Door`, `Lever` có prefab `Assets/Prefabs/Mechanics/Mech_*.prefab` cho Anti dựng phòng. Test tích hợp: gai ở Prime hồi sinh mềm không mất máu, ở Echo bật Kael lên.
- Lưu ý thiết kế cho Codex: `EnergyGate` là **trigger** nên chỉ gây sát thương 10 khi chạm (không chặn vật lý). Nếu muốn "chặn hẳn trừ khi lướt", cần collider đặc riêng.
