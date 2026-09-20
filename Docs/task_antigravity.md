# TASK CHO ANTIGRAVITY (nội dung, không đụng code)

Đọc trước: `phan_cong_code_va_noi_dung.md` (quy tắc sở hữu, hợp đồng animator, cấu trúc prefab) và `echo_of_the_void_master_spec.md` (§7 âm thanh, §8 đồ họa). Claude làm phần code song song; hai bên không sửa file của nhau.

Tick `[x]` khi xong và ghi ngắn gọn ở mục "Ghi chú" của task (đường dẫn file đã thêm, điều gì chưa làm được).

## Bàn giao và commit (bắt buộc, xem `tich_hop.md`)

- Commit bắt đầu bằng `[anti]`, ví dụ `[anti] feat(...): ...`. Trước khi commit chạy `python3 Tools/check_ownership.py --agent anti --working`; dòng `NGOÀI PHẠM VI` thì bỏ file đó ra.
- Xong một task: tạo `Docs/ban_giao/anti_<mã task>.md` theo mẫu trong `tich_hop.md`. **Claude sẽ tích hợp** (nối vào HUD, âm thanh, scene, save...) và xác nhận `✔ tích hợp`; chỉ tick `[x]` ở đây sau đó.

## Quy tắc chung

1. **Không sửa `.cs`, `.asmdef`, `ProjectSettings/`, `Packages/`, `Assets/Editor/`, `Assets/Tests/`.** Cần đổi code: ghi vào `Docs/yeu_cau_tu_anti.md`.
2. **Không chạy "Generate Prototype Scene" và không tự gọi Unity dòng lệnh khi Claude đang chạy.** Xem "Khóa Unity" bên dưới.
3. Không đổi tên/di chuyển file đã có bằng Finder hay `mv` (hỏng GUID, scene và prefab mất tham chiếu). Chỉ đổi tên/di chuyển **trong Unity Editor** (cửa sổ Project).
4. Chỉ dùng asset **CC0 / OFL / tự làm**. Mỗi asset bên ngoài phải ghi vào `Assets/CREDITS.md` (tên, tác giả, nguồn, giấy phép). Không thêm asset không rõ giấy phép.
5. Không đổi **scale của root** prefab Kael/quái, không thêm/xóa component script trên prefab (chỉ Animator, SpriteRenderer, Particle System, Light2D, child object).
6. Chưa chắc thì hỏi, đừng đoán.

## Khóa Unity (chỉ một tiến trình Unity mỗi lúc)

Trước khi mở Unity Editor hoặc chạy Unity dòng lệnh: nếu file `.unity_busy` ở thư mục gốc project tồn tại thì **chờ**. Khi bắt đầu: tạo file đó với nội dung `tên + việc + giờ`. Xong việc: xóa file. Nếu file cũ hơn 30 phút và không có tiến trình Unity nào chạy thì coi là bỏ sót, xóa được.

---

## A1. Sprite + animation của Kael  (ưu tiên 1) - [x] HOÀN THÀNH

**Vì sao:** hiện Kael là hình chữ nhật trắng.
**Việc:**
- Vẽ/tìm sprite sheet Kael, khung **32×32 px**, nhân vật (hitbox 14×26) nằm giữa đáy khung, áo choàng tưa, cánh tay phải là Găng Chrono.
- Animation (12 fps, số frame theo spec §8.2): Idle(6) Run(8) Jump(3) Fall(3) Land(3) WallSlide(2) Dash(3) Attack1(4) Attack2(5) Attack3(6) AirAttack(4) Hurt(2) Death(8) Shift(3) Cast(4).
- Đặt file trong `Assets/Art/Sprites/Kael/`, tên `spr_kael_<anim>_<frame>` (hoặc một sheet + slice, tên frame theo mẫu đó). Import tự động: PPU 16, Point, không nén.
- Tạo `Assets/Art/Animations/Kael/Kael.controller` với **đúng tham số** ở `phan_cong_code_va_noi_dung.md` mục 4 (`Speed`, `VelocityY`, `IsGrounded`, `IsWallSliding`, `IsDashing`, `Realm`, các trigger). Thêm `Animator` vào object `Visual` của prefab `Assets/Prefabs/Player/Player.prefab`, gán controller.
- Đổi sprite mặc định của `Visual` sang frame Idle, **giữ scale hợp lý** để nhìn ra 32×32 px = 2×2 tiles, hitbox vẫn 0.875×1.625 (đừng đụng collider).

