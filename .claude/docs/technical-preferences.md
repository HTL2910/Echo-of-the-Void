# Technical Preferences

<!-- Filled for Echo of the Void. All agents read this. Update it when a decision changes (and log it in Docs/echo_of_the_void_master_spec.md §0.2b). -->

## Engine & Language

- **Engine**: Unity 6000.3.13f1 (Unity 6)
- **Language**: C# (namespace `EchoOfTheVoid.<Module>`, one runtime assembly `EchoOfTheVoid.Runtime`)
- **Rendering**: URP 2D Renderer, pixel-perfect look (PPU 16, Point filter, no compression, no mipmaps)
- **Physics**: Physics 2D, fixed timestep 0.02; custom kinematic-style player (`gravityScale = 0`, gravity applied in code)

## Input & Platform

- **Target Platforms**: PC (macOS, Windows). No mobile/console for now
- **Input Methods**: Keyboard + Gamepad (Xbox layout; PlayStation and Switch Pro must work)
- **Primary Input**: Keyboard, gamepad equally supported
- **Gamepad Support**: Full (rebindable, see `Settings/InputBindings`)
- **Touch Support**: None
- **Platform Notes**: input goes through `IPlayerInput` (`DevicePlayerInput` in the game, `FakeInput` in tests). Never read `Keyboard.current` directly in gameplay code

## Naming Conventions

- **Classes**: PascalCase, file name = class name (`PlayerController.cs`)
- **Variables**: camelCase for `[SerializeField] private` fields, `_camelCase` for other private fields, PascalCase for properties
- **Signals/Events**: C# `event Action<...>`; past-tense or `OnX` (`Jumped`, `OnRealmSwitched`); global buses are static classes in `Core/` (`RealityEventBus`, `BossEvents`)
- **Files**: PascalCase for code; assets follow `Docs/phan_cong_code_va_noi_dung.md` (`spr_<entity>_<anim>_<frame>`, `SFX_<Group>_<Name>_<nn>.ogg`, `MUS_<Zone>_<Prime|Echo>.ogg`)
- **Scenes/Prefabs**: prefab root scale (1,1,1), sprite on a child named `Visual`; `Enemy_<Name>`, `Mech_<Type>`, `Pickup_<Ability>`, `Station_Chrono_<id>`, `Room_Template`
- **Constants**: PascalCase `const` (or UPPER_SNAKE for private tuning constants)

## Performance Budgets

- **Target Framerate**: 60 FPS stable (fixed physics 50 Hz)
- **Frame Budget**: 16.6 ms
- **Draw Calls**: < 150 per room
- **Memory Ceiling**: not measured yet (set at the polish milestone)
- **GC**: no allocations in the gameplay loop (pool VFX, projectiles, ghost trails)

## Testing

- **Framework**: NUnit + Unity Test Framework, PlayMode tests in `Assets/Tests/PlayMode/` (assembly `EchoOfTheVoid.PlayModeTests`)
- **Run**: `EOTV_AGENT=<name> Tools/run_tests_isolated.sh [filter]` (runs on a clone, no Unity lock, Editor may stay open). All tests must pass before a task is ticked
- **Minimum Coverage**: every gameplay feature has a real PlayMode test, including the failure path (missing Animator, missing player...)
- **Required Tests**: controller numbers (jump height, dash distance), damage/affinity, save/load, ability gating, integration flows (boss fight, rooms, sounds)
- **Helpers**: `TestWorld` (floor + wired Kael + `FakeInput`); reset statics (`GameSession.Reset()`, `SettingsService.Reload()`, `GameFlow.IsPaused`, `Time.timeScale`)

## Forbidden Patterns

- Scale on a physics root (colliders get scaled): keep root scale 1, resize `Visual`
- Hard-coded balance numbers in scripts: use ScriptableObjects (`EnemyDataSO`, `BossPhaseData`) or `SettingsData`
- `tag = "..."` for a tag that is not defined (breaks prefab builders silently): use layers
- Continuous damage as `perSecond * deltaTime` (rounds to 0): apply in ticks
- `InputTestFixture` / real devices in tests; `FindObjectOfType` in `Update`; `OnGUI` in shipped UI
- Cinemachine Brain together with `CameraFollow2D` (they fight over the camera)
- Restoring `Time.timeScale = 1` directly: use `GameFlow.RestingTimeScale` (respects pause)
- Changing another agent's folders (see `Docs/ke_hoach_den_100.md` §1); run `python3 Tools/check_ownership.py`

## Allowed Libraries / Addons

- Unity packages already in the project: Input System, URP, 2D Tilemap, Test Framework, UGUI. Cinemachine is installed but unused
- Assets: Kenney packs (CC0), Orbitron and Space Mono (SIL OFL); every external asset is listed in `Assets/CREDITS.md`

## Architecture Decisions Log

- Decisions D1–D22: `Docs/echo_of_the_void_master_spec.md` §0.2 and §0.2b
- Status, gotchas and open questions: `Docs/trang_thai_hien_tai.md`
- ADRs created with `/architecture-decision` go in `Docs/architecture/`
