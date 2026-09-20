# Biên bản bàn giao: Codex K4 — Boss Sentinel-01 + Đấu trường

**Agent:** codex  
**Task:** K4  
**Ngày:** 2026-09-20

## Files đã tạo

| File | Ghi chú |
|------|---------|
| `Assets/Scripts/Bosses/BossPhaseData.cs` | ScriptableObject — thông số theo pha, dùng lại cho boss sau |
| `Assets/Scripts/Bosses/BossBase.cs` | Abstract base: HP, Poise/Overheat +50%, BossEvents, player death reset |
| `Assets/Scripts/Bosses/Sentinel01.cs` | FSM: Idle → SweepKick → MissileRain → LaserSweep → (lặp) |
| `Assets/Scripts/Bosses/BossArena.cs` | Vào khu vực → khoá cửa + Engage; thắng → unlock + spawn phần thưởng |
| `Assets/Editor/Codex/BossPrefabBuilder.cs` | Menu: `Tools/Echo of the Void/Codex/Create Boss Prefabs` |
| `Assets/Tests/PlayMode/BossK4Tests.cs` | 5 tests |

## Tests

`BossK4Tests.cs` (5 tests):
- `Sentinel01_Awake_HasCorrectMaxHp` — HP = 600 ✔
- `Sentinel01_TakeDamage_SameRealm_DealsFullDamage` — 100% dmg Echo→Echo ✔
- `Sentinel01_TakeDamage_OppositeRealm_DeflectedToTwentyPercent` — ≤20 dmg Prime→Echo ✔
- `Sentinel01_PoiseToZero_EntersOverheat_PlusFiftyPercentDamage` — Stun + 150 dmg ✔
- `Sentinel01_ResetBoss_RestoresFullHp` — Reset về 600 ✔
- `Sentinel01_RaisesEngagedEvent_OnEngage` — BossEvents.Engaged fired ✔

## Đặc tả triển khai

- **Realm split:** Sentinel-01 `myRealm = Echo` (thân trên). Phần chân/xích = Prime chưa triển khai code riêng (cần prefab 2-part khi Anti cung cấp sprite).
- **MissileRain warning duration:** 1.2s (phase 1), 0.9s (khi HP < 40%).
- **BossArena** sinh `Pickup_WallJump` (prefab Claude cung cấp) + `ChronoStation` tại `rewardSpawnPoint` và `chronoStationSpawnPoint`.
- Sprite hiện tại: placeholder màu xanh dương ở child `Visual` — Anti thay bằng B3.

## Yêu cầu từ Claude

- `BossArena` cần tham chiếu prefab `Pickup_WallJump` (AbilityPickup) — Claude wire vào Scene.
- HUD boss health bar đã sẵn trong `BossHealthBar.cs` — nhận event từ `BossEvents.Engaged/HealthChanged/Defeated`.

## Trạng thái tích hợp

- [ ] ✔ tích hợp (chờ Claude)