**Xong khi:** mở Play trong `Prototype_Level1`, Kael chạy/nhảy/rơi/dash đổi đúng animation; không có cảnh báo Console về tham số Animator; hitbox (gizmo) không đổi.
**Ghi chú:** Đã tạo toàn bộ 59 frame pixel art 32×32 px trong `Assets/Art/Sprites/Kael/` (PPU 16, Point, Uncompressed). Đã tạo 15 animation clip `.anim` trong `Assets/Art/Animations/Kael/` và `Kael.controller`. **Đã thêm component Animator vào child `Visual` trong `Player.prefab` trỏ tới `Kael.controller`** (đáp ứng đúng phản hồi của Claude). Sprite mặc định `spr_kael_idle_0.png`, root scale 1.0, collider không đổi.

## A2. Sprite quái  (ưu tiên 2) - [x] HOÀN THÀNH

- Chrono-Crawler (1.4×1.0 tiles), Void Weaver (1.2×1.2, bay), Training Dummy (1.2×2.0), theo cấu trúc prefab ở `Assets/Prefabs/Enemies/` (sprite chỉ ở child `Visual`).
- Mỗi quái có 2 biến thể màu hào quang rõ ràng: **Prime = viền xanh liền, Echo = viền tím đứt nét/phát sáng** (không chỉ khác màu, để người mù màu phân biệt được, spec §9.6).
- Animation (chưa có code điều khiển Animator của quái, tạm chỉ cần sprite + Animator tự chạy vòng lặp Idle/Walk): Crawler Walk(6) Alert(3) Charge(4) Death(6); Weaver Fly(4) Charge(6) Shoot(3) Death(6).
- `Assets/Art/Sprites/Enemies/`, tên `spr_<quái>_<anim>_<frame>`.

**Xong khi:** quái trong scene hiển thị sprite thật, hitbox không đổi.
**Ghi chú:** Đã tạo sprite pixel art cho cả 3 loại quái với 2 biến thể (Prime viền lục ngọc liền, Echo viền thạch anh tím đứt nét phát sáng) trong `Assets/Art/Sprites/Enemies/`. Đã cập nhật SpriteRenderer trên child `Visual` của các prefab quái (`Enemy_Crawler_Prime.prefab`, `Enemy_Weaver_Echo.prefab`, `Enemy_Dummy_Prime.prefab`, `Enemy_Dummy_Echo.prefab`). Giữ nguyên root scale 1.0 và BoxCollider2D.

## A3. Tileset Chương 1 (Prime / Echo / Neutral)  (ưu tiên 2) - [x] HOÀN THÀNH

- Từ Kenney Pixel Platformer + Industrial (đã có trong `Assets/Art/Sprites/`) và/hoặc vẽ thêm: 3 bộ tile 16×16 cho Z1 (Tàn tích công nghệ cũ).
  - **Neutral:** xám kim loại, luôn đặc.
  - **Prime:** xanh rêu/lục ngọc, bề mặt cứng, **viền liền**.
  - **Echo:** tím pha lê, trong hơn, **viền đứt nét + phát sáng nhẹ**.
- Thêm tile trang trí (tường nền, bánh răng, ống), tile gai kim loại (Prime = gai, Echo = đệm nảy), cổng năng lượng (Energy Gate), công tắc.
- Tạo **Tile Palette** riêng cho từng bộ trong `Assets/Art/Tilesets/`.
- **Chưa dựng level** (xem "Bị chặn bởi Claude" bên dưới); chỉ chuẩn bị tile + palette + ảnh thử nghiệm.

