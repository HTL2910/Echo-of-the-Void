# Enemy Difficulty Balancing — Echo of the Void

Progressive enemy and boss difficulty curve for Demo Z1 and beyond.

---

## Z1 (Demo) — Progressive Learning Curve

### Minion Tier

| Enemy | HP | Damage | Special | Role | First Seen | Tactics |
|-------|----|----|---------|------|-----------|---------|
| **Chrono Crawler** | 8 | 1 (melee) | Slow patrol, dash attack | Tutorial minion | Room 1 | Run & jump over; no parry needed |
| **Void Weaver** | 12 | 2 (melee) | Jump + spin attack | Parry practice | Room 6 | Time parry when spinning |

### Mini-Boss Tier

| Enemy | HP | Damage | Special | Role | First Seen | Tactics |
|-------|----|----|---------|------|-----------|---------|
| **Void Strider** | 16 | 3 (melee) | Teleport + charge | Dodge/dash timing | Room 13 | Dash under charge attack |
| **Prism Sentry** | 20 | 4 (laser) + 4 (slam) | Laser beam, ground slam | Realm shift required | Room 16 | Shift when laser on you; tank slam in Echo |

### Boss Tier

| Enemy | HP | Damage | Phases | Special | Learning | Defeat Reward |
|-------|----|----|--------|---------|----------|--------|
| **Sentinel-01** | 60 | 5–8 | 3 | Arena-locked, visual tells | Parry timing, positioning, phase transitions | Piston Boots (enables wall jump) |

---

## Z1 Room Placement & Pacing

**Rooms 1–5:** Crawler only  
→ *Goal:* Master run/jump/basic combat. Crawlers can't kill if you walk into spike pits.

**Rooms 6–8:** Crawler + Weaver  
→ *Goal:* Learn parry mechanics. Weavers reward timing.

**Rooms 9–12:** Weaver-heavy, training ground feel  
→ *Goal:* Build confidence with parry combos.

**Rooms 13–15:** Strider introduction (single, then pairs)  
→ *Goal:* Dash under attacks. Learn spacing.

**Rooms 16–17:** Sentry introduction  
→ *Goal:* Realm shift under pressure. Survive laser.

**Boss Arena:** Sentinel-01 (mandatory, no escape)  
→ *Goal:* Apply all learned mechanics. Full focus.

---

## Damage & HP Formula

**Player Kael (reference stats at game start):**
- Max HP: 30
- Attack damage: 3 (basic), 4 (charged), 5 (Resonance)
- Resilience: None (1× damage taken in Prime, 0.2× in Echo)

**Enemy Scaling:**
- Minion HP ≈ 2.5–4× Kael's attack damage
  - Crawler (8 HP) = 2–3 hits to kill
  - Weaver (12 HP) = 3–4 hits to kill
- Mini-boss HP ≈ 5–5.3× Kael's attack damage
  - Strider (16 HP) = 5 hits to kill
  - Sentry (20 HP) = 6–7 hits to kill
- Boss HP ≈ 10–15× Kael's attack damage
  - Sentinel (60 HP) = 12–20 hits to kill (depending on phase)

**Damage to Player:**
- Minion: 1–2 per hit (5–6 hits to kill Kael in Prime, 25–30 in Echo)
- Mini-boss: 3–4 per hit (8–10 hits in Prime, 40+ in Echo)
- Boss: 5–8 per hit (4–6 hits in Prime, 20+ in Echo)

**Sentinel Boss Damage Phases:**
- Phase 1: 5 damage/hit (moderate threat)
- Phase 2: 6 damage/hit (slightly harder)
- Phase 3: 7–8 damage/hit (final test)

---

## Enemy Balance Goals (Post-Demo)

### Z2 — Rift Knight Era

| Enemy | Difficulty | Role |
|-------|-----------|------|
| Void Wraith (new) | Minion++ | Ranged attacker; learn kiting |
| Rift Knight (K5) | Mini-boss+ | Faster Strider; intro to combos |
| Void Guardian (new) | Mini-boss | Shield mechanic; interrupt required |

**Zone Goal:** Players have Wall Jump now. Vertically complex rooms. Rift Knight demo.

### Z3 — Myra/Echo Era

| Enemy | Difficulty | Role |
|-------|-----------|------|
| Void Phantom (new) | Minion+ | Phase in/out; unpredictable |
| Keeper Myra (K6) | Boss | 2v1 with Doppelgänger; mirror mechanic |
| Doppelgänger (K6) | Boss-support | Mimics Myra; destroy together |

