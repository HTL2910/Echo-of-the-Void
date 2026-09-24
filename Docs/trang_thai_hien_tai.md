# TRẠNG THÁI HIỆN TẠI VÀ CÁCH LÀM TIẾP

*Cập nhật: 2026-09-20 (sau đợt hợp nhất lần 1 và đợt Echo Anchor / Gravity Inversion / Rail Grind).*
Đọc file này **đầu tiên** khi quay lại dự án. Thiết kế đích: `echo_of_the_void_master_spec.md`. Phân công và tiến độ: `ke_hoach_den_100.md`. Quy trình tích hợp: `tich_hop.md`.

## 1. Tóm tắt

| | |
|---|---|
| Game | *Echo of the Void*: 2D Metroidvania, Unity `6000.3.13f1`, URP 2D, Input System |
| Tiến độ ước lượng | **~36% cả game**, **~62% bản Demo Chương 1** (ước lượng theo khối lượng, xem `ke_hoach_den_100.md`) |
| Test tự động | **129/129 đạt** (PlayMode, chạy trên bản sao project, 0 lỗi biên dịch) |
| Ba người làm | **Claude** (code lõi + tích hợp + kết thúc), **Codex** (kẻ địch, boss, cơ chế), **Anti/Antigravity** (đồ họa, âm thanh, dựng phòng, văn bản) |
| Git | Sạch tại thời điểm viết. Commit theo tiền tố `[claude]`, `[codex]`, `[anti]` |

**Chơi thử ngay:** mở project bằng Unity Hub → mở `Assets/Scenes/Prototype_Level1.unity` → Play. Điều khiển: `A/D` chạy, `Space` nhảy, `J` chém, `K` dash, `Shift` đổi thế giới, `U` Resonance, `E` tương tác, `F` Echo Anchor, `Q` Gravity, `Esc` tạm dừng. (Gán lại được trong Cài đặt.)

## 2. Lệnh cần nhớ (chạy ở thư mục gốc project)

```bash
# Chạy toàn bộ test (trên BẢN SAO, không cần đóng Unity Editor, không cần khóa). Lọc: thêm tên ở cuối
EOTV_AGENT=claude Tools/run_tests_isolated.sh
EOTV_AGENT=claude Tools/run_tests_isolated.sh Ability

# Kiểm ai đụng ngoài phạm vi (trước khi commit / khi tích hợp)
python3 Tools/check_ownership.py --agent claude --working
python3 Tools/check_ownership.py --range <đầu>..HEAD        # mỗi commit theo tiền tố [agent]
```

**Sinh lại scene** (ghi đè `Prototype_Level1` + `MainMenu`, tạo prefab còn thiếu, đặt Build Settings): menu `Tools → Echo of the Void → Generate Prototype Scene` trong Unity Editor. Qua dòng lệnh chỉ chạy được khi **không có Unity Editor nào đang mở project**:

```bash
Unity -batchmode -nographics -quit -projectPath <đường dẫn> -executeMethod EchoOfTheVoid.Editor.SceneGenerator.GeneratePrototypeScene -logFile <log>
```

Prefab (`Assets/Prefabs/**`) **được giữ** khi sinh lại (generator dùng lại prefab có sẵn và chỉ bổ sung component thiếu), nên chỉnh sửa của Anti không mất. Scene thì bị dựng lại hoàn toàn.

## 3. Bản đồ code (`Assets/Scripts`, một assembly `EchoOfTheVoid.Runtime`)

