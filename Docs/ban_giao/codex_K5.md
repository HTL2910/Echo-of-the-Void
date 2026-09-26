# Bàn giao K5: Rift Knight

- **Người làm:** codex
- **Ngày:** 2026-09-26
- **Trạng thái:** chờ Claude tích hợp vào scene Z4 và xác nhận

## File đã thêm/sửa

- `Assets/Scripts/Enemies/RiftKnight.cs`: AI áp sát, chém, dậm đất, khiên trước và tự đổi realm.
- `Assets/Scripts/Enemies/RiftKnightDataSO.cs`: toàn bộ số cân bằng riêng của Rift Knight.
- `Assets/Settings/Enemies/EnemyData_RiftKnight.asset`: HP 130, tốc độ 3.8, slash 25, stomp 35, shockwave cao 1.2, đổi realm 4 giây/3 hit.
- `Assets/Prefabs/Enemies/RiftKnight.prefab`: root scale 1, layer Enemy, collider thật, child `Visual` placeholder.
- `Assets/Editor/Codex/EnemyPrefabBuilder.cs`: tạo data/prefab K5 nếu chưa tồn tại, không ghi đè.
- `Assets/Tests/PlayMode/EnemyK5Tests.cs`: 10 PlayMode tests.

## Cách dùng

Kéo `Assets/Prefabs/Enemies/RiftKnight.prefab` vào phòng Z4. Anti thay sprite/Animator trên child `Visual`; giữ nguyên root scale và collider. Animator dùng hợp đồng chung `EnemyAnimationDriver` (`Speed`, `Realm`, `Attack`, `Hurt`, `Die`).

## Hành vi

- Phát hiện Kael trong 6 tiles và áp sát với tốc độ 3.8 tiles/s.
- Trong 1.6 tiles: chém ngang 25 damage.
- Trong 4 tiles: dậm đất tạo shockwave cao 1.2 tiles, 35 damage.
- Khiên chặn 100% đòn đến từ phía trước; đánh từ sau đi qua affinity/damage của `EnemyBase`.
- Tự đổi Prime ↔ Echo mỗi 4 giây hoặc sau 3 đòn gây damage; respawn/reset trở lại Prime.

## Test đã chạy

Qua Unity MCP `tests-run`, filter `EnemyK5Tests`: **10/10 đạt**.

Toàn bộ PlayMode suite: **191/192 đạt**. Lỗi duy nhất ngoài phạm vi K5 là
`MechanicsIntegrationTests.SpikesInPrime_SendKaelBackToSafeGround_WithoutLosingHealth`
(Kael dừng ở `x = 4.06046`, trong khi test yêu cầu `< 4.0`).

- Khiên chặn trước, sát thương sau lưng.
- Đổi realm theo 3 hit và theo 4 giây.
- Reset realm/bộ đếm.
- Quay khiên và áp sát theo vị trí Player.
- Slash 25, stomp 35.
- Prefab/data/layer/root scale/Visual đúng hợp đồng.

## Chưa làm / hạn chế

- `Visual` chỉ là placeholder; chờ Anti cung cấp sprite/Animator theo yêu cầu trong `Docs/yeu_cau_giua_agent.md`.
- Chưa đặt prefab vào scene Z4 vì scene thuộc Claude/Anti.

## Cần Claude nối gì

- Đặt Rift Knight vào các phòng Z4 phù hợp và kiểm tra raycast/collider với tilemap thật.
- Chạy test tích hợp scene, xác nhận rồi ghi `✔ tích hợp` trước khi tick K5 trong `Docs/task_codex.md`.

## Trạng thái tích hợp

- [ ] ✔ tích hợp (Claude)
