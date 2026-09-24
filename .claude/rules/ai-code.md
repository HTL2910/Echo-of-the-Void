---
paths:
  - "Assets/Scripts/Enemies/**"
  - "Assets/Scripts/Bosses/**"
---

# AI Code Rules

> **Echo of the Void (Unity):** đường dẫn đã chỉnh sang `Assets/`. Code mẫu bên dưới là GDScript minh họa; áp dụng nguyên tắc cho C#. Test PlayMode ở `Assets/Tests/PlayMode/`, chạy bằng `Tools/run_tests_isolated.sh`; kiểm giá trị cân bằng lấy từ ScriptableObject (`EnemyDataSO`, `BossPhaseData`, `SettingsData`).

- AI update budget: 2ms per frame maximum — profile to verify
- All AI parameters must be tunable from data files (behavior tree weights, perception ranges, timers)
- AI must be debuggable: implement visualization hooks for all AI state (paths, perception cones, decision trees)
- AI should telegraph intentions — players need time to read and react
- Prefer utility-based or behavior tree approaches over hard-coded if/else chains
- Group AI must support formation, flanking, and role assignment from data
- All AI state machines must log transitions for debugging
- Never trust AI input from the network without validation