| Thư mục | Nội dung chính | Chủ |
|---|---|---|
| `Player/` | `PlayerController` (FSM: Idle/Run/Jump/Fall/WallSlide/Dash/Attack/RailGrind, đảo trọng lực), `PlayerStats`, `PlayerCombat`, `PlayerRespawn` (hồi sinh cứng/mềm), `AbilitySet`, `EchoAnchor`, `PlayerAnimationDriver`, `IPlayerInput` + `DevicePlayerInput` | Claude |
| `Core/` | `RealityManager` + `RealityEventBus` (Shift, có chặn kẹt), `GameSession` (save đang chơi), `GameFlow` (chuyển scene, tạm dừng), `BossEvents`, `AudioManager` (SFX + nhóm biến thể), `MusicLayerController` (2 stem), `LowHealthAudio`, `PersistentId`, `AbilityFlags` | Claude |
| `Save/` | `SaveData`, `SaveService` (JSON, 3 slot, ghi an toàn), `SaveBootstrap` | Claude |
| `Settings/` | `SettingsData/Service` (lưu JSON, áp dụng vào game), `InputBindings`, `RebindSession` | Claude |
| `UI/` | `PlayerHUD`, `BossHealthBar`, `MainMenuController`, `PauseController`, `SettingsMenu`, `UiKit` (dựng UI bằng code) | Claude |
| `Environment/` | `RealityPlatform`/`RealityTilemap`/`RealityObstacles`, `ChronoStation`, `AbilityPickup`, `RoomBounds`/`RoomManager`, `GravitonField`, `LevelGoalTrigger` | Claude |
| `Feedback/` | `CameraFollow2D`, `CameraShakeManager`, `HitStopManager`, `ScreenFader`, `VfxLibrary`, `GhostTrail`, `SquashAndStretch`, `TimedDestroy` | Claude |
| `Enemies/` | `EnemyBase` (poise/choáng), `ChronoCrawler`, `VoidWeaver`, `VoidStrider`, `PrismSentry`, `RailCable`, `TrainingDummy`, `EnemyAnimationDriver`, `EnemyRespawner` | Codex |
| `Bosses/` | `BossBase`, `Sentinel01`, `BossArena`, `BossPhaseData` | Codex |
| `Environment/Mechanics/` | `Spikes`, `BouncePad`, `EnergyGate`, `PressurePlate`, `Door`, `Lever` | Codex |
| `Combat/` | `IDamageable`, `DamageInfo` | Codex |
| `Assets/Editor/` | `SceneGenerator` (sinh scene/prefab), `AssetSetupUtility`; `Codex/` = script tạo prefab quái/boss | Claude / Codex |
| `Assets/Tests/PlayMode/` | `TestWorld` (sàn + Kael + input giả `FakeInput`) và các bộ test | theo vùng |

Cấu trúc prefab bắt buộc (Kael, quái, boss, cơ chế): **root scale (1,1,1)**, collider theo đơn vị thật, sprite ở child `Visual`. Layer `Enemy` cho mọi thứ Kael đánh được.

## 4. Đã có (chơi được trong `Prototype_Level1`)

- **Kael:** chạy, nhảy (3.5 tiles, biến thiên theo độ nhấn), coyote/jump buffer, Wall Slide + Wall Jump (khóa tới khi nhặt Piston Boots), Phase Dash (4 tiles, bất tử 0.2 s), chém 3 đòn + chém trên không + Resonance Strike, chết/hồi sinh, Echo Anchor, Gravity Inversion, Rail Grind.
- **Reality Shift:** bệ và tilemap Prime/Echo/Neutral, chặn Shift khi sẽ bị kẹt (`SHIFT BLOCKED`), ma trận sát thương 100%/20%.
- **Kẻ địch:** Chrono-Crawler, Void Weaver, Void Strider, Prism Sentry (tia → cáp ở Echo), Training Dummy; poise/choáng; hồi lại khi nghỉ ở trạm.
- **Boss Sentinel-01:** đấu trường có cổng khóa, thanh máu, phần thưởng (bản thử), trạm sau trận, nhớ đã hạ, đánh lại khi Kael chết.
- **Cơ chế:** gai (Prime hồi sinh mềm / Echo đệm nảy), đệm nảy, cổng năng lượng, tấm đè, cửa, đòn bẩy (có prefab trong `Assets/Prefabs/Mechanics/`).
- **Hệ thống:** trạm Chrono + lưu 3 slot, Continue, phòng/camera, menu chính, tạm dừng, cài đặt (âm lượng, rung, giảm nhấp nháy, bỏ hitstop, assist), gán phím bàn phím + tay cầm.
- **Âm thanh:** SFX Kenney + 30 SFX riêng (bước chân theo thế giới, tiếp đất, bị đánh, chết, trạm), nhịp tim khi máu < 25%, nhạc Z1 2 stem crossfade 0.18 s (có trong scene).
- **Đồ họa (Anti):** sprite + 15 animation Kael, sprite quái + animator, tileset Z1, VFX prefab (8), icon kỹ năng. Đã commit thêm bộ sprite HD mới (xem mục 6).

