# THIẾT KẾ CƠ CHẾ KỸ THUẬT & GAMEPLAY (GDD)

---

## 1. THÔNG SỐ CƠ HỌC NHÂN VẬT (CHARACTER CONTROLLER METRICS)

Mọi thông số được tính toán dựa trên độ phân giải pixel chuẩn ($16 \times 16\text{ px}$ trên 1 tile) với engine 2D (Unity/Godot):

* **Kích thước Hitbox:** $14\text{ px}$ (chiều rộng) $\times 26\text{ px}$ (chiều cao).
* **Tốc độ chạy tối đa ($V_{\max}$):** $12\text{ tiles/second}$.
* **Thời gian gia tốc ($T_{\text{acc}}$):** $0.1\text{s}$ (đáp ứng điều khiển nhạy bén).
* **Thời gian giảm tốc/dừng ($T_{\text{dec}}$):** $0.08\text{s}$ (không trơn trượt).
* **Lực nhảy (Jump Height):** $3.5\text{ tiles}$.
* **Thời gian đạt đỉnh nhảy ($T_{\text{apex}}$):** $0.35\text{s}$.
* **Trọng lực rơi ($G_{\text{fall}}$):** $1.6 \times G_{\text{jump}}$ (tạo cảm giác rơi nhanh, dứt khoát như *Celeste*).
* **Coyote Time:** $0.1\text{s}$ (cho phép nhảy dù chân đã rời khỏi rìa sàn).
* **Jump Buffer:** $0.12\text{s}$ (nhận lệnh nhảy trước khi chạm đất).

---

## 2. CƠ CHẾ CỐT LÕI: REALITY SHIFT (HOÁN ĐỔI THỰC TẠI)

### 2.1. Logic Trạng Thái Toàn Cục
Hệ thống sử dụng một Enum quản lý trạng thái:
```csharp
public enum RealmState { Prime, Echo }
```
Khi người chơi nhấn nút chuyển đổi (`ShiftKey` / `Gamepad R1`):
* Thời gian hồi chiêu (Cooldown): $0.25\text{s}$.
* **Layer Collision Matrix:** Đổi lớp tương tác của Tilemap và Vật thể theo bảng sau:

| Đối Tượng | Trạng Thái: PRIME | Trạng Thái: ECHO |
| :--- | :--- | :--- |
| **Bệ Đá Xanh (Prime Tile)** | Solid (Va chạm cứng) | Pass-through (Đi xuyên, mờ 40%) |
| **Pha Lê Tím (Echo Tile)** | Ghost (Vô hình/Không va chạm) | Solid (Va chạm cứng, có độ nảy) |
| **Bẫy Gai Kim Loại** | Gây chết ngay lập tức | Biến thành đệm nảy (Bouncer pad) |
| **Cổng Lực Trọng Trường** | Vô hiệu | Đảo ngược trục Y của vector trọng lực |

### 2.2. Bộ Kỹ Năng Nhân Vật

```
[BỘ SKILL]
├── Cơ bản
│   ├── Chrono Blade (Combo 3 đòn cận chiến: 15 -> 15 -> 30 Damage)
│   ├── Wall Slide & Jump (Trượt tường ma sát 0.3, bật góc 45 độ)
│   └── Phase Dash (Lướt 4 tiles, 0.2s bất tử, hồi lại khi chạm đất/đánh trúng quái)
└── Mở khóa (Abilities)
    ├── Echo Anchor (Phím F): Tạo bóng tại vị trí cũ, nhấn lại để biến về bóng
    ├── Gravity Flip: Cho phép Kael đảo trọng lực tại các trạm năng lượng
    └── Resonance Blast: Tiêu hao 50% thanh Chrono để bắn đòn tầm xa xuyên giáp
```

---

## 3. THIẾT KẾ KẺ THÙ VÀ AI (ENEMY ARCHETYPES)

### 3.1. Hư Ảnh Tuần Tra (Void Strider)
* **Hành vi:** Tuần tra cố định trên nền tảng. Khi phát hiện Kael (bán kính 6 tiles), tăng tốc lao vào cắn xé.
* **Cơ chế Reality:** Chỉ nhận sát thương đầy đủ khi Kael tấn công ở đúng chiều không gian tương ứng với hào quang của quái (Hào quang Xanh = Prime; Hào quang Tím = Echo).

### 3.2. Cột Pháo Pha Lê (Prism Sentry)
* **Hành vi:** Đứng yên trên trần nhà hoặc tường. Bắn chùm tia năng lượng mỗi $2.5\text{s}$.
* **Cơ chế Reality:** Tia laser ở trạng thái Prime gây sát thương, nhưng khi người chơi đổi sang Echo, chùm tia trở thành "dây cáp năng lượng" mà Kael có thể trượt lên (Rail Grind).

---

## 4. THIẾT KẾ MÀN CHƠI MẪU (LEVEL 1-1 BREAKDOWN)

* **Phân đoạn 1 (Safe Sandbox):** Phòng kín không có bẫy chết, chỉ có 1 bệ xanh và 1 bệ tím. Trên tường hiển thị icon phím bấm. Người chơi buộc phải nhấn Shift mới nhảy được qua gờ tường cao.
* **Phân đoạn 2 (Risk Introduction):** Vực sâu có bẫy gai. Gai ở tầng Prime, nếu chuyển sang Echo sẽ thành bệ nảy giúp vươn tới mỏm đá tiếp theo.
* **Phân đoạn 3 (Skill Chaining):** Nhảy khỏi vách tường $\to$ Đổi thực tại trên không $\to$ Lướt (Dash) xuyên qua cổng năng lượng $\to$ Chém quái để hồi Dash $\to$ Tiếp đất an toàn.