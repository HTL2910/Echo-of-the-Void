# ECHO OF THE VOID — MASTER SPEC (v1.0)

Tài liệu đặc tả tổng hợp, **nguồn sự thật duy nhất (SSOT)**. Hợp nhất 10 tài liệu trong `Docs/` và đã giải quyết các mâu thuẫn giữa chúng (xem §0.2). Khi tài liệu cũ và file này khác nhau, **file này thắng**.

- Engine: Unity `6000.3.13f1`, URP 2D, Input System 1.19, Cinemachine 3.1.7
- Thể loại: 2D Metroidvania / Platformer, pixel art, đơn người chơi
- Nền tảng: PC (Windows/macOS) trước; tay cầm là bắt buộc. Mobile không nằm trong phạm vi.
- Trạng thái repo hiện tại: có greybox (`Prototype_Level1`), `RealityManager`, `RealityEventBus`, `RealityPlatform`, `PlayerController` (chưa FSM), tileset Kenney. Chi tiết ở §13.

---

## 0. QUY ƯỚC & QUYẾT ĐỊNH THIẾT KẾ

### 0.1. Đơn vị (bắt buộc cho toàn bộ dự án)

| Đại lượng | Quy ước |
|---|---|
| 1 tile | 16 × 16 px = **1 Unity unit** (PPU = 16) |
| "m" trong các tài liệu cũ | = **1 tile** (không phải mét thật) |
| Thời gian | giây; frame tính ở 60 FPS (1 frame = 0.0167 s) |
| Tốc độ | tiles/s |
| Khung nhìn chuẩn | 480 × 270 px = 30 × 16.875 tiles (render ×4 ở 1920×1080, pixel-perfect) |

### 0.2. Sổ quyết định (các chỗ tài liệu cũ mâu thuẫn)

| # | Mâu thuẫn | Quyết định | Lý do |
|---|---|---|---|
| D1 | Tốc độ chạy: 12 tiles/s (GDD) vs 8 m/s (bảng thuộc tính) | **12 tiles/s** | Khớp code hiện có; cho phép chỉnh 8–12 khi playtest |
| D2 | Lực nhảy: 14.5 m/s vs công thức $V_0=2h/t$ | **Công thức**: h=3.5, t=0.35 → $V_0=20$ tiles/s, $g=57.1$ | 14.5 không thỏa 3.5 tile trong 0.35 s |
| D3 | Gia tốc rơi: ×1.6 vs ×1.8 | **×1.6** ($g_{fall}=91.4$) | Khớp code + GDD; chỉnh khi playtest |
| D4 | Dash: 4 tiles / 3.75 tiles / 24 speed | **4 tiles, 0.2 s, tốc độ 20 tiles/s**, cooldown 0.8 s | Code hiện dùng 24 (=4.8 tiles), cần sửa |
| D5 | Sát thương combo: 15/15/30 vs 15/18/28 | **15 / 18 / 28** | Bảng chi tiết hơn |
| D6 | Reward boss: Story và bản đồ Metroidvania khác nhau | Theo **§4** (bản đồ khóa-chìa). Cốt truyện được sửa lại cho khớp | Ability gating cần chặt chẽ hơn lời kể |
| D7 | Story có 4 màn, bản đồ có 4 khu + Core | **5 khu vực** (Z1–Z4 + Core); Z4 thêm mini-boss | Cần nơi trao Resonance Strike |
| D8 | Wall Slide/Dash là kỹ năng cơ bản hay bị khóa? | Wall Slide + Phase Dash: mở ở Z1. **Wall Jump** bị khóa (Piston Boots) | Level 1-1 dùng dash + bám tường, nhưng Z2 cần khóa wall jump |
| D9 | Gai "chết ngay" vs HP không tự hồi | **Hazard tức thì = respawn nhẹ** (về ô an toàn cuối, HP giữ nguyên). **Hết HP = chết thật** (về Trạm Chrono) | Tránh trừng phạt quá nặng khi thử bẫy |
| D10 | Phím `E` vừa Shift vừa (ngầm) Interact | Shift = `Left Shift`/`RB`. Interact = `E`/`Y` | Tránh trùng phím |
| D11 | Orbitron thiếu dấu tiếng Việt | Orbitron: **tiêu đề, HUD, số, tên riêng tiếng Anh**. Space Mono: mọi đoạn văn/đối thoại | Đã kiểm tra cmap: Orbitron thiếu 51/57 ký tự tiếng Việt, Space Mono đủ |

### 0.3. Giá trị do spec này đề xuất (KHÔNG có trong 10 tài liệu gốc)

Các mục sau là **đề xuất mới để lấp chỗ trống**, cần bạn duyệt hoặc chỉnh khi playtest:

- **Kael:** vận tốc rơi tối đa 30, tốc độ wall slide tối đa 3, variable jump ×0.5, khóa input wall jump 0.12 s, cửa sổ nối combo 0.25 s.
- **Sinh tồn:** 4 Chrono Heart (tối đa 180 HP), HP hồi sinh 50%, số slot lưu 3.
- **Kỹ năng:** chi phí/thời hạn Echo Anchor (25 CE, 8 s), luật Gravity Inversion (chỉ trong Graviton Field), phím Q/U/LB/LT/RT.
- **Quái:** toàn bộ số của Void Strider (HP 50, 12 dmg, 4.0→6.5) và Prism Sentry (HP 60, 20 dmg).
- **Boss:** toàn bộ §5.3 (Myra, Doppelganger, Rift Knight Prime); Poise −25/−50 và ngưỡng 40% của Sentinel-01; Rift Knight Prime là boss Z4.
- **Thế giới:** số phòng mỗi khu (18/24/24/24/3), 12 Monolith, điều kiện mở Ending C.
- **Hệ thống:** cấu trúc layer/asmdef, danh sách animation + số frame, mọi tiêu chuẩn accessibility (§9.6), toàn bộ §11–§14.

---

## 1. TẦM NHÌN SẢN PHẨM

**Pitch:** Kael thức dậy giữa tàn tích Aethelgard, nơi thực tại bị xé thành hai tầng chồng nhau. Người chơi bẻ cong thế giới bằng một nút bấm (Reality Shift) để vượt địa hình, né đòn và đánh bại kẻ địch.