## 5. Việc còn lại theo người

**Claude (xem `task_claude.md`)**
1. Hội thoại Iris, 12 Memory Monolith, cutscene, 3 kết thúc, credits, màn thống kê (cần văn bản của Anti; cần chốt cách localization).
2. Bản đồ Blueprint + dịch chuyển nhanh giữa trạm.
3. AudioMixer, snapshot Prime/Echo, LPF 800 Hz khi máu thấp.
4. Tint/LUT toàn cảnh + viền màn hình theo thế giới; đổi va chạm bằng layer khi dựng tilemap.
5. Tiếp cận còn thiếu: palette mù màu, assist "tự nhảy".
6. Hiệu năng (pooling), build macOS/Windows, kiểm tay cầm Xbox/PS/Switch Pro.
7. Chọn slot lưu trong menu; (tùy chọn) chuyển UGUI `Text` sang TextMeshPro.
8. Sau khi có nội dung mới: tích hợp bằng quy trình `tich_hop.md`.

**Codex (xem `task_codex.md`)**: K5 Rift Knight; K6 Keeper Myra + Mirror Doppelganger; K7 Rift Knight Prime + Chronos (3 giai đoạn); K8 cơ chế Z2/Z3 (bánh răng, con lắc, hơi nước, axit, gió; hãy dùng `Mech_GravitonField`, không tạo lại component); K9 cân bằng. Nợ tích hợp cần Codex xem: `EnergyGate` là trigger (không chặn vật lý), boss chưa tách 2 hệ.

**Anti (xem `task_antigravity.md`)**: **B1 dựng 18 phòng Z1 bằng `Room_Template.prefab` (việc lớn nhất, 20% cả game)**, B2 sprite quái còn thiếu, B3 sprite boss còn lại, B4 tileset/nền Z2–Z4 + Core, B5 dựng 75 phòng còn lại, B6 UI (logo, menu, bản đồ, khung hội thoại, cutscene), B7 nhạc còn lại, B8 văn bản (Iris, Monolith, kết thúc), B9 VFX boss/kỹ năng, B10 ánh sáng.

## 6. Việc đang chờ hợp nhất

- **Bộ sprite HD mới của Anti** (commit `14e190a`, `3eb075e`: Kael, quái, Sentinel-01, tileset Z1, HUD): mới có ảnh + dữ liệu cắt, **chưa có bản bàn giao** (`Docs/ban_giao/`) và chưa rõ đã gắn vào prefab/animation chưa. Cần: Anti viết bàn giao; Claude kiểm Animator/prefab bằng test, chạy đủ bộ test, cập nhật generator nếu đổi đường dẫn.
- **Chưa ai chơi thử bằng tay** một lượt đầy đủ (mọi kiểm chứng hiện là test tự động). Nên chơi và ghi cảm giác (nhảy, camera, chiến đấu) vào `yeu_cau_giua_agent.md` hoặc báo Claude.

## 7. Những chỗ dễ vấp (đã tốn thời gian một lần rồi)

