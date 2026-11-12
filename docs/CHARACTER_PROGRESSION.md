# Character progression balancing

The dashboard at `#dashboard` configures effective character progression globally. It does not edit characters or introduce a second combat calculator. Preview requests and gameplay both call `PlayerCombat.SetAbility`.

## Original progression

`CharacterProgressionRules.OriginalPotential` and `OriginalGrowth` centralize the existing defaults from Java `Char.selectClass` / `Char.addExp`:

| Class | Level-1 potential: strength / dexterity / vitality / spirit | Automatic potential per level |
| --- | --- | --- |
| 0, unclassed | 15 / 5 / 5 / 5 | 2 / 1 / 1 / 1 |
| 1, 3, 5, outer classes | 15 / 5 / 5 / 5 | 0 / 0 / 0 / 0 |
| 2, 4, 6, inner classes | 5 / 5 / 10 / 10 | 0 / 0 / 0 / 0 |

Classed characters receive free points and allocate them manually. Their base-only progression preview deliberately makes no allocation assumptions. Classed level 1 is a theoretical formula reference; school-entry level requirements are unchanged. Selected/passive skills and equipment remain additional real-character inputs. The unclassed sample includes the existing automatically grown basic skill.

The original derived formulas remain in `PlayerCombat.SetAbility` (the port of `AbilityFromEquip.setAbility`), including integer steps and the MP floor. Defense in this project means `DameDown`, flat damage reduction; the preview does not invent an armor statistic.

## Profiles and inheritance

`GameBalanceRules.CharacterProgression` owns profiles keyed by:

- `0`: unclassed characters.
- `classed`: shared defaults for classes 1 through 6.
- `1` through `6`: per-class overrides, inheriting each missing parameter from `classed`.

A stat rule has optional `base`, `growthPercent` and `growthFlat`. Missing fields inherit; deleting the rule/profile restores inheritance. Final fallback is the original base, 100% native growth and zero extra per-level growth. Reset of a class-specific profile restores its shared group, which may itself be customized.

For each stat, define:

- `F(L)`: the existing calculation for a naked reference character of that class/level, after tuned input potentials and existing legacy coefficients.
- `V(L)`: the existing calculation for the actual character, including allocated points, gear and skills.
- `B`: configured level-1 result, or `F(1)` if inherited from the original curve.

The effective result is:

`V(L) + [B - F(1)] + [F(L) - F(1)] * (growthPercent - 100) / 100 + (L - 1) * growthFlat`

Equivalently, this is the chosen base plus scaled native growth plus per-level additions and the character's separate contributions `V(L) - F(L)`. Derived base values are naked-result targets; they are not a second set of allocated potential points. Editing a potential base changes dependent native HP/MP/attack formulas. Explicit derived-base targets calibrate against those tuned potential bases.

Potentials use the same overlay with original class potential curves as their reference, preserving the difference represented by manually allocated raw points. Calculations operate on temporary arrays; saves retain their original allocation. Existing advanced whole-game coefficients apply before these profiles and remain in an expandable compatibility panel.

## Limits and persistence

- Bases: nonnegative, positive HP/MP maxima; potential 0..30000, speed 0..17, other configurable stats up to 100000000.
- Growth percent: 0..1000. Additional per-level growth: -10000..10000 with up to two decimals.
- Final game values truncate fractional parts and respect the same stat limits. Minimum attack cannot exceed maximum attack.
- `game-balance.json` version 2 adds optional `balance.characterProgression`. The reader accepts version 1 and preserves old player/equipment/EXP settings. No player-save migration or settings file is written merely by reading or previewing it.
- Each published balance snapshot memoizes native curves by `(classId, level)`. A new configuration gets a fresh cache. Reflection mappings are cached against the explicit existing stat allowlist.

## API and UI boundaries

The existing protected `POST /control/dashboard` handles:

- `kind: progression`, `scope`, `values`: save the selected profile; `reset: true` removes it.
- `kind: progressionPreview`, `scope`, `values`, `from`, `to`, `classes`: calculate a draft without publishing, saving or sending game packets.

Both use the existing token/Host/Origin/body-size checks and dashboard revision. Ranges are bounded by the game's max level and 200; classes come from the server metadata. A preview computes each requested level/class through the gameplay calculator. Original level-1 values are generated with default rules, not maintained as a duplicate frontend table.

`progression.js` owns form drafts, inheritance presentation, input debouncing, comparison rows and stale-response rejection. It contains labels and formula explanations, not stat math. Changing group/class retains its local draft; the selected draft alone is previewed, while comparison classes use saved settings. Saving affects only the chosen scope. The advanced legacy panel continues to use the existing save path.

## Extension and validation

To add a numeric stat, implement it once in the gameplay calculator/PlayerState and add it to `GameBalanceRules.PlayerFields`. Default/preview tables discover the allowlist; optionally provide a readable label and native formula explanation in the frontend. To port another class, update original class defaults and `MaxClassId` together with its actual school/skill mechanics; the UI consumes class IDs from metadata.

User requested compilation/Console checks only. No browser, preview request, save/restart or gameplay tests were executed by the agent. Manual checks should cover empty-profile parity, both class families, inherited/overridden fields, potential-dependent derived stats, non-compounding recalculation, saved allocations after relog, and immediate preview without persistence.

## In-dashboard guidance
Step 02 includes a term-by-term explanation and a separate illustrative Level-5 HP example: base 80, native cumulative increase 40, growth 150%, per-level extra 2 and separate contribution 20 yields 168. The example is static guidance, not the current draft result. Blank/zero semantics and classed zero native growth are explained. An expandable glossary describes all displayed attributes, including critical chance versus critical damage and internal movement speed. UI help does not duplicate or change the calculator.

## Wooden-sword comparison scenario
Step 03 separately lists unclassed without equipment, unclassed with the quest wooden sword and classed characters. includeWoodenSword=true adds rows with classId=0, scenario=woodenSword; the normal classed/unclassed rows retain their own scenario tags. The preview uses the shared real quest reward factory for item #194, upgrade zero, and recalculates through SetAbility with the current equipment overlays. No new class/profile, free-point allocation, actual inventory mutation or persistence is introduced. The alternate NPC menu sword variant is not this scenario.
