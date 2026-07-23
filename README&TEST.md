# Feed or Fallow

A Vintage Story **server-side** mod: domesticated and tamed animals grow hungry and **starve to
death** if you don't feed them. Keep your livestock fed, or let your pens fall fallow.

Targets **Vintage Story 1.22 / .NET 10**. Server-only — clients do **not** need to install it.

## How it works

Each affected adult animal carries a small server-side `EntityBehaviorFeedOrFallow`:

- **Grazing feeds them.** When an animal eats on its own — grazing grass/crops or feeding from a
  trough — its hunger clock resets automatically. So animals with forage keep themselves fed; the
  danger is animals shut in a **barren pen** with nothing to eat. (Toggle with `GrazingCountsAsFeeding`.)
- **Hand-feeding** also works: right-click an animal while holding any food item (grain, vegetables,
  fruit, bread, …). This resets its hunger clock and, by default, consumes one item. The feeding
  player gets a confirmation message.
- If an animal goes longer than `GraceHours` (default **72 in-game hours = 3 days**) without eating,
  it starts taking hunger damage every `DamageIntervalHours` until it is fed again — or dies.

All state is stored in the entity's `WatchedAttributes`, so it persists across save/reload.

### What counts as "domesticated/tamed"

Only animals of **generation ≥ 1** are affected (`MinGeneration` in the config). Wild animals spawn
at generation 0; anything bred or raised in captivity climbs from there, so wild herds you stumble
across won't starve, but your farm stock will. Set `MinGeneration: 0` to affect every animal carrying
the behavior. **Babies are exempt automatically** — the patches only attach to the `*-adult` entity
files (and `ExemptBabyCodeParts` is a second safety net).

### Affected animals

Patches attach the behavior to the vanilla domesticated/tamable adults:

| Animal | Entity file | Notes |
|--------|-------------|-------|
| Sheep | `sheep-adult` | bred livestock (gen ≥ 1) |
| Chicken | `chicken-adult` | bred livestock (gen ≥ 1) |
| Pig | `pig-adult` | bred livestock (gen ≥ 1) |
| Goat | `goat-adult` | bred livestock (gen ≥ 1) |
| Elk (tamed) | `elk-tamed` | mount — `minGeneration: 0`, affected once tamed |
| Elk (semi-tamed) | `elk-semitamed` | mount — `minGeneration: 0`, affected during/after taming |

Wild predators and game (bear, wolf, fox, hyena, deer, moose, gazelle, hare, raccoon) are intentionally
**excluded** — they aren't domesticated and don't eat from your hand.

Each patch's behavior value may carry a **`minGeneration`** override. Livestock use the global config
default (1, so only captive-bred animals starve); the tamed elk use `0` because "tamed" is independent
of breeding generation — a tamed mount should need feeding regardless. To cover more animals, copy a
file in `assets/feedorfallow/patches/` and point its `file` at the new adult entity.

## Configuration

On first launch the server writes `ModConfig/feedorfallow.json` (under `~/.config/VintagestoryData`).

| Field | Default | Meaning |
|-------|---------|---------|
| `GraceHours` | `72` | In-game hours without food before damage starts |
| `DamageIntervalHours` | `6` | In-game hours between damage ticks while starving |
| `DamagePerInterval` | `2` | HP lost per tick |
| `MinGeneration` | `1` | Only animals at/above this generation starve (0 = all) |
| `ConsumeFedItem` | `true` | Hand-feeding uses up one held item |
| `GrazingCountsAsFeeding` | `true` | Grazing / trough-feeding resets the hunger clock (false = hand-feed only) |
| `ExemptBabyCodeParts` | `["lamb","chick","piglet","calf","baby","cub","kid"]` | Entity-code substrings never affected |
| `CheckIntervalSeconds` | `10` | Real seconds between starvation checks |

Out-of-range values are clamped on load (e.g. negative damage, or a sub-`0.1h` damage interval) and the
correction is logged as a `[feedorfallow] Config adjusted: …` warning, so a config typo can't brick the mod.

## Installing (for testers)

You do **not** need the .NET SDK or to build anything — just the packaged `FeedOrFallow.zip`.

- **Single-player:** drop `FeedOrFallow.zip` into `~/.config/VintagestoryData/Mods/` (Linux),
  `%appdata%/VintagestoryData/Mods/` (Windows), then start/restart your world.
- **On a server:** this is a **server-side** mod — only the server needs it. Put the zip in the
  server's `Mods/` folder and restart. Connecting players don't install anything.

On first launch the server writes `ModConfig/feedorfallow.json` with the defaults in the table above;
edit it and restart to retune. To confirm the mod is live, look for a line like
`[feedorfallow] Active. GraceHours=72, ...` in `Logs/server-main.log`.

**What to test / report back:** does penned livestock (no grass/trough) start starving after ~3 in-game
days? Do animals with forage or a stocked trough stay fed on their own? Does right-clicking an animal
with grain/veg/fruit feed it ("The … eats hungrily.") and consume one item? Are babies and wild herds
left alone? Please include your `feedorfallow.json` and the mod version (`1.2.0`) with any report.

## Notes

- **Grazing integration.** Self-feeding is detected by reading `lastMealEatenTotalHours`, the in-game
  timestamp the vanilla `seekfoodandeat` AI stamps on an animal every time it eats — grazing grass/crops
  or feeding from a trough (the same value the vanilla body-condition/harvestable system already uses).
  No Harmony patching, no block scanning: any food source the animal reaches on its own resets the
  hunger clock. Disable with `GrazingCountsAsFeeding: false` for a hand-feed-only mode.
- Hand-feeding swallows the right-click (`PreventSubsequent`). The vanilla `multiply` (breeding) behavior
  runs *before* ours, so feeding a breeding-ready female to breed her may not also reset the hunger
  clock that same click — a minor edge case. Tune via `ConsumeFedItem` if needed.
- Patch `file` targets were verified against the 1.22 install
  (`assets/survival/entities/animal/...`, resolved via the `game:` domain). If a future update moves
  an entity, the game logs a patch-failed warning at startup — search `feedorfallow` in
  `Logs/server-main.log` and fix the `file` value.
