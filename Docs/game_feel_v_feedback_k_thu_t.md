# TÀI LIỆU KỸ THUẬT: GAME FEEL & FEEDBACK SYSTEM

Tài liệu này xác định các tham số vi mô (micro-parameters) nhằm đảm bảo phản hồi điều khiển sắc nét, thỏa mãn và rõ ràng về mặt thị giác/xúc giác.

---

## 1. HITSTOP / SLEEP DURATION (DỪNG KHUNG HÌNH)

Khi hitbox đòn đánh chạm hurtbox mục tiêu, engine đóng băng toàn bộ hoạt ảnh và chuyển động trong một khoảng thời gian cực ngắn ($T_{\text{stop}}$).

| Loại tương tác | Thời lượng Hitstop ($T_{\text{stop}}$) | Time Scale | Đối tượng áp dụng |
| :--- | :--- | :--- | :--- |
| Đòn chém 1 & 2 (Chrono Blade) | $0.05\text{s}$ (3 frames @ 60 FPS) | $0.0$ | Cả Kael và Kẻ địch trúng đòn |
| Đòn chém 3 (Phá giáp) | $0.09\text{s}$ (5 frames @ 60 FPS) | $0.0$ | Toàn bộ Scene (ngoại trừ UI) |
| Đòn phản đòn / Deflect (Lệch thực tại)| $0.03\text{s}$ (2 frames @ 60 FPS) | $0.1$ | Chỉ Kael và kẻ địch tương tác |
| Resonance Strike (Chiêu thức lớn) | $0.14\text{s}$ (8 frames @ 60 FPS) | $0.0$ | Toàn bộ Scene kèm phóng to Camera nhẹ $3\%$ |
| Kael nhận sát thương từ Boss | $0.10\text{s}$ (6 frames @ 60 FPS) | $0.05$| Nhân vật chính |

---

## 2. CAMERA SHAKE PROFILE (RUNG MÀN HÌNH)

Camera sử dụng bộ lọc Perlin Noise đa trục để tránh giật hình cục bộ.

* **Light Shake (Nhảy chạm đất từ độ cao $> 6\text{ tiles}$, Dash):**
  * Biên độ ($A$): $0.08\text{ units}$
  * Tần số ($F$): $25\text{ Hz}$
  * Thời gian suy hao ($T_{\text{decay}}$): $0.12\text{s}$
* **Medium Shake (Combo chém thứ 3, Tiêu diệt quái thường):**
  * Biên độ ($A$): $0.22\text{ units}$
  * Tần số ($F$): $35\text{ Hz}$
  * Thời gian suy hao ($T_{\text{decay}}$): $0.20\text{s}$
* **Heavy Shake (Trúng đòn Boss, Nổ điểm kỳ dị, Resonance Blast):**
  * Biên độ ($A$): $0.55\text{ units}$
  * Tần số ($F$): $45\text{ Hz}$
  * Thời gian suy hao ($T_{\text{decay}}$): $0.40\text{s}$
  * Góc nghiêng xoay (Camera Roll): $\pm 1.5^\circ$

---

## 3. SQUASH & STRETCH MATRIX (CO DÃN HÌNH THỂ)

Áp dụng nội suy Vector3 Transform cho Sprite Root để giữ nguyên thể tích: $Scale_x \times Scale_y = 1.0$.

| Trạng thái chuyển động | $Scale_x$ | $Scale_y$ | Thời gian hoàn trả ($T_{\text{recover}}$) | Easing Function |
| :--- | :--- | :--- | :--- | :--- |
| **Bật nhảy từ mặt đất (Jump)** | $0.75$ | $1.25$ | $0.15\text{s}$ | `EaseOutQuad` |
| **Rơi tự do tốc độ cao ($V_y < -15$)** | $0.85$ | $1.15$ | Duy trì theo $V_y$ | Linear |
| **Tiếp đất (Hard Landing)** | $1.30$ | $0.70$ | $0.12\text{s}$ | `EaseOutElastic` |
| **Bắt đầu Lướt (Phase Dash)** | $1.40$ | $0.70$ | $0.10\text{s}$ | `EaseInQuad` |
| **Va vào tường khi bám (Wall Touch)** | $0.80$ | $1.20$ | $0.08\text{s}$ | `EaseOutBounce` |

---

## 4. HIỆU ỨNG THỊ GIÁC HỖ TRỢ (VFX & TRAILS)

1. **Ghost Trail (Bóng mờ lướt):**
   * Khi thực hiện *Phase Dash*, sinh ra 1 sprite ảo sau mỗi $0.03\text{s}$ (tổng cộng 5 bóng mờ).
   * Độ mờ giảm dần từ $\alpha = 0.7 \to 0.0$ trong vòng $0.25\text{s}$.
   * Màu sắc bóng mờ: Xanh lam lục nếu ở *Prime*, Tím thẫm neon nếu ở *Echo*.
2. **Impact Particles (Mảnh vỡ va chạm):**
   * Chém đúng hệ: Bắn ra 12-16 hạt ánh sáng hình tam giác sắc cạnh bay theo hướng vung kiếm (góc xòe $30^\circ$).
   * Chém lệch hệ: Bắn ra các tia lửa điện tròn dẹt kèm một vòng tròn chấn động trong suốt (Shockwave Distortion Sprite) dãn nở nhanh từ $0.2\text{m} \to 1.5\text{m}$ trong $0.1\text{s}$.