**Trụ cột thiết kế (dùng để phán quyết mọi tranh cãi):**
1. **Một nút, hai thế giới.** Mọi màn chơi, kẻ địch, boss đều xoay quanh Reality Shift.
2. **Cảm giác điều khiển trước, nội dung sau.** Nếu di chuyển không sướng thì không thêm nội dung.
3. **Thông tin nằm trong thế giới.** HUD tối giản, diegetic; người chơi đọc trạng thái qua nhân vật và môi trường.
4. **Mở khóa bằng năng lực, không bằng cấp độ.** Không có XP/level; tiến bộ = kỹ năng mới + khám phá.

**Phạm vi & độ dài mục tiêu:** ~4–6 giờ lần đầu, ~90 phòng (Z1 18, Z2 24, Z3 24, Z4 24, Core 3). **Demo/Vertical Slice = trọn Z1** (18 phòng + Sentinel-01).

**Đối tượng:** người thích Celeste / Hollow Knight / Ori. Độ khó: trung bình–khó, có Assist Mode (§9.6).

---

## 2. NỀN TẢNG KỸ THUẬT

### 2.1. Render & Camera
- URP 2D Renderer, **Pixel Perfect Camera** (ref 480×270, PPU 16), filter Point, không nén texture, không mipmap (đã có `PixelArtPostprocessor`).
- Cinemachine 3: 1 virtual camera / phòng, `CinemachineConfiner2D` theo `RoomBounds`, look-ahead nhẹ theo hướng chạy, damping tắt theo trục Y khi đứng trên đất.
- Chuyển phòng: trigger biên → fade 0.15 s ra/vào, đóng băng input 0.1 s.
- Post-process: 2 URP Volume profile (Prime/Echo), blend **0.18 s** khi Shift (§7).

### 2.2. Physics 2D & Layers

| Layer | Vai trò |
|---|---|
| `Player` | Kael |
| `Enemy` | Kẻ địch |
| `Neutral` | Nền/tường luôn đặc |
| `PrimeSolid` | Tile đặc ở Prime |
| `EchoSolid` | Tile đặc ở Echo |
| `Hazard` | Gai, axit, laser |
| `PlayerHitbox` / `EnemyHitbox` | Vùng gây sát thương |
| `Interactable` | Công tắc, trạm, monolith |
| `Anchor` | Bóng Echo Anchor |

- **Shift trong 1 frame:** gọi `Physics2D.IgnoreLayerCollision` cho cặp (`Player`,`Enemy`) × (`PrimeSolid`,`EchoSolid`) theo realm hiện tại. Không bật/tắt từng collider (giữ `RealityPlatform` cho vật thể lẻ có phản hồi hình ảnh).
- Tilemap: mỗi phòng có 3 Tilemap (`Neutral`, `Prime`, `Echo`), mỗi cái `TilemapCollider2D` + `CompositeCollider2D`.
- **Shift bị chặn** nếu vùng đặc của realm đích chồng lên hitbox Kael (`OverlapBox`): phát `Shift_Denied` (buzz ngắn), Găng Chrono nháy đỏ, không tốn cooldown. Quy tắc cho level designer: không đặt vị trí mà việc này gây kẹt vĩnh viễn.
- Tick vật lý: Fixed Timestep = `1/60`.

### 2.3. Input
- Dùng **Input System** qua asset `InputSystem_Actions` (hiện code đọc `Keyboard.current` trực tiếp → phải đổi). Hỗ trợ rebind, lưu binding vào file settings.
- Bảng phím mặc định:

| Hành động | Bàn phím | Tay cầm (Xbox layout) |
|---|---|---|
| Di chuyển | `A/D`, `←/→` | Stick trái / D-pad |
| Nhảy | `Space`, `W`, `↑` | A |
| Chém (Chrono Blade) | `J`, chuột trái | X |
| Phase Dash | `K`, `Left Ctrl` | B |
| **Reality Shift** | `Left Shift` | **RB** |
| Echo Anchor | `F` | LB |
| Resonance Strike | `U` | RT |
| Gravity Flip | `Q` | LT |
| Tương tác | `E` | Y |
| Bản đồ | `M` | Select/View |
| Tạm dừng | `Esc` | Start |

- Deadzone stick 0.15. Hỗ trợ Xbox, PlayStation, Switch Pro (icon đổi theo thiết bị, dùng Input Prompts Pixel).

### 2.4. Hiệu năng
- 60 FPS ổn định trên GPU tích hợp; không GC alloc trong gameplay loop (pool cho đạn, hạt, ghost trail, damage text).
- Draw call mục tiêu < 150 / phòng; sprite atlas theo khu vực.

---

## 3. HỆ THỐNG CỐT LÕI

### 3.1. Reality Shift

```
enum RealmType { Prime, Echo }          // đã có
RealityManager.ToggleRealm()  ->  RealityEventBus.OnRealmSwitched(RealmType)
```

- Cooldown **0.25 s** (đã có). Shift bị khóa khi đang chết, đang cutscene, hoặc bị chặn (§2.2).
- Mọi thực thể chỉ **subscribe** `OnRealmSwitched`; không tham chiếu chéo với `RealityManager`.
- Shift hoạt động giữa không trung và cả khi đang dash.

**Ma trận đối tượng:**

| Đối tượng | Prime | Echo |
|---|---|---|
| Bệ đá xanh (Prime Tile) | Đặc | Xuyên qua, mờ 40% |
| Pha lê tím (Echo Tile) | Vô hình, không va chạm | Đặc, hơi nảy |
| Gai kim loại | Chết tức thì (D9) | Thành đệm nảy (bouncer) |
| Cổng lực trọng trường | Vô hiệu | Đảo trục Y của trọng lực |
| Laser Prism Sentry | Gây sát thương | Thành dây cáp: **Rail Grind** |
| Đạn Void Weaver | Xuyên qua Kael, vô hại | Thành vật thể phát nổ khi chạm |

**Nhận diện realm không chỉ bằng màu** (§9.6): Prime = viền liền, bề mặt cứng; Echo = viền đứt nét phát sáng + hạt bay ngược.

### 3.2. Nhân vật Kael

**Hitbox 14 × 26 px (0.875 × 1.625 tiles).**

