# HoN-Derived Card Battle RPG: Master Plan v2

Revised from v1. Same vision, different order of work: prove the game is fun first, research in parallel, build identity last.

## 1. Vision and Design Pillars

**Goal.** Build an original turn-based card battle RPG whose combat depth comes from the systems of Heroes of Newerth (HoN): heroes with attributes, ability kits, cooldowns, items, summons and status effects. We are not recreating the MOBA. We are keeping what makes its combat interesting and rebuilding it as tactical card play.

**Final game identity.** All names, characters, art, lore and terminology are original. HoN is a mechanical reference only.

**Design pillars** (every decision is checked against these):

1. **Heroes matter.** A hero is a kit of meaningful decisions, not a stat block.
2. **Builds matter.** Equipment and leveling change how a hero plays, not just the numbers.
3. **Readable combat.** A player can predict a result before committing. No hidden math.
4. **Deterministic by default.** Same state plus same action gives the same result. Randomness exists only where a rule explicitly says so.
5. **Short matches.** Target 15 to 25 minutes (tunable).

## 2. Guiding Rules

Carried over from v1 and tightened:

1. **Understand before changing.** Document a mechanic's purpose and dependencies before adapting it.
2. **No fabricated data.** Missing HoN values stay marked unknown until verified or explicitly reconstructed.
3. **Two data layers.** The original reference layer is never overwritten by card-game values.
4. **Track every adaptation.** Each change from HoN has a recorded reason.
5. **Playable beats complete.** A small working game beats a large untested database.
6. **Mechanics before lore.** Names and worldbuilding come after the rules are validated.
7. **Research is on demand.** Extract and verify only what the next prototype needs, plus a low-priority background archive.
8. **Every value has a source and version** whenever one is known.

## 3. Open Decisions (resolve in Phase 0)

These shape the whole architecture. v1 left them open; v2 requires an answer, even if provisional, before engine work starts.

