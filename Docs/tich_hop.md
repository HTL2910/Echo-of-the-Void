# TÍCH HỢP VÀ KẾT THÚC (vai trò của Claude)

Ba người làm song song: **Claude** (code lõi), **Codex** (kẻ địch, boss, cơ chế), **Anti** (nội dung). Khi hai người kia xong việc, **Claude tích hợp** mọi thứ vào một game chạy được và **lo phần kết thúc** (cảnh kết, credits, build). File này là quy trình để việc đó nhanh và không sót.

## 1. Quy ước chung cho cả ba

**Thông điệp commit:** `[agent] loại(phạm vi): nội dung`, ví dụ `[codex] feat(boss): Sentinel-01 FSM`, `[anti] feat(art): Z1 room 03`. Agent là `claude`, `codex` hoặc `anti`. Claude thêm `[integrate]` khi phải chạm file của người khác để nối (vd. `[claude][integrate] feat(scene): wire Sentinel-01 arena`).

**Tự kiểm phạm vi trước khi commit** (Anti và Codex đều chạy):

```bash
python3 Tools/check_ownership.py --agent codex --working   # hoặc --agent anti
```

Có dòng `NGOÀI PHẠM VI` thì bỏ file đó khỏi commit và ghi vào `yeu_cau_giua_agent.md`. Claude chạy `python3 Tools/check_ownership.py --range <đầu>..<cuối>` để kiểm mọi commit của hai người kia.

**Bàn giao khi xong một task:** tạo `Docs/ban_giao/<agent>_<mã task>.md` (vd. `codex_K4.md`) theo mẫu:

```
# Bàn giao <mã task>: <tên>
- Người làm: codex | anti
- File đã thêm/sửa: (đường dẫn)
- Cách dùng: (prefab nào kéo vào đâu, tham số nào chỉnh)
- Test đã chạy: (lệnh + kết quả "đạt/tổng")
- Chưa làm / hạn chế:
- Cần Claude nối gì: (sự kiện, UI, SceneGenerator, âm thanh...)
```

Không có bàn giao thì Claude không tích hợp. Chỉ tick `[x]` trong file task **sau khi** Claude xác nhận đã tích hợp và kiểm (Claude ghi `✔ tích hợp` vào bàn giao).

**Khóa Unity:** một tiến trình Unity mỗi lúc; dùng `.unity_busy` (xem `task_antigravity.md`). Mở Unity Editor bằng tay cũng là giữ khóa: tạo `.unity_busy` khi mở, xóa khi đóng.

## 2. Quy trình tích hợp của Claude (mỗi lần nhận bàn giao)

1. **Đọc bàn giao** và `git log` các commit mới.
2. `python3 Tools/check_ownership.py --range ...`: có vi phạm thì trả lại người làm.
3. **Chạy toàn bộ test PlayMode** bằng `Tools/run_tests_isolated.sh`: phải đạt 100% trước và sau khi nối (chạy trên bản sao nên không vướng khóa Unity).
4. **Kiểm asset:** prefab đúng cấu trúc (root scale 1, `Visual` con), Animator đúng tham số hợp đồng, file âm thanh/hình có thật và import đúng, không có tham chiếu thiếu.
5. **Nối vào game** theo bảng ở mục 3 (`SceneGenerator`, HUD, âm thanh, sự kiện, save).
6. **Test tích hợp:** viết test PlayMode chơi thử chuỗi thật (vd. Kael vào đấu trường → boss `Engaged` → thanh máu hiện → boss chết → phần thưởng xuất hiện).
7. **Cập nhật** `ke_hoach_den_100.md` (%), ghi `✔ tích hợp` hoặc lỗi cụ thể vào bàn giao.
8. Xung đột giữa hai bên: **Claude quyết**; `echo_of_the_void_master_spec.md` thắng tài liệu cũ.

## 3. Bản đồ nối: cái gì nối với cái gì

### Codex → Claude