| Thông số | Giá trị |
|---|---|
| Tốc độ chạy tối đa | 12 tiles/s |
| Tăng tốc / giảm tốc | 0.1 s / 0.08 s |
| Độ cao nhảy | 3.5 tiles, đỉnh sau 0.35 s |
| Vận tốc nhảy $V_0$ | 20 tiles/s |
| Gravity lên / rơi | 57.1 / 91.4 tiles/s² |
| Vận tốc rơi tối đa | 30 tiles/s |
| Coyote time / Jump buffer | 0.10 s / 0.12 s |
| Variable jump | nhả nút sớm: nhân $v_y$ với 0.5 |
| Wall slide | ma sát 0.3 (tốc độ trượt tối đa 3 tiles/s) |
| Wall jump (Z2+) | bật góc 45°, khóa input ngang 0.12 s |
| Phase Dash | 4 tiles / 0.2 s, i-frame 0.2 s, CD 0.8 s, **reset khi chạm đất hoặc chém trúng** |

**Số liệu thiết kế màn chơi (suy ra từ trên):**
- Nhảy ngang tối đa ≈ **7.5 tiles** (dùng tối đa **6** cho khoảng cách thoải mái); có dash ≈ 11 tiles.
- Bậc/tường nhảy được: ≤ **3 tiles** (dư 0.5). Trần tối thiểu: **2 tiles**.
- Chu kỳ shift + nhảy: cho người chơi ≥ 0.4 s giữa hai lần bắt buộc shift.

**FSM (State Pattern):** `Idle, Run, Jump, Fall, WallSlide, Dash, Attack(1..3), AirAttack, Hurt, Dead, ResonanceCast, AnchorSwap, Interact(Locked)`. Mỗi state là 1 class implement `IPlayerState`; `PlayerController` chỉ giữ state hiện tại + dữ liệu chia sẻ. Cấm chuỗi `if/else` dài trong `Update()`.

### 3.3. Chỉ số sinh tồn & Năng lượng

| Chỉ số | Giá trị |
|---|---|
| HP tối đa | 100 (+20 mỗi **Chrono Heart**, tối đa 4 → **180**) |
| HP hồi | Không tự hồi. +25 tại Trạm Chrono; +25 khi kết liễu quái bằng Resonance Strike |
| Chrono Energy (CE) | 100. Hồi 15 CE/s khi chạm đất; +10 CE/đòn chém trúng; +5 CE khi Chrono-Crawler chết |
| i-frames sau khi bị đánh | 0.8 s, nhấp nháy đỏ mờ |
| Chết (HP=0) | Về Trạm Chrono gần nhất, HP = 50% max, quái thường hồi lại, boss reset |
| Respawn hazard (D9) | ≤ 1 s, HP giữ nguyên, đặt lại ở ô an toàn cuối cùng |

### 3.4. Chiến đấu

| Đòn | Sát thương | Tầm | Knockback | Startup / Tổng |
|---|---|---|---|---|
| Combo 1 (chém ngang) | 15 | 1.8 | 0.5 | 3f / 12f |
| Combo 2 (chém chéo 45°) | 18 | 2.0 | 1.0 | 4f / 14f |
| Combo 3 (đâm phá giáp) | 28 | 2.6 | 3.5 | 6f / 20f |
| Air Slash | 20 | vòng cung 180° | treo 0.1 s | 3f / 10f |
| Resonance Strike | 65 | tia 8.0 | 4.0 | 8f, tốn 50 CE |

- Combo: cửa sổ nối đòn 0.25 s sau khi đòn hiện tại vào recovery. Hủy đòn bằng Dash được (sau frame active).
- **Reality Affinity:** đánh đúng hệ = 100%; lệch hệ = **20%** (+ âm `Blade_Deflect`, chữ "DEFLECT", hitstop 0.03 s).
- Boss gục (Poise = 0): nhận thêm +50% sát thương trong 3.5 s.
- Sát thương Kael nhận: theo bảng từng quái (§5). Không có sát thương ngẫu nhiên, không crit.

### 3.5. Game Feel
Nguyên văn thông số từ `game_feel_v_feedback_k_thu_t.md` được giữ nguyên; bảng tóm tắt (cài ở `FeelProfile` ScriptableObject để chỉnh không cần sửa code):

| Sự kiện | Hitstop | Time scale | Shake | Squash |
|---|---|---|---|---|
| Chém 1, 2 | 0.05 s | 0.0 | — | — |
| Chém 3 | 0.09 s | 0.0 | Medium | — |
| Deflect | 0.03 s | 0.1 | — | — |
| Resonance Strike | 0.14 s | 0.0 + zoom 3% | Heavy | — |
| Kael bị boss đánh | 0.10 s | 0.05 | Heavy | — |
| Nhảy | — | — | — | 0.75 × 1.25, 0.15 s |
| Tiếp đất mạnh | — | — | Light nếu rơi > 6 tiles | 1.30 × 0.70, 0.12 s |
| Bắt đầu Dash | — | — | Light | 1.40 × 0.70, 0.10 s |
| Chạm tường | — | — | — | 0.80 × 1.20, 0.08 s |

**Shake:** Light (A 0.08 / 25 Hz / 0.12 s), Medium (0.22 / 35 Hz / 0.20 s), Heavy (0.55 / 45 Hz / 0.40 s, roll ±1.5°); dùng Cinemachine Impulse với nhiễu Perlin.
Squash giữ thể tích: `scaleX × scaleY = 1`. Ghost trail: 5 bóng, mỗi 0.03 s, alpha 0.7→0, xanh lam-lục (Prime) / tím neon (Echo).
Hitstop chạy trên `unscaledTime`; UI không bị ảnh hưởng.

### 3.5b. Save/Load & Checkpoint
- **Trạm Chrono (Save Point):** tương tác `E` → lưu, +25 HP, đầy CE, quái thường hồi lại. Đây cũng là điểm dịch chuyển nhanh sau khi mở khóa.
- 3 slot lưu, JSON tại `Application.persistentDataPath`, có trường `version` để migrate.
- Auto-save: khi qua trạm, sau boss, khi nhặt vật phẩm vĩnh viễn.
- Dữ liệu: `slotId, playtime, zoneId, stationId, abilityFlags, maxHP, collectedIds[], bossDefeated[], monolithsRead[], mapExplored[], endingFlags`. Settings lưu riêng (không nằm trong slot).
- Mọi vật thể lưu trạng thái phải có `PersistentId` (GUID ổn định, sinh ở editor).