| Decision | Options | Recommended default |
| --- | --- | --- |
| **Ability model** | (A) Abilities are fixed on the hero with cooldowns. (B) Abilities are cards drawn from a deck. (C) Hybrid: kit is fixed, deck supplies items, units and buffs. | **C.** Keeps HoN-style kits and cooldowns, and adds card-game variety through the deck. |
| **Time quantum** | How many HoN seconds equal one turn? | Pick a fixed value T (for example 5 s) and derive conversions from it (see section 6). |
| **Spatial model** | None / lanes / small grid / front-back rows | **Front and back rows** per side. Enough for melee vs ranged and area effects without a full map. |
| **Randomness** | None / seeded RNG for crits and evasion | Seeded RNG, off by default in early prototypes. |
| **Match structure** | Heroes only / heroes + units / full deck draw | Heroes + units, then add deck draw in the second prototype. |
| **Win condition** | Kill all heroes / destroy a base card / points | Defeat all enemy heroes (simplest to test). |
| **Tech stack** | Engine-agnostic core + any front end | **Godot 4 .NET (C#)** — desktop application (Windows EXE + installer). Engine, data, simulator and UI all in one Godot project. C# for all game logic; Godot scenes for UI and rendering. No web build. |

## 4. Roadmap

| Phase | Goal | Exit criteria |
| --- | --- | --- |
| **0. Foundations** (about 1 week) | Pillars, open decisions, repo, legal boundary | Decisions table filled in; repo and folder structure exist |
| **1. Rules engine and simulator** (2 to 3 weeks) | Headless, deterministic combat core plus bot-vs-bot runner | A scripted 2v2 hero match runs end to end and produces an event log |
| **2. Vertical slice content** (1 to 2 weeks) | 6 heroes and about 15 items/units/buffs, hand-built using HoN as reference | All content loads from JSON; every card has a source note |
| **3. Playable prototype** (2 weeks) | Minimal UI over the engine with a combat log | A human can play a full match |
| **4. Test and balance loop** (ongoing) | Simulation metrics plus human playtests | Scenarios A to I (section 9) pass; dominant strategies identified |
| **5. Adaptation and attribute redesign** | Formalize the translation registry; define the new 3-attribute model | Adaptation registry covers every mechanic used so far |
| **6. Original identity** | Names, factions, art direction, lore | Every hero, item and unit has an original identity |
| **7. Content expansion** | Grow roster and card pool only on validated rules | Each addition passes simulation regression |
| **8. Advanced systems** | PvE, AI opponents, multiplayer, deck building | Scoped separately after Phase 7 |

**Parallel track R (HoN research):** starts in Phase 1 at low priority and runs through Phase 5. It feeds each phase with exactly the data it needs (section 5).

## 5. Research Track (HoN Source Work)

The v1 research plan stays, but it supports the prototype instead of blocking it.

**Sources to look for:** original client resource archives, community-maintained repositories and documentation, historical patch notes, public wikis and databases. Different sources may represent different HoN versions, so each must be logged.

**Source inventory (one row per source):** ID, name, type (game files / code / docs / database), version, collection date, origin (original / community / reconstructed), completeness, reliability, notes.

**Archive integrity:**

- Never modify original archives; extract into separate folders.
- Record paths, sizes and checksums; detect duplicates and conflicts.
- Keep the original file paths.

**Extraction:** identify which formats are standard containers (many are ZIP-compatible) and which need special tools. Automate batch extraction and indexing. Output JSON for game data, Markdown for human notes.

**Verification statuses** (every extracted mechanic gets one): Verified, Partially verified, Reconstructed, Community documented, Historical (specific old version), Unknown.

**On-demand rule.** When a prototype needs a hero or item, extract and verify that entry fully, then add it to the reference layer. Breadth comes later.

```text
HON_RESEARCH_ARCHIVE/
  00_SOURCE_INVENTORY/   sources.md, collection_log.md
  01_ORIGINAL_ARCHIVES/  (read-only)
  02_EXTRACTED/          scripts, heroes, abilities, items, units, effects
  03_PATCH_HISTORY/
  05_FILE_INDEX/         file_manifest.csv, checksums.csv, duplicate_report.csv
```

## 6. Adaptation Method (Real-Time to Turn-Based)

Many MOBA values are tied to real time, position and projectiles. They cannot be copied directly. The core design work is **abstraction**, and it needs a consistent rule.

**Time quantum.** Define T = seconds per turn. Then, as starting formulas to be tuned in testing:

- Cooldown in turns = ceil(cooldown seconds / T)
- Attacks per turn = f(attack speed, T), capped for readability
- Regeneration per turn = regeneration per second x T
- Duration in turns = ceil(duration seconds / T)
- Range, radius and movement collapse into row/targeting rules

**Translation record** (one entry per mechanic in the adaptation registry):

| Field | Purpose |
| --- | --- |
| Original mechanic and function | What it does in HoN |
| Original parameters and source | Values with version and verification status |
| Dependencies | Other systems it relies on |
| Card adaptation | Proposed implementation |
| Changed parameters and reason | What changed and why |
| Balance risks | Known problems |
| Test status | Untested / testing / validated |

**Attributes.** Document what each original attribute actually does (it may affect several stats) before naming or restructuring anything. New names come after the analysis, in Phase 5.

## 7. Architecture

**Layers (strict separation):**

1. **Data**: JSON definitions (heroes, abilities, items, units, effects).
2. **Engine**: a pure, headless rules library. `apply(state, action) -> { newState, events[] }`. No UI, no I/O, no hidden global state.
3. **Simulator**: bots that play matches through the engine and collect metrics.
4. **Client**: a UI that renders state and events and sends actions.

**Engine requirements:**

- Every action is validated; invalid actions are rejected with a reason.
- Seeded RNG; matches are fully replayable from (initial state, seed, action list).
- Every state change emits a human-readable event (powers the combat log).
- Combat resolution follows one documented order: validate action, validate target, check restrictions, base values, bonuses, reductions, defenses, apply damage or healing, secondary effects, triggers, cooldown and state updates, victory check.
- Effects are data-driven (stacking, refresh, dispel, immunity rules are fields, not code branches).

**Data object example** (two layers on every entry):

```json
{
  "id": "ability_001",
  "source": { "hon_id": "unknown", "version": "unknown", "status": "unverified" },
  "original": { "cooldown_s": null, "mana_cost": null, "damage": null },
  "adapted": { "cooldown_turns": 2, "energy_cost": 1, "damage": 30 },
  "effects": [],
  "notes": "Adaptation reason goes here."
}
```

**Repo layout (Godot 4 .NET project):**

```text
/data              heroes, abilities, items, units, effects (JSON) — loaded at runtime
/src
  /Engine          C# rules library: state model, apply(), event log, seeded RNG
  /Sim             C# bots, batch runner, metrics (headless, no scene required)
  /UI              Godot scenes + C# scripts for the prototype client
/research          HoN reference layer (never shipped, excluded from export)
/docs              design decisions, adaptation registry, rules
project.godot      Godot 4 .NET project file
```


## 8. Prototype Specification

**Prototype 1 (engine-first):** 2 players, 3 heroes each, 6 distinct heroes total, each with 3 abilities and an ultimate; basic leveling; cooldowns; damage types and armor/resistance; at least one each of shield, heal, stun/slow/silence, summon and DoT. About 10 to 15 equipment, buff and unit cards. Heroes and units only; no deck draw yet.

**Prototype 2:** add deck draw, hand, resource system and full equipment slots.

**UI priorities:** hero stats, ability availability and cooldowns, unit health, equipment, active effects, turn indicator, and a **combat log**. Visual polish is not a goal.

**Resources:** add a resource (mana, energy, action points) only if it has a clear gameplay purpose that cooldowns alone do not provide.

## 9. Simulation and Balance

v1 asked for win rates and dominant combinations but gave no way to measure them. v2 adds the tooling.

**Bots:** random legal-move bot, then a greedy bot, then (later) search-based bots (such as MCTS).

**Metrics per batch of thousands of matches:** win rate by hero, item and card; average match length; damage per turn; healing efficiency; hero survival; unused cards; first-player advantage; stalemate and infinite-loop detection.

**Regression safety:** golden replays (saved seed plus actions plus expected result) run on every rule change.

**Test scenarios (human and scripted):**

- A: Basic combat exchange.
- B: Defensive play (shields, armor, healing).
- C: Crowd control chains.
- D: Unit and hero interaction.
- E: Summoning.
- F: Different equipment builds.
- G: Buff interactions.
- H: Progression during a match.
- I: Extreme interactions: infinite loops, permanent control, runaway damage or resource generation.

Change log rule: each balance change records old value, new value, reason, expected result and actual result. Avoid changing several unrelated systems at once.

## 10. Original Identity (Phase 6)

Only after the rules are validated:

- New names for heroes, abilities, items, units, attributes, factions and resources, with one consistent naming system.
- Per hero: concept, faction, role, personality, kit, strengths, weaknesses, lore.
- Per item and unit: visual concept, function, origin.
- World bible (history, factions, locations) supports the mechanics rather than dictating them.

## 11. Lean Documentation

v1 listed 21 documents. A solo project needs these:

1. **Master plan** (this file).
2. **Design decisions log** (decision, date, reason).
3. **Adaptation registry** (section 6 table).
4. **Core rules** (turn structure, targeting, combat order, cooldowns).
5. **Data dictionary** (JSON schema).
6. **Source inventory** (research track).
7. **Test and balance log** (results and change log).
8. **World bible** (created in Phase 6).

Other references (hero, ability, item databases) live as data, not prose.

## 12. IP and Legal Hygiene

Not legal advice; consult a lawyer before any public release.

- HoN material is **reference only**. No HoN assets, names, text, icons, models or audio in any shipped build.
- Keep `/research` out of the game build and out of any public repository.
- Document provenance of every source, especially community and unofficial ones.
- Hero concepts must be independently designed, not renamed copies.

## 13. Risks

| Risk | Mitigation |
| --- | --- |
| Research absorbs all the time | On-demand extraction; research track is lower priority than the prototype |
| Real-time values do not translate | Time quantum plus the adaptation registry; accept redesign where needed |
| Too much complexity for the first prototype | 6 heroes, few cards, no deck draw until Prototype 2 |
| Hidden or unclear combat math | Event log; one documented resolution order |
| Balance work is guesswork | Headless simulator and metrics before manual tuning |
| Legal exposure from unofficial sources | Section 12 boundary; clean separation of reference and shipped content |
| Scope creep (PvP, multiplayer, AI) | Deferred to Phase 8 |

## 14. Milestones and Definition of Done

| Milestone | Deliverable |
| --- | --- |
| M1 | Design pillars and open decisions resolved |
| M2 | Deterministic engine with event log and replay |
| M3 | Simulator running bot matches with metrics |
| M4 | Vertical slice content (6 heroes, about 15 cards) loaded from data |
| M5 | Human-playable prototype with combat log |
| M6 | Scenarios A to I run; first balance pass done |
| M7 | Adaptation registry and new attribute model |
| M8 | Original identity for all prototype content |
| M9 | Content expansion on validated rules |
| M10 | PvE, AI, multiplayer scoped and started |

**Ready for full content production when:** the engine is deterministic and documented; a complete match plays from start to finish; cooldowns, progression, status effects and equipment work; simulation shows no dominant strategies or loops; and the attribute model is settled.

## 15. First 14 Days

1. Fill the open-decisions table (section 3), even provisionally.
2. Write the design pillars and match structure on one page.
3. Set up the repo and folder structure (section 7).
4. Define the JSON schema for hero, ability, effect, unit and item.
5. Implement the state model and `apply(state, action)` with an event log and seeded RNG.
6. Implement damage, armor, cooldowns and one status effect; add a golden replay test.
7. Build a random-move bot and run the first 1,000 matches.
8. Pick the first 2 heroes from HoN to study in depth and extract their data into the reference layer.

**Final principle:** Build the smallest game that proves the combat is fun. Research what it needs. Measure it with simulation. Then build the world.
