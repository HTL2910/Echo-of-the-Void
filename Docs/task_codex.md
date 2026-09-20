# TASK CHO CODEX (code: kẻ địch, boss, cơ chế môi trường)

Đọc trước: `ke_hoach_den_100.md` (**ai sở hữu thư mục nào**, khóa Unity, quy tắc tick), `echo_of_the_void_master_spec.md` (§3.1 ma trận thực tại, §3.4 chiến đấu, §5 kẻ địch và boss, §6 màn chơi), `phan_cong_code_va_noi_dung.md` (cấu trúc prefab, hợp đồng animator).

## Phạm vi của Codex

**Được sửa/tạo:** `Assets/Scripts/Enemies/**`, `Assets/Scripts/Combat/**`, `Assets/Scripts/Bosses/**` (mới), `Assets/Scripts/Environment/Mechanics/**` (mới), `Assets/Editor/Codex/**` (mới), test `Assets/Tests/PlayMode/{Enemy,Boss,Mechanic}*Tests.cs`, prefab **mới** trong `Assets/Prefabs/Enemies|Bosses|Mechanics/`, số liệu `Assets/Settings/Enemies/*.asset`.

**Không sửa:** `Player/`, `Core/`, `UI/`, `Save/`, `Feedback/`, các file **đã có** trong `Environment/`, `SceneGenerator.cs`, `TestWorld.cs`, `ProjectSettings/`, `*.asmdef`, mọi thứ trong `Art/`, `Audio/`. Cần đổi: ghi `Docs/yeu_cau_giua_agent.md`.

## Bàn giao và commit (bắt buộc, xem `tich_hop.md`)

- Commit bắt đầu bằng `[codex]`, ví dụ `[codex] feat(...): ...`. Trước khi commit chạy `python3 Tools/check_ownership.py --agent codex --working`; dòng `NGOÀI PHẠM VI` thì bỏ file đó ra.
- Xong một task: tạo `Docs/ban_giao/codex_<mã task>.md` theo mẫu trong `tich_hop.md`. **Claude sẽ tích hợp** (nối vào HUD, âm thanh, scene, save...) và xác nhận `✔ tích hợp`; chỉ tick `[x]` ở đây sau đó.

## Quy ước kỹ thuật (bắt buộc)

- Đơn vị: **1 tile = 1 Unity unit** (spec §0.1). Mọi số cân bằng nằm trong `ScriptableObject` (`EnemyDataSO`, `BossPhaseData`), **không hard-code** trong script.
- Prefab: **root scale 1**, collider theo đơn vị thật, sprite ở child `Visual` (Anti thay sprite/animation mà không đụng code). Không scale root.
- Prefab quái/boss được **tạo bởi script editor riêng** trong `Assets/Editor/Codex/` (menu `Tools/Echo of the Void/Codex/...`), chạy **chỉ tạo nếu prefab chưa có** (không ghi đè, để giữ chỉnh sửa của Anti).
- Kẻ địch chỉ phản ứng với thế giới qua `RealityEventBus.OnRealmSwitched` và `RealityManager.Instance.CurrentRealm`. Không tham chiếu chéo trực tiếp.
- Sát thương đi qua `IDamageable`/`DamageInfo` (trong `Combat/`, của Codex). Ma trận thực tại 100%/20% đã cài trong `EnemyBase`; giữ nguyên hành vi.
- Layer: quái = `Enemy`, đòn của quái = `EnemyHitbox`, cơ chế = `Hazard`/`Interactable`. Không tạo layer mới nếu chưa xin (Claude sở hữu `ProjectSettings`).
- Hiệu ứng: `VfxLibrary.Play(VfxId, pos)` (id mới: xin Claude). Âm thanh: `AudioManager.Instance` (clip mới: xin Claude).
- Test bằng `TestWorld` (sàn + Kael + input giả). Mỗi tính năng mới cần test chạy thật trong batchmode, gồm cả trường hợp lỗi (không có Animator, không có Player...).

## API mà Claude cung cấp cho Codex (đã/đang có)

| API | Dùng để |
|---|---|
| `PlayerRespawn.SoftRespawn()` | Gai/hố: đưa Kael về ô an toàn, không mất máu (spec D9) |
| `ChronoStation.AnyStationUsed` (static event `Action<string>`) | Quái thường **hồi lại** khi Kael nghỉ ở trạm |
| `BossEvents.Engaged / HealthChanged / Defeated` (`Core/BossEvents.cs`) | Bắn sự kiện boss; UI thanh máu boss do Claude vẽ |
| `AbilityPickup` (prefab `Pickup_WallJump`...) | Phần thưởng sau boss |
| `PlayerStats.TakeDamage(DamageInfo)`, `PlayerController`, `AbilitySet` | Gây sát thương / đọc trạng thái Kael |
| `RealityObstacles` + `IRealityObstacle` | Vật cản chỉ đặc ở một thế giới (chặn Shift khi kẹt) |