---

## 4. TIẾN TRÌNH METROIDVANIA (KHÓA & CHÌA)

Quy tắc: **khóa của khu N được mở bởi chìa lấy ở cuối khu N−1.**

| Chìa (năng lực) | Nhận từ | Mở khóa cho | Khóa vật lý điển hình |
|---|---|---|---|
| Reality Shift | Găng Chrono (đầu Z1, tutorial) | Z1 | Vực với bệ Prime nhấp nháy |
| Phase Dash + Wall Slide | Z1, khoảng phòng 5–6 (Iris hướng dẫn) | Đường trong Z1 | Vực rộng ≥ 9 tiles |
| **Piston Boots → Wall Jump** | Boss **Sentinel-01** (cuối Z1) | **Z2** Tháp Bánh Răng | Ống thông hơi 15 tiles, không chỗ đứng |
| **Graviton Core → Gravity Inversion** | Boss **Keeper Myra** (cuối Z2) | **Z3** Rừng Phản Chiếu | Trần phẳng, sàn là biển axit thời gian |
| **Echo Anchor** | Boss **Mirror Doppelganger** (cuối Z3) | **Z4** Hầm Mộ Nghịch Đảo | Cổng sập 1.5 s, cách công tắc 8 tiles |
| **Resonance Strike** | Mini-boss **Rift Knight Prime** (cuối Z4) | **Core** + vách pha lê tím ở Z1 | Vách pha lê tím phong ấn |

**Kỹ năng chi tiết:**
- **Echo Anchor** (`F`): đặt bóng tại vị trí hiện tại (tốn 25 CE, tối đa 1, tồn tại 8 s); nhấn `F` lần nữa để hoán đổi chỗ với bóng (miễn phí). Bóng **đè được công tắc áp lực** — đó là cơ chế giải đố chính.
- **Gravity Inversion** (`Q`): chỉ trong vùng **Graviton Field** (luồng khí tím); đảo trọng lực Kael, cho phép chạy trên trần. Rời vùng: trọng lực tự trả lại sau 1 s (có cảnh báo).
- **Resonance Strike** (`U`): tia xuyên giáp, phá vách pha lê tím, 65 sát thương, tốn 50 CE.

**Bản đồ tuyến tính:**
```
Z1 Tàn Tích ──(Wall Jump)──► Z2 Tháp Bánh Răng ──(Gravity)──► Z3 Rừng Phản Chiếu
   │  ▲                                                          │
   │  └─ thang máy tắt (mở từ Z2)                                (Anchor)
(Resonance)                                                        ▼
   ▼                                                        Z4 Hầm Mộ ──(Resonance)──► CORE: Chronos
Phòng thưởng #1 (+20 Max HP)
```

**Sưu tầm:**
- **Chrono Heart** ×4 (+20 Max HP). Một trái nằm sau vách pha lê Z1.
- **Memory Monolith** ×12 (3 mỗi khu; hồi ký của Iris, mở lore).
- **Mảnh Lõi (Core Fragment)** ×4 = 4 boss. Đủ 4 mảnh mới vào Core.

---

## 5. KẺ ĐỊCH & BOSS

Toàn bộ chỉ số nằm trong `EnemyData` (ScriptableObject); code AI không chứa số cứng.

```csharp
[CreateAssetMenu] class EnemyData : ScriptableObject {
  string id; int maxHp; int contactDamage; float moveSpeed;
  RealmType? fixedRealm;        // null = biến đổi chủ động
  float detectRadius, chargeSpeed, attackCooldown;
  int ceDrop; float poise;      // poise=0: không có thanh choáng
}
```

Quái tuần tra phải có **raycast mép vực** và **không tự rơi**. Quái chết: hiệu ứng nổ mảnh (không sát thương), drop CE.

### 5.1. Quái thường

| Quái | HP | Sát thương | Tốc độ | Hệ | Hành vi |
|---|---|---|---|---|---|
| **Chrono-Crawler** | 40 | 10 (va chạm) | 3.2 | Prime cố định | `PATROL` → `ALERT` (bán kính 5, dừng 0.3 s, mắt đỏ) → `CHARGE` (5.0, không dừng giữa chừng) → `DEATH` (rớt 5 CE) |
| **Void Weaver** | 35 | 18 (đạn) | 2.5 (bay) | Echo cố định | Giữ cách Kael 6; lùi khi < 3; mỗi 3.0 s nạp 0.8 s (có tia ngắm) rồi bắn, đạn 12 tiles/s (cơ chế đạn ở §3.1) |
| **Rift Knight** | 130 | Chém 25, dậm đất 35 (sóng cao 1.2) | 3.8 | Tự đổi (mỗi 4 s hoặc sau 3 đòn liên tiếp) | Khiên chặn 100% trực diện; Kael phải Phase Dash ra sau lưng |
| **Void Strider** *(docs gameplay)* | 50 | 12 | 4.0 → 6.5 khi phát hiện | Theo hào quang (xanh/tím) | Tuần tra; phát hiện ở 6 tiles thì lao vào |
| **Prism Sentry** | 60 | Tia 20 | tĩnh | Prime | Trần/tường, bắn mỗi 2.5 s; ở Echo tia thành cáp Rail Grind |

**Phân bổ khu vực:** Z1: Crawler, Strider. Z2: + Sentry. Z3: + Weaver. Z4: + Rift Knight. Không thêm loại mới trong Z1 ngoài các loại trên.

### 5.2. Sentinel-01 (Z1 boss, HP 600, Poise 100)
Thân trên = **Echo**, chân/bánh xích = **Prime**: đánh phần nào thì dùng đúng realm phần đó.

| State | Mô tả | Cách phá |
|---|---|---|
| `SweepKick` | Quét chân 360° | Nhảy lên |
| `MissileRain` | 4 tên lửa, vòng cảnh báo 1.2 s trước khi rơi | Đổi realm để núp sau tường chắn |
| `LaserSweep` | Quét laser toàn sàn | Nhảy + Dash trên không |
| `Overheat` | Sau khi Poise = 0: gục 3.5 s, +50% sát thương | Dồn combo |