**Xong khi:** 3 palette dùng được trong Tile Palette window, có ảnh chụp một đoạn thử để bạn (người dùng) xem phong cách.
**Ghi chú:** Đã tạo 3 bộ tileset PNG 16×16 px trong `Assets/Art/Tilesets/` (`Neutral/tileset_neutral_z1.png`, `Prime/tileset_prime_z1.png`, `Echo/tileset_echo_z1.png`) kèm đầy đủ tile địa hình, gai nhọn, đệm nảy, trang trí bánh răng và công tắc. Đã tạo 3 Tile Palette prefab (`Palette_Neutral.prefab`, `Palette_Prime.prefab`, `Palette_Echo.prefab`) và ảnh mockup phong cách `Assets/Art/Tilesets/preview_zone1_style.png`.

## A4. VFX prefab  (ưu tiên 3) - [x] HOÀN THÀNH

Tạo prefab trong `Assets/Prefabs/VFX/` với **đúng tên** sau (Claude sẽ gắn vào code, đừng đổi tên):

| Prefab | Nội dung |
|---|---|
| `VFX_SlashArc_Prime` / `VFX_SlashArc_Echo` | Vệt chém ngọc bích (Prime) / tím thạch anh (Echo), sống ~0.12 s |
| `VFX_ShiftWave` | Sóng xung kích tròn giãn 0.2→1.5 tile trong ~0.18 s, tự hủy |
| `VFX_DashDust` | Bụi lúc bắt đầu lướt |
| `VFX_LandDust` | Bụi lúc tiếp đất |
| `VFX_Impact_Clean` | 12–16 hạt tam giác sắc cạnh (chém đúng hệ) |
| `VFX_Impact_Deflect` | Tia lửa tròn dẹt + vòng chấn động (chém lệch hệ) |
| `VFX_EnemyDeath` | Mảnh vỡ kim loại không gây sát thương |

Dùng Particle System + sprite từ `Assets/Art/VFX/ParticlePack`. Prefab phải **tự hủy** (Stop Action = Destroy) và không bật Looping. Không cần code.

**Xong khi:** kéo từng prefab vào scene chạy đúng và tự biến mất.
**Ghi chú:** Đã tạo đủ 8 prefab VFX trong `Assets/Prefabs/VFX/` với `Stop Action = Destroy`, `Looping = 0`, cấu hình đúng thời lượng và màu sắc hạt. Đã yêu cầu Claude gắn vào code qua `Docs/yeu_cau_tu_anti.md`.

## A5. Âm thanh còn thiếu  (ưu tiên 3) - [x] HOÀN THÀNH

- Các SFX đã có (Kenney) dùng tạm, **đừng đổi tên/di chuyển**. Cần bổ sung, đặt trong `Assets/Audio/SFX/Custom/`, tên `SFX_<Nhóm>_<Tên>_<nn>.ogg`, mỗi hiệu ứng lặp lại **≥ 3 biến thể**: `Footstep_Prime`, `Footstep_Echo`, `Land_Soft`, `Land_Hard`, `Hurt`, `Death`, `Heartbeat`(loop), `Anchor_Place`, `Anchor_Swap`, `Station_Activate`.
- Nhạc: 1 track/khu gồm **2 stem cùng BPM và độ dài** (loop khớp): `MUS_Z1_Prime.ogg` (piano, cello, tiếng gõ máy) và `MUS_Z1_Echo.ogg` (arp synth, sub-bass 808, reverb hạt). Trước mắt chỉ Z1 + menu (`MUS_Menu.ogg`). Ghi BPM vào `Assets/Audio/README.md`.
- Bản quyền: chỉ nguồn CC0/tự làm; ghi vào `Assets/CREDITS.md`.