**Zone Goal:** Realm shift affects enemy behavior. Myra teaches cooperation against multiple enemies.

### Z4 — Chronos Era

| Enemy | Difficulty | Role |
|-------|-----------|------|
| Void Titan (new) | Mini-boss | Heavy hitter; tank-and-parry game |
| Rift Knight Prime (K7) | Boss-phase | Rift Knight with Chronos gravity effect |
| Chronos (K7) | Final Boss | 3 phases, gravity manipulation, bullet-hell segment |

**Zone Goal:** All mechanics converge. Chronos demands mastery of all toolsets.

---

## Difficulty Modifiers (Assist Mode Integration)

**Standard (Normal):**
- 1.0× damage taken
- 3.5-tile jump height
- 0.1s coyote time
- 0.15s invincibility frames (i-frames) after hit

**Assist Mode (Easy):**
- 0.75× damage taken
- 3.5-tile jump height (same)
- 0.2s coyote time (+0.1s)
- 0.2s invincibility frames (+0.05s)

**Expected Impact:**
- Crawler: 3 hits → 4 hits to kill
- Weaver: 4 hits → 5 hits to kill
- Sentinel: 12 hits → 15 hits to kill
- **Effective Difficulty:** ~20% easier. Meant for players learning the game, not "easy mode" trivialization.

---

## Playtime Targets per Boss

| Boss | Target Playtime | Notes |
|------|-----------------|-------|
| Sentinel-01 (Z1) | 10–12 min | Single-phase learning; 3 phases to master |
| Rift Knight (Z2) | 8–10 min | Faster than Sentinel; less complex |
| Myra + Doppel (Z3) | 12–15 min | Slowest; 2v1 coordination learning |
| Chronos (Z4) | 15–20 min | Final boss; longest; 3 intense phases |

**Total Demo Z1 playtime target:** 30–40 min (rooms + Sentinel).

---

## Playtest Feedback Loop

### Early Playtest (Week 3)
- 3 players minimum
- Target: Learn if difficulty ramps correctly
- Metric: "Did you feel challenged but not cheap?"
- Action: Adjust HP / damage if "cheesy" (1-button cheese), or if 3+ hits feel required vs 1

### Mid-Sprint Playtest (Week 4)
- 5+ players (more diverse skill levels)
- Target: Validate Sentinel boss fight is fair
- Metric: Average clear time, number of deaths, feedback on tells
- Action: Adjust phase transitions, visual/audio tells

### Pre-Release QA (Week 5)
- Full playthrough with no guides
- Target: Verify no softlock (enemy KO'd but player can't progress)
- Metric: 1 blind playthrough → notes for balance
- Action: Final tweaks to Sentinel AI or room difficulty spike

---

## Special Cases

### Echo vs. Prime Damage

When Kael is in Echo realm:
- All incoming damage is **0.2×** (×0.2)
- This means a Weaver doing 2 damage does 0.4 → rounds down to 0 most hits
- **Strategy:** Echo realm is the "safe" mode. Players learning should use Echo to tank. Advanced players should play in Prime.

### Assist Mode + Low Health

- When Kael HP < 25%, LPF filter applies (800 Hz cutoff) + heartbeat audio
- Damage does not change; only audio/visual feedback increases
- This is a **stress cue**, not a balance change

### Chronos Special Case

- Gravity reversal flips "up" and "down"
- Enemies in reversed gravity have **+1.5× HP** to account for Kael's inverted toolkit
- Chronos phases: 60 HP → 70 HP → 80 HP (increase per phase)

---

## Balance Philosophy

**"Learn first, master second."**

- Z1 teaches one concept per few rooms (run → jump → parry → realm shift)
- Z2–Z4 layer concepts: "You know this now; combine it with this new thing"
- No enemy is designed to be "unkillable" — all are beatable with standard combo (3 attacks = 9 damage vs 8 HP Crawler)
- Assist Mode exists so players who struggle mechanically can still progress (intended for ~10% of audience)
- Post-defeat review: Sentinel deaths should feel like "I messed up the timing" not "boss is unfair"

---

**Last updated:** 2026-09-24  
**Prepared by:** Claude (based on Codex + Anti input on art/balance)  
**Target audience:** Blind playtesters (Week 3–5)
