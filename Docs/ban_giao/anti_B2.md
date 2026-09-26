# Bàn giao B2: Bộ Sprite Kẻ Địch Dark Fantasy Aether-punk (Đa dạng 10 Archetypes - 82+ Sprites)

- **Người làm:** anti
- **Ngày bàn giao:** 2026-09-26
- **Ưu tiên:** B2 & Mở rộng hệ sinh thái quái vật đa dạng (Bestiary Expansion)

---

## 1. Mục tiêu hoàn thành
Mở rộng hệ sinh thái quái vật từ 5 loài cơ bản lên **10 Chủng loài Đa dạng (Archetypes)** đặc trưng của dòng game Metroidvania / Dark Fantasy Gothic Aether-punk:
- **Đầy đủ 2 Realm:** Tất cả đều có phiên bản **Prime Realm (Lõi Neon Cyan `#00D9FF`)** và **Echo Realm (Lõi Neon Orange `#FF6B35`)**.
- **Độ phân giải chuẩn:** PPU = 32 (pixel-perfect), Point Filter, Pivot chuẩn `(0.5, 0.0)` cho quái đất, `(0.5, 0.5)` cho quái bay, `(0.5, 0.9)` cho quái bám trần.
- **Tập hợp 10 vai trò chiến thuật:** Đảm bảo độ sâu cho màn chơi (Level Design) từ quái tuần tra, quái phục kích trần nhà, bẫy dịch thể phân tách, xạ thủ bắn tỉa, tới quái tạ đập đất và pháp sư triệu hồi.

---

## 2. Danh mục 10 Archetypes Kẻ Địch (`Assets/Art/Sprites/Enemies/DarkFantasy/`)

### I. 5 Chủng Loài Cơ Bản Đã Nâng Cấp
1. **Rift Knight (64×64 - Heavy Melee Knight)**
   - Sprites: `rift_knight_{prime/echo}_idle.png`, `slash.png`, `stomp.png`
   - Vai trò: Đấu sĩ thiết giáp, khiên tháp và cự kiếm, dậm đất tạo địa chấn.
2. **Void Strider (64×64 - Fast Predator)**
   - Sprites: `void_strider_{prime/echo}_idle.png`, `slash.png`, `pounce.png`
   - Vai trò: Quái săn mồi di chuyển nhanh, chân khớp ngược, vồ mồi từ trên không.
3. **Void Weaver (48×48 - Ranged Caster)**
   - Sprites: `void_weaver_{prime/echo}_idle.png`, `shoot.png`
   - Vai trò: Bóng ma áo choàng bay lơ lửng, tích tụ cầu hư không bắn tầm xa.
4. **Chrono Crawler (48×48 - Armored Beetle)**
   - Sprites: `chrono_crawler_{prime/echo}_walk.png`, `charge.png`
   - Vai trò: Bọ thiết giáp bánh răng bò đất, tích điện húc thẳng.
5. **Prism Sentry (48×48 - Laser Turret)**
   - Sprites: `prism_sentry_{prime/echo}_idle.png`, `beam.png`
   - Vai trò: Con mắt pha lê bay lơ lửng, quét và khóa mục tiêu bắn laser thẳng.

### II. 5 Chủng Loài Mới Bổ Sung (Đa dạng hóa Hệ sinh thái)
6. **Blight Gargoyle (48×48 - Ceiling Ambusher & Dive Bomber)**
   - Sprites:
     - `blight_gargoyle_{prime/echo}_perch.png`: Bám trần/gờ tường, thu cánh ngụy trang thành tượng đá.
     - `blight_gargoyle_{prime/echo}_swoop.png`: Bung cánh sà xuống bổ nhào với vận tốc cao.
     - `blight_gargoyle_{prime/echo}_slash.png`: Vung móng vuốt cào xé mục tiêu.
   - Gameplay: Trị lối chơi rush nhanh của player ở các hành lang cao, tạo bất ngờ.

7. **Void Ooze & Mini Spawn (32×32 & 20×20 - Splitting Biohazard)**
   - Sprites:
     - `void_ooze_{prime/echo}_crawl.png`: Khối nhờn bò trườn co giãn.
     - `void_ooze_{prime/echo}_jump.png`: Nảy vọt lên cao bắn giọt acid.
     - `void_ooze_{prime/echo}_split.png`: Phân chia tế bào khi bị tiêu diệt.
     - `void_ooze_spawn_{prime/echo}_hop.png`: Slime con nảy lắt nhắt gây phiền toái.
   - Gameplay: Bẫy hầm ngục hẹp, khi chết phân chia thành 2 quái con.

8. **Void Cultist (48×48 - Backline Hexer & Ritual Summoner)**
   - Sprites:
     - `void_cultist_{prime/echo}_idle.png`: Tà áo rách bay phập phồng, cầm trượng đầu lâu.
     - `void_cultist_{prime/echo}_cast.png`: Giơ cao trượng tích tụ quả cầu nguyền rủa.
     - `void_cultist_{prime/echo}_ritual.png`: Cắm trượng tạo vòng tròn ma pháp nổ dưới chân player.
   - Gameplay: Quái hỗ trợ đứng sau đội hình, buộc người chơi phải dash vào ám sát trước.

9. **Crystalline Golem (64×64 - Heavy Bruiser / Unstoppable Colossus)**
   - Sprites:
     - `void_golem_{prime/echo}_idle.png`: Quái đá tinh thể khổng lồ, mắt cyclops rực sáng.
     - `void_golem_{prime/echo}_smash.png`: Giáng tay búa đá xuống đất tạo sóng chấn động.
     - `void_golem_{prime/echo}_charge.png`: Húc vai càn quét với khiên chắn tinh thể.
   - Gameplay: Siêu quái trâu bò, miễn nhiễm stagger đòn đánh nhẹ, yêu cầu né tránh hoặc dùng kỹ năng không gian.

10. **Bone Skulker (48×48 - Gothic Long-range Crossbowman)**
    - Sprites:
      - `bone_skulker_{prime/echo}_idle.png`: Bộ xương khoác khăn choàng canh gác.
      - `bone_skulker_{prime/echo}_aim.png`: Giương nỏ ngắm thẳng với tia laser định vị.
      - `bone_skulker_{prime/echo}_shoot.png`: Bắn tên hư không xuyên thấu có vệt neon.
    - Gameplay: Đặt trên các mỏm đá cao/platform xa, ép người chơi phải vừa di chuyển né tên vừa leo trèo.

### III. Master Showcase Preview
- `dark_fantasy_bestiary_preview.png`: Bảng tổng hợp showcase đầy đủ cả 10 Chủng Loài Kẻ Địch đặt song song 2 thể Prime & Echo.

---

## 3. Hướng dẫn Tích hợp dành cho Codex & Claude
1. **Asset Location:** `Assets/Art/Sprites/Enemies/DarkFantasy/`
2. **Import Settings:** Đã cấu hình sẵn toàn bộ `.meta` file:
   - `FilterMode: Point (no filter)`
   - `TextureFormat: RGBA32 (no compression)`
   - `Pixels Per Unit: 32`
3. **Phân công tiếp theo:**
   - **Codex:** Kế thừa `EnemyBase` để viết AI behavior cho 5 quái mới (`BlightGargoyle.cs`, `VoidOoze.cs`, `VoidCultist.cs`, `CrystallineGolem.cs`, `BoneSkulker.cs`).
   - **Claude:** Tạo Prefab trong `Assets/Prefabs/Enemies/` và đưa vào các màn chơi mở rộng (Zone 2, Zone 3).
