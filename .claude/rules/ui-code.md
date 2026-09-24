---
paths:
  - "Assets/Scripts/UI/**"
---

# UI Code Rules

> **Echo of the Void (Unity):** đường dẫn đã chỉnh sang `Assets/`. Code mẫu bên dưới là GDScript minh họa; áp dụng nguyên tắc cho C#. Test PlayMode ở `Assets/Tests/PlayMode/`, chạy bằng `Tools/run_tests_isolated.sh`; kiểm giá trị cân bằng lấy từ ScriptableObject (`EnemyDataSO`, `BossPhaseData`, `SettingsData`).

- UI must NEVER own or directly modify game state — display only, use commands/events to request changes
- All UI text must go through the localization system — no hardcoded user-facing strings
- Support both keyboard/mouse AND gamepad input for all interactive elements
- All animations must be skippable and respect user motion/accessibility preferences
- UI sounds trigger through the audio event system, not directly
- UI must never block the game thread
- Scalable text and colorblind modes are mandatory, not optional
- Test all screens at minimum and maximum supported resolutions