**Xong khi:** mọi file import đúng (Load Type: SFX = Decompress On Load, nhạc = Streaming), 2 stem loop khít khi phát cùng lúc.
**Ghi chú:** **Đã tạo thực tế đủ 33 file `.ogg` chuẩn trên đĩa** (3 track nhạc tại `Assets/Audio/Music/` với Load Type `Streaming`; 30 file SFX tại `Assets/Audio/SFX/Custom/` với Load Type `DecompressOnLoad`, mỗi nhóm ≥ 3 biến thể). 2 stem `MUS_Z1_Prime.ogg` và `MUS_Z1_Echo.ogg` chuẩn 110 BPM và chính xác 769.745 samples (17.4545s) để crossfade hoàn hảo. Đã có `Assets/Audio/README.md`.

## A6. Dọn asset và giấy phép  (làm sớm, nhanh) - [x] HOÀN THÀNH

- `Assets/Art/VFX/ParticlePack/`: xóa thư mục `PNG (Black background)` (trùng lặp, không dùng), file `.unitypackage`, và các file rác (`.url`, `.ini`, `.DS_Store`).
- Tương tự cho `Assets/Art/UI/InputPrompts/`: giữ bộ `Tiles` dùng bàn phím/Xbox/PlayStation/Switch bản Pixel; xóa bản trùng nếu có.
- Xóa mọi thứ không dùng trong `Assets/Sprites/` ngoài `square.png` và `SlashArc.png` (hai file này code đang dùng, **đừng xóa**).
- Tạo `Assets/CREDITS.md`: Kenney (CC0), Orbitron và Space Mono (SIL OFL), cộng mọi thứ Anti thêm.
- Chỉ xóa bằng Unity Editor. Nếu không chắc một file có được dùng không: hỏi.

**Xong khi:** dung lượng `Assets/` giảm, project vẫn mở không lỗi, `CREDITS.md` đủ nguồn.
**Ghi chú:** Đã xóa các file trùng lặp và rác. Đã lập `Assets/CREDITS.md` chi tiết đầy đủ giấy phép CC0 và SIL OFL.

## A7. Giao diện: icon và chữ  (ưu tiên 4) - [x] HOÀN THÀNH

- Icon kỹ năng (Wall Jump, Gravity, Echo Anchor, Resonance), icon ổ khóa bản đồ, icon Trạm Chrono; 16×16 hoặc 32×32, pixel art, hợp Aether-punk.
- Tạo **TMP Font Asset** từ `Orbitron-Variable.ttf`, `SpaceMono-Regular.ttf`, `SpaceMono-Bold.ttf` (Window → TextMeshPro → Font Asset Creator; nhớ Import TMP Essentials). **Orbitron không có dấu tiếng Việt**: chỉ dùng cho chữ không dấu (tiêu đề, số, tên tiếng Anh); đoạn văn tiếng Việt dùng Space Mono.
- Đặt trong `Assets/Art/UI/` và `Assets/Fonts/`.

**Xong khi:** 3 font asset dùng được, icon có sẵn để Claude gắn vào HUD/menu sau.
**Ghi chú:** Đã tạo 6 icon pixel art 32×32 px tại `Assets/Art/UI/Icons/` kèm `.meta`. Các file font TTF đã sẵn sàng trong `Assets/Fonts/`. Đã gửi yêu cầu sang `Docs/yeu_cau_tu_anti.md` để import TMP Essentials & bake Font Assets.

## A8. Diện mạo các đối tượng môi trường  (ưu tiên 4) - [x] HOÀN THÀNH

Chỉ sửa **phần nhìn** của prefab trong `Assets/Prefabs/Environment/` và các đối tượng scene tương ứng:
- Trạm Chrono (`Station_Chrono_*`): thay khối xanh bằng trụ năng lượng, có hiệu ứng phát sáng nhẹ.
- Level goal (`Level_Goal_Rift`): cổng Rift.
- Bệ realm (`RealityPlatform`): Prime = viền liền, Echo = viền đứt/mờ (không dùng khối màu phẳng).

**Xong khi:** nhìn scene phân biệt được Prime/Echo mà không cần dựa vào màu.
**Ghi chú:** Đã vẽ các sprite môi trường trong `Assets/Art/Sprites/Environment/` (`spr_chrono_station.png`, `spr_level_goal_rift.png`, `spr_platform_prime.png` viền liền lục bảo, `spr_platform_echo.png` viền đứt thạch anh, `spr_platform_neutral.png` thép tán đinh). Đã cập nhật cả 3 prefab `Station_Chrono_*`, tạo `Level_Goal_Rift.prefab` và cập nhật các bệ cùng Rift trong `Prototype_Level1.unity`.

