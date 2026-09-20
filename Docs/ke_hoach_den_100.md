# KẾ HOẠCH TỚI 100%: 3 NGƯỜI LÀM SONG SONG

Mục tiêu: hoàn thành game theo `echo_of_the_void_master_spec.md`. Ba người, ba mảng, **không sửa file của nhau**. Claude là **người tích hợp**: khi Codex và Anti bàn giao (mẫu và quy trình trong `tich_hop.md`), Claude nối vào game, kiểm, và làm phần kết (cảnh kết, credits, build).

| Người | Mảng | File task |
|---|---|---|
| **Claude** | Code lõi (người chơi, thế giới, lưu, UI, âm thanh, cấu hình Unity) **+ tích hợp mọi phần của hai người kia + lo phần kết thúc và phát hành** | `task_claude.md`, quy trình ở `tich_hop.md` |
| **Codex** | Code nội dung chơi: kẻ địch, boss, cơ chế môi trường | `task_codex.md` |
| **Anti (Antigravity)** | Nội dung: đồ họa, âm thanh, nhạc, dựng phòng, văn bản | `task_antigravity.md` |

Tiến độ dưới đây là **ước lượng theo khối lượng công việc**, không phải số đo. 100% = toàn bộ game theo spec (5 khu, ~90 phòng, 5 boss). Demo = Chương 1 (Z1) + boss Sentinel-01.

---

## 1. Ai sở hữu thư mục nào (đọc kỹ, tránh đụng nhau)

| Thư mục / file | Chủ |
|---|---|
| `Assets/Scripts/Player/**`, `Core/**`, `UI/**`, `Save/**`, `Settings/**`, `Feedback/**` | **Claude** |
| `Assets/Scripts/Environment/**` (các file **đã có**), `Assets/Editor/SceneGenerator.cs`, `AssetSetupUtility.cs` | **Claude** |
| `ProjectSettings/**`, `Packages/**`, `*.asmdef`, `Docs/echo_of_the_void_master_spec.md` | **Claude** |
| `Assets/Scripts/Enemies/**`, `Assets/Scripts/Combat/**` | **Codex** |
| `Assets/Scripts/Bosses/**` (thư mục mới) | **Codex** |
| `Assets/Scripts/Environment/Mechanics/**` (thư mục mới: gai, cổng, đòn bẩy...) | **Codex** |
| `Assets/Editor/Codex/**` (script editor riêng, vd. dựng prefab quái) | **Codex** |
| `Assets/Tests/PlayMode/<Vùng>*Tests.cs` | người sở hữu vùng đó (Claude: Player/Core/Save/UI; Codex: Enemy/Boss/Mechanics) |
| `Assets/Tests/PlayMode/TestWorld.cs` | **Claude** (Codex xin thêm hàm qua `yeu_cau_giua_agent.md`) |
| `Assets/Art/**`, `Assets/Audio/**`, `Assets/Fonts/**`, phần nhìn của `Assets/Prefabs/**`, `Assets/Scenes/Zone*` | **Anti** |
| `Assets/Settings/Enemies/*.asset` (số quái) | **Codex** cấu trúc, **Anti/Codex** cân chỉnh số, ghi vào `yeu_cau_giua_agent.md` nếu đổi |

**Quy tắc chung**
1. Cần đổi file của người khác: ghi một dòng vào `Docs/yeu_cau_giua_agent.md` (ai cần, ai xử lý, cần gì, để làm gì). Người sở hữu xử lý và tick.
2. Khóa Unity (chỉ cần khi sửa **project thật**: sinh scene, mở Editor, import asset; **không cần để chạy test**, xem mục 3): chỉ **một** tiến trình Unity mỗi lúc. Trước khi chạy: `.unity_busy` phải không tồn tại; tạo nó với `tên + việc + giờ`, xóa khi xong (chi tiết trong `task_antigravity.md`). Kiểm tra thêm `ps aux | grep "[U]nity.app/Contents/MacOS/Unity"` rỗng.
3. **Chạy test trước khi báo xong bằng `Tools/run_tests_isolated.sh`** (chạy trên bản sao project, **không cần khóa `.unity_busy` và không đụng Unity Editor đang mở**; lọc theo tên: `Tools/run_tests_isolated.sh Ability`). Hoặc trực tiếp: `Unity -batchmode -nographics -projectPath <đường dẫn> -runTests -testPlatform PlayMode -testResults <xml> -logFile <log>` (không dùng `-quit`). **Mọi test phải đạt** mới được tick.
4. Chỉ tick `[x]` cho việc **có thật trên ổ đĩa** và đã kiểm tra. (Đã có 2 lần báo xong nhưng file thiếu.)
5. Không đổi tên/di chuyển asset ngoài Unity Editor (hỏng GUID).
6. Commit nhỏ với tiền tố `[claude]`/`[codex]`/`[anti]` trong thông điệp, và chạy `python3 Tools/check_ownership.py --agent <mình> --working` trước khi commit. Không commit khi test đỏ. Chi tiết: `tich_hop.md`.
7. Xong một task thì viết **bàn giao** (`Docs/ban_giao/`) để Claude tích hợp; chỉ tick khi Claude xác nhận.

---

## 2. Bảng khối lượng (tổng = 100%)

