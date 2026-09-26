# Bàn giao B2: Bộ Sprite Kẻ Địch Dark Fantasy Aether-punk (50 Sprites)

- **Người làm:** anti
- **Ngày bàn giao:** 2026-09-26
- **Ưu tiên:** B2 (Sprite kẻ địch: Rift Knight, Void Strider, Chrono Crawler, Void Weaver, Prism Sentry)

---

## 1. Mục tiêu hoàn thành
Thay thế toàn bộ các sprite placeholder cũ (16x16 / 32x24 thô sơ) bằng bộ sprite **Dark Fantasy Gothic Aether-punk** chi tiết cao, thiết kế đồng bộ với tone Void của game:
- Bộ giáp Gothic thép đen, viền sừng, gai nhọn hư không.
- Đồng bộ cơ chế 2 Realm: **Prime Realm (Lõi Neon Cyan `#00D9FF`)** và **Echo Realm (Lõi Neon Orange `#FF6B35`)**.
- Độ phân giải chuẩn PPU = 32 (hoặc 64 tùy scale prefab), Pixel-Perfect (Point Filter, no compression, no anti-aliasing).
- Đầy đủ `.meta` file có cấu hình sẵn Pivot chuẩn (Bottom-Center `(0.5, 0.0)` cho quái đất, Center `(0.5, 0.5)` cho quái bay).

---

## 2. Danh mục Assets bàn giao (`Assets/Art/Sprites/Enemies/DarkFantasy/`)

### A. Rift Knight (64×64 - Heavy Gothic Plate Armor, Horned Great-Helm, Tower Shield & Greatsword)
- `rift_knight_prime_idle.png` & `rift_knight_echo_idle.png`: Tư thế thủ với khiên tháp và cự kiếm sau lưng.
- `rift_knight_prime_slash.png` & `rift_knight_echo_slash.png`: Đòn chém vung cự kiếm kèm vệt chém lưỡi liềm năng lượng Void.
- `rift_knight_prime_stomp.png` & `rift_knight_echo_stomp.png`: Đòn dậm đất tạo sóng xung kích địa chấn.

### B. Void Strider (64×64 - Shadow Predator, Digitigrade Legs, Spine Spikes, Twin Energy Claws)
- `void_strider_prime_idle.png` & `void_strider_echo_idle.png`: Tư thế rình mồi cúi thấp, gai lưng phát sáng.
- `void_strider_prime_slash.png` & `void_strider_echo_slash.png`: Đòn cào vuốt đôi cận chiến với tia vuốt năng lượng.
- `void_strider_prime_pounce.png` & `void_strider_echo_pounce.png`: Tư thế vồ mồi từ trên không (Apex Pounce).

### C. Void Weaver (48×48 - Tattered Hooded Specter with Glowing Void Orb)
- `void_weaver_prime_idle.png` & `void_weaver_echo_idle.png`: Áo choàng rách bay lơ lửng, ánh mắt ma quái trong mũ trùm, cầu năng lượng trước ngực.
- `void_weaver_prime_shoot.png` & `void_weaver_echo_shoot.png`: Giơ tay bắn đạn cầu hư không Rift Projectile.

### D. Chrono Crawler (48×48 - Gothic Clockwork Scarab, Bladed Mandibles & Clockwork Core)
- `chrono_crawler_prime_walk.png` & `chrono_crawler_echo_walk.png`: Bọ cơ khí 6 chân gai nhọn, mai thép viền đồng.
- `chrono_crawler_prime_charge.png` & `chrono_crawler_echo_charge.png`: Nạp điện tích năng lượng vào cặp kìm sắc nhọn để lao tới húc.

### E. Prism Sentry (48×48 - Floating Gothic Reliquary Eyeball & Focusing Energy Prism)
- `prism_sentry_prime_idle.png` & `prism_sentry_echo_idle.png`: Con mắt pha lê lơ lửng với các vòng xoay cơ khí viền vàng đồng.
- `prism_sentry_prime_beam.png` & `prism_sentry_echo_beam.png`: Khẩu pháo mắt phát tia laser bắn thẳng.

### F. Master Showcase Preview
- `dark_fantasy_bestiary_preview.png`: Bảng tổng hợp showcase tất cả các chủng loài kẻ địch đặt cạnh nhau để duyệt phong cách đồ họa.

---

## 3. Trạng thái đáp ứng Yêu cầu giữa các Agent
- Giải quyết trực tiếp yêu cầu từ Codex trong `Docs/yeu_cau_giua_agent.md`:
  > *"Kích thước và thông số Visual cho Rift Knight (K5) và Boss Z2 Myra & Z3 Doppelganger (K6) | Assets/Art/Sprites/Enemies/ & Bosses/"*
- Các prefab `RiftKnight.prefab`, `VoidStrider.prefab`, `PrismSentry.prefab` giờ đây đã có sẵn sprite Visual chuẩn Dark Fantasy để gắn trực tiếp vào `SpriteRenderer` của child `Visual` mà không làm lệch root scale hay collider.

---

## 4. Cần Claude / Codex nối gì
1. **Prefab Visual:** Kéo sprite `*_idle.png` tương ứng vào `SpriteRenderer.m_Sprite` của child `Visual` trong:
   - `Assets/Prefabs/Enemies/RiftKnight.prefab`
   - `Assets/Prefabs/Enemies/VoidStrider.prefab`
   - `Assets/Prefabs/Enemies/PrismSentry.prefab`
2. **Animation:** Nối các frame action (`slash`, `stomp`, `pounce`, `shoot`, `charge`) vào `AnimationClip` của từng quái theo state trong `EnemyAnimationDriver`.
