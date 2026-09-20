# PHÂN CÔNG: CODE (Claude) và NỘI DUNG (Anti)

Mục đích: hai bên cùng sửa project mà **không ghi đè nhau** (đã xảy ra: mất `using`, gắn trùng component). Đọc file này trước khi sửa bất cứ thứ gì. Spec game: `echo_of_the_void_master_spec.md`.

## 1. Ai sở hữu thư mục nào

| Thư mục | Chủ | Ghi chú |
|---|---|---|
| `Assets/Scripts/**`, `Assets/Editor/**`, `Assets/Tests/**` | **Claude** | Anti **không sửa file `.cs`** |
| `ProjectSettings/**`, `Packages/**` | **Claude** | Layer, input, build settings… |
| `Docs/echo_of_the_void_master_spec.md` | **Claude** | Anti được đề xuất, không tự sửa |
| `Assets/Art/**` (sprite, tileset, VFX, UI) | **Anti** | Import PPU 16, Point, Uncompressed (tự động qua `PixelArtPostprocessor`) |
| `Assets/Audio/**`, `Assets/Fonts/**` | **Anti** | Xem quy ước tên ở mục 4 |
| `Assets/Prefabs/**` | **Anti** (phần nhìn) | Đổi sprite, thêm Animator/child/material/particle. **Không thêm, xóa, đổi component script** |
| `Assets/Scenes/**` | **Anti** (bố cục) | Sắp xếp platform/tilemap, đặt prefab. Xem mục 3 |
| `Assets/Settings/Enemies/*.asset` | **Anti** cân chỉnh số | Thêm trường mới là việc của Claude |

## 2. Cần đổi code thì làm sao

Anti ghi yêu cầu vào `Docs/yeu_cau_tu_anti.md` (một dòng một yêu cầu: bạn cần gì, ở đâu, để làm gì). Claude xử lý theo thứ tự và đánh dấu xong. **Không sửa `.cs` trực tiếp**, kể cả một dòng.

## 3. Scene: quy tắc quan trọng

`Tools → Echo of the Void → Generate Prototype Scene` sinh scene từ code và **ghi đè toàn bộ** `Prototype_Level1.unity`.

- Từ khi Anti bắt đầu chỉnh scene bằng tay: **đừng chạy lại menu đó** (mất việc).
- Kael và quái nay là **prefab** trong `Assets/Prefabs/` (tự tạo lần đầu, các lần sau generator dùng lại prefab hiện có nên chỉnh sửa của Anti được giữ).
- Level đề xuất tách sang scene riêng (`Zone1_Room01…`) do Anti dựng bằng Tilemap; scene prototype giữ làm bãi thử của Claude.

## 4. Hợp đồng dữ liệu (đừng đổi tên tùy tiện)

**Phím tay cầm (đã khớp spec):** Nhảy A · Chém X · Dash B · Shift RB · Resonance RT · Tương tác Y.

**Tag/Layer:** `Player` (tag + layer), layer `Neutral`, `PrimeSolid`, `EchoSolid`, `Enemy`, `Hazard`, `Interactable`.
Tilemap nền dùng layer `Neutral`; tile chỉ đặc ở Prime dùng `PrimeSolid`; chỉ đặc ở Echo dùng `EchoSolid`.

**Animator (Kael):** nếu prefab Player có `Animator`, code sẽ điều khiển các tham số sau (thiếu tham số nào thì bỏ qua, không lỗi):

| Tham số | Kiểu | Ý nghĩa |
|---|---|---|
| `Speed` | float | \|vận tốc ngang\| |
| `VelocityY` | float | vận tốc đứng |
| `IsGrounded` | bool | chạm đất |
| `IsWallSliding` | bool | đang trượt tường |
| `IsDashing` | bool | đang lướt |
| `Jump`, `Dash`, `Attack1`, `Attack2`, `Attack3`, `AirAttack`, `Resonance`, `Hurt`, `Die` | trigger | sự kiện |
| `Realm` | int | 0 = Prime, 1 = Echo |

Sprite Kael: khung 32×32 px, hitbox 14×26 px nằm ở giữa/đáy khung.

**Cấu trúc prefab (Kael và quái), giữ nguyên khi chỉnh:**

```
Player            <- root: scale (1,1,1), Rigidbody2D, BoxCollider2D 0.875 x 1.625, các script
└── Visual        <- SpriteRenderer (sprite 32x32 của Anti, Animator gắn ở đây hoặc ở root)
```
- **Đừng đổi scale của root** (hitbox tính theo đơn vị thật). Đổi kích thước hình ảnh bằng scale của `Visual`.
- Đổi sprite/animation: chỉ sửa trong `Visual`. Squash & stretch, ghost trail, nháy đỏ đều tự tìm `SpriteRenderer` ở `Visual`.
- Quái cũng theo cấu trúc trên (`Assets/Prefabs/Enemies/`).

