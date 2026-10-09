# Farm Quest — Phase 0 Pacing Simulation Report

**Date:** 2026-10-08 · **Sim:** `phase0-pacing-sim.py` (re-runnable, seed-fixed)
**Scope:** Validates the v1 tuning numbers in `game-design-document.md` against its
own stated targets — then re-runs against the **actual built numbers** in
`unity-project/Assets/Game/Editor/GameDataGenerator.cs`, which turn out to differ.
Model: casual player, 2 short sessions/day. Conservative: ignores orders, story
and achievement XP unless noted.

## ⚠️ Finding 0 — The build has drifted from the GDD (decide source of truth first)

The Unity data generator disagrees with the GDD on **13 of 14 crops**:

- **Growth times roughly halved** in the build (potato 8→4 min, corn 20→10,
  strawberry 30→12, watermelon 45→20, wheat 45→25, sunflower 40→20…; only
  pumpkin matches at 15 min).
- **Apple/orange are regular one-shot crops in the build** (80/90-coin seeds,
  30/35-min growth) — the GDD designs them as **permanent orchard trees**
  (250/300 saplings, regrow timers, the whole perennial-vs-annual teaching point
  in §35). That design is currently not implemented.
- **Wheat moved L24 → L20** in the build (likely to fill the Phase-2 roster).
- **4 GDD crops have no build counterpart**: cucumber (L8), spinach (L10),
  chili (L16), bell pepper (L18) — so build levels 8/10/16/18 unlock no new crop.
- **Harvest XP is per-crop baked (8–26) in the build** vs the GDD formula
  (8 + 2/star); missions in the build pay 40–120 XP each vs the GDD's flat 30.

Seed costs, sell prices, yields and watering counts all match. The drift is
confined to growth times, XP awards, 4 missing crops, and the apple/orange tree
design. **Before any retune, decide: is the GDD or the build the source of
truth?** Everything below is computed against the *build's* numbers (what
players will actually feel), with GDD deltas noted.

## What the numbers say vs the targets (build numbers)

| Target (GDD §25) | Sim result (build data) | Verdict |
|---|---|---|
| L10 in ~2 weeks | L10 at **~13.5 days** | ✅ Hit — the faster build pacing fixed this |
| L30 in ~3 months | L30 at **~149 days** | **Miss by ~1.65×** — see finding 1 |
| L50 in 6–9 months | **L39 after 300 days** | **Miss** — see finding 1 |
| "Profit per plot-hour rises gently" | Per-day earnings rise nicely (see finding 2) | ⚠️ Holds per-day, not per-hour |

(The earlier GDD-numbers run gave L10 ~24d / L30 ~247d — the build's halved
growth times + per-crop XP + generous missions roughly halved those.)

## Finding 1 — The XP curve still outruns the loop after L20

Curve `100 × n^1.5` needs 189k XP for L30 and 690k for L50. Against the build's
(faster) numbers the loop + missions deliver ~800 XP/day early, ~1,500/day
mid-game — then it plateaus, because XP per cycle is similar across crops and
attention is capped at 2 sessions/day. Result: L30 at ~149 days vs the 90-day
target; L50 unreachable in 300 days (L39). The build's faster pacing closed
about half the gap; the rest is structural.

**What this means:** the pacing targets are achievable *only if* orders,
missions, animals, story chapters and achievements consistently deliver a large
share of XP from mid-game on. With Phase 3's animals (~475 XP/day for a full
setup, see addendum), farming + animals ≈ 1,500–1,700 XP/day vs ~2,100/day
needed — so **orders/missions/achievements must still deliver ~400–600 XP/day
sustained**. That dependency is never stated in the GDD — right now the design
reads as if farming carries pacing, and it can't.

**Options (your call):**
- **A.** Keep the curve; state the dependency explicitly and tune the order
  board (L22+) to ~500 XP/day (e.g. 4–5 orders/day at ~120 XP avg). Cheapest
  change, but pacing then lives or dies on order tuning.
- **B.** Flatten the curve to ~`100 × n^1.35` so the loop carries ~70%.
- **C.** Move the targets: L30 in ~5 months, L50 in ~12 months. (Honest, but
  slows the "dream farm" fantasy.)

