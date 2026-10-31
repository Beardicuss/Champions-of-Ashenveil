# First playtest rules (provisional)

These are test settings, not permanent balance decisions. The goal is to measure a 15–25 minute MOBA-inspired card battle. The player's established direction is: complete Hero Cards with fixed level-based kits; automatic lane creeps; Hero summons as combat units; a deck and hand; separate match and account economies; and Normal, Deal, and Ranked modes.

## Match format

- Two players, three Heroes each, one contested lane, front and back rows. Defeat all opposing Heroes to win.
- A round has one turn for each player, then one creep phase. First player alternates each round.
- At the start of a round, spawn the scheduled creep wave and announce objective timing.
- At the start of a player turn, grant base gold, draw one card, and allow available Hero ability upgrades.
- Each living Hero may take one action: attack, use an unlocked ability, or change row. A player may play up to two cards, before or between Hero actions.
- After both player turns, creeps act. Damage over time resolves, durations and cooldowns tick once, then the next round begins.
- Cooldowns are measured in rounds. A two-round cooldown used in round 2 is available in round 4.

## Heroes and progression

- Each Hero Card represents its entire Hero. Abilities are fixed kit actions; they are never drawn or consumed.
- Test kit: three standard abilities and one Ultimate. Heroes start at level 1 with one standard ability chosen during setup. Level cap is 6. Each level gained grants one ability point. Ultimate becomes eligible at level 4 and costs a point.
- XP comes from nearby creep kills, Hero kills, objectives, and a small shared component. XP thresholds and upgrade values require measured tuning.

## Cards and economy

- Test deck: 20 cards, up to 8 equipment cards, at most two copies per card. Opening hand 4, draw 1 per player turn, hand limit 7, one discard reshuffle. No further draw after second exhaustion.
- Card categories: equipment, consumable, tactical; deployable units are reserved for a later test. Hero abilities, ordinary creeps, and ability buffs are not cards.
- Match gold starts at zero. Base income arrives each player turn; last hits, Hero kills, and objectives award more. Basic equipment is available from a small in-match catalog. Specialized equipment is drawn and paid for with match gold. Each Hero has two equipment slots.
- Gold amounts, card costs, XP thresholds, wave size, and objective timing are tuning values, not settled rules.
- Account currency is distinct from gold and is awarded after a finalized match. A permanent shop sells cards directly. A functional starter collection is guaranteed; additional cards should be sidegrades.

## Modes

- Normal: no stakes, no card rewards or losses, small account currency reward.
- Deal: pre-match escrow of eligible surplus card copies or capped account currency. Valid winner receives the agreed stake; draw or invalid match returns it. Forfeit and technical failure rules must be finalized before release.
- Ranked: rating changes with results and account currency is awarded. Competitive starter access is guaranteed; gameplay rarity is not part of the first test.

## Measurement and exit criteria

Record human turn elapsed time, actions per Hero, cards played and drawn, gold earned and spent, round count, and match wall-clock duration. Review at least several complete matches before tuning numbers. Initial targets: 15–25 minute matches, approximately 45–75 seconds per learned player turn, near-even first-player win rate, at least two meaningful equipment purchases per match, and regular useful card decisions. These are hypotheses to test.

## Implementation sequence

1. Make action ownership and once-per-Hero turn limits enforceable in the engine. Use three-Hero match setup and show turn metrics in the UI.
2. Add deterministic round timing, automatic waves, creep combat, and gold/XP events.
3. Add Hero ability ranks, level choices, and per-round cooldowns.
4. Add deck, hand, discard, card validation/play, basic equipment catalog, and gold spending.
5. Add one timed objective, then collect complete match metrics and tune pacing.
6. Build account collection, permanent shop, and Normal settlement. Add Deal escrow and Ranked rating only after match replay and settlement are reliable.

The implementation steps may be delivered incrementally. A step is complete only when its rules can be exercised in a full playable match and its relevant metrics are recorded.

## Current implementation status

The first instrumented slice starts three Heroes per player, enforces one Hero action per turn, grants 100 match gold at each player-turn transition, draws from a 20-card equipment test deck, permits two equipment plays per turn, and limits each Hero to two items. The battle log reports round count, elapsed match time, average human turn time, Hero actions, cards played, and gold earned/spent. These are provisional mechanics for measuring the loop, not completion of the full rules above.

The initial equipment-only slice was verified with an offline build and headless engine batch. Its first bot results are kept below as a comparison baseline.

Automatic creep waves and creep combat are now implemented for the one-lane test. Two creeps per side spawn in round 1 and every three rounds thereafter, with a cap of four living creeps per side. Creeps attack after both players have acted, targeting enemy creeps before Heroes. Hero last hits earn 75 match gold; Hero kills earn 200. Creep HP is provisionally half the source definition to fit turn-based pacing. The UI displays waves through a separate `CreepLane` scene. Alternating initiative, objectives, tactical cards, and account settlement remain open; Hero progression is described below.

## Bot batch baseline

The main menu **Run 100 Bot Matches** button runs seeded three-Hero matches and displays a summary of wins, draws, rounds, cards played, gold earned/spent, and simulation time. The bots select random Hero actions and equip the cheapest affordable equipment card when a slot is available. This is a stability and pacing baseline, not a balance-quality AI.

In a headless engine run with seeds 1–100 and the six current Heroes split into two alternating teams: player 0 won 48, player 1 won 52, with no draws. Mean match length was 18.3 rounds (range 5–41); mean total equipment cards played was 9.2; mean total gold earned was 3,519 and spent was 2,124. The runner recorded zero invalid actions. These numbers measure simulated rounds, **not** human match duration or turn time. They should not be treated as balance approval because automatic creeps, XP, objectives, tactical cards, and several other planned rules are not in the match yet.

With waves enabled and creep HP adapted to half of the source definition, the same 100 seeds produced 53–47 wins, zero draws, and 27.9 rounds on average (range 5–60). Matches averaged 11.4 Hero last hits, 854 farming gold, 29.4 recorded creep spawns after setup, and 5.0 surviving creeps at match end. There were zero invalid actions. This is a random-bot baseline; the longer matches and frequent surviving creeps indicate that wave pressure and targeting need further tuning before judging match duration.

## Hero progression test

The engine now tracks match XP, ability points, and ability rank per Hero. The first standard ability starts at rank 1; other abilities must be learned. A Hero can spend an earned point on any eligible standard ability up to rank 3. The Ultimate is eligible at Hero level 4 and has one test rank. Hero level cap is 6. The test XP threshold for the next level is `40 + 20 × (current level − 1)`. All living allied Heroes receive 10 XP each round, 25 XP after a Hero last-hits an enemy creep, and 80 XP after a Hero kill. Each level grants 20 maximum/current HP, 2 basic attack damage, and one ability point. Ability damage and healing rise by 25% of base value per additional rank; cooldown and energy cost remain unchanged in this test.

Ability upgrades are free match actions and do not consume the Hero's combat action. A dedicated Hero progression scene shows level, XP, points, and legal upgrade buttons. Bots spend points with a deterministic policy: take the Ultimate when eligible, then prefer the least-upgraded standard ability.

In the same 100-seed headless batch after progression was enabled: 52–48 wins, zero draws, 29.3 average rounds (range 10–65), 14.0 Hero creep kills, 1,048 farming gold, 25.8 level-ups, 25.2 ability upgrades, and 5.6 Ultimate unlocks per match across six Heroes. There were zero invalid actions. These are random-action bot results and do not establish balance or human match duration. The Godot battle scene loaded headlessly after the change.