| Codex cung cấp | Claude nối vào |
|---|---|
| `BossEvents` (Engaged/HealthChanged/Defeated/Reset) | Thanh máu boss trên HUD, nhạc boss, khóa camera |
| Boss `Defeated` | Sinh `AbilityPickup` đúng kỹ năng, Trạm Chrono, ghi `bossDefeated` vào save; Chronos → cảnh kết |
| `EnemyRespawner` nghe `ChronoStation.AnyStationUsed` | Kiểm bằng test: nghỉ ở trạm thì quái hồi |
| Gai/hố gọi `PlayerRespawn.SoftRespawn()` | Kiểm đúng điểm an toàn cuối |
| `PressurePlate` (Kael hoặc bóng Anchor) | Echo Anchor (layer `Anchor`) do Claude viết |
| `RailCable` (Prism Sentry ở Echo) | Trạng thái Rail Grind của Kael |
| `GravitonField` | Kỹ năng Gravity Inversion |
| Prefab quái/boss/cơ chế | `SceneGenerator` (bãi thử), phòng do Anti dựng |
| `VfxId`/clip âm thanh mới | Claude thêm id, nối `VfxLibrary`/`AudioManager` |

### Anti → Claude

| Anti cung cấp | Claude nối vào |
|---|---|
| Animator Kael/quái (tham số đúng hợp đồng) | `PlayerAnimationDriver`, `EnemyAnimationDriver`; kiểm bằng test |
| Prefab VFX | `VfxLibrary` (thêm id nếu có hiệu ứng mới) |
| Nhạc 2 stem, SFX riêng | `MusicLayerController` (đổi nhạc theo khu/boss), `AudioManager`, mixer |
| Font, icon, khung UI | HUD, menu, bản đồ, hội thoại |
| Scene phòng `Zone*_RoomNN` | `RoomBounds`, chuyển phòng, camera confiner, trạm, bản đồ |
| Văn bản `Docs/noi_dung/*.md` | Bộ nạp hội thoại/Monolith (định dạng: mỗi mục có mã `monolith_z1_01`, dòng `EN:`/`VI:`) |
| Ảnh cutscene, logo, credits | Cảnh kết, menu, màn credits |

## 4. Cổng chất lượng mỗi giai đoạn (Claude quyết định "qua cổng")

**Cổng 1: Demo Z1**
- Chơi liền mạch từ menu → 18 phòng → Sentinel-01 → nhận Piston Boots → màn hình cảm ơn.
- Menu, tạm dừng, cài đặt, rebind, Continue hoạt động; nhạc 2 stem đổi mượt khi Shift.
- Toàn bộ test đạt; 60 FPS; không lỗi/cảnh báo đỏ trong Console; blind playtest ≥ 3 người.

**Cổng 2: Z2 + Z3** như trên cộng Wall Jump, Gravity Inversion, Echo Anchor, bản đồ, dịch chuyển nhanh, Myra + Doppelganger.

**Cổng 3: Z4 + Core + kết** như trên cộng Rift Knight Prime, Chronos 3 giai đoạn, 12 Monolith, hội thoại Iris, 3 kết thúc, credits.

**Cổng 4: Phát hành** xem mục 6.

## 5. Phần kết thúc (do Claude làm)

- **Cảnh kết:** nghe `BossEvents.Defeated("chronos")` → lựa chọn cuối: **A Reset**, **B Convergence**; **C Void Sovereign** chỉ hiện khi đã đọc đủ **12 Monolith** (cờ trong save).
- Mỗi kết thúc: chuỗi ảnh tĩnh + văn bản của Anti (`Docs/noi_dung/endings.md`), nhạc kết, lưu `endingFlags` vào save.
- **Credits:** tự sinh từ `Assets/CREDITS.md` (Kenney CC0, Orbitron/Space Mono OFL, mọi nguồn Anti ghi) + tên các agent/người làm; quay về menu chính; mở khóa "Chơi lại".
- **Màn thống kê** (thời gian chơi, số lần chết, Monolith, Chrono Heart).

## 6. Danh sách phát hành (Cổng 4)

- [ ] Build macOS + Windows chạy độc lập, không cần Editor; kiểm ở máy sạch.
- [ ] Bàn phím + Xbox + PlayStation + Switch Pro; icon phím đổi theo thiết bị.
- [ ] 60 FPS ổn định trên GPU tích hợp; không GC trong vòng lặp chơi (kiểm profiler).
- [ ] Tùy chọn: mù màu, giảm nhấp nháy khi Shift, thanh trượt rung màn hình, Assist Mode.
- [ ] Mọi test tự động đạt; không cảnh báo thiếu tham chiếu (Missing) trong scene/prefab.
- [ ] `CREDITS.md` đủ nguồn và giấy phép; không có asset không rõ bản quyền.
- [ ] Trang cửa hàng (itch.io/Steam): mô tả, ảnh chụp, trailer (Anti làm ảnh/trailer).
- [ ] Bản Demo (Z1) đóng gói riêng.
