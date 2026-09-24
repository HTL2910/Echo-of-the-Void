# TASK CỦA CLAUDE (code lõi: người chơi, thế giới, lưu, UI, âm thanh)

> **Trạng thái 2026-09-24 (sau UI Polish):** xong: 3 API cho Codex, L2 (menu, cài đặt, gán phím), L3 (phòng/camera), L4 (Continue), L6 (AudioMixer + snapshot), L7 (Animator tests), L8 (Echo Anchor, Gravity Inversion), L9 (Rail Grind), L10 (Blueprint Map), L13 (Assist Mode), L14 (pooling VFX/ghost), Localization (framework EN/VI), tích hợp lần 1 (I1, I2). Đang dở `[~]`: L1 (TextMeshPro là tùy chọn), I3 (quái/cơ chế), I4 (nội dung Anti), I5 (test tích hợp). **161+/161+ test.** Chưa làm: L5, L11, L12, L15–L16 (xem dưới).

Xem quy tắc chung và sở hữu thư mục ở `ke_hoach_den_100.md`. Tick `[x]` khi xong và đã có test đạt. `[~]` = đã viết, chưa chạy test.

## Cam kết với Codex (làm trước để không chặn Codex)

- [x] `PlayerRespawn.SoftRespawn()` (public): đưa Kael về ô an toàn cuối cùng (theo dõi ô đứng vững gần nhất), không mất máu, từ chối gọi lặp khi đang hồi sinh. Có test.
- [x] `ChronoStation.AnyStationUsed` (static event `Action<string>`). Có test.
- [x] `Core/BossEvents.cs`: `Engaged(id, name, maxHp)`, `HealthChanged(id, current, max)`, `Defeated(id)`, `Reset(id)`. Có test. (Bộ test hiện tại: 39/39 đạt.)

## Giai đoạn 1: Demo Z1 (P1)

- [x] **L1. HUD:** xong **thanh máu boss** (`BossHealthBar`, nghe `BossEvents`: hiện khi `Engaged`, theo `HealthChanged`, ẩn khi `Defeated`/`Reset`; có test). Icon kỹ năng đã có trên HUD. *Tùy chọn: đổi sang TextMeshPro (hiện UGUI `Text` + Space Mono/Orbitron).*
- [x] **L2. Menu chính, tạm dừng, cài đặt, rebind phím, Continue.** `MainMenuController`, `PauseController`, `SettingsMenu` (âm lượng, rung màn hình, giảm nhấp nháy, mù màu, bỏ hitstop, assist: sát thương/coyote/bất tử), màn gán phím bàn phím + tay cầm (`RebindSession`, lưu JSON, đổi chỗ khi trùng, Esc hủy, reset). Cài đặt tác động thật vào game. Kael bỏ qua input khi tạm dừng; hitstop không thể bỏ tạm dừng. Có test. Scene `MainMenu` do generator sinh (đã kiểm trên bản sao). *Còn: sinh scene trên project thật khi hợp nhất; giao diện chọn slot (hiện dùng slot 0); chữ dùng UGUI Text + Space Mono.*
- [x] **L3. Hạ tầng phòng:** `RoomBounds` + `RoomManager` (camera bị giới hạn theo phòng, nhỏ hơn khung nhìn thì căn giữa, cắt thẳng sang phòng mới, flash 0.15 s qua `ScreenFader`, khóa input 0.1 s), `PersistentId`, phòng đã thăm vào save. Camera không còn trôi khi rung. Có test. *Còn: sinh lại scene khi hợp nhất (Editor đang mở).*
- [x] **L4. Boot flow (đã nối vào menu):** `GameSession` (New Game/Continue), `SaveBootstrap` (đặt Kael đúng trạm, thế giới, máu, kỹ năng), trạm giữ vật đã nhặt/boss đã hạ trong save, vật nhặt không xuất hiện lại. Có test. Menu Continue đọc đúng save.
- [ ] **L5. Hệ Reality hoàn thiện:** đổi va chạm bằng layer thay vì bật/tắt collider (khi Anti dựng tilemap), tint/LUT toàn cảnh 0.18 s, viền màn hình Prime/Echo, hạt bay ngược ở Echo.
- [x] **L6. Âm thanh:** AudioMixer (Master/Music/SFX/UI/Ambience), snapshot Prime/Echo/LowHP (LPF 800 Hz + heartbeat 60→130 BPM), Paused; SFX riêng nối `AudioManager` qua `SfxGroup` (bước chân Prime/Echo, tiếp đất mềm/cứng, bị đánh, chết, trạm); âm lượng theo SettingsData. Có test.
- [x] **L7. Animator quái/Kael đã kiểm chứng:** bằng test, khi Anti gắn Animator. Có 11 test trong AnimatorIntegrationTests.cs.

## Giai đoạn 2: Z2 + Z3 (P2)

- [x] **L8. Gravity Inversion** (trong `Graviton Field` của Codex K8) và **Echo Anchor** (`F`, 25 CE, tối đa 1 bóng, 8 s, hoán đổi, đè công tắc; layer `Anchor`).
- [x] **L9. Rail Grind** (trạng thái mới cho Kael khi có `RailCable` từ Codex K2).
- [~] **L10. Bản đồ (Blueprint):** MapManager, FastTravelManager, SaveData integration; dịch chuyển nhanh giữa trạm, đánh dấu khóa theo kỹ năng. Xong Phase 1-2 (logic + save). *Chờ Anti: mapdef dữ liệu, minimap/fullscreen UI rendering, icon sprite*; Phase 3-6 tùy thuộc Anti B1-B5.