---

## K1. Khung AI quái  (P1)

- `EnemyAnimationDriver` cho quái (giống `PlayerAnimationDriver`): tham số `Speed`, `IsGrounded`, `Realm`(int), trigger `Alert`, `Charge`, `Attack`, `Hurt`, `Die`; thiếu tham số/Animator thì bỏ qua. Ghi hợp đồng vào `phan_cong_code_va_noi_dung.md` mục 4 (thêm mục 4b "Animator quái") để Anti biết.
- **Poise/choáng** trong `EnemyBase` (spec §5.2): thanh Poise từ `EnemyDataSO.PoiseMax`, giảm theo loại đòn, khi 0 thì gục N giây và nhận +50% sát thương.
- **`EnemyRespawner`**: quái thường trở lại vị trí/trạng thái ban đầu khi có `ChronoStation.AnyStationUsed` hoặc Kael hồi sinh sau khi chết. Boss không hồi (trừ khi Kael chết giữa trận: boss reset).
- Sửa nếu có lỗi: quái không được tự rơi khỏi mép (raycast mép vực), không đi xuyên tường.

**Xong khi:** test đạt (driver đặt tham số đúng, poise gục và +50%, respawner khôi phục), Crawler/Weaver dùng khung mới không đổi hành vi cũ.
**Ghi chú:** Đã hoàn thành 100% K1 (EnemyAnimationDriver, Poise & Stun +50% dmg, EnemyRespawner, cập nhật ChronoCrawler & VoidWeaver tránh rơi mép). Test PlayMode `EnemyAITests` đạt 3/3, toàn bộ test suite đạt 50/50. Đã lập biên bản bàn giao tại `Docs/ban_giao/codex_K1.md`.


## K2. Void Strider và Prism Sentry  (P1)

- **Void Strider** (`h_th_ng_c_ch_k_thu_t_v_gameplay.md` §3.1): tuần tra, phát hiện bán kính 6 tiles thì lao vào; hệ theo hào quang (xanh = Prime, tím = Echo); chỉ nhận đủ sát thương khi Kael ở đúng thế giới. Giá trị đề xuất trong spec §5.1 (HP 50, 12 sát thương, 4.0 → 6.5 tiles/s).
- **Prism Sentry** (§3.2): đứng yên trần/tường, bắn tia mỗi 2.5 s (sát thương 20). Ở Prime tia gây sát thương; khi Kael chuyển sang Echo thì tia thành **cáp năng lượng** (`RailCable`: expose điểm đầu/cuối, hướng; **Claude** sẽ thêm trạng thái Rail Grind cho Kael, xin qua `yeu_cau_giua_agent.md`).
- `EnemyDataSO` mới cho mỗi loại, prefab trong `Assets/Prefabs/Enemies/`, script editor tạo prefab.

**Xong khi:** test hành vi (phát hiện, lao, chuyển thế giới của tia), prefab tạo được, số liệu đọc từ SO.
**Ghi chú:**

## K3. Cơ chế môi trường Z1  (P1)

Trong `Assets/Scripts/Environment/Mechanics/`, mỗi cơ chế là prefab trong `Assets/Prefabs/Mechanics/` (root scale 1, sprite ở `Visual`):

- **Spikes (gai kim loại):** ở Prime = chạm là `PlayerRespawn.SoftRespawn()` (không mất máu); ở Echo = thành **đệm nảy** (đẩy Kael lên).
- **BouncePad**: đệm nảy độc lập.
- **EnergyGate (cổng năng lượng):** chỉ **lướt (i-frame) mới đi xuyên**; bước bình thường bị chặn/sát thương.
- **PressurePlate + Door**: đè bằng Kael **hoặc bóng Echo Anchor** (layer `Anchor`); cửa mở khi có vật đè, đóng lại sau `closeDelay` (spec §4: Z4 dùng 1.5 s). Cửa có sự kiện `Opened/Closed`.
- **Lever**: bật/tắt bằng `E` (dùng `PlayerController.InteractPressed`).

**Xong khi:** mỗi cơ chế có test (gai giết ở Prime/nảy ở Echo, cổng cho lướt qua, cửa đóng sau độ trễ...), prefab dùng được trong scene.
**Ghi chú:**

## K4. Boss Sentinel-01 + đấu trường  (P1, quan trọng nhất)

Theo `echo_of_the_void_master_spec.md` §5.2. HP 600, Poise 100. Thân trên hệ **Echo**, chân/xích hệ **Prime** (đánh phần nào thì phải đúng thế giới của phần đó).

