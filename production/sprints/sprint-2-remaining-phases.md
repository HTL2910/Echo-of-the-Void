# Sprint 2 — Phases 3-4: Reality Polish, Dialogue, Integration & Builds

**Status:** Planning  
**Review Mode:** Lean (skip director gates)  
**Created:** 2026-09-24

---

## Sprint Goal

Complete remaining major features (Reality polish, dialogue system, Chronos), finish all integration work with Codex/Anti deliveries, and establish build & release pipeline for Demo Z1.

---

## Capacity & Timeline

| Metric | Value |
|--------|-------|
| Sprint Duration | 4–6 weeks (estimated) |
| Team | Claude (core), Codex (enemies/bosses K5–K9), Anti (art/audio/content B1–B10) |
| Working Format | Parallel streams + integration gates |
| Buffer (20%) | ~1 week reserved for blockers/reviews |

---

## Parallel Work Streams

### Stream A: Claude (Core Systems & Integration)

| Phase | Task | Depends On | Estimate | Acceptance |
|-------|------|-----------|----------|-----------|
| **3A** | **L5: Reality Polish** | Anti B4 (tileset) | 3–4 days | Tint/LUT transitions, vignette per-realm, particle inversion in Echo |
| **3B** | **L11: Dialogue & Endings** | Anti A9 (text) | 4–5 days | Iris dialogue, 12 Monolith UI, 3 endings, credits auto-gen |
| **3C** | **L12: Chronos (Kael part)** | Codex K9 prefab | 2–3 days | Checkpoint system, gravity effect on Kael, final phase logic |
| **4A** | **I3: Wire Mechanics** | Codex K5–K8 prefabs | 2–3 days | PressurePlate↔Echo Anchor, RailCable↔Rail Grind, GravitonField↔Inversion |
| **4B** | **I4: Scene Rooms** | Anti B1–B5 (18+75 rooms) | 5–7 days | Generator updates, room prefabs, camera confiner, Chrono Station placement |
| **4C** | **I5: Integration Testing** | B1, K5–K9 complete | 3–4 days | Playthrough each zone, boss fight chains, save/load full runs |
| **L15** | **Build & Release** | I5 complete | 2–3 days | macOS/Windows builds, platform testing (Xbox/PS/Switch), CI setup |
| **L16** | **Test Expansion** | Ongoing | 1–2 days | Expand coverage: RealityTilemap, EnemyAI, BossAI edge cases |

### Stream B: Codex (K5–K9 Enemies & Bosses)

| Task | Depends On | Boss/Enemy | Estimate | Integration Needed |
|------|-----------|-----------|----------|-------------------|
| **K5** | Nothing | Rift Knight (Z2) | 4 days | Add to Scene, test vs Kael, balance damage |
| **K6** | K5 | Keeper Myra + Doppelgänger (Z2/Z3) | 5 days | Boss arena, phase transitions, rewards |
| **K7** | K6 | Rift Knight Prime + Chronos (Z4) | 6 days | 3 phases, cutscene handoff, ending trigger |
| **K8** | Nothing | GravitonField, mechanics (Z2/Z3) | 3 days | Field prefab, interaction with Kael ability |
| **K9** | K7 | Chronos final balancing | 2 days | Difficulty curve, feedback loop, rewards |

### Stream C: Anti (Art, Audio, Content B1–B10)

| Task | Type | Estimate | Integration Needed |
|------|------|----------|-------------------|
| **B1** | 18 rooms Z1 (largest chunk) | 8–10 days | Generator prefab, Chrono Stations, exits |
| **B2–B3** | Enemy sprites, boss art | 4 days | Animator slice data, visual polish |
| **B4** | Tileset Z2–Z4, nền | 5 days | Truth-to-life tilemap, layer masks |
| **B5** | 75 rooms (Z2–Z4 + Core) | 12–15 days | Phased: Z2 (30), Z3 (30), Z4+Core (15) |
| **B6** | UI art, cutscene ảnh, credits | 4 days | Logo, font/icon, end screens |
| **B7** | Music (remaining stems) | 2–3 days | Boss themes, ending cue, mixing |
| **B8** | Dialogue text & Monolith content (A9) | 5 days | Iris script, 12 Monolith texts, 3 endings |
| **B9** | Boss/ability VFX | 3 days | Sentinel, Myra, Chronos attacks |
| **B10** | Lighting & polish | 2–3 days | Per-room mood, darkness, glow |