| Mã | Gói công việc | Trọng số | Xong | Chủ |
|---|---|---:|---:|---|
| C1 | Người chơi: di chuyển, nhảy, dash, tường | 4 | 95% | Claude |
| C2 | Hệ Reality: bệ/tilemap, chặn Shift, layer, tint toàn cảnh, viền màn hình | 3 | 65% | Claude |
| C3 | Khung chiến đấu + AI quái | 4 | 85% | Codex |
| C4 | Quái: Strider, Sentry, Rift Knight, hoàn thiện Crawler/Weaver | 4 | 65% | Codex |
| C5 | 5 boss (Sentinel-01, Myra, Doppelganger, Rift Knight Prime, Chronos 3 giai đoạn) | 8 | 20% | Codex |
| C6 | Cơ chế: gai/đệm nảy, cổng năng lượng, công tắc/cửa, bánh răng/con lắc, rail grind, axit, gió | 5 | 55% | Codex |
| C6b | Kỹ năng: Echo Anchor, Gravity Inversion (Resonance đã có) | 3 | 90% | Claude |
| C7 | Lưu/nạp, slot, Continue, cài đặt | 2 | 75% | Claude |
| C8 | UI: HUD TMP, menu chính, tạm dừng, cài đặt, rebind, bản đồ, màn kết | 5 | 55% | Claude |
| C9 | Hệ âm thanh: mixer, snapshot Prime/Echo/LowHP, footstep, SFX registry | 2 | 55% | Claude |
| C10 | Hạ tầng phòng: chuyển phòng, camera confiner, PersistentId, dịch chuyển nhanh | 3 | 45% | Claude |
| C11 | Cốt truyện trong game: hội thoại Iris, Monolith, cutscene, 3 kết thúc | 3 | 0% | Claude |
| C12 | Tiếp cận + tối ưu: assist mode, mù màu, giảm nhấp nháy, pooling | 2 | 35% | Claude |
| C13 | Test tự động, build macOS/Windows | 2 | 75% | Claude + Codex |
| A1 | Nghệ thuật Kael | 3 | 85% | Anti |
| A2 | Nghệ thuật quái (7 loại + biến thể Prime/Echo) | 3 | 50% | Anti |
| A3 | Nghệ thuật boss ×5 | 5 | 0% | Anti |
| A4 | Tileset + nền 5 khu | 5 | 20% | Anti |
| A5 | **Dựng phòng** (Z1 18, Z2 24, Z3 24, Z4 24, Core 3) | 20 | 1% | Anti |
| A6 | VFX | 2 | 70% | Anti |
| A7 | Nghệ thuật UI: logo, menu, HUD, bản đồ, cutscene | 3 | 20% | Anti |
| A8 | Nhạc (Z1–Z4, Core, boss, menu, kết) + SFX riêng | 7 | 25% | Anti |
| A9 | Văn bản: đối thoại, 12 Monolith, kết thúc | 3 | 0% | Anti |
| A10 | Nền parallax, ánh sáng môi trường | 2 | 0% | Anti |
| | **Tổng** (trọng số cộng lại 103, đã quy về 100) | **100** | **≈ 36%** | |

(Sau đợt hợp nhất lần 1: ~19% → ~34%. **Demo Z1 ~62%**: còn dựng 18 phòng, boss có sprite thật, nhạc/vòng chơi hoàn chỉnh.)

**Phần khó nhất** là A5 (dựng 90 phòng, 20%) và C5 (5 boss, 8%): đặt ưu tiên sớm.

---

## 3. Thứ tự giao (để mỗi người không phải chờ nhau)

**Giai đoạn 1: Demo Z1** (mục tiêu ~50% tổng)
- Claude: HUD TMP, menu + Continue, hạ tầng phòng/camera, `PlayerRespawn.SoftRespawn()`, sự kiện nghỉ ở trạm (cho quái hồi lại), UI thanh máu boss.
- Codex: Strider, Sentry, cơ chế gai/cổng/công tắc, Sentinel-01 + arena.
- Anti: bù A1/A5 còn thiếu, sprite boss Sentinel-01, nhạc Z1, dựng 18 phòng Z1.
- **Chốt giai đoạn 1** khi: chơi từ đầu Z1 tới hết Sentinel-01 không lỗi chặn, có menu, có lưu, có nhạc, 60 FPS.

**Giai đoạn 2: Z2 + Z3** (Wall Jump → Gravity Inversion → Echo Anchor)
- Claude: Gravity Inversion, Echo Anchor, dịch chuyển nhanh, bản đồ.
- Codex: Myra, Doppelganger, cơ chế Z2/Z3 (bánh răng, con lắc, axit, luồng khí).
- Anti: tileset/nền Z2, Z3; dựng 48 phòng.

**Giai đoạn 3: Z4 + Core + kết**
- Codex: Rift Knight Prime, Chronos 3 giai đoạn.
- Claude: Monolith, hội thoại, cutscene, 3 kết thúc, màn credits.
- Anti: Z4, Core, cutscene, nhạc còn lại.

**Giai đoạn 4: đánh bóng và phát hành** (M6, M7): cả ba. Claude dẫn: hiệu năng, tiếp cận, tay cầm, build.

---

## 4. Tiêu chí "100%"

- Chơi được từ đầu tới cuối cả 3 kết thúc, không lỗi chặn tiến trình.
- Mọi kỹ năng mở khóa đúng thứ tự (spec §4); mọi khóa có chìa.
- 60 FPS ổn định, không GC trong vòng lặp chơi, chạy đúng bàn phím + Xbox + PlayStation + Switch Pro.
- Rebind phím hoạt động; tùy chọn mù màu, giảm nhấp nháy, assist.
- Bản build macOS + Windows chạy độc lập.
- Toàn bộ test đạt; có blind playtest ≥ 5 người cho Z1.
