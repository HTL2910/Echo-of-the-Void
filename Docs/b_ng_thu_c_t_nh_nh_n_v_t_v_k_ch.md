# TÀI LIỆU THIẾT KẾ: HỆ THỐNG THUỘC TÍNH, KẺ ĐỊCH & COMBAT

---

## 1. BẢNG THUỘC TÍNH NHÂN VẬT CHÍNH (KAEL)

### 1.1. Chỉ số Sinh tồn & Năng lượng
* **HP Tối đa:** 100 điểm.
* **Quy tắc hồi phục:** Không tự hồi máu. Hồi 25 HP khi tương tác tại Trạm Chrono (Save Point) hoặc tiêu diệt quái bằng đòn kết liễu *Resonance Strike*.
* **Năng lượng Chrono (CE):** 100 điểm.
  * Tốc độ hồi thụ động: 15 CE/giây khi chân chạm đất.
  * Đánh trúng đòn chém cơ bản: Hồi +10 CE/hit.
* **Thời gian bất tử (i-frames):** 0.8 giây sau khi nhận sát thương; nhân vật nhấp nháy đỏ mờ.

### 1.2. Chỉ số Động lực học & Di chuyển
* **Tốc độ chạy cơ bản ($V_{\text{run}}$):** $8.0\text{ m/s}$.
* **Lực bật nhảy ($V_{\text{jump}}$):** $14.5\text{ m/s}$ (Độ cao tối đa: $3.5\text{ tiles}$, thời gian bay lên: $0.35\text{s}$).
* **Hệ số trọng lực khi rơi:** $1.8\times$ (Tạo cảm giác tiếp đất nhanh và chắc chắn).
* **Lướt không gian (Phase Dash):**
  * Khoảng cách lướt: $6\text{ m}$ ($3.75\text{ tiles}$).
  * Thời gian hoàn tất lướt: $0.2\text{s}$.
  * Thời gian bất tử (i-frame): $0.2\text{s}$ (Trùng với toàn bộ thời gian lướt).
  * Thời gian hồi chiêu (Cooldown): $0.8\text{s}$ (Reset ngay lập tức khi chạm sàn hoặc chém trúng quái).

### 1.3. Bảng Sát thương & Hitbox
| Đòn tấn công | Sát thương | Phạm vi tác động | Đẩy lùi (Knockback) | Frame Khởi động / Hoạt ảnh |
| :--- | :--- | :--- | :--- | :--- |
| **Combo 1 (Chém ngang)** | 15 DMG | $1.8\text{ m}$ phía trước | $0.5\text{ m}$ | 3 frames / 12 frames |
| **Combo 2 (Chém chéo)** | 18 DMG | $2.0\text{ m}$ góc $45^\circ$ | $1.0\text{ m}$ | 4 frames / 14 frames |
| **Combo 3 (Đâm phá giáp)**| 28 DMG | $2.6\text{ m}$ thẳng hàng | $3.5\text{ m}$ (Hất văng) | 6 frames / 20 frames |
| **Air Slash (Chém trên không)**| 20 DMG | Vòng cung $180^\circ$ | Treo lơ lửng $0.1\text{s}$ | 3 frames / 10 frames |
| **Resonance Strike (Kỹ năng)**| 65 DMG | Tia sóng $8.0\text{ m}$ | $4.0\text{ m}$ | Tiêu hao 50 CE, 8 frames vận chiêu |

---

## 2. QUY TẮC THỰC TẠI TƯƠNG TÁC (REALITY AFFINITY MATRIX)

Mỗi thực thể sở hữu một thuộc tính: **Prime (Hiện Sinh)** hoặc **Echo (Nghịch Ảnh)**.

```
                  ┌─────────────────────────────────────┐
                  │ Sát thương Kael tác động lên Quái   │
┌─────────────────┼──────────────────┬──────────────────┤
│ Thuộc tính Kael │ Quái hệ PRIME    │ Quái hệ ECHO     │
├─────────────────┼──────────────────┼──────────────────┤
│ Trạng thái PRIME│ 100% Sát thương  │ 20% (Giáp hư vô) │
├─────────────────┼──────────────────┼──────────────────┤
│ Trạng thái ECHO │ 20% (Giáp hư vô) │ 100% Sát thương  │
└─────────────────┴──────────────────┴──────────────────┘
```

*Lưu ý Game Design:* Khi quái nhận sát thương 20% do lệch hệ thực tại, phát ra âm thanh kim loại cùn vang lên và hiệu ứng chữ "DEFLECT", nhắc nhở người chơi nhấn phím chuyển trạng thái ngay lập tức.

---

## 3. THIẾT KẾ KẺ ĐỊCH THƯỜNG (MOB SYSTEM)

### 3.1. Chrono-Crawler (Bọ Kim Loại Tuần Tra)
* **Vai trò:** Quái tản bộ tầng đáy, dạy cơ chế nhảy qua hoặc chém combo cơ bản.
* **Chỉ số:**
  * HP: 40
  * Sát thương va chạm: 10 DMG
  * Tốc độ di chuyển: $3.2\text{ m/s}$
  * Hệ thực tại: Cố định **PRIME**
* **Logic AI (State Machine):**
  * `PATROL`: Di chuyển qua lại trên một bề mặt, dùng raycast hướng xuống phía trước để phát hiện mép vực (Edge Detector) và đảo chiều.
  * `ALERT`: Khi Kael bước vào bán kính $5\text{ m}$, dừng lại 0.3s, mắt đổi sang màu đỏ.
  * `CHARGE`: Lao tới Kael với tốc độ $5.0\text{ m/s}$. Không thể dừng lại giữa chừng nếu bị hụt.
  * `DEATH`: Nổ tung tạo mảnh vỡ kim loại không gây sát thương, rớt 5 CE.