---

## Must Have (Critical Path to Demo Z1)

These tasks must complete to pass the **Production → Polish gate**.

| ID | Task | Owner | Days | Blocker? | Criteria |
|----|------|-------|------|----------|----------|
| 3A | L5: Reality Polish | Claude | 3–4 | Anti B4 | Shift tint 0.18s, vignette renders, particles inverted in Echo |
| 3B | L11: Dialogue & Endings | Claude | 4–5 | Anti A9 | 3 endings work, Iris dialogue loads, Monolith UI opens |
| 4A | I3: Wire Mechanics | Claude | 2–3 | Codex K5–K8 | Echo Anchor holds on PressurePlate, Rail Grind engages, Gravity Inversion toggles |
| 4B | I4: Scene Rooms (Z1 + Z2) | Claude + Anti | 5–7 | Anti B1–B4 | 18 Z1 rooms playable, exits connect, no missing colliders |
| 4C | I5: Sentinel Boss Run | Claude | 3–4 | Codex K4 | Menu → Z1 → Sentinel → defeat → reward → end (full playthrough) |
| L15 | macOS Build | Claude | 2–3 | All code done | Build + run, no crashes, 60 FPS |
| L15 | Windows Build | Claude | 2–3 | All code done | Build + run, no crashes, 60 FPS |
| K5–K7 | Bosses K5–K7 | Codex | 15 | None | Each boss has FSM, phase system, visual polish |
| B1 | 18 Z1 Rooms | Anti | 8–10 | None | Tileset placed, colliders done, Stations set |

---

## Should Have (Quality & Polish)

These improve the Demo but are deferrable if time runs short.

| ID | Task | Owner | Days | Criteria |
|----|------|-------|------|----------|
| 3C | L12: Chronos (Kael) | Claude | 2–3 | Checkpoint + gravity effect works, can skip if boss not done |
| K8 | GravitonField mechanics | Codex | 3 | Field prefab in Z2/Z3 ready for Claude wire |
| K9 | Boss balance pass | Codex | 2 | All bosses 10–15 min playtime, no trivial kills |
| B2–B3 | Enemy/boss art polish | Anti | 4 | HD sprite sheets used, Animator updates applied |
| B6 | UI/cutscene art | Anti | 4 | Logo & end screens render correctly |
| B7 | Boss music stems | Anti | 2–3 | Sentinel, Myra, Chronos themes (2-stem crossfade) |
| B9 | Boss VFX | Anti | 3 | Sentinel, Myra attacks have visual feedback |

---

## Nice to Have (Post-Demo Polish)

These are for a future polish sprint if budget allows.

| Task | Owner | Days | Notes |
|------|-------|------|-------|
| L16: Expand Tests | Claude | 1–2 | RealityTilemap, EnemyAI edge cases |
| B5: Z3–Z4 Rooms | Anti | 12–15 | Can be phased: Z3 now, Z4+Core later |
| B10: Advanced Lighting | Anti | 2–3 | Per-room mood setup |
| TextMeshPro Migration | Claude | 2–3 | Optional: upgrade UGUI Text → TMP |

---

## Enemy Difficulty Ladder (Cấp Quái)

**Zone Z1 (Demo):**

| Enemy | Type | HP | Damage | Behavior | Purpose |
|-------|------|----|----|----------|---------|
| Chrono Crawler | Minion | 8 | 1 per hit | Slow patrol, dash attack | Learn controls |
| Void Weaver | Minion | 12 | 2 per hit | Jump + spin attack | Parry practice |
| Void Strider | Mini-boss | 16 | 3 per hit | Teleport + charge | Dodge/dash timing |
| Prism Sentry | Mini-boss | 20 | 4 per hit | Laser + ground slam | Learn realm shift |
| **Sentinel-01** | **Boss** | 60 | 5–8 per hit | 3 phases, realm-split | DPS + pattern learn |

**Z1 Pacing:**
- Rooms 1–5: Crawlers only (no threat, learn run/jump)
- Rooms 6–12: Crawlers + Weavers (introduce parry)
- Rooms 13–17: Striders + Sentries (realm shift required)
- Boss arena: Sentinel (single enemy, full focus)

**Future Zones (Post-Demo):**
- **Z2 (K5):** Rift Knight (harder Strider clone) → introduces wall mechanics
- **Z3 (K6):** Myra + Doppelgänger (2v1 coordination) → teaches teamwork
- **Z4 (K7):** Chronos phases → full toolset required