1. **Một tiến trình Unity mỗi project.** Unity Editor đang mở giữ khóa nên batchmode trên project thật sẽ thoát mã 1 không có kết quả. Test thì dùng `Tools/run_tests_isolated.sh` (bản sao). Sinh scene thì dùng menu trong Editor.
2. **Bản sao test bị `rsync --delete` mỗi lần chạy test:** thứ bạn sinh trong bản sao (scene/prefab) bị xóa ở lần chạy sau. Muốn kiểm kết quả sinh scene thì kiểm ngay sau khi sinh, đừng chạy test xen giữa.
3. **Không dùng `InputTestFixture`** (phím ảo không ổn định khi chạy nền). Dùng `FakeInput` trong `TestWorld`.
4. **Trạng thái static giữa các test:** `GameSession.Reset()`, `SettingsService.Reload()` + `InputBindings.RefreshFromSettings()`, `GameFlow.IsPaused = false`, `Time.timeScale = 1`, hủy `ScreenFader`/`EventSystem`/`Canvas` còn sót. Thiếu là test sau lỗi ngẫu nhiên.
5. **Tag phải tồn tại** (`root.tag = "..."` với tag chưa khai báo ném lỗi và làm script tạo prefab hỏng im lặng). Project chưa có tag riêng nào; dùng layer.
6. **Kael chỉ đánh trúng layer `Enemy`.** Boss/quái nằm layer khác thì không bị đánh.
7. **`Time.timeScale = 0` khi tạm dừng/hitstop:** `Update` vẫn chạy, `FixedUpdate` thì không. Mọi thứ khôi phục `timeScale` phải dùng `GameFlow.RestingTimeScale`.
8. **Prefab dùng lại khi sinh scene:** thêm component mới cho prefab có sẵn phải khai báo trong `SaveOrReusePrefab(..., typeof(...))` của generator (đã có cho `PlayerAnimationDriver`, `AbilitySet`, `EchoAnchor`).
9. **Sát thương liên tục** (laser, gai đứng) phải theo nhịp, không `giây × deltaTime` (làm tròn về 0).
10. **Camera:** không dùng Cinemachine Brain cùng `CameraFollow2D` (xung đột, camera đứng yên hoặc màn hình đen).
11. Sprite bằng scale ở **root** làm collider bị nhân đôi tỉ lệ. Luôn giữ scale root = 1, đổi kích thước ở `Visual`.
12. **Cảnh báo tự kiểm:** báo "xong" mà file không có trên ổ đĩa đã xảy ra 2 lần. Kiểm bằng `ls`/test trước khi tick.

## 8. Quyết định còn mở

- **Localization** (Q3): chuỗi hiện nằm trong code. Cần chốt trước khi làm hội thoại/Monolith (đề xuất: bảng chuỗi `EN`/`VI` trong `Docs/noi_dung/`, một bộ nạp).
- **Chọn slot lưu** trong menu và có cần nút "Xóa save" không.
- **Z1 thật:** Piston Boots là phần thưởng Sentinel-01 (§4 spec). Màn thử đang khác (D22).
- Có cho tách hệ Prime/Echo của Sentinel-01 ngay không (cần prefab 2 phần từ Anti).

## 9. Bản đồ tài liệu

| File | Dùng để |
|---|---|
| `trang_thai_hien_tai.md` | **File này**: trạng thái + cách làm tiếp |
| `echo_of_the_void_master_spec.md` | Thiết kế đích (thắng mọi tài liệu cũ), gồm sổ quyết định D1–D22 |
| `ke_hoach_den_100.md` | Bảng 24 gói việc, %, ai sở hữu thư mục nào, thứ tự giao |
| `tich_hop.md` | Quy trình bàn giao/tích hợp, bản đồ nối, cổng chất lượng, danh sách phát hành |
| `task_claude.md` / `task_codex.md` / `task_antigravity.md` | Việc của từng người, có tick |
| `phan_cong_code_va_noi_dung.md` | Hợp đồng Animator, cấu trúc prefab, quy ước tên (một số danh sách việc trong đó đã cũ; tham khảo file task) |
| `yeu_cau_giua_agent.md` / `yeu_cau_tu_anti.md` | Nơi các bên xin nhau đổi file |
| `ban_giao/` | Bản bàn giao từng task và kết quả tích hợp |
| 10 tài liệu gốc (`b_ng_...`, `c_t_...`, ...) | Nguồn ý tưởng ban đầu; spec đã hợp nhất, khi mâu thuẫn thì spec thắng |

