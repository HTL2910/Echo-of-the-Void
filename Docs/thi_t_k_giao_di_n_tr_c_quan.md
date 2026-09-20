# HỆ THỐNG GIAO DIỆN & DIEGETIC INTERFACE (UI/UX)

Tối đa hóa mức độ tập trung (Immersion) bằng cách giảm bớt các thanh hiển thị thông thường, chuyển phần lớn thông tin vào bản thân nhân vật và môi trường.

---

## 1. DIEGETIC HUD (CHỈ BÁO NỘI HÀM TRÊN NHÂN VẬT)

### 1.1. Lượng Máu (Health Indicator)
* Không dùng thanh máu chữ nhật đặt cố định trên góc màn hình.
* **Hiển thị trực quan:** Áo choàng của Kael có dệt các sợi cáp quang năng lượng.
  * Máu $100\% - 70\%$: Sợi cáp phát sáng liên tục, ổn định.
  * Máu $69\% - 30\%$: Sợi cáp chớp nháy chậm; khói mờ bốc ra từ cánh tay cơ khí.
  * Máu $< 30\%$: Áo choàng tắt điện hoàn toàn, viền màn hình xuất hiện hiệu ứng "Vignette" màu đỏ đập theo nhịp tim.

### 1.2. Năng Lượng Chrono (CE Gauge)
* **Hiển thị:** 3 vòng kim loại phát sáng xoay tròn trên Găng Chrono (cánh tay phải Kael).
  * Đầy 100 CE: Cả 3 vòng xoay đồng tốc độ, tỏa hạt ánh sáng ra xung quanh.
  * Khi dùng kỹ năng (Echo Anchor / Blast): Các vòng dừng lại và tụt năng lượng tương ứng.

---

## 2. REALM TRANSITION FEEDBACK (CHỈ BÁO THỰC TẠI TOÀN CẢNH)

Người chơi phải nhận biết tức thì trạng thái thực tại hiện hành mà không cần đọc văn bản.

| Thuộc tính quan sát | Trạng thái: PRIME REALM | Trạng thái: ECHO REALM |
| :--- | :--- | :--- |
| **Viền màn hình (Screen Edge)** | Sương mù kim loại mỏng, hơi nước bốc lên | Hạt bụi tím lơ lửng bay ngược từ dưới lên |
| **Màu Ánh Sáng Toàn Cảnh (LUT)** | Ánh sáng vàng đồng pha xanh rêu ($5200\text{K}$) | Tông màu Cyber-Neon, tím sẫm ($8500\text{K}$) |
| **Vật cản tương tác (Colliders)** | Khối đặc cứng cáp, bề mặt bê tông/sắt | Trong suốt nhẹ, cạnh viền phát sáng chấn động |
| **Vết chém kiếm của Kael** | Vệt chém màu ngọc bích (Emerald Trail) | Vệt chém màu tím thạch anh (Amethyst Trail) |

---

## 3. NON-DIEGETIC UI (KHI MỞ MENU / BẢN ĐỒ)

* **Bản đồ (Minimalist Grid):**
  * Thiết kế theo phong cách bản vẽ thiết kế cơ khí (Blueprint).
  * Phòng đã khám phá hiển thị màu trắng.
  * Cửa chưa đi hiển thị icon dấu chấm hỏi nhấp nháy.
  * Các chướng ngại vật chưa mở khóa tự động đánh dấu icon kỹ năng tương ứng (ví dụ: Icon Tường Đá, Icon Cổng Trọng Lực).
* **Font chữ quy định:** Sans-serif hình học, không chân, độ rộng nét đều (vd: Orbitron, Space Mono) tạo cảm giác công nghệ thời gian cổ xưa.