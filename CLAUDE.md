# Echo of the Void

2D Metroidvania (Unity 6000.3.13f1, C#, URP 2D). Kael switches between two overlapping realities (Prime / Echo). Built by three agents in parallel; Claude is the core-code owner **and the integrator**.

@Docs/trang_thai_hien_tai.md

## Technology & conventions

@.claude/docs/technical-preferences.md

## How work is organised

- **Read first:** `Docs/trang_thai_hien_tai.md` (above), then `Docs/ke_hoach_den_100.md` (who owns which folder, % table) and `Docs/tich_hop.md` (handoff and integration process). Design target: `Docs/echo_of_the_void_master_spec.md` (wins over the older Vietnamese docs).
- **Three agents, split by folder:** Claude = core code + integration; Codex = enemies, bosses, mechanics; Anti (Antigravity) = art, audio, room building, text. Do not edit another agent's folders; ask through `Docs/yeu_cau_giua_agent.md`.
- **Commits:** message starts with `[claude]`, `[codex]` or `[anti]` (`[claude][integrate]` when wiring others' work). **Never commit or push unless the user asks.** Check scope with `python3 Tools/check_ownership.py --agent claude --working`.
- **Verify before saying "done":** `EOTV_AGENT=claude Tools/run_tests_isolated.sh` (all tests must pass; it runs on a clone so the Unity Editor may stay open). A task is only ticked when its files exist on disk and the tests pass.
- **Unity:** one Unity process per project. Regenerating the scene = menu `Tools > Echo of the Void > Generate Prototype Scene` in the Editor (or batchmode with the Editor closed).
- **The user decides when to merge** ("hợp nhất"): only then integrate Codex's and Anti's handoffs from `Docs/ban_giao/`.

## Working style

- The user gives direct instructions in Vietnamese; answer in Vietnamese, keep identifiers in English. For clear implementation requests, do the work and report facts (what passed, what did not, what is unverified).
- For open design questions use the studio protocol in `Docs/COLLABORATIVE-DESIGN-PRINCIPLE.md` (Question → Options → Decision → Draft → Approval): ask, present options with a recommendation, wait for the choice.
- Studio skills (`/start`, `/brainstorm`, `/design-system`, `/code-review`, `/qa-plan`, `/sprint-plan`, `/release-checklist`, `/help`...) and 38 specialist agents live in `.claude/`. Review mode is **lean** (`production/review-mode.txt`).