Poise giảm bởi Combo 3 (−25) và Resonance Strike (−50). Vòng lặp: Sweep → Missile → Laser → Sweep… Dưới 40% HP: rút ngắn cảnh báo còn 0.9 s.

### 5.3. Keeper Myra (Z2), Mirror Doppelganger (Z3), Rift Knight Prime (Z4)
Chưa có số liệu trong docs; mức ban đầu để cân bằng khi tới giai đoạn 3 (M5):

| Boss | HP | Cơ chế chủ đạo |
|---|---|---|
| Keeper Myra | 900 | Bánh răng/con lắc đồng bộ nhịp tích tắc; đánh trúng nhịp = né được. Tận dụng Wall Jump + dọc ống |
| Mirror Doppelganger | 1000 | Bắt chước lại đòn của Kael sau 1 s trễ; đối xứng qua realm (đổi realm đối nghịch Kael). Tận dụng Gravity Inversion |
| Rift Knight Prime | 500 | Phiên bản nâng cấp Rift Knight, đổi hệ mỗi 3 s. Cho Resonance Strike |

### 5.4. Chronos (Boss cuối, 3 giai đoạn)

| Giai đoạn | HP | Đòn / luật |
|---|---|---|
| 1 Kẻ Điều Phối | 1500 | *Reality Cleave*: chia sàn Prime/Echo; đứng sai realm mất 12 HP/s. *Chrono Burst*: 8 tia xoay như kim đồng hồ, lách kẽ bằng Dash |
| 2 Nghịch Lý Thời Gian | 1800 | *Time Rewind*: mỗi 400 HP mất, dư ảnh lùi 2.5 s + sóng chấn động (Dash đúng frame nổ). *Sàn rơi*: nửa số bệ tan biến, dùng Echo Anchor + bám tường |
| 3 Điểm Kỳ Dị (Enrage) | 1000 | Màn hình âm bản xám-đen; đảo trọng lực mỗi 6 s; Chronos dịch chuyển sau lưng Kael, chém 50 sát thương, **báo hiệu 0.4 s** |

Không có checkpoint giữa các giai đoạn ngoài checkpoint Trạm; thêm **Checkpoint Phase 3** để tránh làm lại 2 giai đoạn (đề xuất, xem §14).

---

## 6. THẾ GIỚI & MÀN CHƠI

### 6.1. Các khu vực

| Khu | Chủ đề | Tileset | Palette | Số phòng | Boss |
|---|---|---|---|---|---|
| Z1 The Shattered Outskirts | Tàn tích, bánh răng chết | Kenney Pixel Platformer + Industrial | Xám kim loại, xanh rêu | 18 | Sentinel-01 |
| Z2 The Clockwork Abyss | Tháp đồng hồ, hơi nước, con lắc | Industrial (mở rộng) | Đồng, xanh lục đậm | 24 | Keeper Myra |
| Z3 The Mirrored Wilds | Rừng đột biến, pha lê | *Cần asset mới* | Xanh ngọc, tím | 24 | Mirror Doppelganger |
| Z4 Catacombs of Duality | Hầm mộ hai tầng | *Cần asset mới* | Xám tím, âm bản | 24 | Rift Knight Prime |
| Core: The Core Singularity | Sàn vỡ tái tạo | *Cần asset mới* | Trắng-đen-tím | 3 | Chronos |

### 6.2. Level 1-1 (bắt buộc cho Vertical Slice)
Theo **Kishōtenketsu** (Giới thiệu → Phát triển → Bước ngoặt → Tổng hợp):

1. **Safe Sandbox** — phòng kín, không bẫy chết, 1 bệ xanh + 1 bệ tím, icon phím trên tường; bắt buộc bấm Shift để qua gờ cao.
2. **Risk Introduction** — vực gai. Gai ở Prime; đổi Echo thành đệm nảy đưa tới mỏm kế tiếp.
3. **Skill Chaining** — bám tường → Shift trên không → Dash xuyên Energy Gate → chém quái để hồi Dash → hạ cánh an toàn.
4. **Bài kiểm tra tổng hợp** — phòng ngay trước Sentinel-01, kết hợp cả ba.

### 6.3. Quy tắc thiết kế phòng
- Mỗi phòng dạy hoặc thử đúng **một ý tưởng** rồi mới trộn.
- Luôn có lối thoát khỏi kẹt (đã có ở §2.2).
- Khu chưa có chìa: người chơi thấy ổ khóa (icon trên bản đồ) trước khi có khả năng mở.
- Checkpoint (Trạm) ≤ 6 phòng cách nhau; boss có Trạm ≤ 2 phòng phía trước.

---

## 7. ÂM THANH

### 7.1. Kiến trúc
- **AudioMixer:** `Master → Music, SFX, UI, Ambience`. Snapshot: `Prime`, `Echo`, `LowHP`, `Pause`.
- **Nhạc 2 stem** mỗi khu, cùng BPM/nhịp, chạy đồng thời: Stem A (Prime: piano acoustic, cello, tiếng gõ máy), Stem B (Echo: analog arp, 808 sub, granular reverb; cắt 300 Hz, tăng 10 kHz).
- Crossfade **0.18 s, equal-power** (`gainA=cos(t·π/2)`, `gainB=sin(t·π/2)`); dùng 2 `AudioSource` điều khiển bằng code, không dùng snapshot tuyến tính dB.
- **Low-health (HP < 25%):** LPF 800 Hz lên bus Music+Ambience, thêm Heartbeat 60→130 BPM theo HP.

### 7.2. SFX Registry (đã lập ánh xạ với pack Kenney đề xuất)

| ID | Mô tả | Nguồn đề xuất |
|---|---|---|
| `Shift_Activate` | Kính vỡ nén + chấn động hạ âm (80→40 Hz / 0.15 s) | Sci-fi Sounds |
| `Shift_Denied` | Buzz ngắn | Interface Sounds |
| `Blade_Swing_1..3` | Vung kiếm | Impact/Sci-fi |
| `Blade_Hit_Clean` | Kim loại sắc, pitch 0.95–1.05 ngẫu nhiên | Impact Sounds |
| `Blade_Deflect` | Chuông cùn + bandpass | Impact Sounds |
| `Dash` | Lướt không gian | Sci-fi Sounds |
| `Footstep_Prime` | Đế da trên gạch + sỏi | Impact Sounds |
| `Footstep_Echo` | Điện tử ngắn + reverse reverb | Digital Audio |
| `Jump`, `Land_Soft`, `Land_Hard` | Nhảy / tiếp đất | Impact Sounds |
| `Hurt`, `Death` | Nhận sát thương / chết | Digital/Impact |
| `Anchor_Place/Swap` | Đặt bóng / hoán đổi | Sci-fi Sounds |
| `Resonance_Charge/Fire` | Nạp / bắn | Sci-fi Sounds |
| `Enemy_*`, `Boss_*` | Theo từng quái | — |
| `UI_Move/Select/Back` | Menu | Interface Sounds |
| `Heartbeat` | Nhịp tim | Tự tạo/Digital |