### 3.2. Void Weaver (Nhện Bay Bắn Tỉa)
* **Vai trò:** Ép người chơi phải nhảy platform trên không và né đạn tầm xa.
* **Chỉ số:**
  * HP: 35
  * Sát thương đạn: 18 DMG
  * Tốc độ bay: $2.5\text{ m/s}$
  * Hệ thực tại: Cố định **ECHO**
* **Cơ chế đạn đặc biệt:** Viên đạn hình cầu tím. Nếu Kael đang ở trạng thái *Prime*, viên đạn đi xuyên qua người không gây hại. Nếu Kael đổi sang *Echo*, viên đạn lập tức trở thành vật thể vật lý phát nổ khi tiếp xúc.
* **Logic AI:**
  * Luôn duy trì khoảng cách $6.0\text{ m}$ so với Kael.
  * Khi Kael tiếp cận gần hơn $3.0\text{ m}$, lùi lại phía sau.
  * Chu kỳ bắn: Mỗi $3.0\text{s}$, tích tụ năng lượng $0.8\text{s}$ (có vệt ngắm laser báo trước), bắn 1 tia năng lượng vận tốc $12\text{ m/s}$.

### 3.3. Rift Knight (Hiệp Sĩ Song Trùng)
* **Vai trò:** Quái hạng nặng; kiểm tra khả năng phối hợp giữa lướt né đòn và đổi thực tại.
* **Chỉ số:**
  * HP: 130
  * Sát thương chém ngang: 25 DMG
  * Sát thương dậm đất: 35 DMG (Tạo sóng chấn động mặt đất cao $1.2\text{ m}$)
  * Tốc độ di chuyển: $3.8\text{ m/s}$
  * Hệ thực tại: **BIẾN ĐỔI CHỦ ĐỘNG** (Tự đổi hệ sau mỗi 4 giây hoặc sau khi dính 3 đòn liên tiếp).
* **Cơ chế phòng thủ:** Cầm khiên phía trước chặn $100\%$ sát thương trực diện. Kael phải lướt ra sau lưng (*Phase Dash*) để gây sát thương.

---

## 4. MINI-BOSS & BOSS SPECIFICATIONS

### 4.1. Mini-Boss: Sentinel-01 (Pháo Đài Sứt Mẻ)
* **HP:** 600
* **Thanh Kháng Choáng (Poise Bar):** 100 điểm. Giảm khi nhận đòn combo thứ 3 hoặc Resonance Strike. Khi Poise = 0, gục ngã trong $3.5\text{s}$ (nhận thêm $50\%$ sát thương).
* **Cơ chế Chia Nửa Thực Tại:**
  * Thân trên: Mang hệ **ECHO** (Phát sáng tím).
  * Chân & Bánh xích: Mang hệ **PRIME** (Kim loại xanh).
* **Vòng lặp tấn công:**
  1. *Xung Kích Đẩy Lùi:* Quét chân xoay vòng $360^\circ$ (buộc người chơi nhảy lên).
  2. *Mưa Hỏa Tiễn:* Bắn 4 quả tên lửa rơi xuống các tọa độ cố định trên sàn đấu (hiển thị vòng tròn cảnh báo trước 1.2s).

---

### 4.2. Boss Cuối: Chronos – Hiện Thân Của Vết Nứt

#### Giai đoạn 1: Kẻ Điều Phối (HP: 1500)
* **Tốc độ di chuyển:** $6.0\text{ m/s}$ (Lướt không để lại dấu vết).
* **Pattern 1 - Reality Cleave (Phân Tách Không Gian):** Chém kiếm chia đôi sàn đấu. Nửa trái thành Prime, nửa phải thành Echo. Nếu Kael đứng ở bên Prime mà mang dạng Echo, nhận $12\text{ DMG/s}$.
* **Pattern 2 - Chrono Burst:** Bắn chùm 8 tia đạn xoay tròn như cánh quạt đồng hồ. Người chơi phải căn nhịp lướt qua kẽ hở giữa các tia.

#### Giai đoạn 2: Nghịch Lý Thời Gian (HP: 1800 - Kích hoạt khi hết HP Giai đoạn 1)
* **Cơ chế Time Rewind (Tua Ngược):** Mỗi khi mất $400\text{ HP}$, Chronos để lại một dư ảnh, lùi thời gian về $2.5\text{s}$ trước đó và giải phóng sóng chấn động toàn map (phải dùng *Phase Dash* trong đúng frame nổ để sống sót).
* **Pattern 3 - Sàn Đấu Rơi:** Một nửa số bệ đỡ trên trần nhà và mặt đất tan biến vào hư vô; buộc người chơi phải bám tường và dùng *Echo Anchor* để giữ vị trí trên không.

#### Giai đoạn 3: Điểm Kỳ Dị Hư Vô (HP: 1000 - Enrage Mode)
* Toàn bộ màn hình chuyển sang tông màu âm bản xám - đen.
* Trọng lực tự động đảo ngược mỗi 6 giây một lần.
* Chronos liên tục biến mất và xuất hiện ngay sau lưng người chơi với đòn chém kết liễu gây $50\text{ DMG}$ (thời gian báo hiệu trước đòn chém: chỉ $0.4\text{s}$).