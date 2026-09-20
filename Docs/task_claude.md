# TASK CỦA CLAUDE (code lõi: người chơi, thế giới, lưu, UI, âm thanh)

Xem quy tắc chung và sở hữu thư mục ở `ke_hoach_den_100.md`. Tick `[x]` khi xong và đã có test đạt. `[~]` = đã viết, chưa chạy test.

## Cam kết với Codex (làm trước để không chặn Codex)

- [x] `PlayerRespawn.SoftRespawn()` (public): đưa Kael về ô an toàn cuối cùng (theo dõi ô đứng vững gần nhất), không mất máu, từ chối gọi lặp khi đang hồi sinh. Có test.
- [x] `ChronoStation.AnyStationUsed` (static event `Action<string>`). Có test.
- [x] `Core/BossEvents.cs`: `Engaged(id, name, maxHp)`, `HealthChanged(id, current, max)`, `Defeated(id)`, `Reset(id)`. Có test. (Bộ test hiện tại: 39/39 đạt.)

## Giai đoạn 1: Demo Z1 (P1)

- [~] **L1. HUD:** xong **thanh máu boss** (`BossHealthBar`, nghe `BossEvents`: hiện khi `Engaged`, theo `HealthChanged`, ẩn khi `Defeated`/`Reset`; có test). Icon kỹ năng đã có trên HUD. *Còn (tùy chọn): đổi sang TextMeshPro; hiện dùng UGUI `Text` + Space Mono/Orbitron nên đã hiển thị đúng font, chỉ khác là chưa bake TMP Font Asset.*
- [x] **L2. Menu chính, tạm dừng, cài đặt, rebind phím, Continue.** `MainMenuController`, `PauseController`, `SettingsMenu` (âm lượng, rung màn hình, giảm nhấp nháy, mù màu, bỏ hitstop, assist: sát thương/coyote/bất tử), màn gán phím bàn phím + tay cầm (`RebindSession`, lưu JSON, đổi chỗ khi trùng, Esc hủy, reset). Cài đặt tác động thật vào game. Kael bỏ qua input khi tạm dừng; hitstop không thể bỏ tạm dừng. Có test. Scene `MainMenu` do generator sinh (đã kiểm trên bản sao). *Còn: sinh scene trên project thật khi hợp nhất; giao diện chọn slot (hiện dùng slot 0); chữ dùng UGUI Text + Space Mono.*
- [x] **L3. Hạ tầng phòng:** `RoomBounds` + `RoomManager` (camera bị giới hạn theo phòng, nhỏ hơn khung nhìn thì căn giữa, cắt thẳng sang phòng mới, flash 0.15 s qua `ScreenFader`, khóa input 0.1 s), `PersistentId`, phòng đã thăm vào save. Camera không còn trôi khi rung. Có test. *Còn: sinh lại scene khi hợp nhất (Editor đang mở).*
- [x] **L4. Boot flow (đã nối vào menu):** `GameSession` (New Game/Continue), `SaveBootstrap` (đặt Kael đúng trạm, thế giới, máu, kỹ năng), trạm giữ vật đã nhặt/boss đã hạ trong save, vật nhặt không xuất hiện lại. Có test. Menu Continue đọc đúng save.
- [ ] **L5. Hệ Reality hoàn thiện:** đổi va chạm bằng layer thay vì bật/tắt collider (khi Anti dựng tilemap), tint/LUT toàn cảnh 0.18 s, viền màn hình Prime/Echo, hạt bay ngược ở Echo.
- [ ] **L6. Âm thanh:** AudioMixer (Master/Music/SFX/UI/Ambience), snapshot `LowHP` (LPF 800 Hz + nhịp tim 60→130 BPM), footstep Prime/Echo, nối SFX riêng của Anti vào `AudioManager`.
- [ ] **L7. Animator quái/Kael đã kiểm chứng:** bằng test, khi Anti gắn Animator.

## Giai đoạn 2: Z2 + Z3 (P2)

- [ ] **L8. Gravity Inversion** (trong `Graviton Field` của Codex K8) và **Echo Anchor** (`F`, 25 CE, tối đa 1 bóng, 8 s, hoán đổi, đè công tắc; layer `Anchor`).
- [ ] **L9. Rail Grind** (trạng thái mới cho Kael khi có `RailCable` từ Codex K2).
- [ ] **L10. Bản đồ** (Blueprint), dịch chuyển nhanh giữa trạm, đánh dấu khóa theo kỹ năng.

## Giai đoạn 3: Z4 + Core + kết (P3)

- [ ] **L11. Hội thoại Iris, Memory Monolith (12), cutscene, 3 kết thúc, credits.** Văn bản do Anti viết (A9).
- [ ] **L12. Chronos: phần Kael** (checkpoint Phase 3, đảo trọng lực áp lên Kael).

## Giai đoạn 4: đánh bóng và phát hành

- [ ] **L13. Tiếp cận:** Assist Mode, mù màu, giảm nhấp nháy khi Shift, thanh trượt rung màn hình.
- [ ] **L14. Hiệu năng:** pooling VFX/đạn/ghost trail, không GC trong vòng lặp, 60 FPS.
- [ ] **L15. Build macOS + Windows**, kiểm tra Xbox/PlayStation/Switch Pro.
- [ ] **L16. Test tự động** cho mọi mảng của Claude; tổng bộ test luôn đạt.

## Vai trò tích hợp và kết thúc (Claude, chạy song song và cuối dự án)

Quy trình và bản đồ nối chi tiết ở `tich_hop.md`. Mỗi lần Codex/Anti bàn giao (`Docs/ban_giao/`), Claude làm mục tương ứng:

- [ ] **I1. Nhận và kiểm bàn giao:** `check_ownership.py`, toàn bộ test, kiểm asset, phản hồi vào bản bàn giao.
- [ ] **I2. Nối boss:** `BossEvents` → thanh máu HUD, nhạc boss, khóa camera; `Defeated` → thưởng kỹ năng, trạm, lưu.
- [ ] **I3. Nối quái/cơ chế:** `EnemyRespawner` ↔ trạm, gai ↔ `SoftRespawn`, tấm đè ↔ Echo Anchor, cáp ↔ Rail Grind, luồng khí ↔ Gravity Inversion.
- [ ] **I4. Nối nội dung Anti:** Animator, VFX, nhạc/SFX, font/icon, scene phòng ↔ `RoomBounds`, văn bản ↔ bộ nạp hội thoại/Monolith.
- [ ] **I5. Test tích hợp:** chuỗi chơi thật cho từng khu và từng boss (PlayMode).
- [ ] **I6. Cổng chất lượng 1/2/3** theo `tich_hop.md` mục 4, cập nhật % trong `ke_hoach_den_100.md`.
- [ ] **I7. Phần kết thúc:** cảnh kết (3 kết thúc), màn thống kê, credits tự sinh, quay về menu.
- [ ] **I8. Phát hành:** danh sách ở `tich_hop.md` mục 6 (build macOS/Windows, thiết bị, hiệu năng, giấy phép, bản Demo).