Quy ước tên: `SFX_<Nhóm>_<Tên>.wav`; SFX lặp dùng ≥ 3 biến thể + ngẫu nhiên hóa pitch ±5%.

### 7.3. Nhạc cần có
Menu theme (1) · Zone theme ×4 (mỗi cái 2 stem) · Boss theme ×4 · Chronos theme (3 phase) · Ending ×3 · Sting (giành năng lực). **Nguồn nhạc: chưa chốt** (§14).

---

## 8. ĐỒ HỌA & ASSET

### 8.1. Hiện có / cần thêm

| Nhóm | Hiện có | Còn thiếu |
|---|---|---|
| Tileset Z1–Z2 | Kenney Pixel Platformer + Industrial (`Assets/Art/Sprites/`) | Tile phân biệt **Prime / Echo / Neutral**, tile cổng, công tắc, gai, Energy Gate |
| Tileset Z3–Core | — | Rừng pha lê, hầm mộ, Core |
| Kael (áo choàng, Găng Chrono) | — | Sprite riêng (placeholder: nhân vật Kenney) |
| Kẻ địch (5 loại) + boss (5) | — | Sprite riêng |
| VFX | — | Impact particles, shockwave, ghost trail, bụi chân |
| UI/Icon | — | Icon phím/tay cầm (Kenney Input Prompts Pixel), icon kỹ năng, ổ khóa bản đồ |
| Font | Orbitron, Space Mono (đã cài, `Assets/Fonts/`) | Tạo TMP Font Asset |
| Audio | — | Toàn bộ (§7) |

### 8.2. Animation set (mặc định 8 frame; nhịp 12 fps trừ khi ghi khác)

| Nhân vật | Animation |
|---|---|
| Kael | Idle(6), Run(8), Jump(3), Fall(3), Land(3), WallSlide(2), Dash(3), Attack1/2/3 (4/5/6), AirAttack(4), Hurt(2), Death(8), Shift(3), Cast(4), Interact(4) |
| Crawler | Walk(6), Alert(3), Charge(4), Death(6) |
| Weaver | Fly(4), Charge(6), Shoot(3), Death(6) |
| Rift Knight | Walk(6), Swing(6), Stomp(8), ShieldUp(3), Death(8) |
| Bosses | Idle, từng state ở bảng §5, Death |

- Khung sprite Kael: chuẩn bị **32×32 px** (hitbox 14×26 nằm trong).
- Import: Sprite Multiple, PPU 16, Point, Uncompressed (đã auto qua `PixelArtPostprocessor`).
- Tên asset: `spr_<entity>_<anim>_<frame>`; folder theo §12.

### 8.3. Hình ảnh diegetic

| Trạng thái | Prime | Echo |
|---|---|---|
| Viền màn hình | Sương kim loại mỏng, hơi nước | Hạt tím bay ngược lên |
| LUT/nhiệt màu | 5200 K, vàng đồng + xanh rêu | 8500 K, cyber-neon tím sẫm |
| Va chạm | Đặc, bê tông/sắt | Trong suốt nhẹ, viền chấn động |
| Vệt chém | Ngọc bích (emerald) | Tím thạch anh (amethyst) |

Shift: sóng xung kích từ vị trí Kael (shader distortion) 0.18 s.

---

## 9. GIAO DIỆN & TRẢI NGHIỆM

### 9.1. HUD diegetic
- **HP:** sợi cáp phát sáng trên áo choàng. 100–70% sáng đều · 69–30% chớp chậm + khói cánh tay cơ khí · < 30% tắt điện + vignette đỏ đập theo nhịp tim.
- **CE:** 3 vòng kim loại xoay trên Găng Chrono; đầy = xoay đều + hạt sáng; dùng kỹ năng = vòng dừng và tụt.
- **Tùy chọn HUD phụ:** bật `Accessible HUD` để hiện thanh HP/CE cố định góc trái trên (cho người khó đọc diegetic).
- Đã có `RealityUIIndicator` (dùng `OnGUI`): **chỉ dùng tạm cho debug**, sẽ thay bằng UGUI/TMP.

### 9.2. Menu
Main Menu (Continue, New Game, Load, Settings, Credits, Quit) · Pause (Resume, Map, Settings, Quit to Menu) · Settings (§9.5) · Bản đồ (§9.3) · Màn hình kỹ năng · Màn hình kết thúc.

### 9.3. Bản đồ (Blueprint)
Phòng đã thăm = trắng; cửa chưa đi = dấu `?` nhấp nháy; ổ khóa hiển thị icon kỹ năng cần (tường đá, cổng trọng lực…). Có marker Trạm Chrono, Monolith, Chrono Heart (ẩn cho tới khi phát hiện).

### 9.4. Đối thoại & Kể chuyện
- **Iris** nói qua khung thoại nhỏ (chữ Space Mono), không dừng game, trừ tại Monolith (khóa input).
- Memory Monolith: chạm `E` → hồi ký 20–40 s, có thể bỏ qua.
- Cutscene: tối đa chuỗi ảnh tĩnh + text; bỏ qua được bằng `Start`.

### 9.5. Cài đặt
Âm lượng (Master/Music/SFX/UI), rebind phím & tay cầm (**bắt buộc**), độ phân giải/fullscreen/vsync, ngôn ngữ, và các mục §9.6.

