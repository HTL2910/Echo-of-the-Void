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

**Tên file (âm thanh):** `SFX_<Nhóm>_<Tên>.ogg`, ví dụ `SFX_Blade_Hit_Clean_01.ogg`. Nhạc: `MUS_<Khu>_<Prime|Echo>.ogg` (2 stem cùng BPM).

**Tên sprite:** `spr_<entity>_<anim>_<frame>`.

## 5. Đầu việc của Claude (thứ tự)

1. Prefab hóa Kael và quái (đang làm).
2. `PlayerAnimationDriver` (đọc controller, điều khiển Animator theo bảng trên).
3. Trạm Chrono (checkpoint, +25 HP) + save/load JSON.
4. Chặn Shift khi vật đặc đè Kael; đổi va chạm realm bằng layer trong 1 frame.
5. Hệ thống mở khóa kỹ năng (`AbilitySet`); khóa Wall Jump tới khi có Piston Boots.
6. Test tự động (nhảy 3.5 tiles, dash 4 tiles, hồi sinh…).
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