**Tên file (âm thanh):** `SFX_<Nhóm>_<Tên>.ogg`, ví dụ `SFX_Blade_Hit_Clean_01.ogg`. Nhạc: `MUS_<Khu>_<Prime|Echo>.ogg` (2 stem cùng BPM).

**Tên sprite:** `spr_<entity>_<anim>_<frame>`.

## 5. Đầu việc của Claude (thứ tự)

1. ~~Prefab hóa Kael và quái~~ (xong; `Assets/Prefabs/`).
2. ~~`PlayerAnimationDriver`~~ (xong; gắn sẵn trên prefab Kael, chỉ cần thêm `Animator` vào `Visual` và tạo tham số theo bảng mục 4).
3. ~~Trạm Chrono + lưu JSON~~ (xong: `Assets/Prefabs/Environment/Station_Chrono_*`; Kael đứng trong vùng trạm và bấm `E` / Y trên tay cầm. Chưa có màn Continue để nạp lại).
4. ~~Chặn Shift khi vật đặc của thế giới đích chồng lên Kael~~ (xong: hiện chữ `SHIFT BLOCKED` + âm lỗi, không tốn cooldown). Còn lại: đổi va chạm realm bằng layer thay vì bật/tắt collider (làm cùng lúc dựng tilemap).
5. ~~`AbilitySet` + khóa kỹ năng~~ (xong: Wall Jump khóa tới khi nhặt Piston Boots, Resonance tới khi nhặt Resonance Core; Shift và Dash mở sẵn theo D8). Cũng xong: `RealityTilemap` và `Room_Template.prefab` để Anti dựng level.
6. ~~Test tự động~~ (đã có 7 test PlayMode trong `Assets/Tests/PlayMode`, mở rộng dần).
7. Void Strider, Prism Sentry.
8. Boss Sentinel-01 (FSM, Poise).
9. Nhạc 2 stem + crossfade equal-power, AudioMixer, low-HP snapshot.
10. Input System + rebind phím.

## 6. Đầu việc đề xuất cho Anti

1. Sprite Kael (32×32) + animation theo bảng mục 4; sprite quái (Crawler, Weaver, Strider, Sentry).
2. Tileset Z1 phân biệt **Prime / Echo / Neutral** (Echo: viền đứt/phát sáng, để không chỉ phân biệt bằng màu).
3. VFX: vệt chém xanh ngọc/tím, ghost trail, bụi, sóng Shift (dùng Kenney Particle Pack).
4. Nhạc + SFX còn thiếu (xem `echo_of_the_void_master_spec.md` §7).
5. Dựng phòng Z1 (18 phòng) bằng Tilemap khi bộ tile xong.

## 7. Chạy Unity ở dòng lệnh: tránh đụng nhau

Project chỉ mở được bởi **một** tiến trình Unity tại một thời điểm (khóa `Temp/UnityLockfile`). Nếu Unity Editor hoặc một lệnh batchmode khác đang chạy, lệnh thứ hai sẽ thoát ngay không báo lỗi rõ ràng (exit 1, không có file kết quả).

- Trước khi chạy: `ps aux | grep "[U]nity.app/Contents/MacOS/Unity"`, phải rỗng.
- Anti và Claude không chạy Unity cùng lúc. Dùng file khóa `.unity_busy` ở thư mục gốc project (chi tiết trong `task_antigravity.md`): thấy file tồn tại thì chờ; tạo file khi bắt đầu, xóa khi xong.
- Chạy test: `Unity -batchmode -nographics -projectPath <đường dẫn> -runTests -testPlatform PlayMode -testResults <file.xml> -logFile <file.log>` (không thêm `-quit`).

## 8. Đã phát hiện và sửa (để khỏi lặp lại)

- Kael lơ lửng: Idle/Run không có trọng lực nhưng chỉ chuyển sang Fall khi đã rơi. Đã sửa; có test hồi quy.
- Hitbox Kael cao 2.64 thay vì 1.625 (root có scale trùng với size collider), ô dò đất/tường sai vị trí. Đã sửa bằng cấu trúc `Visual` và dò theo `collider.bounds`.
- `SquashAndStretch` co giãn cả root (kéo theo collider). Nay co giãn `Visual`.