### 9.6. Accessibility
- **Phân biệt realm không chỉ bằng màu:** Prime = viền liền; Echo = viền đứt + hạt. Tùy chọn **Colorblind** (đổi palette tím/xanh sang cặp cam/xanh dương).
- **Giảm nhấp nháy:** giới hạn cường độ flash lúc Shift ≤ 3 lần/giây và độ sáng ≤ 25%; tùy chọn tắt hẳn flash.
- **Screen shake** thanh trượt 0–100%; tắt hitstop được.
- **Assist Mode:** kéo dài coyote time, giảm sát thương nhận (50%/75%), i-frame dài hơn, tự nhảy được cạnh… (mỗi mục bật độc lập; đánh dấu save là "assisted").
- Chữ ≥ 12 px ở 480×270; tương phản chữ/nền ≥ 4.5:1.

### 9.7. Font (D11)
- **Orbitron** → logo, menu, số HUD, tên khu vực, tên kỹ năng (chỉ ký tự không dấu).
- **Space Mono Regular/Bold** → toàn bộ đoạn văn, đối thoại, mô tả.
- Nếu đa ngôn ngữ: bảng chuỗi (Localization) từ ngày đầu; text tiếng Việt không được dùng Orbitron.

---

## 10. CỐT TRUYỆN — ĐIỀU CHỈNH SO VỚI TÀI LIỆU GỐC

Nội dung nhân vật/thế giới/kết thúc giữ nguyên. Chỉnh **phần thưởng theo màn** để khớp §4:

| Màn | Boss | Nhận được (mới) | Sự kiện tự sự |
|---|---|---|---|
| 1 Thức Tỉnh | Sentinel-01 | Piston Boots (Wall Jump) + Mảnh Lõi #1 | Gặp Iris, học Shift |
| 2 Nhịp Tích Tắc | Keeper Myra | Graviton Core (Gravity Inversion) + Mảnh #2 | Iris tiết lộ Kael là người khởi động thí nghiệm |
| 3 Rừng Phản Chiếu | Doppelganger | Echo Anchor + Mảnh #3 | Kael chấp nhận phần bóng tối |
| 4 Hầm Mộ | Rift Knight Prime | Resonance Strike + Mảnh #4 | Sự thật về Hội Đồng Vô Cực |
| 5 Core | Chronos | Kết thúc | — |

**Kết thúc:** Chronos gục → lựa chọn.
- **A – Reset:** đảo ngược thời gian, Kael bị lãng quên.
- **B – Convergence:** hợp nhất hai realm.
- **C – Void Sovereign (bí mật):** chỉ hiện nếu **đọc đủ 12 Monolith**; phá Chrono Core.

---

## 11. DỮ LIỆU (ScriptableObjects)

| Asset | Trường chính |
|---|---|
| `PlayerStats` | Bảng §3.2–3.3 |
| `FeelProfile` | Bảng §3.5 |
| `EnemyData` | §5 |
| `BossPhaseData` | HP, danh sách pattern, ngưỡng chuyển |
| `AbilityData` | id, tên, icon, mô tả, cờ mở khóa |
| `RoomData` | id, zoneId, bounds, tile layers, nhạc |
| `DialogueData` | id, speaker, dòng, điều kiện |
| `AudioSet` | `SFX_ID → clip[]`, pitch range |

Không hard-code số cân bằng trong script.

---

## 12. KIẾN TRÚC CODE

### 12.1. Cấu trúc thư mục
```
Assets/
  Art/{Sprites,Animations,VFX,UI,Tilesets}/
  Audio/{Music,SFX,Mixers}/
  Fonts/
  Data/                (ScriptableObjects)
  Prefabs/{Player,Enemies,Bosses,Environment,UI}/
  Scenes/{Boot,MainMenu,Zone1..Zone4,Core}/
  Scripts/
    Core/  Player/  Combat/  Enemies/  Bosses/  Environment/
    UI/  Audio/  Save/  Data/  Feel/  Input/
  Editor/
```
Namespace `EchoOfTheVoid.<Module>`. Chia asmdef: `Core`, `Gameplay`, `UI`, `Editor` để rút ngắn thời gian compile.

### 12.2. Mô-đun và nguyên tắc

| Mô-đun | Trách nhiệm |
|---|---|
| `Core` | `RealityManager`, `RealityEventBus`, `GameState`, `PersistentId`, service locator nhẹ |
| `Player` | FSM + `PlayerInput`, `PlayerStats`, `AbilitySet` |
| `Combat` | `Hitbox/Hurtbox`, `DamageInfo`, `Health`, `Poise`, `AffinityCalculator` |
| `Enemies`/`Bosses` | FSM dùng chung `IState<T>`; dữ liệu từ SO |
| `Feel` | `HitStop`, `CameraShake`, `SquashStretch`, `GhostTrail` (subscribe event, không bị gọi trực tiếp) |
| `Audio` | `AudioManager`, `MusicLayerController`, `SfxPlayer` |
| `Save` | `SaveService`, `SaveData`, migrate |
| `UI` | HUD, menu, map, dialogue (UGUI + TMP) |

**Event chính (Observer):** `OnRealmSwitched`, `OnDamageDealt`, `OnPlayerDamaged`, `OnEnemyKilled`, `OnBossPhaseChanged`, `OnAbilityUnlocked`, `OnRoomEntered`, `OnPlayerDied`, `OnPlayerRespawn`, `OnCheckpointReached`.

**Nguyên tắc:**
- Đối tượng chỉ lắng nghe sự kiện; không `FindObjectOfType` trong gameplay.
- Chỉ `MonoBehaviour` ở rìa; logic tính toán (`AffinityCalculator`, `DamageCalculator`) là class thuần C# để test.
- Xóa mọi subscribe ở `OnDisable`/`OnDestroy` (đã đúng trong `RealityPlatform`).

### 12.3. Việc lệch giữa code hiện tại và spec

| Hiện trạng | Cần làm |
|---|---|
| `PlayerController` đơn khối, không FSM | Tách state theo §3.2 |
| Đọc phím trực tiếp (`Keyboard.current`), `E` cũng là Shift | Dùng `InputSystem_Actions` + đổi theo D10 |
| `dashSpeed = 24` | 20 (D4); thêm cooldown 0.8 s + reset khi chém trúng |
| `groundLayer = ~0` (mọi layer) | Chỉ `Neutral/PrimeSolid/EchoSolid` |
| `RealityPlatform` chỉ toggle collider | Thêm `OverlapBox` chặn Shift + IgnoreLayerCollision theo §2.2 |
| Không có combat/Health/Enemy | Xây theo §3.4, §5 |
| `RealityUIIndicator` dùng `OnGUI` | Thay UGUI |
| TagManager chưa có layer riêng | Tạo theo §2.2 |

