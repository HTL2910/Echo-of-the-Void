# BẢN ĐÁNH GIÁ THIẾT KẾ TRÒ CHƠI: CÁC YẾU TỐ CẦN BỔ SUNG

Dưới đây là các khoảng trống nghiêm trọng giữa một **"Ý tưởng kịch bản hay"** và một **"Sản phẩm game chơi được"** mà một Lead Game Designer cần chuẩn hóa trước khi bắt tay vào code:

---

## 1. GAME FEEL & FEEDBACK (CẢM GIÁC ĐIỀU KHIỂN) - *RẤT QUAN TRỌNG*
Một game platformer thành công hay thất bại phụ thuộc 70% vào Game Feel, không phải cốt truyện. Tài liệu hiện tại chưa có các thông số này:

* **Hitstop / Freeze Frames (Khựng hình khi đánh trúng):** Khi kiếm chém trúng quái, game phải dừng hình trong $0.05\text{s} - 0.08\text{s}$ để tạo cảm giác chém trúng vật thể đặc cứng.
* **Screen Shake (Rung màn hình):** Cần chia làm 3 mức: Nhẹ (khi tiếp đất từ trên cao), Trung bình (khi chém phát thứ 3 của combo), Nặng (khi ăn đòn của Boss hoặc dùng Resonance Strike).
* **Squash & Stretch (Co giãn nhân vật):** Khi Kael nhảy lên, sprite dãn dài theo trục dọc ($1.2\times$). Khi chạm đất, sprite bẹp ngang ($0.8\times$). Nếu không có cơ chế này, chuyển động nhân vật sẽ trông như một khối hộp gỗ di động.

---

## 2. METROIDVANIA ABILITY GATING (CHẶN ĐƯỜNG BẰNG NĂNG LỰC)
Nếu bạn định phát triển theo hướng phi tuyến tính (như Hollow Knight hay Ori), bạn cần xác định rõ loại chướng ngại vật tương ứng với từng kỹ năng:

```
[Chướng Ngại Vật]                     [Năng Lực Cần Mở Khóa]
┌───────────────────────────────────┐ ┌───────────────────────────┐
│ Khe nứt hẹp trên tường cao        │ ➔ Wall Jump & Phase Dash    │
├───────────────────────────────────┤ ├───────────────────────────┤
│ Cổng từ trường trọng lực nghịch   │ ➔ Gravity Inversion         │
├───────────────────────────────────┤ ├───────────────────────────┤
│ Hai công tắc cửa cách xa 10 mét   │ ➔ Echo Anchor (Để lại bóng) │
├───────────────────────────────────┤ ├───────────────────────────┤
│ Vách đá pha lê phát sáng tím      │ ➔ Resonance Strike phá hủy │
└───────────────────────────────────┘ └───────────────────────────┘
```

---

## 3. UI/UX & DIEGETIC INTERFACE (GIAO DIỆN NGƯỜI DÙNG)
Làm thế nào để người chơi biết mình đang ở thực tại nào mà không cần nhìn vào chữ?
* **Môi trường:** Khi ở *Prime*, viền màn hình có tông lạnh ánh xanh cơ khí. Khi ở *Echo*, viền màn hình có khói tím tỏa ra và các hạt phân tử bay ngược lên trần nhà.
* **Thanh máu/Năng lượng:** Cần thiết kế dạng thanh cong tối giản gắn quanh người nhân vật hoặc đặt cố định ở góc trái trên, không che tầm nhìn khi nhảy platform.

---

## 4. DESIGN PATTERNS CHO LẬP TRÌNH (SOFTWARE ARCHITECTURE)
Để AI code hoặc lập trình viên thực thi mà không làm nát dự án sau 2 tuần, kiến trúc code phải tách bạch:

1. **State Pattern (FSM):** Dành cho Character Controller (`IdleState`, `RunState`, `JumpState`, `DashState`, `HurtState`). Không viết hàng chục câu lệnh `if/else` lồng nhau trong hàm `Update()`.
2. **Observer Pattern / Event-Driven:** Khi đổi thực tại, hệ thống bắn ra một Event: `OnRealityShifted(RealmState newState)`. Tất cả các Tilemap, Bẫy, và Quái tự động đăng ký lắng nghe (Subscribe) Event này và đổi trạng thái độc lập, tránh việc một script quản lý phải nắm giữ toàn bộ tham chiếu map.
3. **ScriptableObjects (cho Unity) hoặc Custom Resources (cho Godot):** Toàn bộ chỉ số quái (HP, Speed, Sát thương) phải nằm ngoài file dữ liệu (Data-driven), không hard-code số cứng vào script AI.

---

## 5. HỆ THỐNG ÂM THANH TƯƠNG TÁC (ADAPTIVE AUDIO)
Game 2 thực tại cần kỹ thuật **Horizontal Layering** trong âm nhạc:
* Bản nhạc nền có 2 stem chạy song song cùng BPM:
  * Track A: Nhạc cụ mộc, tiếng đàn violin u sầu và tiếng kim loại va chạm (Prime).
  * Track B: Synthesizer điện tử, tiếng bass dồn dập, hiệu ứng reverb không gian vỡ vụn (Echo).
* Khi người chơi bấm nút đổi thực tại, âm lượng của Track A kéo về 0 và Track B kéo lên 100 trong $0.2\text{s}$ thông qua Audio Mixer Snapshot.