---

## Risk Register

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Anti delays B1 rooms | Medium | High (blocks playtest) | Start with template room; Claude can build simple rooms if needed |
| Codex K5–K7 unbalanced | Medium | Medium (feels cheap) | Early playtest; Codex balance pass 1 week before release |
| AudioMixer not wired to Anti's music stems | Low | Medium (silent game) | Pre-integrate 2-stem switching with existing Sentinel track |
| Build fails on CI | Low | High (release blocker) | Test builds locally macOS/Windows before pushing to CI |
| Monolith text too long for UI | Low | Low (truncate or scroll) | Set max char limit in UI (proposal: 300 chars per monolith) |
| Echo Anchor visuals not clear | Low | Low (tutorial text) | Add tooltip: "E to confirm anchor placement" |

---

## Dependencies & Gates

### Before L5 (Reality Polish) Starts
- ✅ AudioMixerController code done
- ⏳ **Anti B4:** Tileset (layer masks, visual themes)
- **Gate:** Ask Anti for B4 preview → can stub with temp colors if needed

### Before L11 (Dialogue) Starts
- ✅ LocalizationService framework done
- ⏳ **Anti A9:** All dialogue text (Iris, 12 Monoliths, 3 endings)
- **Gate:** Can build UI with lorem ipsum; swap in real text when A9 done

### Before I4 (Rooms) Starts
- ✅ RoomBounds + RoomManager code done
- ⏳ **Anti B1:** 18 Z1 rooms from template
- ⏳ **Codex K1–K4:** Enemy prefabs (already done ✅)
- **Gate:** Generator reads prefab list; can place generic boxes if needed

### Before I5 (Integration Testing) Starts
- ✅ All I3 mechanics wired
- ⏳ **Anti B1 + B4:** Rooms + tileset complete
- ⏳ **Codex K5–K7:** Bosses K5–K7 placed in scenes
- **Gate:** Full Z1 playthrough without crashes

---

## Definition of Done for Sprint 2

- [ ] **Code:** All L5, L11, L12, L15, L16 tasks completed & tested
- [ ] **Integration:** I3–I5 all complete; no missing references
- [ ] **Content:** Anti deliveries B1, B4, A9, B6 integrated
- [ ] **Builds:** macOS & Windows standalone built & tested on clean machines
- [ ] **Tests:** 180+ tests pass; no S1 bugs; CI green
- [ ] **Art:** All sprites, music, VFX integrated; no placeholder assets
- [ ] **QA:** Demo Z1 plays from menu → Sentinel → end without crashes
- [ ] **Documentation:** BUILD.md updated with results; release notes drafted

---

## Next Steps (Immediate)

### Week 1: Kick-off & Parallel Starts

**Claude:**
1. Send formal requests to Codex & Anti via `yeu_cau_giua_agent.md`:
   - Codex: K5–K7 prefabs + schedule
   - Anti: B1 (rooms), B4 (tileset), A9 (text) dates
2. Create placeholder rooms for Generator testing
3. Begin L5 (Reality Polish) code — can stub without B4 assets

**Codex:**
- Deliver K5 prefab for testing by end of week 1

**Anti:**
- Deliver 3 sample Z1 rooms by end of week 1
- Provide B4 tileset layer guide (even if incomplete)

### Week 2–3: Full Parallel Work

- Claude: L5, L11 (UI + dialogue loading)
- Codex: K5–K7 design + testing
- Anti: B1 room sweep (all 18), B4 tileset finalization

### Week 4: Integration Crunch

- I3, I4, I5 all active
- QA playtest cycle starts
- Bug fixes + balance tuning

### Week 5–6: Polish & Release Prep

- L15 builds (macOS/Windows)
- Final QA pass
- Documentation & credits
- Potential carryover: B5 (Z3–Z4 rooms) → Next sprint

---

## Carryover from Sprint 1

No carryover — Sprint 1 tasks all complete.

---

## Review & Approval

- **Review Mode:** Lean (no director gates spawn)
- **Producer Feasibility:** Reviewed inline; realistic with parallel work
- **QA Plan:** Exists → `/qa-plan sprint` before implementation starts
- **Scope Check:** `/scope-check echo-of-the-void` after finalization

---

**Prepared by:** Claude  
**Last updated:** 2026-09-24  
**Target completion:** ~6 weeks (mid-to-late November 2026)