---

## 13. LỘ TRÌNH & TIÊU CHÍ HOÀN THÀNH

Theo `l_tr_nh_ph_t_tri_n_to_n_di_n.md`, thêm tiêu chí kiểm tra:

| Mốc | Nội dung | Tiêu chí hoàn thành (DoD) | Hiện tại |
|---|---|---|---|
| **M1 Greybox** | Bước 1–2: controller, jump, coyote, buffer | Nhảy đúng 3.5 tiles ±0.1, thời gian đỉnh 0.35 s ±0.03 (test tự động EditMode/PlayMode) | Có 1 phần |
| **M2 Reality Shift** | Bước 3 | Shift trong 1 frame; chặn Shift khi đè; shader/LUT đổi tức thì; 3 phòng thử nghiệm | Có 1 phần |
| **M3 Core loop** | Bước 4 + combat cơ bản | Combo 3 đòn, Health/Hitbox, Crawler + Strider; playtest nội bộ 10 phút không lỗi | Chưa |
| **M4 Vertical Slice (Z1)** | Bước 5–8: art, level 1-1, Sentinel-01, audio | 18 phòng chơi được từ đầu tới boss; hết audio/VFX cốt lõi; save/load; 60 FPS | Chưa |
| **M5 Full Production** | Bước 9–10: Z2–Core, UI, narrative, save đủ | Toàn bộ 5 khu chơi được; 3 kết thúc; rebind hoạt động | Chưa |
| **M6 Polish** | Bước 11–12 | Không lỗi chặn tiến trình; 3 tay cầm đã test; hiệu năng đạt §2.4 | Chưa |
| **M7 Launch** | Bước 13 | Demo Z1 lên itch.io/Steam; trang store; bản build cho macOS + Windows | Chưa |

**Quy tắc cổng:** không bắt đầu M4 nếu chưa qua playtest M3 (game feel đạt).

### QA
- **Test tự động (PlayMode/EditMode):** thông số nhảy/dash, `AffinityCalculator`, `DamageCalculator`, save/load round-trip, trình tự mở khóa năng lực, chặn Shift.
- **Blind playtest:** ≥ 5 người ngoài dự án cho Z1; đo điểm kẹt > 3 phút và điểm chết liên tiếp > 8 lần.
- **Cấu hình test:** bàn phím, Xbox, PS, Switch Pro; 1080p/1440p/ultrawide (không lộ phần ngoài phòng).

---

## 14. CÂU HỎI MỞ & RỦI RO

| # | Vấn đề | Đề xuất mặc định | Cần quyết định trước |
|---|---|---|---|
| Q1 | **Nguồn nhạc** 2 stem/khu | Nhạc tự làm/thuê hoặc tạo bằng công cụ AI (kiểm tra bản quyền) | M4 |
| Q2 | **Sprite Kael + quái/boss** riêng | Placeholder Kenney tới hết M3; tìm pack/thuê từ M4 | M4 |
| Q3 | Ngôn ngữ phát hành | Tiếng Anh gốc + tiếng Việt (localization ngay từ đầu, §9.7) | Trước M4 |
| Q4 | Checkpoint giữa các phase của Chronos | Có, ở đầu Phase 3 | M5 |
| Q5 | Số liệu Myra, Doppelganger, Rift Knight Prime | Lấy §5.3 làm giá trị khởi đầu | M5 |
| Q6 | Kích thước trạm dịch chuyển nhanh | Bật sau khi qua boss Z2 | M5 |
| Q7 | Có New Game+ không | Không, ngoài phạm vi | Sau M7 |

**Rủi ro chính**
1. **Phạm vi (~90 phòng):** rủi ro lớn nhất với dự án cá nhân. Giảm: khóa Demo = Z1; chỉ mở rộng khi Z1 vượt qua playtest.
2. **Mù màu + nhấp nháy khi Shift:** đã xử lý ở §9.6; phải test sớm.
3. **Asset nhân vật:** không có nguồn miễn phí khớp phong cách Kael. Giảm: giữ placeholder tới khi tay nghề/ngân sách rõ.
4. **Cân bằng số liệu D1–D3:** giá trị có thể sai khi chơi thật. Toàn bộ nằm trong SO (`PlayerStats`) để chỉnh nhanh.
5. **Đồng bộ Realm và vật lý:** vật lý đổi trong cùng 1 frame có thể tạo chồng lấn với Kael; xử lý bằng cơ chế chặn ở §2.2.

---

## PHỤ LỤC A — Số liệu suy ra nhanh

| Đại lượng | Giá trị |
|---|---|
| Hitbox Kael | 0.875 × 1.625 tiles |
| $V_0$ | 20 tiles/s |
| $g$ / $g_{fall}$ | 57.14 / 91.43 tiles/s² |
| Thời gian rơi từ đỉnh | ≈ 0.277 s |
| Thời gian bay tổng của 1 cú nhảy phẳng | ≈ 0.63 s |
| Tầm nhảy ngang tối đa | ≈ 7.5 tiles (dùng ≤ 6 khi thiết kế) |
| Dash | 4 tiles, 20 tiles/s |
| Khung nhìn | 30 × 16.875 tiles |

## PHỤ LỤC B — Nguồn gốc của mỗi mục

| Mục spec | Tài liệu gốc |
|---|---|
| §3.1–3.2 | `h_th_ng_c_ch_k_thu_t_v_gameplay.md`, `ki_n_tr_c_ph_n_m_m_v_code_m_u.md` |
| §3.3–3.4, §5 | `b_ng_thu_c_t_nh_nh_n_v_t_v_k_ch.md` |
| §3.5 | `game_feel_v_feedback_k_thu_t.md` |
| §4 | `s_m_kh_a_metroidvania.md`, `ph_n_t_ch_kho_ng_tr_ng_thi_t_k.md` |
| §7 | `k_thu_t_m_thanh_t_ng_t_c.md` |
| §8.3, §9 | `thi_t_k_giao_di_n_tr_c_quan.md` |
| §10 | `c_t_truy_n_v_th_gi_i_quan.md` |
| §13 | `l_tr_nh_ph_t_tri_n_to_n_di_n.md` |