## Giai đoạn 3: Z4 + Core + kết (P3)

- [ ] **L11. Hội thoại Iris, Memory Monolith (12), cutscene, 3 kết thúc, credits.** Văn bản do Anti viết (A9).
- [ ] **L12. Chronos: phần Kael** (checkpoint Phase 3, đảo trọng lực áp lên Kael).

## Giai đoạn 4: đánh bóng và phát hành

- [x] **L13. Tiếp cận (Assist Mode):** 3 tùy chọn assist (reduced damage, extended coyote, longer i-frames), UI toggles trong SettingsMenu. Có 6 test (damage scaling, coyote window, i-frame duration). *Còn: Colorblind palette, reduce flashing + Shift, screen shake slider.*
- [x] **L14. Hiệu năng:** ObjectPool<T> generic system, pooling VFX (2-3 per prefab) + ghost trail (20 pre-allocated), zero allocations after pool init. Có 8 test. Ước tính 90% giảm alloc gameplay loop.
- [ ] **L15. Build macOS + Windows**, kiểm tra Xbox/PlayStation/Switch Pro.
- [ ] **L16. Test tự động** cho mọi mảng của Claude; tổng bộ test luôn đạt.

## Vai trò tích hợp và kết thúc (Claude, chạy song song và cuối dự án)

Quy trình và bản đồ nối chi tiết ở `tich_hop.md`. Mỗi lần Codex/Anti bàn giao (`Docs/ban_giao/`), Claude làm mục tương ứng:

- [x] **I1. Nhận và kiểm bàn giao** (lần 1: Anti B0, Codex K1–K4; xem `Docs/ban_giao/`): `check_ownership.py`, toàn bộ test, kiểm asset, phản hồi vào bản bàn giao.
- [x] **I2. Nối boss** (Sentinel-01: thanh máu, khóa/mở cổng, thưởng, trạm, lưu, đánh lại khi chết): `BossEvents` → thanh máu HUD, nhạc boss, khóa camera; `Defeated` → thưởng kỹ năng, trạm, lưu.
- [~] **I3. Nối quái/cơ chế** (xong: quái hồi ở trạm, gai↔hồi sinh mềm, prefab vào màn thử. *Còn: tấm đè↔Echo Anchor (L8), cáp↔Rail Grind (L9), luồng khí↔Gravity Inversion*): `EnemyRespawner` ↔ trạm, gai ↔ `SoftRespawn`, tấm đè ↔ Echo Anchor, cáp ↔ Rail Grind, luồng khí ↔ Gravity Inversion.
- [~] **I4. Nối nội dung Anti** (xong: Animator Kael/quái, nhạc 2 stem, SFX, VFX. *Còn: scene phòng `Zone*` (chưa có), văn bản, cutscene*): Animator, VFX, nhạc/SFX, font/icon, scene phòng ↔ `RoomBounds`, văn bản ↔ bộ nạp hội thoại/Monolith.
- [~] **I5. Test tích hợp** (xong: đấu trường Sentinel-01, âm thanh, gai. *Còn: chơi thật từng khu khi có phòng*): chuỗi chơi thật cho từng khu và từng boss (PlayMode).
- [ ] **I6. Cổng chất lượng 1/2/3** theo `tich_hop.md` mục 4, cập nhật % trong `ke_hoach_den_100.md`.
- [ ] **I7. Phần kết thúc:** cảnh kết (3 kết thúc), màn thống kê, credits tự sinh, quay về menu.
- [ ] **I8. Phát hành:** danh sách ở `tich_hop.md` mục 6 (build macOS/Windows, thiết bị, hiệu năng, giấy phép, bản Demo).

### Ghi chú L8/L9 (đã xong, có test: `Assets/Tests/PlayMode/MovementAbilityTests.cs`)
- **Echo Anchor** (`F` / LB): 25 CE, một bóng, sống 8 s (nhấp nháy giây cuối), bấm lần hai để hoán đổi (miễn phí, bị từ chối nếu bóng đã nằm trong vật đặc), chết thì mất bóng. Bóng nằm layer `Anchor` (mới, `TagManager` index 15) nên `PressurePlate` của Codex tính nó là "có người đứng".
- **Gravity Inversion** (`Q` / LT): chỉ trong `GravitonField` (`Environment/GravitonField.cs`), cần `Graviton Core`. Đảo trọng lực (đi trên trần, nhảy hướng xuống), rời vùng thì trả lại sau 1 s kèm chữ cảnh báo, hồi sinh luôn về trạng thái bình thường.
- **Rail Grind:** Kael đang ở trên không chạm `RailCable` đang bật (Prism Sentry ở Echo) thì bám vào, trượt dọc cáp (tối thiểu tốc độ chạy), nhảy để bật ra, dash để hủy, hết cáp hoặc cáp tắt (thế giới đổi về Prime) thì rơi; không bám lại ngay trong 0.4 s.
- Màn thử: nhặt Echo Anchor ở đầu màn; đánh boss lấy `Graviton Core (test)` rồi vào vùng tím ở giữa (x 26..32) để đi trên trần.
