# BẢN ĐẶC TẢ KỸ THUẬT: ADAPTIVE AUDIO SYSTEM

Trò chơi áp dụng phương pháp **Vertical Stem Layering** và **Dynamic Snapshots** để phản ánh tình trạng phân tách không gian theo thời gian thực.

---

## 1. KIẾN TRÚC ÂM NHẠC HAI TẦNG (DUAL STEM MIXING)

Tại mỗi khu vực, bản nhạc nền bao gồm 2 track chạy song song, căn chỉnh đồng nhất theo nhịp (BPM và Time Signature):

* **Stem A (Prime Reality):**
  * Nhạc cụ: Piano acoustic mộc, Cello kéo chậm, tiếng gõ kim loại nhịp nhàng (Mechanical ticks).
  * EQ: Mid-heavy, ấm áp, âm lượng tự nhiên.
* **Stem B (Echo Reality):**
  * Nhạc cụ: Arpeggiator Analog Synth, 808 Sub-bass dồn dập, hiệu ứng không gian hạt bụi (Granular Reverb).
  * EQ: Cắt Low-mid ($300\text{Hz}$), tăng High-end ($10\text{kHz}$), không gian vang rộng rãi.

### Ma trận chuyển đổi âm lượng (Volume Transition Curve)
* **Thời gian chuyển đổi đổi nhánh ($T_{\text{crossfade}}$):** $0.18\text{s}$.
* **Đường cong chuyển dịch:** `Equal Power Crossfade` (Đảm bảo áp suất âm thanh tổng thể không bị hẫng tại thời điểm giao nhau).

---

## 2. SOUND EFFECTS (SFX) REGISTRY

| Sự kiện âm thanh | Mô tả kỹ thuật | Tần số & Xử lý |
| :--- | :--- | :--- |
| **Shift_Activate** | Tiếng kính vỡ nén lại kết hợp chấn động hạ âm | Quét tần số từ $80\text{Hz} \to 40\text{Hz}$ trong $0.15\text{s}$ |
| **Blade_Hit_Clean** | Lưỡi kiếm cắt qua kim loại sắc lẹm | Pitch ngẫu nhiên từ $0.95 \to 1.05$ để không lặp chán |
| **Blade_Deflect** | Tiếng chuông kim loại cùn, chói gắt | Kèm hiệu ứng lọc thông dải hẹp (Bandpass filter) |
| **Footstep_Prime** | Tiếng đế giày da dẫm lên gạch đá tàn tích | Kèm tiếng sỏi vụn nhỏ |
| **Footstep_Echo** | Tiếng bước chân tạo tiếng vang điện tử ngắn | Thêm hiệu ứng âm thanh lùi ngược (Reverse Reverb tail) |

---

## 3. LOW-HEALTH AUDIO SNAPSHOT

Khi HP của Kael giảm xuống dưới mức $25\%$:
* Kích hoạt một Master Audio Filter (Low-pass Filter) tại ngưỡng cắt $800\text{Hz}$, bóp nghẹt toàn bộ âm thanh môi trường xung quanh.
* Tách hẳn tiếng đập tim (Heartbeat SFX) với tần số tăng dần từ $60\text{ BPM} \to 130\text{ BPM}$ tùy theo lượng máu còn lại, tạo áp lực sinh tồn trực tiếp lên người chơi.