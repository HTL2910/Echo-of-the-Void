# KỸ THUẬT: QUY CHUẨN DỰNG 18 PHÒNG ZONE 1 (ABANDONED FOUNDRY)

## 1. Mục tiêu kiến trúc
- **Bài toán:** Demo Chương 1 (Z1) gồm đúng **18 phòng** kết nối liền mạch, từ Lối vào xưởng đúc (`Z1_R01`) đến Đấu trường Sentinel-01 (`Z1_R18`).
- **Nguyên tắc kỹ thuật:**
  - Mỗi phòng là một GameObject trong Scene hoặc Prefab riêng biệt có component `RoomBounds.cs`.
  - Không tạo camera riêng cho mỗi phòng: Dùng **một Camera chính** kèm `CameraConfiner` co dãn theo `RoomBounds.Collider`.
  - Chuyển phòng mượt mà bằng trigger tại cửa kết nối, không load lại scene, không giật màn hình (Zero hitch).
  - Tọa độ phòng tuân thủ lưới bản đồ Blueprint (`RoomMapCoordinate`) để đồng bộ với Minimap.

---

## 2. Bản đồ 18 phòng Zone 1 & Lưới tọa độ

```
[Row 3]                          [R08] ── [R09] ── [R10] (Trạm 2)
                                   │                  │
[Row 2]             [R05] ── [R06] ── [R07]         [R11] ── [R12] (Gai/Đệm)
                      │                                        │
[Row 1]   [R01] ── [R02] (Trạm 1) ── [R03] ── [R04]          [R13]
(Start)               │                                        │
[Row 0]             [R14] ── [R15] (Cable) ── [R16] ── [R17] ── [R18] (Boss Arena)
```

| Phòng | Tên phòng | Kích thước (ô) | Đặc điểm / Cơ chế |
|---|---|---|---|
| `Z1_R01` | Foundry Entrance | 20 × 12 m | Điểm xuất phát của Kael, hướng dẫn chạy nhảy |
| `Z1_R02` | Outer Bastion | 24 × 14 m | **Chrono Station 1** (Trạm nghỉ đầu tiên) |
| `Z1_R03` | Broken Conduits | 30 × 12 m | Crawler tuần tra, gờ tường kiểm tra Wall Slide |
| `Z1_R04` | Steam Vent Shaft | 16 × 24 m | Phòng đứng (vertical room), kiểm tra nhảy bật |
| `Z1_R05` | Cooling Core West | 26 × 14 m | Void Weaver bay, đạn năng lượng |
| `Z1_R06` | Piston Gallery | 32 × 14 m | Bệ Prime/Echo đổi qua lại khi Shift |
| `Z1_R07` | Upper Conduit Cross | 20 × 20 m | Ngã tư nối trục trên và dưới |
| `Z1_R08` | Smelting Chamber | 36 × 16 m | Bẫy gai (Spikes) + đệm nảy (BouncePad) |
| `Z1_R09` | Resonance Archive | 24 × 12 m | **Memory Monolith 1** (Iris giải thích lịch sử) |
| `Z1_R10` | Central Maintenance | 24 × 14 m | **Chrono Station 2** |
| `Z1_R11` | Overheat Chute | 18 × 28 m | Hố sâu rơi tự do có bệ dừng an toàn |
| `Z1_R12` | Slag Siphon | 30 × 14 m | Void Strider tuần tra, cần chém phản xạ |
| `Z1_R13` | Prism Watchtower | 22 × 26 m | Prism Sentry đầu tiên: tia beam Prime / Cáp Echo |
| `Z1_R14` | Sub-Foundry Access | 24 × 12 m | Cửa đóng (Door) mở bằng Tấm đè (PressurePlate) |
| `Z1_R15` | Rail Transit Line | 40 × 14 m | Cáp quang năng lượng dài: Kael biểu diễn Rail Grind |
| `Z1_R16` | Security Airlock | 20 × 12 m | Cổng năng lượng (EnergyGate): lướt Dash qua |
| `Z1_R17` | Boss Antechamber | 24 × 14 m | **Chrono Station 3** (Save point trước Boss) |
| `Z1_R18` | Sentinel Foundry (Arena) | 48 × 18 m | **Boss Sentinel-01** + Cổng khóa + Phần thưởng Piston Boots |

---

## 3. Cấu trúc Chuẩn của Prefab Phòng (`Room_Template.prefab`)

Mọi phòng đều có cấu trúc cây GameObject bắt buộc:
```
[Z1_Rxx_RoomName] (Root, Scale 1,1,1)
  ├── RoomBounds (BoxCollider2D Trigger, Layer: Neutral)
  ├── Geometry/
  │     ├── Neutral_Platforms (CompositeCollider2D, Layer: Neutral)
  │     ├── Prime_Solids (RealityPlatform, Layer: PrimeSolid)
  │     └── Echo_Solids (RealityPlatform, Layer: EchoSolid)
  ├── Mechanics/
  │     ├── Spikes, BouncePads, Doors, Levers...
  ├── Enemies/
  │     └── Crawlers, Weavers, Striders... (có EnemyRespawner)
  ├── Interactive/
  │     └── ChronoStation, Monolith, Pickups...
  ├── Doors/
  │     ├── Door_West (Trigger chuyển phòng)
  │     └── Door_East (Trigger chuyển phòng)
  └── Lighting & Decals/
        └── PointLights 2D, Bụi hạt bụi, Parallax BG
```

---

## 4. Hợp đồng Chuyển phòng (`RoomTransitionTrigger.cs`)
- Tại mỗi cửa ra của phòng: Đặt một trigger 2D.
- Khi Kael chạm trigger:
  1. `RoomManager` kích hoạt phòng mới, đổi kích thước `CameraConfiner`.
  2. Bắn event `MapEvents.OnRoomDiscovered(newRoomId)`.
  3. Cập nhật `GameSession.Current.currentRoomId`.
  4. (Tùy chọn) Chạy `ScreenFader.Fade(0.1f)` nếu là cửa ngăn cách lớn giữa các khu vực.

---

## 5. Danh sách đầu việc & File bàn giao
- [ ] Dựng prefab `Assets/Prefabs/Rooms/Z1/Z1_R01.prefab` đến `Z1_R18.prefab`.
- [ ] Đăng ký danh sách phòng vào `Zone1_Database.asset` (`MapDefinitionData`).
- [ ] Tích hợp vào scene tổng `Zone1_AbandonedFoundry.unity`.
- [ ] PlayMode Test: `Zone1FlowTests.cs` — Kael di chuyển từ R01 xuyên suốt đến R18 mà không bị rơi khỏi thế giới hay kẹt camera.