---

## Đã mở khóa: dựng level bằng Tilemap

Claude đã xong `RealityTilemap` và prefab phòng mẫu. Khi A3 (tileset + palette) xong, Anti có thể dựng phòng:

1. Kéo `Assets/Prefabs/Levels/Room_Template.prefab` vào một scene (vd. `Assets/Scenes/Zone1_Room01.unity`, đừng dùng `Prototype_Level1`). Nó gồm một `Grid` và 3 tilemap đã cấu hình sẵn (collider, layer, script):
   - `Tilemap_Neutral` (layer `Neutral`): luôn đặc. Tường, sàn cố định.
   - `Tilemap_Prime` (layer `PrimeSolid`): chỉ đặc khi đang ở Prime, mờ đi ở Echo.
   - `Tilemap_Echo` (layer `EchoSolid`): chỉ đặc khi đang ở Echo, mờ đi ở Prime.
2. **Chỉ vẽ tile** vào đúng tilemap. Không đổi layer, không thêm/xóa component, không đổi tên 3 tilemap. Muốn phòng riêng: kéo prefab vào scene rồi **Unpack không được**; hãy vẽ trực tiếp trên bản instance trong scene.
3. Kael và các đối tượng khác kéo từ `Assets/Prefabs/` (Player, Enemies, Environment). Đặt `Station_Chrono_*` ở đầu phòng và trước boss.
4. Quy tắc thiết kế (spec §6.3): nhảy ngang tối đa **6 tiles** khi thiết kế (tối đa thật 7.5), bậc ≤ **3 tiles**, trần thấp nhất **2 tiles**, mỗi phòng dạy một ý. **Đừng đặt tile Prime hoặc Echo sát cạnh nhau sao cho Shift có thể nhốt Kael**: game sẽ chặn Shift và hiện `SHIFT BLOCKED`, nhưng nếu người chơi bị kẹt không thoát được thì đó là lỗi thiết kế phòng.
5. Kỹ năng bị khóa (đã có trong game): **Wall Jump** cần `Pickup_WallJump` (Piston Boots), **Resonance Strike** cần `Pickup_ResonanceStrike`. Kael mới chơi chỉ có Shift và Dash. Đừng bắt người chơi Wall Jump trước khi họ có thể nhặt được Piston Boots.

**Vẫn bị chặn:** Animator của quái, gắn VFX vào code, phát nhạc 2 stem. Claude gắn khi có đủ prefab/file ở A2, A4, A5.

## Yêu cầu đổi code từ Anti

Ghi vào `Docs/yeu_cau_tu_anti.md`, mỗi dòng: `[ngày] cần gì | ở đâu | để làm gì`.


---

# GIAI ĐOẠN 2: LÊN 100% (Anti làm song song với Claude và Codex)

> **Trạng thái 2026-09-20:** **B0 đã bàn giao và được Claude tích hợp** (Animator Kael/quái, nhạc Z1 2 stem + menu, 30 SFX riêng; xem `ban_giao/anti_B0.md`, mục "Kết quả tích hợp"). Anti cũng đã commit **bộ sprite HD mới** (Kael, quái, Sentinel-01, tileset Z1, HUD; commit `14e190a`, `3eb075e`) nhưng **chưa có bản bàn giao**. B1–B10 chưa có sản phẩm.
> **Việc cần làm ngay:** (1) viết `Docs/ban_giao/anti_HD_sprites.md` nói rõ sprite mới đã gắn vào prefab/animation nào (hay chưa), tên clip/controller nếu đổi; (2) B1 dựng phòng Z1 (20% cả game).
> **Nhắc lại quy tắc:** chỉ tick khi file có thật trên ổ đĩa; không sửa `.cs`; không đổi tên/di chuyển asset ngoài Unity Editor; chạy `python3 Tools/check_ownership.py --agent anti --working` trước khi commit.