## 10. Bộ khung Claude Code Game Studios (đã cài, dùng ở các phiên sau)

Nguồn: <https://github.com/donchitos/claude-code-game-studios> (MIT), cài vào `.claude/` ngày 2026-09-20. Chi tiết nguồn/commit: `.claude/CCGS-SOURCE.md`. **Chỉ Claude Code đọc `.claude/` và `CLAUDE.md`**; Codex và Anti vẫn làm theo `Docs/`.

**Có gì:** 38 agent chuyên môn (đã bỏ Godot, Unreal, mạng), 73 skill (lệnh `/`), 11 hook, 10 luật theo thư mục, 41 mẫu tài liệu, bộ tham chiếu API Unity (`Docs/engine-reference/unity/`). Mở **phiên Claude Code mới** để các skill và `CLAUDE.md` được nạp.

**Đã chỉnh cho dự án này:**
- `CLAUDE.md` mới (ngắn) nhập `trang_thai_hien_tai.md` và `technical-preferences.md`. **Không dùng** giao thức "hỏi trước mỗi lần ghi file" của bản gốc cho việc code đã được yêu cầu rõ; giao thức Câu hỏi → Lựa chọn → Quyết định → Nháp → Duyệt chỉ áp dụng khi thiết kế (skill `/brainstorm`, `/design-system`...).
- `.claude/docs/technical-preferences.md` đã điền cho Unity (quy ước tên, hiệu năng, test, mẫu cấm dùng rút từ các lỗi đã gặp). `directory-structure.md` ánh xạ `src/`→`Assets/Scripts/`, `docs/`→`Docs/`.
- Luật theo thư mục trỏ sang `Assets/Scripts/**`, `Assets/Tests/**`. `production/stage.txt = Production`, `production/review-mode.txt = lean`.
- **Hook đang bật** (`.claude/settings.json`): đầu phiên in nhánh/commit gần nhất; `validate-commit` **chỉ cảnh báo** (thiếu tiền tố `[claude]/[codex]/[anti]`, file ngoài phạm vi qua `Tools/check_ownership.py`, tag chưa khai báo, TODO không chủ); thông báo macOS khi cần bạn; ghi log agent vào `production/session-logs/` (gitignore); status line hiện `ctx% | model | stage`.
- Quyền: cho phép tự chạy các lệnh git đọc, `ls`, `Tools/run_tests_isolated.sh`, `Tools/check_ownership.py`; **cấm** `rm -rf`, `git push --force`, `git reset --hard`, `git clean -f`, `sudo`, đọc `.env`. (Quy tắc cấm `rm -rf` đã tự chặn một lệnh thử của tôi khi cài: đúng thiết kế.)
- Bỏ hook `validate-assets` (bản gốc ép tên file chữ thường trong `assets/`, trái quy ước PascalCase của Unity).

**Skill đáng dùng cho mình:** `/help` (hỏi đang ở đâu, làm gì tiếp), `/project-stage-detect`, `/adopt` (rà soát tài liệu hiện có theo mẫu của bộ khung; nên chạy một lần khi bạn muốn chuẩn hóa `Docs/`), `/architecture-decision` (ADR vào `Docs/architecture/`), `/sprint-plan` + `/sprint-status`, `/gate-check` (cổng giữa các giai đoạn, khớp với `tich_hop.md` §4), `/qa-plan`, `/playtest-report`, `/team-level` (thiết kế phòng), `/team-combat`, `/team-audio`, `/team-polish`, `/release-checklist`, `/code-review`.

**Lưu ý:** hook chạy tự động trên máy bạn; đã đọc hết mã nguồn hook, không có lệnh mạng, `sudo` hay xóa. Nâng cấp lên bản mới: đọc `UPGRADING.md` của repo gốc, **đừng chép đè** (đã chỉnh sửa). Skill dùng tên thư mục chữ thường `docs/`, `design/`: trên macOS `docs/` trùng `Docs/` nên vẫn đúng.