- FSM: `Idle → SweepKick (quét 360°, buộc nhảy) → MissileRain (4 tên lửa, vòng cảnh báo 1.2 s, đổi thế giới để núp sau tường) → LaserSweep (quét sàn, nhảy + dash) → (lặp)`; **Overheat** khi Poise = 0: gục 3.5 s, +50% sát thương. Dưới 40% HP: rút ngắn cảnh báo còn 0.9 s.
- `BossPhaseData` (SO): HP, danh sách pattern, thời lượng, sát thương, ngưỡng chuyển pha. Dùng lại cho các boss sau.
- `BossArena`: vào vùng thì khóa lối vào (`Engaged`), hết máu boss thì mở khóa, `Defeated`, sinh **`Pickup_WallJump`** (Piston Boots) và một Trạm Chrono gần đó; Kael chết thì boss reset về đầu trận.
- Bắn sự kiện qua `BossEvents` (Claude vẽ thanh máu).
- Sprite: chờ Anti (task B3). Trước mắt dùng hình chữ nhật màu ở child `Visual`.

**Xong khi:** test mô phỏng đủ pha (vòng lặp đòn, Overheat +50%, reset khi Kael chết), thắng được bằng tay trong scene thử, phần thưởng xuất hiện.
**Ghi chú:**

## K5. Rift Knight (quái nặng)  (P2)

Spec §5.1: HP 130, chém 25, dậm đất 35 (sóng cao 1.2 tile), tốc độ 3.8; **tự đổi hệ** mỗi 4 s hoặc sau 3 đòn liên tiếp; **khiên phía trước chặn 100%**, Kael phải lướt ra sau lưng.

**Xong khi:** test (khiên chặn trước, sát thương từ sau, đổi hệ đúng thời điểm).
**Ghi chú:**

## K6. Keeper Myra và Mirror Doppelganger  (P2)

- **Myra** (Z2, HP đề xuất 900): bánh răng/con lắc đồng bộ nhịp tích tắc; né trúng nhịp. Tận dụng Wall Jump.
- **Doppelganger** (Z3, HP 1000): bắt chước lại đòn của Kael sau 1 s trễ, đối xứng qua thế giới (đổi thế giới ngược Kael). Tận dụng Gravity Inversion.
- Cùng khung `BossPhaseData` + `BossArena` như K4. Phần thưởng: `Pickup_GravityInversion`, `Pickup_EchoAnchor` (Claude cung cấp prefab khi có kỹ năng).

**Xong khi:** giống K4 cho mỗi boss.
**Ghi chú:**

## K7. Rift Knight Prime và Chronos  (P3)

- **Rift Knight Prime** (Z4 mini-boss, HP 500): Rift Knight nâng cấp, đổi hệ mỗi 3 s; thưởng Resonance Strike.
- **Chronos** (boss cuối, spec §5.4), 3 giai đoạn: (1) Reality Cleave chia sàn Prime/Echo (đứng sai thế giới mất 12 HP/s), Chrono Burst 8 tia xoay; (2) Time Rewind mỗi 400 HP mất, sàn rơi; (3) Enrage: âm bản, đảo trọng lực mỗi 6 s, dịch chuyển sau lưng chém 50 sát thương báo trước 0.4 s. Có checkpoint đầu Phase 3 (spec Q4).
- Phase 2/3 cần Echo Anchor và Gravity Inversion của Claude (Core: `AbilityFlags`).

**Xong khi:** test từng giai đoạn, chuyển pha đúng ngưỡng, kết thúc phát sự kiện `Defeated` (Claude nối vào cảnh kết).
**Ghi chú:**

## K8. Cơ chế môi trường Z2/Z3  (P2)

Bánh răng quay và luân phiên hai thế giới, con lắc, ống hơi (steam vent) theo chu kỳ, biển axit thời gian (sát thương/chết mềm), luồng khí tím (**Graviton Field**: Claude sẽ dùng cho Gravity Inversion, hãy tạo trigger volume có tên `GravitonField` để Claude nối), gai trần.

**Xong khi:** mỗi cơ chế có prefab + test.
**Ghi chú:**

## K9. Cân bằng và kiểm thử  (liên tục)

Mọi số nằm trong SO; bảng giá trị trong spec §5 là điểm khởi đầu, chỉnh khi playtest. Mỗi loại quái/boss có test tự động về **hành vi chính** (không phải số thập phân chính xác). Báo cáo cuối mỗi task: liệt kê test đã thêm và kết quả.

---

## Thứ tự khuyến nghị

K1 → K2 + K3 (song song được) → K4 → K5 → K6 → K8 → K7. Xong K4 là Demo có boss.

## Cách báo tiến độ

Tick `[x]` ở đầu mỗi task và ghi "Ghi chú" (file đã thêm, test đã thêm, điều chưa làm). Kèm kết quả lần chạy test cuối (số đạt/tổng).