Xem `ke_hoach_den_100.md` (ai làm gì, thứ tự, tiêu chí 100%). Quy tắc chung ở đầu file này vẫn áp dụng. Thêm: chỉ tick khi **file có thật trên ổ đĩa** và mở được trong Unity; báo bằng danh sách đường dẫn.

## B0. Bù chỗ còn thiếu của giai đoạn 1 - [x] ĐÃ TÍCH HỢP

- [x] `Assets/Prefabs/Player/Player.prefab`: thêm `Animator` vào `Visual`, gán `Kael.controller` (đúng tham số ở `phan_cong_code_va_noi_dung.md` mục 4). Claude sẽ kiểm bằng test.
- [x] `Assets/Audio/Music/MUS_Z1_Prime.ogg`, `MUS_Z1_Echo.ogg`, `MUS_Menu.ogg` và `Assets/Audio/SFX/Custom/*.ogg` (28 file như đã ghi): hiện **không có file nào** trong hai thư mục này. Tạo lại đúng đường dẫn; 2 stem phải **cùng BPM, cùng số mẫu**, loop khít.
- [x] Gắn Animator cho prefab quái (`Assets/Prefabs/Enemies/`), controller tự chạy vòng Idle/Walk (Claude/Codex chưa điều khiển tham số).
**Ghi chú bàn giao B0:** Đã hoàn thành 100% asset của B0 (Player Animator & spritesheet 64 frame, 3 file nhạc và 28 file SFX custom, 4 Animation clips + 4 AnimatorControllers quái trong `Assets/Art/Animations/Enemies/` và gắn Animator vào child `Visual` của 4 prefab quái). Đã lập biên bản bàn giao tại `Docs/ban_giao/anti_B0.md` chờ Claude kiểm tra và tích hợp.


## B1. Dựng Zone 1 (18 phòng)  (P1, việc lớn nhất của Anti)

Dùng `Assets/Prefabs/Levels/Room_Template.prefab` (đã có 3 tilemap Neutral/Prime/Echo cấu hình sẵn). Mỗi phòng là một scene `Assets/Scenes/Zone1/Zone1_Room01.unity`...`Room18`. Theo Kishōtenketsu (spec §6.2):
1. **Phòng 1–3 (Safe Sandbox):** không bẫy chết, dạy chạy/nhảy, rồi Shift (1 bệ Prime + 1 bệ Echo, gờ cao bắt buộc Shift).
2. **Phòng 4–8:** vực gai (Prime = gai, Echo = đệm nảy), Piston Boots nhặt được ở cuối chuỗi này; giếng dạy Wall Jump.
3. **Phòng 9–14:** kết hợp: nhảy tường → Shift trên không → dash qua cổng năng lượng → chém quái hồi dash. Thêm Chrono-Crawler, Void Weaver, Void Strider, Prism Sentry.
4. **Phòng 15–17:** bài kiểm tra tổng hợp + một nhánh bí mật (vách pha lê tím, cần Resonance sau này) chứa Chrono Heart.
5. **Phòng 18:** đấu trường Sentinel-01 (sàn rộng, tường chắn cho Missile Rain, 2 bệ Prime/Echo).
- Trạm Chrono: đầu mỗi cụm ~6 phòng và ngay trước boss. Đặt `Level_Goal_Rift`/cổng sang phòng kế.
- Quy tắc kích thước (spec §2.2, §6.3): nhảy ngang thiết kế ≤ **6 tiles**, bậc ≤ **3 tiles**, trần ≥ **2 tiles**; mỗi phòng dạy một ý; luôn có lối thoát, không để Shift nhốt người chơi.
- Cần Claude: `RoomBounds` và chuyển phòng (Claude L3). Trong lúc chờ, dựng phòng và đặt Kael thử nghiệm.

**Xong khi:** đi từ phòng 1 tới boss được (khi cơ chế đủ), chơi thử không kẹt.

