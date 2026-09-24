---
paths:
  - "design/narrative/**"
  - "Docs/noi_dung/**"
---

# Narrative Rules

> **Echo of the Void (Unity):** đường dẫn đã chỉnh sang `Assets/`. Code mẫu bên dưới là GDScript minh họa; áp dụng nguyên tắc cho C#. Test PlayMode ở `Assets/Tests/PlayMode/`, chạy bằng `Tools/run_tests_isolated.sh`; kiểm giá trị cân bằng lấy từ ScriptableObject (`EnemyDataSO`, `BossPhaseData`, `SettingsData`).

- All new lore must be cross-referenced against existing lore for contradictions
- Every lore entry must specify canon level: Established / Provisional / Under Review
- Character dialogue must match the voice profile defined for that character
- World rules (what is possible/impossible) must be explicitly documented and consistent
- Mysteries must have documented "true answers" even if players never learn them
- Faction motivations, relationships, and power structures must be internally logical
- All narrative text must be localization-ready: no idioms that don't translate, named placeholders for variables
- No line of dialogue should exceed 120 characters for dialogue box constraints
