# LỘ TRÌNH PHÁT TRIỂN DỰ ÁN GAME 2D PLATFORMER
*Từ Ý Tưởng Sơ Khởi Đến Bản Phát Hành Hoàn Chỉnh (Game Production Pipeline)*

---

## GIAI ĐOẠN 1: TIỀN KỲ & NỀN TẢNG (PRE-PRODUCTION & PROTOTYPING)
**Mục tiêu:** Tạo ra một bản "Greybox Prototype" (không cần đồ họa, chỉ dùng khối vuông) có cảm giác chơi (Game Feel) đạt chuẩn.

* [ ] **Bước 1: Chọn Engine & Cài đặt Dự án**
  * Đề xuất: **Godot 4.x** (rất mạnh về 2D, gọn nhẹ) hoặc **Unity (2D URP)**.
  * Cấu hình tỉ lệ khung hình (60 FPS cố định), Pixel Perfect Camera, Input System mới nhất.
* [ ] **Bước 2: Xây dựng Character Controller**
  * Lập trình di chuyển mặt đất: Tăng tốc, ma sát dừng, kiểm tra dốc.
  * Lập trình nhảy: Tính toán công thức trọng lực theo $V_0 = \frac{2h}{t}$ và $g = \frac{2h}{t^2}$.
  * Thêm "Game Juice" cơ bản: Squash & Stretch khi nhảy/tiếp đất, Coyote time, Jump buffer.
* [ ] **Bước 3: Lập trình Cơ chế Hoán Đổi Thực Tại (Reality Shift)**
  * Thiết lập 2 Tilemap riêng biệt cho Prime và Echo.
  * Viết script đổi trạng thái Collider bằng bitmask chỉ trong 1 frame.
  * Tạo shader đổi màu màn hình tức thì khi ấn nút chuyển đổi.
* [ ] **Bước 4: Kiểm thử Vòng lặp Cốt lõi (Core Loop Test)**
  * Dựng 3 màn chơi thử nghiệm bằng các khối xám: 1 màn nhảy căn bản, 1 màn kết hợp bẫy, 1 màn chiến đấu cơ bản.
  * Đánh giá: Cơ chế chuyển đổi có gây nhức mắt không? Nhịp thao tác tay có mượt không?

---

## GIAI ĐOẠN 2: SẢN XUẤT TẬP TRUNG (VERTICAL SLICE PRODUCTION)
**Mục tiêu:** Hoàn thiện 100% một màn chơi hoàn chỉnh (Chương 1) bao gồm cả đồ họa, âm thanh, UI và một trận đấu Boss mẫu.

* [ ] **Bước 5: Định hình Mỹ thuật & Animation**
  * Vẽ Tileset $16 \times 16$ hoặc $32 \times 32$ cho Chương 1 (Tàn tích công nghệ cũ).
  * Vẽ sprite nhân vật Kael: Idle, Run, Jump, Dash, Attack (8 frame/animation).
  * Tạo hiệu ứng hạt (VFX): Bụi chân, vệt lướt không gian (Ghost trail), sóng xung kích khi đổi thực tại.
* [ ] **Bước 6: Thiết kế & Lắp ráp Màn chơi (Level Design)**
  * Ứng dụng quy tắc thiết kế 4 bước của Nintendo (Kishōtenketsu):
    1. Giới thiệu cơ chế trong môi trường an toàn.
    2. Phát triển độ khó và kết hợp thử thách.
    3. Tạo bước ngoặt bất ngờ (Twist).
    4. Bài kiểm tra tổng hợp trước cửa ải kết thúc.
* [ ] **Bước 7: Lập trình Trùm Chương 1 (Sentinel-01)**
  * Xây dựng máy trạng thái hữu hạn (FSM - Finite State Machine) cho Boss:
    * State 1: Bắn tên lửa định vị (buộc đổi thực tại để trốn sau tường chắn).
    * State 2: Quét tia laser toàn sàn đấu (buộc căn thời gian nhảy và lướt trên không).
    * State 3: Quá nhiệt (điểm yếu lộ ra cho Kael dồn combo).
* [ ] **Bước 8: Âm thanh & Âm nhạc (Audio Integration)**
  * Tích hợp FMOD/Wwise hoặc hệ thống âm thanh nội tại của Engine:
    * 2 bản phối khí cho cùng một bài nhạc nền: Bản Acoustic/Orchestral (Prime) và Synth/Bass (Echo), chuyển đổi chéo âm lượng (Cross-fade) mượt mà khi người chơi đổi thực tại.

---

## GIAI ĐOẠN 3: MỞ RỘNG NỘI DUNG (FULL PRODUCTION)
**Mục tiêu:** Xây dựng toàn bộ các chương còn lại dựa trên khung hệ thống đã hoàn thiện ở Giai đoạn 2.

* [ ] **Bước 9: Phát triển Chương 2, 3 và 4**
  * Lắp ráp các cơ chế mới: *Gravity Inversion*, *Echo Anchor*.
  * Tăng độ phức tạp của câu đố kết hợp platforming tốc độ cao.
* [ ] **Bước 10: Tích hợp Giao diện & Kể chuyện (UI/UX & Narrative)**
  * Menu chính, bảng cài đặt phím (Key rebinding là bắt buộc với game platformer).
  * Hộp thoại đối thoại với Iris, bia đá hồi ức, màn hình chuyển cảnh.
  * Hệ thống điểm lưu (Checkpoints) và lưu dữ liệu người chơi (Save/Load system).

---

## GIAI ĐOẠN 4: ĐÁNH BÓNG & PHÁT HÀNH (POLISH, QA & LAUNCH)
**Mục tiêu:** Loại bỏ lỗi, tối ưu hiệu năng và phát hành bản thử nghiệm/chính thức.

* [ ] **Bước 11: Playtest & Tinh chỉnh (Balancing)**
  * Mời người chơi ngoài dự án thử nghiệm (Blind test).
  * Ghi nhận các điểm người chơi bị "kẹt" quá lâu hoặc các đoạn chết liên tục gây ức chế.
  * Tinh chỉnh lại độ trễ hit-stop, rung màn hình (screen shake) và âm thanh phản hồi khi va đập.
* [ ] **Bước 12: Tối ưu hóa Kỹ thuật**
  * Giảm Draw Calls, kiểm soát Garbage Collection tránh hiện tượng giật khung hình (micro-stutter).
  * Hỗ trợ đầy đủ các tay cầm thông dụng (Xbox, PlayStation, Switch Pro).
* [ ] **Bước 13: Xây dựng Bản Demo & Phát hành Steam/Itch.io**
  * Đóng gói bản Demo chứa toàn bộ Chương 1 để tham gia các sự kiện như Steam Next Fest.
  * Thu thập phản hồi cộng đồng để chuẩn bị cho đợt phát hành Early Access hoặc Full Release.