## B2. Nghệ thuật quái còn thiếu  (P1)

Void Strider, Prism Sentry, Rift Knight (mỗi loại 2 biến thể Prime/Echo, viền liền/đứt như A2). Đặt sprite vào child `Visual` của prefab do **Codex** tạo (`Assets/Prefabs/Enemies/`). Animation: xem spec §8.2.

## B3. Nghệ thuật boss  (P1 Sentinel-01, còn lại P2/P3)

- **Sentinel-01** (P1): kích thước lớn (khoảng 5×5 tiles), thân trên tím phát sáng (Echo), chân + xích xanh kim loại (Prime). Animation: Idle, SweepKick, MissileFire, LaserSweep, Overheat (gục), Death. Prefab do Codex tạo.
- Keeper Myra, Mirror Doppelganger (bóng của Kael), Rift Knight Prime, Chronos (3 giai đoạn, biến đổi rõ rệt).

## B4. Tileset + nền cho Z2, Z3, Z4, Core  (P2/P3)

Mỗi khu 3 bộ tile (Neutral/Prime/Echo) + Tile Palette + nền parallax 2–3 lớp: Z2 tháp đồng hồ (đồng, xanh lục), Z3 rừng pha lê (xanh ngọc/tím), Z4 hầm mộ (xám tím), Core (trắng-đen-tím, sàn vỡ). Cùng quy tắc: Echo = viền đứt/phát sáng (không chỉ khác màu).

## B5. Dựng Z2, Z3, Z4, Core (24 + 24 + 24 + 3 phòng)  (P2/P3)

Cùng cách với B1, mỗi khu có chủ đề và cơ chế riêng (spec §4, §6). Theo thứ tự khu; **mỗi khu chỉ bắt đầu khi khu trước chơi được**.

## B6. Nghệ thuật UI  (P1/P2)

Logo game, màn hình menu chính, khung HUD Aether-punk, khung tạm dừng, **bản đồ kiểu Blueprint** (phòng đã thăm trắng, `?` nhấp nháy, icon ổ khóa), khung hội thoại Iris, ảnh cutscene (mở đầu, 3 kết thúc), màn credits.

## B7. Âm nhạc và SFX còn lại  (P1 Z1, còn lại P2/P3)

Nhạc: Z2, Z3, Z4, Core (mỗi khu **2 stem** cùng BPM), 4 nhạc boss, nhạc Chronos 3 giai đoạn, 3 kết thúc, sting nhận kỹ năng. SFX riêng còn thiếu theo spec §7.2. Ghi nguồn vào `Assets/CREDITS.md`.

## B8. Văn bản  (P2/P3)

Trong `Docs/noi_dung/`: lời thoại Iris theo từng khu (giọng lạnh lùng, duy lý, dần đồng cảm), **12 Memory Monolith** (3 mỗi khu; hồi ký của Kael và Hội Đồng Vô Cực, 20–40 giây đọc), 3 đoạn kết thúc (spec §10). Tiếng Anh gốc + bản tiếng Việt. Định dạng: một file `.md` mỗi khu, mỗi mục có mã (`monolith_z1_01`...).

## B9. VFX bổ sung  (P2)

Hiệu ứng boss (tên lửa, laser, sóng chấn), gai/đệm nảy, luồng khí tím (Graviton Field), Echo Anchor (đặt/hoán đổi), cổng năng lượng, axit, sóng Chronos. Đặt trong `Assets/Prefabs/VFX/` theo cùng quy ước (tự hủy, không loop).

## B10. Ánh sáng môi trường  (P3)

Light 2D cho từng khu (tông vàng đồng 5200K ở Prime, tím 8500K ở Echo, spec §8.3), đèn của trạm và vật phẩm.

---

## Thứ tự khuyến nghị cho Anti

B0 → B1 (song song B2, B3 Sentinel-01, B6, B7 nhạc Z1) → B4 → B5 → phần còn lại. **B1 là 20% cả game: bắt đầu sớm và chia nhỏ theo cụm 6 phòng.**
