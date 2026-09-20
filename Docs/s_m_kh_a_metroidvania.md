# BẢN ĐỒ TIẾN TRÌNH & ABILITY GATING (METROIDVANIA GRAPH)

Tài liệu này xác định tính phi tuyến tính, các ổ khóa logic (Ability Locks) và chuỗi mở rộng bản đồ của *Echo of the Void*.

---

## 1. MA TRẬN KHÓA & CHÌA (LOCK & KEY MATRIX)

```
[KỸ NĂNG / CHÌA KHÓA]             [CHƯỚNG NGẠI VẬT / KHÓA]              [KHU VỰC TIẾP CẬN MỚI]
├── Basic Reality Shift  ───────►  Vực nứt bệ nhảy luân phiên    ───────►  Khu Vực 1: Tàn Tích Hoang Phế
├── Wall Slide & Dash    ───────►  Vách trơn hẹp & vực rộng 5m   ───────►  Khu Vực 2: Tháp Đồng Hồ Ngầm
├── Gravity Inversion    ───────►  Trần nhà gai nhọn & giếng sâu ───────►  Khu Vực 3: Khu Rừng Phản Chiếu
├── Echo Anchor (Bóng)   ───────►  Cửa công tắc đúp theo giây    ───────►  Khu Vực 4: Hầm Mộ Nghịch Đảo
└── Resonance Strike     ───────►  Vách pha lê tím phong ấn      ───────►  Tâm Chấn Kỷ Dị (Core Room)
```

---

## 2. TIẾN TRÌNH KHÁM PHÁ CHI TIẾT THEO VÙNG

### Khu Vực 1: Tàn Tích Hoang Phế (The Shattered Outskirts)
* **Khóa Khởi Đầu:** Vực thẳm không đáy với bệ đá Prime nhấp nháy.
* **Chìa khóa nhận được:** Kích hoạt *Găng Chrono* $\to$ Mở khóa chuyển trạng thái Hiện Sinh / Nghịch Ảnh.
* **Đường rẽ nhánh bí mật (Tease):** Một bức tường nứt phát sáng tím (Cần *Resonance Strike* - quay lại sau) chứa Tim Năng Lượng mở rộng (+20 Max HP).

### Khu Vực 2: Tháp Bánh Răng (Clockwork Abyss)
* **Khóa:** Ống khói thông hơi thẳng đứng cao $15\text{ m}$ không có chỗ đứng chân.
* **Vật cản:** Bánh răng quay tốc độ cao luân phiên giữa 2 trạng thái.
* **Chìa khóa nhận được:** Sau khi đánh bại *Sentinel-01*, nhận *Piston Boots* $\to$ Mở khóa kỹ năng **Wall Jump**.
* **Lối tắt mở ngược:** Kéo đòn bẩy mở thang máy quay lại Trạm nghỉ Khu Vực 1.

### Khu Vực 3: Khu Rừng Phản Chiếu (Mirrored Wilds)
* **Khóa:** Vực thẳm với trần nhà bằng phẳng nhưng toàn bộ mặt sàn là biển a-xít thời gian.
* **Chìa khóa nhận được:** Đánh bại *Keeper Myra* $\to$ Hấp thụ *Graviton Core* $\to$ Kỹ năng **Gravity Inversion**.
* **Cơ chế:** Cho phép bám và chạy trên trần nhà ở các vùng có luồng khí tím.

### Khu Vực 4: Hầm Mộ Nghịch Đảo (Catacombs of Duality)
* **Khóa:** Cổng sập cơ khí đóng trong $1.5\text{s}$ sau khi rời khỏi công tắc đạp chân; khoảng cách cửa là $8\text{ m}$.
* **Chìa khóa nhận được:** Hạ gục bóng ma nội tâm (*Mirror Doppelganger*) $\to$ Nhận **Echo Anchor**.
* **Ứng dụng:** Đứng trên công tắc $\to$ Đặt Anchor $\to$ Chạy đến cửa $\to$ Dịch chuyển gián tiếp để kéo đòn bẩy thứ hai từ bên trong.

---

## 3. BIỂU ĐỒ BẢN ĐỒ VĂN BẢN (TOPOLOGY GRAPH)

```
[Khu Vực 1: Tàn Tích] ──(Wall Jump)──► [Khu Vực 2: Tháp Đồng Hồ]
        │                                       │
  (Resonance)                               (Gravity)
        ▼                                       ▼
[Phòng Trùm Ẩn #1]                     [Khu Vực 3: Rừng Phản Chiếu]
                                                │
                                            (Anchor)
                                                ▼
                                       [Khu Vực 4: Hầm Mộ]
                                                │
                                         (Tất cả mảnh vỡ)
                                                ▼
                                       [Tâm Chấn Kỷ Dị - Chronos]
```