Also watch the **endgame XP desert**: achievements and story are one-time; by
L40 the loop must be carried by repeatable sources (mastery orders, farm
rating). The GDD's L41–50 "rating chase" should be an XP source, not just a
leaderboard.

## Finding 2 — Crop economy: works for casual players, wheat is a trap (in the build too)

For the intended audience (short sessions, come-back-tomorrow), the metric that
matters is **coins per day per plot** (2 sessions ≈ 2 cycles). On the build's
numbers it rises nicely with level: carrot 40 → tomato 126 → watermelon 240 →
orange 220 → coffee-tier… — taps *feel* better as you level. The design intent
holds for casual play.

But **wheat (L20) earns 100 coins/day/plot — less than corn (L13, 164/day) and
less than everything unlocked since L9.** A kid unlocking wheat at L20 and
earning less than their old crops feels like anti-progression, and the trap
survived the port from GDD to build unchanged. (Sunflower L21 at 160/day vs
corn's 164 is borderline; its +10% honey synergy arguably justifies it.)

**Options:** buff wheat's raw numbers (e.g. sell 8→10, or yield 10–15→14–18)
so each tier beats the last per-day — *or* explicitly make processing its
payoff (flour/bread chain) and note it in the GDD so the raw numbers aren't
read as the crop's value. Separately: **tomato's 756 coins/plot-hour is still
never beaten**, so a grindy player has no economic reason to ever plant anything
else while actively playing. If "active play = tomatoes, idle play = long
crops" is the intent, fine — but then crop *variety* rides entirely on
missions/orders, and that's worth stating.

## Finding 3 — Late-game coin surplus

Sim ends day 300 with **~430k coins** and nothing left to buy after Mill Grounds
(25k at L28). Equipment T5 (50k+) and decor absorb some, but a repeatable coin
sink (seasonal decor sets, farm-rating investments, charity/events) will be
needed or coins go numb in the last third of the game.

## What's healthy (no action)

- **Starting capital is safe:** every crop is profitable per cycle at ⭐, so a
  kid can never go broke farming — only overspending on decor/land drains coins.
- **Early pacing feels right:** L2 on day 1, L5 ~day 5, L9 + affordable land 2
  (1,000 coins) at ~day 20 — the first expansion lands exactly when the level
  gate opens. Good.
- **Per-harvest revenue climbs 30 → 405 coins** — each new tier's harvest tap
  feels meaningfully bigger.

## Suggested next step

1. **Decide source of truth** (Finding 0): GDD or build? In particular whether
   apple/orange become real orchard trees (a GDD §35 teaching pillar) or stay
   one-shot crops, and whether the 4 missing crops (cucumber, spinach, chili,
   bell pepper) get build slots or get cut from the GDD.
2. Pick a direction on Finding 1 (A/B/C) and Finding 2 (buff wheat vs
   processing-payoff), then I'll apply the retune to the chosen source —
   GDD tables and/or the Unity `CropData`/`AnimalData` ScriptableObjects —
   and re-run the sim to confirm the targets land.

Doing this *before* Phase 4 (tractor/equipment/orchard/irrigation) matters:
Phase 4 bakes in equipment costs and irrigation effects against these same
economy numbers.

## Addendum — animal layer (Phase 3 now built)

Phase 3 is implemented (AnimalData/AnimalService, 7 animals, barn/house/decor).
Steady-state estimate for a full mid-game animal setup at 2 sessions/day:

| Animals | Coins/day | XP/day (collect 6 + care 5 + sell) |
|---|---|---|
| 4 chickens, 2 cows, 2 sheep, 2 goats, 2 ducks, 2 hives | ~1,750 | ~475 |

Farming (~1,000–1,200 XP/day mid-game) + animals (~475) ≈ **~1,500–1,700 XP/day**
vs the ~2,100/day the L30-in-90-days target needs. So even with Phase 3's
animals, **orders/missions/achievements must still deliver ~400–600 XP/day
sustained** — reinforcing Finding 1: state this dependency in the GDD and tune
the order board (L22+) as the pacing engine, not a side activity. Animal coin
rates are attention-light by design (chicken 90 coins/hr vs tomato plot
756/hr) — fine for a kids' game, but it means animals don't fix coin pacing
either; they fix engagement variety.
