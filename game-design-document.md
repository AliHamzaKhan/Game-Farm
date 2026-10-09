# 🌱 Farm Life / Farm Quest — Game Design Document (GDD)

> **Status:** Draft v1.0 · **Date:** 2026-10-08 · **Technology:** Unity (C#, 2.5D isometric, orthographic) — decided 2026-10-08
> **Audience:** kids 6–12 and teens 13–16 · **Platforms:** mobile-first, offline-first
>
> **How to read the numbers:** every timing, price, and XP value in this document is a
> **v1 tuning value** — chosen with a consistent balancing methodology (see Appendix A),
> meant to be simulated in a spreadsheet and adjusted in playtests during Phase 0.
> They are starting points, not final answers. Design rules (the *systems*) are the
> stable part; numbers will move.
>
> Companion document: `phased-development-plan.md` (which phase builds what).

---

## 1. High Concept & Pillars

**One-line pitch:** A cozy farming life sim where kids grow crops, raise animals,
run a farm business, and build their dream farm — no violence, no gambling, no pressure.

**Design pillars (every feature must serve at least two):**

1. **Always something to do** — PLAN → FARM → CARE → EXPLORE → PRODUCE → SELL →
   UPGRADE → CUSTOMIZE → DISCOVER → EXPAND. Never WAIT → COLLECT.
2. **Kind by default** — crops never die, animals never suffer, failure only ever
   costs *quality*, never progress. Mistakes are how you learn, in-game and out.
3. **Earn everything that matters** — coins and both currencies are earnable in-game;
   money only ever buys cosmetics, convenience, or content packs. Never power.
4. **Learning hides inside play** — composting, seasons, profit math, and water
   conservation are mechanics first, lessons second.
5. **Short sessions, long dreams** — 5 minutes must feel complete; 500 hours must
   still have a horizon (the Dream Farm, Master Farmer).

---

## 2. Target Audience & Design Constraints

| Group | Needs | Design answer |
|-------|-------|---------------|
| Kids 6–9 | Understand in 2 minutes, no reading walls, no failure states | Visual-first tutorial, tap-everything, crops can't die, big buttons |
| Kids 10–12 | Collection, customization, pets, showing off | Collection book, decorations, pets, farm rating |
| Teens 13–16 | Depth, strategy, optimization, identity | Dynamic prices, production chains, orders, min-maxing quality, outfits |
| Parents | Safety, trust, no surprise spending | Parent mode, parental gate, one-time unlock, offline-first, no ads |

**Hard constraints (non-negotiable):** no open chat, no public UGC, no location,
no gambling-like mechanics, no loot boxes, no energy systems that punish kids for
stopping, no ads in the kids' experience, all IAP behind a parental gate.

---

## 3. Core Gameplay Loop (detailed)

The minute-to-minute loop, with what the player *does* and *decides* at each step:

1. **Survey** — walk the farm; droplet icons show thirsty crops, sparkle shows ready harvest.
   *Decision: what needs me most?*
2. **Prepare** — choose an empty plot → dig → compost → plant. *Decision: which crop
   fits my time? (3-minute carrots vs 45-minute watermelon)*
3. **Care** — water on time, add nutrients. *Decision: chase ⭐⭐⭐ quality or plant more?*
4. **Harvest** — tap ripe crops (harvest mini-game at higher levels).
5. **Route** — sell now / store / process / fill an order. *Decision: coins today
   vs bigger coins tomorrow.*
6. **Reinvest** — seeds, feed, equipment, land, animals, decorations.
7. **Return** — missions, orders, events, and growing things pull the player back.

**Anti-boredom rule:** at any point past the tutorial, the farm must surface at
least 3 concurrent meaningful goals (dashboard "Today's Goals" + order board +
a growth timer + a mission). Phase exits audit this (§48 in plan).

---

## 4. Onboarding — First 15 Minutes (FTUE script)

No text walls. An NPC farmer ("Grandpa Hamza" — friendly, warm) guides by *doing*:

| Minute | Beat |
|--------|------|
| 0–1 | Arrive at the old farm. Grandpa: "This was mine once. Let's wake it up." Camera pans the tiny farm. |
| 1–3 | **First planting, hand-held:** tap glowing plot → dig → plant 1 carrot seed (free) → water. Sprout animation. "Plants drink water like you do!" |
| 3–6 | While carrot grows (3 min, slightly accelerated the very first time), tour: well (refill can), house, storage. Plant 2nd plot freely. |
| 6–8 | Harvest carrot → basket animation → Grandpa walks you to the market stall → sell → **+coins fanfare**. |
| 8–11 | Buy 5 tomato seeds with earned coins (guided purchase). Compost tutorial: "Compost is old food turned into plant food." |
| 11–15 | Free play with 3 goals pinned: *Plant 3 tomatoes · Water 2 crops · Sell 5 vegetables.* Level 2 ding → carrot unlock celebration → "Come back tomorrow, farmer!" |

**Rules:** skippable after first completion (for replays), never blocks free play
longer than 60 seconds, all tutorial rewards are coins/XP (no exclusive items).

**Starting resources (§2):** 🪙 500 coins · 🌱 5 carrot seeds · 🌱 5 tomato seeds ·
💧 watering can · 🧺 harvest basket · basic hand tools · small farmhouse · well ·
storage shed · 4 plots.

---

## 5. Character Customization (§3)

**Available at start, all free, never monetized:**

- Body: 3 builds · Skin tones: 6 · Hair: 12 styles × 8 colors
- Shirts: 16 · Pants: 10 · Shoes: 8 · Hats: 12 (straw hat, cap, beanie…)
- Glasses: 6 · Accessories: 10 (freckles, bandana, earrings…)

**Unlocked through play (levels/achievements, never purchases):**
farmer overalls, raincoat (unlocks with first storm survived), gardening outfit,
tractor outfit (first tractor), 4 seasonal outfits, festival costumes (events).

**Rule:** customization is identity, not power. Nothing wearable affects gameplay stats.

---

## 6. Land System (§4)

The farm is a fixed hand-designed map; plots unlock in order (no procedural layout —
readability for kids matters more than variety).

| Land | Name | Unlock | Cost | Contents |
|------|------|--------|------|----------|
| 1 | Home Farm | Start | — | House, well, storage shed, **4 plots** |
| 2 | Sunny Field | L9 | 🪙 1,000 | **+8 plots** (12 total) |
| 5 | Animal Meadow | L12 | 🪙 4,000 | Pasture, chicken coop slot |
| 7 | Barn Plot | L14 | 🪙 8,000 | Barn building (storage ×4), animal slots |
| 4 | Orchard Hill | L16 | 🪙 3,500 | 6 fruit-tree spots (trees are permanent, regrow) |
| 3 | Big Field | L18 | 🪙 6,000 | **+12 plots** (24 total) |
| 6 | Harvest Plains | L23 | 🪙 15,000 | **+16 plots** (40 total), tractor garage |
| 8 | Mill Grounds | L28 | 🪙 25,000 | Factory building, loading dock |

**Orchard rule:** fruit trees are planted once (sapling cost) and produce on regrow
timers — a distinct rhythm from field crops, teaching perennials vs annuals (§35).

---

## 7. Soil Preparation (§5)

Five steps, each one tap with a satisfying animation. Never skippable *consequence*-wise
(the effects matter), but equipment automates the *tapping*:

1. **Clear** (only on new/debris plots) — remove stones/weeds.
2. **Dig** — hand shovel → tractor later auto-digs whole fields.
3. **Prepare** — rake smooth (quality foundation).
4. **Compost** — optional but +1 potential quality star; teaches composting (§35).
5. **Nutrients** — optional; needed for ⭐⭐⭐+ (see §9).

**Kid rule:** skipping compost/nutrients never fails the crop — it only caps quality
at ⭐⭐. The game *shows* the difference ("With compost, these could be ⭐⭐⭐!")
rather than punishing.

---

## 8. Crop Database (§6, §7)

**Columns:** Unlock level · Seed cost · Growth time (real time) · Waterings needed ·
Compost needed for top quality · Yield range (at ⭐) · Sell price per unit ·
Best season (+25% yield).

**Design intent:** early crops = minutes (session-friendly); late crops = hours
(come-back-tomorrow rhythm). Profit per plot-hour rises gently with level —
progression you can feel, never a cliff.

### Vegetables

| Crop | Lv | Seed | Growth | Water | Compost | Yield | Sell | Season |
|------|----|------|--------|-------|---------|-------|------|--------|
| Carrot 🥕 | 2 | 10 | 3 min | 1 | — | 4–6 | 6 | Spring |
| Tomato 🍅 | 3 | 15 | 5 min | 2 | 1 | 5–8 | 12 | Summer |
| Lettuce 🥬 | 4 | 12 | 4 min | 1 | — | 4–6 | 7 | Spring |
| Onion 🧅 | 5 | 14 | 6 min | 2 | — | 5–7 | 8 | — |
| Peas 🫛 | 6 | 16 | 6 min | 2 | 1 | 5–8 | 9 | Spring |
| Potato 🥔 | 7 | 12 | 8 min | 2 | 1 | 6–9 | 8 | Autumn |
| Cucumber 🥒 | 8 | 18 | 8 min | 2 | 1 | 5–8 | 10 | Summer |
| Pumpkin 🎃 | 9 | 25 | 15 min | 2 | 1 | 3–5 | 22 | Autumn |
| Spinach 🌱 | 10 | 20 | 10 min | 2 | 1 | 6–9 | 11 | Spring |
| Corn 🌽 | 13 | 30 | 20 min | 3 | 1 | 6–10 | 14 | Summer |
| Chili 🌶️ | 16 | 35 | 25 min | 3 | 2 | 5–8 | 18 | Summer |
| Bell pepper 🫑 | 18 | 40 | 30 min | 3 | 2 | 5–8 | 20 | Summer |

### Fruits (orchard trees marked 🌳 — planted once, regrow)

| Crop | Lv | Seed/Sapling | Growth/Regrow | Water | Compost | Yield | Sell | Season |
|------|----|--------------|---------------|-------|---------|-------|------|--------|
| Strawberry 🍓 | 12 | 45 | 30 min | 3 | 2 | 6–10 | 16 | Spring |
| Watermelon 🍉 | 14 | 60 | 45 min | 4 | 2 | 3–5 | 45 | Summer |
| Apple 🍎🌳 | 17 | 250 | 60 min / 45 min regrow | 3 | 2 | 8–12 | 18 | Autumn |
| Orange 🍊🌳 | 19 | 300 | 75 min / 60 min regrow | 3 | 2 | 8–12 | 20 | — |
| Grapes 🍇🌳 | 22 | 400 | 90 min / 75 min regrow | 4 | 2 | 8–12 | 24 | Autumn |
| Mango 🥭🌳 | 26 | 600 | 2 h / 90 min regrow | 4 | 3 | 6–10 | 40 | Summer |
| Banana 🍌🌳 | 28 | 650 | 2 h / 90 min regrow | 4 | 3 | 8–12 | 32 | — |
| Peach 🍑🌳 | 30 | 750 | 3 h / 2 h regrow | 4 | 3 | 6–10 | 45 | — |

### Special crops

| Crop | Lv | Seed | Growth | Water | Compost | Yield | Sell | Notes |
|------|----|------|--------|-------|---------|-------|------|-------|
| Sunflower 🌻 | 21 | 70 | 40 min | 2 | 1 | 4–6 | 30 | Near bees: +10% honey |
| Wheat 🌾 | 24 | 50 | 45 min | 2 | 2 | 10–15 | 8 | Bulk; mills to flour |
| Rice 🌾 | 27 | 90 | 90 min | 5 | 2 | 10–15 | 12 | Thirsty — teaches water use |
| Cotton ☁️ | 29 | 120 | 2 h | 3 | 2 | 6–10 | 28 | — |
| Cocoa 🍫 | 32 | 200 | 4 h | 4 | 3 | 5–8 | 60 | Long-haul crop |
| Coffee ☕ | 35 | 220 | 4 h | 4 | 3 | 5–8 | 65 | Long-haul crop |

**Growth stages (visual):** seed → sprout → growing → flowering/budding → ripe
(4–5 stages, §45). Ripe crops wait patiently — **nothing ever rots or dies.**

---

## 9. Crop Quality System (§8)

| Stars | Name | Requirement | Effect |
|-------|------|-------------|--------|
| ⭐ | Normal | Planted and harvested | Base yield, base price |
| ⭐⭐ | Good | All waterings on time | +20% yield, ×1.3 price |
| ⭐⭐⭐ | Excellent | On-time water + compost + nutrients | +40% yield, ×1.7 price |
| ⭐⭐⭐⭐ | Premium | ⭐⭐⭐ + organic fertilizer + harvest within the "perfect window" (L30+) | +60% yield, ×2.2 price |

**Rules:**
- Watering windows are generous (each watering has a ±50% time window); the UI
  shows droplet icons filling — no hidden timers, no anxiety.
- Missing a watering drops *potential* quality by one star. The game always shows
  *why* ("Missed one watering — still a tasty ⭐⭐ tomato!").
- Quality is per-plot, creating satisfying variety across the farm.

---

## 10. Watering Equipment (§9)

| Tool | Lv | Cost | Effect |
|------|----|------|--------|
| Watering can | Start | — | 1 plot per fill; refill at well (free, 3-sec walk) |
| Better watering can | 7 | 🪙 250 | 2 plots per fill |
| Water pump | 8 | 🪙 500 | Refill anywhere; waters 4 plots per fill |
| Sprinkler | 14 | 🪙 1,500 | Auto-waters 8 surrounding plots on a timer |
| Irrigation system | 20 | 🪙 5,000 | Waters a whole field section automatically |
| Automatic irrigation | 30 | 🪙 15,000 | Whole farm; player only handles quality extras |

**Rain** counts as a watering for every outdoor crop due — a gift, never a schedule-breaker.

---

## 11. Compost & Nutrients (§10)

| Supply | Lv | Cost | Effect | Lesson line |
|--------|----|------|--------|-------------|
| Compost | 4 | 🪙 8/use (or crafted free from food scraps bin!) | +1 potential quality star | "Compost is old food turned into plant food." |
| Fertilizer | 10 | 🪙 15 | −20% growth time | "A little help for hungry plants." |
| Nutrients | 12 | 🪙 20 | Required for ⭐⭐⭐ | "Plants need nutrients to grow healthy." |
| Organic fertilizer | 28 | 🪙 60 | Unlocks ⭐⭐⭐⭐ path | "Slow, natural, and the very best." |

**Compost bin (L6, free to build):** food scraps from harvests slowly become free
compost — the recycling loop (§35). This is the kindest economy in the game and
should feel like a small triumph every time it fills.

---

## 12. Weather System (§11)

Weather changes 1–3 times per in-game day (a day ≈ 12 real minutes; purely visual
clock, never pressures the player). Forecast shows the next change — always
know what's coming.

| Weather | Effect | Frequency |
|---------|--------|-----------|
| ☀️ Sunny | Normal growth | Common |
| ☁️ Cloudy | Normal growth, −10% water need | Common |
| 🌧️ Rain | Auto-waters all outdoor crops due; free! | Regular |
| 💨 Windy | Visual only (trees sway, petals) | Occasional |
| ⛈️ Storm | Unprotected crops lose 1 potential quality star — **unless** covered (greenhouse/shutters, cheap one-time build) | Rare; never in first 10 levels |
| ❄️ Snow | Visual + greenhouse synergy in winter | Winter only |

**Kid-safety rule:** weather *helps* more than it hurts. Storms are forecast a full
day ahead, protection is cheap and permanent, and the first storm comes with a
guided "let's protect the farm!" quest that rewards the raincoat outfit.

---

## 13. Seasons (§12)

Each season lasts **7 real days**. Season wheel on the dashboard; season-change
festival morning is a small celebration.

| Season | Bonus crops (+25% yield) | Special |
|--------|--------------------------|---------|
| 🌸 Spring | Carrot, lettuce, peas, spinach, strawberry | Planting festival; flowers bloom |
| ☀️ Summer | Tomato, cucumber, corn, chili, watermelon, mango | Long sunny days; bees busiest |
| 🍁 Autumn | Potato, pumpkin, apple, grapes, wheat | Harvest Festival event; falling leaves |
| ❄️ Winter | Greenhouse crops (any, +25% inside) | Snow; greenhouse farming shines; Snow Festival |

Out-of-season crops still grow at 75% yield — experimentation is rewarded, never punished.

---

## 14. Equipment Progression (§13)

Equipment saves *time and taps*, never gates fun. Each tier should produce an
audible "ahh, that's better" moment.

| Tier | Lv | Item | Cost | Effect |
|------|----|------|------|--------|
| 0 | Start | Hand tools, bucket, watering can, basket | — | The basics |
| 1 | 6–9 | Wheelbarrow | 🪙 200 | Carry +50% harvest per trip (fewer walks) |
| 1 | 6–9 | Better shovel | 🪙 150 | Dig 2 plots per tap |
| 1 | 6–9 | Better watering can | 🪙 250 | 2 plots per fill |
| 2 | 15 | **Small tractor** 🚜 | 🪙 2,500 | Auto-digs/prepares a field; the §45 hero animation |
| 2 | 15 | Seeder | 🪙 1,200 | Plants 4 plots at once |
| 2 | 16 | Sprayer | 🪙 900 | Applies nutrients to 6 plots at once |
| 3 | 23 | Large tractor | 🪙 8,000 | Prepares + plants whole field |
| 3 | 24 | Automated planter | 🪙 6,000 | One tap plants a full section |
| 3 | 25 | Harvesting machine | 🪙 10,000 | One tap harvests a full section |
| 4 | 28 | Advanced tractor | 🪙 20,000 | Field prep + plant + water in one pass |
| 4 | 30 | Automatic harvester | 🪙 25,000 | Timed auto-harvest of ripe sections |
| 5 | 35+ | Premium machinery set | 🪙 50,000+ | Near-full automation; the "farm runs itself" fantasy |

**Design note:** automation never plays *for* the player — it removes chores so the
player spends time on decisions (what to plant, what to process, which order to fill).

---

## 15. Animals (§14)

**Core loop per animal:** buy → house it (needs space in coop/barn/pasture) →
care (feed daily-ish, brush/pet for happiness) → collect product on timer →
product sells raw or goes to processing.

**Kindness rules:** animals never get sick, never die, never run away. Low care =
slower production, never suffering. The animal doctor NPC gives tips, not bad news.

| Animal | Lv | Cost | Housing | Product | Cycle | Product value | Notes |
|--------|----|------|---------|---------|-------|---------------|-------|
| 🐔 Chicken | 10 | 🪙 150 | Coop (4 slots, L12 meadow) | Egg 🥚 | 1 per 10 min, stores 4 | 15 | The starter; eggs → orders early |
| 🐄 Cow | 18 | 🪙 800 | Barn (L14) | Milk 🥛 | 1 per 30 min, stores 3 | 40 | Milk → cheese |
| 🐑 Sheep | 20 | 🪙 700 | Barn/pasture | Wool 🧶 | 1 per 45 min, stores 3 | 60 | Wool → (V4 textile chain) |
| 🐐 Goat | 22 | 🪙 900 | Pasture | Milk 🥛+ | 1 per 30 min | 45 | Slightly better milk |
| 🦆 Duck | 24 | 🪙 500 | Pond (L18) | Egg 🥚+ / feather | 1 per 20 min | 25 | Pond synergy with fishing |
| 🐎 Horse | 24 | 🪙 3,000 | Stable (L23) | — (rides! + speed boost) | — | — | Emotional/prestige; speeds travel |
| 🐝 Bees | 26 | 🪙 1,200 | Hive (orchard) | Honey 🍯 | 1 per 60 min, stores 4 | 70 | +10% near sunflowers |

**Pigs** are held for regional review — include only if the target market is
comfortable; the slot is designed to be swappable (same mechanics, different animal).

**Feed:** feed bag 🪙 10 feeds one animal for ~2 hours of production. A silo
(L20, 🪙 2,000) auto-feeds — another "ahh, better" moment. Running out of feed
pauses production; the animal just naps.

**Happiness:** petting/brushing (one tap, cute animation + hearts) boosts production
speed +10% for an hour. Pure warmth mechanic, zero downside.

---

## 16. Pets (§15)

Separate from farm animals: **no economic function**, pure companionship and collection.

| Pet | Unlock | Behaviors |
|-----|--------|-----------|
| 🐶 Dog | L8 (gift from Grandpa — story beat) | Follows player, barks at harvest, sleeps by house |
| 🐱 Cat | L14 | Naps in sunbeams, chases butterflies (never catches) |
| 🐰 Rabbit | L22 (event/quest) | Hops around garden, nibbles clover |
| 🐦 Bird | L26 | Perches on shoulder, sings at sunrise |

**Accessories** (earn via achievements/events): scarves, hats, bows. Pets react to
player actions (happy bounce when you level up, sleepy at night). Naming your pet
is mandatory — it's the law of cozy games.

---

## 17. House Building (§16)

| Level | Lv | Cost | What changes |
|-------|----|------|--------------|
| 1 — Small Farm House | Start | — | 1 room, bed, table |
| 2 — Larger House | 12 | 🪙 3,000 | 2 rooms, porch, flower boxes |
| 3 — Farmhouse | 22 | 🪙 12,000 | 3 rooms, kitchen, fireplace, garden |
| 4 — Dream Farmhouse 🏰 | 35 | 🪙 40,000 | Wraparound porch, tower nook, full interior |

**Customization (all in-game earnable):** wall colors/patterns, roof styles,
doors, windows, furniture sets, beds, kitchen, rugs, wall art (including framed
"photos" of your own harvests — generated from game art, a lovely touch).

---

## 18. Farm Decorations (§17)

Decorations are the creative sandbox — especially beloved by the younger audience.
All buyable with coins; rare ones from events/achievements.

**Set 1 (L8+):** fences, flower beds, trees, lamps, paths, signs, benches, well upgrade.
**Set 2 (L20+):** windmill (spins!), fountain, picnic area, bridges, birdhouse
(attracts birds — §34 synergy), scarecrow (cute, waves).
**Set 3 (L30+):** playground (swing, slide), garden gnomes, pond decorations,
string lights (glow at night — §46 synergy), seasonal decor.

**Placement:** free drag-and-drop on a grid; rotate; store in a decor inventory.
No "wrong" placement — it's *your* farm.

---

## 19. Market & Economy (§18, §19)

### V1–V2: Fixed prices (simple, safe)
The market stall lists every unlocked crop/product at its §8 sell price.
Sell any quantity in two taps. The shopkeeper reacts ("What a harvest!").

### V3+: Dynamic prices (teen depth, kid-safe)
- Each item has a **base price** (§8) and a live price drifting **±30%**.
- Drift follows **visible rules**, shown in-game: season demand, weather,
  festival demand, and a simple supply echo (sell a lot → price dips slightly, recovers).
- A **3-day forecast** with trend arrows is always visible: "🍅 Tomato rising —
  Harvest Festival demand!" The player *learns the system*, never guesses blindly.
- **Never:** randomness without explanation, real-money influence, or pressure
  timers ("sell in 10 seconds!").

### Economy safety rules
- Storage (§20) means never *forced* to sell low — waiting is always a valid strategy.
- The spreadsheet model must show: no infinite-money loop, no item that trivializes
  all others, coin sinks (land, equipment, decorations) pacing income at every tier.

---

## 20. Storage (§20)

| Building | Lv | Cost | Capacity |
|----------|----|------|----------|
| Storage shed | Start | — | 50 slots (stacks) |
| Barn L1 | 14 | 🪙 8,000 (land 7) | 200 slots |
| Barn L2 | 24 | 🪙 12,000 | 500 slots |
| Silo | 20 | 🪙 2,000 | Auto-feed storage (feed only) |

Stores: crops, seeds, fertilizer, animal products, materials. Clean grid UI with
category tabs; "what can I make with this?" hints linking to processing (§21).

---

## 21. Processing (§21)

Turn raw goods into artisan products for ~1.5–2× value. The value-add must always
be *explainable* to a kid: "You did extra work, you earn extra coins."

**Manual workstation (L20, 🪙 1,500):** one recipe at a time, short timers.

| Recipe | Input | Time | Output | Sells |
|--------|-------|------|--------|-------|
| Strawberry jam 🍓 | 6 strawberries | 10 min | 2 jam jars | 55 each |
| Cheese 🧀 | 4 milk | 20 min | 2 cheese | 110 each |
| Flour 🌾 | 8 wheat | 15 min | 3 flour | 30 each |

**Factory (L28, land 8, 🪙 25,000 to build):** 3 queues, longer chains.

| Recipe | Input | Time | Output | Sells |
|--------|-------|------|--------|-------|
| Tomato sauce 🍅 | 8 tomatoes | 20 min | 3 sauce | 90 each |
| Apple juice 🍎 | 6 apples | 25 min | 3 juice | 75 each |
| Honey jar 🍯 | 4 honey | 30 min | 2 jars | 180 each |

**V4 advanced chains:** flour + eggs → (order-only) baked goods; fruit + honey →
gift baskets. Multi-step, high reward, entirely optional.

---

## 22. Delivery / Order System (§22)

The objective layer beyond free farming.

**Order board** (village market, L20; 3 slots, refreshes every 4 hours or on completion):

| Order type | Example | Reward rule |
|------------|---------|-------------|
| Market order | 20 tomatoes | Coins = 1.5× market value + 50 XP |
| Restaurant order (L24+) | 50 tomatoes, 20 onions, 10 peppers | Coins = 1.6× + 150 XP |
| Grand order (L30+) | Mixed incl. processed goods | Coins = 1.8× + 300 XP + gem (first completion) |

**Rules:** orders only ever ask for things the player *can* produce at their level;
a "not yet" order never appears. Declining is free and instant — no punishment.
Completing all 3 slots grants a bonus chest (coins + XP + occasional decor).

---

## 23. Daily Missions (§23)

5 missions per day, drawn from a pool scaled to unlocked systems, e.g.:

- Harvest 10 carrots · Water 5 crops · Sell 20 vegetables · Feed 3 animals ·
  Complete 1 market order · Catch 2 fish · Brush 1 animal · Earn 500 coins

**Rewards:** 🪙 coins (scaled to level) + ⭐ XP (30 each) + every 5th day a 🎁 item
(seeds, decor, or feed). Streaks are *celebrated, never punished* — missing a day
just starts a new streak. No FOMO design.

---

## 24. Achievements (§24)

~50 achievements across categories; each grants coins and most grant 💎 gems
(the earnable premium currency). A starter set:

| Achievement | Reward |
|-------------|--------|
| First Harvest 🌱 | 🪙 100 |
| 100 Crops Sold | 🪙 500 + 💎 5 |
| First Tractor 🚜 | 🪙 800 + 💎 10 |
| First Cow 🐄 | 🪙 600 + 💎 10 |
| 10,000 Coins | 💎 15 |
| Fruit Collector (all fruits) | 💎 20 |
| Animal Lover (all animals) | 💎 20 |
| Mega Farm (all land) | 💎 30 |
| Master Farmer (L50) 🏆 | 💎 50 + exclusive statue decor |

Full list in the GDD appendix spreadsheet; categories: Farming, Animals, Business,
Creative, Exploration, Events, Mastery.

---

## 25. Player XP & Levels (§25, §49)

**Titles:** 1 Beginner Farmer → 5 Garden Farmer → 10 Skilled Farmer →
20 Farm Expert → 35 Big Farm Owner → 50 **Master Farmer** 🌎.

**XP sources:** plant 3 · water 2 · harvest 8 (+2 per quality star) · sell 1 per
10 coins · collect animal product 6 · care action 5 · mission 30 · order 50–300 ·
unlock land 100 · achievement 50–500.

**Level curve (v1):** XP to go from level *n* to *n+1* = `round(100 × n^1.5)`.
Pacing target: L10 in ~2 weeks of casual play, L30 in ~3 months, L50 in 6–9 months.
(Curve to be validated against playtest session data in Phase 0 simulation.)

**Unlock table (extends the concept's 1–30 to the full 50):**

| Level | Unlock | Level | Unlock |
|-------|--------|-------|--------|
| 1 | Farm, tutorial | 26 | Greenhouse, mango, bees |
| 2 | Carrots | 27 | Rice, premium crops begin |
| 3 | Tomatoes | 28 | Factory land, banana, adv. tractor |
| 4 | Compost, lettuce | 29 | Cotton, large orders |
| 5 | Market, onion | 30 | Peach, auto-irrigation, ⭐⭐⭐⭐ |
| 6 | Storage, peas, compost bin | 31–34 | Cocoa, decor set 3, advanced chains |
| 7 | Potatoes, better tools | 35 | Coffee, premium machinery, Dream House |
| 8 | Water system, cucumber, dog 🐶 | 36–40 | Wildlife full, world complete |
| 9 | Farm expansion (land 2), pumpkin | 41–45 | Mastery achievements, rating chase |
| 10 | Chicken 🐔, spinach | 46–49 | Endgame polish, seasonal mastery |
| 11 | Eggs (products) | 50 | **Master Farmer** 🏆 |
| 12 | Animal meadow, strawberry, house L2 | | |
| 13 | Corn | | |
| 14 | Barn plot, sprinkler, watermelon, cat 🐱 | | |
| 15 | Tractor 🚜, seeder | | |
| 16 | Orchard, chili | | |
| 17 | Apples | | |
| 18 | Cow 🐄, milk, bell pepper, pond | | |
| 19 | Orange | | |
| 20 | Processing (workstation), sheep, irrigation | | |
| 21 | Sunflower, cheese | | |
| 22 | Delivery orders, grapes, goat, house L3, rabbit 🐰 | | |
| 23 | Big field land, large tractor | | |
| 24 | Horse 🐎, duck, wheat, restaurant orders, barn L2 | | |
| 25 | Advanced equipment, automated planter | | |

---

## 26. Currencies (§26)

- **🪙 Coins** — earned from selling, orders, missions, achievements. Spent on
  seeds, animals, equipment, land, buildings, decorations. The workhorse.
- **💎 Gems (Farm Tokens)** — **earnable only**: achievements (5–50), story chapters
  (20), events (10–30), grand orders (first completions). **Never required** for
  core progression. Spent on: exclusive decorations, pet accessories, mission
  rerolls, extra order-board slot. If IAP ever touches gems, it is parent-gated
  and capped — design default is *no gem IAP at all*.

---

## 27. Collection Book (§27)

The "gotta collect them all" system — discovery-based, never random or paid:

- **Crops** (34): discovered on first harvest. Shows art, season, best price achieved.
- **Animals** (8): discovered on first purchase. Shows product and care tips.
- **Fish** (12): discovered on first catch.
- **Equipment** (18): discovered on first buy.
- **Decor** (rare sets): discovered on placement.

Completion rewards per category (gems + exclusive decor). The book itself is
browsable from level 1 — kids love seeing what's *coming*.

---

## 28. World Map (§28)

```
              🏔️ Mountain (L38, scenic + forage)
                   |
🌲 Forest (L30, forage, wood) — 🏘️ Village — 🏪 Market (orders)
                   |
              🚜 YOUR FARM
                   |
          🌊 River (L18, fishing + ducks)
```

Areas unlock by level with a story beat each. Foraging (forest/mountain) yields
free compost materials and rare decor — a gentle exploration reward, never required.

---

## 29. Village NPCs (§29)

| NPC | Role | Gives |
|-----|------|-------|
| 👨‍🌾 Grandpa Hamza | Mentor | Tutorial, story chapters, wisdom lines |
| 👩‍🌾 Seed Seller Amara | Shop | Seeds, saplings, gardening tips |
| 🧑‍🔧 Equipment Dealer Rafiq | Shop | Tools, machines; "try the tractor!" quests |
| 👩‍🍳 Restaurant Owner Lina | Orders | Restaurant orders, recipes, cooking quests |
| 🧑‍🏫 Farming Teacher Ms. Sana | Learning | Educational quests ("compost quest"), fun facts |
| 👨‍⚕️ Animal Doctor Dr. Tariq | Care | Animal tips, happiness quests |
| 🏪 Market Owner Uncle Bashir | Market | Market tutorial, price-forecast explainer |

NPCs have simple day schedules (shop open/closed signs — teaches planning, never
locks progress cruelly) and ambient lines that react to your farm
("Your tomatoes look great today!").

---

## 30. Story Mode (§30)

*"You inherit your grandfather's small farm… turn it into the best farm in the valley."*

| Chapter | Title | Core objective | Reward |
|---------|-------|----------------|--------|
| 1 | Wake Up, Farm! | Tutorial + first market sale | 🪙 300, dog 🐶 |
| 2 | Green Fields | Expand to land 2, grow 5 crop types | 🪙 800 |
| 3 | Feathered Friends | Build coop, raise 4 chickens | 💎 20, coop decor |
| 4 | Horsepower | Buy the small tractor | 💎 20, tractor outfit |
| 5 | Growing Big | Own 4 lands, reach L20 | 🪙 2,000 |
| 6 | The Mill | Build the factory, ship first sauce | 💎 30 |
| 7 | Master of the Valley | 5-star farm rating, L50 | 🏆 statue + 💎 50 |

Chapters are *guides*, not gates — a sandbox player ignoring the story loses
nothing but the chapter rewards.

---

## 31. NPC Relationships (§31)

Friendship points per NPC, earned by completing their quests and gifting
(they love specific farm goods — shown in UI, no guessing). Thresholds unlock:
new dialogue, small gifts, shop discounts (max 10%), and the "Valley Friend"
achievement. **Strictly friendship/community — no romance, ever.**

---

## 32. Mini-Games (§32)

Short (30–60 sec), optional, always skippable — variety without leaving the farm:

| Mini-game | Unlock | How it plays | Reward |
|-----------|--------|--------------|--------|
| Harvest Rush | L6 | Tap ripe crops as they glow; combo for speed | +5% yield that harvest |
| Planting Beat | L12 | Tap in rhythm to plant a row perfectly | +1 quality step chance |
| Market Packing | L20 | Drag goods into order boxes correctly | Order bonus +10% coins |
| Animal Care | L10 | Brush/soap/feed in the right order | Happiness boost ×2 duration |
| Tractor Run | L15 | Steer the tractor between rows (no crashing — cones just wobble) | Field prepped 25% faster |

**Rule:** mini-games grant *bonuses*, never gate progress. A player who skips them
all still reaches Master Farmer.

---

## 33. Fishing (§33)

Pond (farm, L18) + river (world map). Friendly presentation: fish are "collected,"
shown in the collection book, and released-or-kept (player's choice — kept fish
sell or cook in V4).

- **12 species** across 3 rarities, tied to season/weather/time of day
  (e.g. "Moonfin — night, rain, autumn").
- **Rod tiers:** stick (start) → better rod (L20) → pro rod (L28): faster bites,
  deeper spots.
- **Fishing challenges:** weekly "catch 3 Sunperch" from the order board.

---

## 34. Nature & Wildlife (§34)

Ambient life that makes the farm feel magical — **purely visual/delight**,
zero mechanics pressure:

| Creature | Appears when |
|----------|--------------|
| 🐦 Birds | Trees planted; morning |
| 🦋 Butterflies | Flowers planted; spring/summer day |
| 🐝 Bees | Hives or sunflowers nearby |
| 🐿️ Squirrels | Forest-adjacent decor, autumn |
| 🐸 Frogs | Pond + rain |
| ✨ Fireflies | Night + string lights/flowers |

Birdhouses and flower beds *attract* wildlife — decorations with a living payoff.

---

## 35. Educational Layer (§35)

Learning objectives mapped to mechanics (surfaced in Parent Mode reports, §36):

| Mechanic | Learning objective | In-game delivery |
|----------|-------------------|------------------|
| Soil prep + compost | Soil health, decomposition, recycling | Ms. Sana's compost quest; compost bin loop |
| Watering/weather | Water cycle, conservation | Rain auto-waters; rice teaches water cost |
| Seasons | Crop seasonality, planning | Season wheel + bonuses; winter greenhouse |
| Market/prices | Profit = revenue − cost; supply & demand | Price forecast + "How much did you earn?" recap after sales |
| Orders/processing | Value-added production, business basics | Factory chains; Lina's restaurant quests |
| Animals | Care, responsibility, product origins | Care loop; "milk comes from cows" collection entries |
| Math | Counting, addition, budgeting | Harvest counts, coin recaps, land saving goals |

**Rule:** one concept per quest, one sentence per tip, always inside play.
Never quizzes, never grades, never "school mode."

---

## 36. Parent Mode (§36)

Behind a parental gate (simple math question / device-auth — never just "are you
a parent? tap yes"):

- **Dashboard:** play time (today/week), current level, farm rating, recent achievements
- **Learning report:** "This week: composting 🌱, profit math 🧮, seasons 🍂" —
  generated from actual mechanics used
- **Purchases:** full history; spending controls (disable IAP entirely)
- **Settings:** session reminders ("remind after 45 min" — gentle, kid-respectful),
  sound/music toggles, reset progress (with double confirm)

Positioning: *"A game you can feel good about"* — this is a marketing pillar,
not a checkbox.

---

## 37. Child Safety (§37) — architecture requirements

Designed in Phase 0, audited in Phase 5:

- **No:** open chat, DMs, friend requests with messaging, public UGC, photo sharing,
  location, contacts access, behavioral advertising, or third-party trackers.
- **Social features (if ever):** farm *visits* are view-only + predefined reactions
  ("Nice farm! 🌱"); leaderboards are opt-in, anonymous-able, non-social weekly
  challenges only.
- **Data:** minimal (save file + privacy-safe analytics, no PII); COPPA/GDPR-K
  aligned; clear privacy policy written in plain language.
- **Purchases:** parent gate on every IAP; no consumable traps; receipts in Parent Mode.

---

## 38. Multiplayer (§38) — deferred by design

**Not in V1–V4.** Architecture notes for later: view-only farm visits, predefined
reactions, no real-time sync, no communication channels. Revisit only after the
safety audit framework exists and with a dedicated safety review.

---

## 39. Leaderboards (§39) — optional, gated

*Only if* the safety review passes: weekly opt-in challenges
("Most carrots harvested 🌽"), anonymous display names, no direct competition
mechanics, no rewards that affect progression (cosmetic badges only).
Default: **off** for under-13 profiles.

---

## 40. Events (§40)

**Event template** (reused monthly): theme → themed crop/decor/activity →
3–5 event quests → temporary cosmetics + gems → closing celebration.
**Never requires purchases;** latecomers get a "catch-up" quest path.

| Event | Season | Focus |
|-------|--------|-------|
| Spring Garden 🌸 | Spring | Flowers, butterflies, decor |
| Harvest Festival 🌾 | Autumn | Harvest challenges, price bonuses |
| Pumpkin Festival 🎃 | Autumn | Pumpkin growing contest (vs. own best, not others) |
| Animal Day 🐄 | Summer | Animal care challenges |
| Snow Festival ❄️ | Winter | Greenhouse growing, winter decor |

---

## 41. Farm Rating (§41)

0–5 stars, computed from: crop diversity + animal count/happiness + buildings +
decorations + production output + achievements + cleanliness (debris cleared).
Shown on the dashboard and the farm's entrance sign. The climb from 4→5 stars is
the endgame grind — long, visible, satisfying.

---

## 42. Monetization (§42)

**Model: free core + one-time unlock + optional content packs. No ads. No energy.
No loot boxes.**

| SKU (placeholder prices — TBD in Phase 0) | Content |
|-------------------------------------------|---------|
| Free | Full core game: farming, animals, story ch. 1–3, village, seasons |
| Unlock Full Farm (~$9.99) | All lands, crops, animals, factory, chapters 4–7, full customization — **the whole game, forever** |
| 🌴 Tropical Farm Pack (~$3.99) | Tropical crops/decor/pet skin, island farm theme |
| 🏔️ Mountain Farm Pack (~$3.99) | Hardy crops, mountain decor, goat variants |
| 🐎 Horse Ranch Pack (~$3.99) | Horse breeds, ranch decor, riding trails |
| 🌸 Flower Garden Pack (~$3.99) | Flower crops, garden decor, butterfly sanctuary |

All IAP via Apple/Google systems, parent-gated, with a "restore purchases" that
actually works. Pricing validated against kids-category norms in Phase 0.

---

## 43. Offline Mode (§43)

**Always offline-capable:** plant, water, harvest, animals, building, customization,
mini-games, fishing, story — the entire core game. **Online only for:** cloud
backup (opt-in), events, updates, purchases, parent features, leaderboards.
Timers run on local clock with anti-cheat sanity bounds (absurd clock jumps are
clamped, not punished — kids share devices).

---

## 44. Visual Style (§44)

**Cute stylized cartoon — 2.5D/3D look to be locked in Phase 0** (tech decision).
Either way, the bible mandates:

- Rounded, chunky shapes; bright-but-not-neon palette; soft shadows
- Expressive characters (big eyes, squash-and-stretch) and friendly animals
- Environmental animation everywhere: swaying crops, drifting clouds, butterflies
- Readable at a glance: ripe crops *glow*, thirsty crops show droplets, ready
  products bounce — the farm is its own UI
- Distinct seasonal palettes (spring pastels → autumn golds → winter blues)

---

## 45. Animation List (§45)

The polish checklist — each is a small moment of delight, budgeted in the art plan:

- Plant: seed drops → soil puffs → sprout pops 🌱
- Growth: stage transitions with a tiny scale-bounce
- Harvest: character pulls/stretches the vegetable out; basket toss arc
- Tractor: drives rows leaving tilled soil; little dust puffs
- Animals: walk-to-player when called; hearts on petting; sleep "Z"s at night
- Weather: clouds drift; rain streaks; storm clouds gather (forecast readable!)
- Day/night: full sun → sunset → night (lamps/fireflies on) → sunrise cycle
- Market: truck arrives, player loads crates, truck drives off with a honk
- Water: can pour arc, sprinkler spin, rain ripples in pond

---

## 46. Day/Night Cycle (§46)

A 12-real-minute day: 🌅 sunrise → ☀️ day → 🌇 sunset → 🌙 night. **Cosmetic +
gentle mechanics only:** animals sleep (collect in the morning — a lovely return
hook), house/farm lights turn on, fireflies appear, certain fish only bite at
night. Nothing punishes playing at any hour; night is cozy, not scary.

---

## 47. UI Screens & Game Flow

### Screen list (Phase 0 wireframes every Phase 1 screen)

1. Splash → 2. Title (Continue / New) → 3. Character creator → 4. Farm (main view —
   the game lives here) → 5. Plot sheet (context actions: dig/plant/water/harvest) →
6. Seed shop → 7. Market stall (sell + price forecast) → 8. Inventory →
9. Storage/barn → 10. Animal area → 11. Equipment shop → 12. Land map (expand) →
13. House customize → 14. Decor shop + placement mode → 15. Collection book →
16. Dashboard (level, coins, goals, farm value) → 17. Missions → 18. Achievements →
19. Order board → 20. Factory → 21. Fishing spot → 22. World map → 23. Village →
24. NPC dialogue → 25. Story journal → 26. Pets → 27. Compost bin → 28. Settings →
29. Parent gate → 30. Parent dashboard → 31. Shop (IAP) → 32. Events →
33. Pause menu.

**UI principles:** max 3 taps to any core action; big touch targets (kids);
every number has an icon; no nested menus deeper than 2 levels.

### Key flows (text)

**FTUE (first session):** Title → creator → farm pan → guided plant/water →
tour → guided harvest/sell → guided seed buy → free play + 3 pinned goals (see §4).

**Core session loop:** Open → dashboard glance (what's ready?) → care round
(water/harvest/collect) → route goods (sell/store/process/order) → plant next
round → check missions/orders → decorate/tinker → close (night falls, animals sleep).

**Order flow:** Board → pick order → auto-checklist of needed goods (with "grow"
shortcuts to the right seeds) → deliver → fanfare → slot refreshes.

---

## 48. Anti-Boredom Design (§48)

Enforced at every phase exit: the farm must always present **≥3 concurrent
meaningful goals** from different systems (a timer about to finish + an order
to fill + a mission + a land saving goal + an event). Systems are *interleaved
by design*: crops feed processing feeds orders; animals feed processing;
decorations feed wildlife; seasons rotate crop strategy. No system is ever
"done" while others still grow.

---

## 49. System Architecture Map (§50)

```
                        FARM LIFE
                            │
        ┌───────────┬───────┴───────┬───────────┐
        │           │               │           │
     FARMING     ANIMALS         PLAYER       WORLD
        │           │               │           │
   Crops(§8)    Chicken→Horse   Level/XP(§25) Village(§29)
   Soil(§7)     Care loop(§15)  Character(§5) NPCs(§29-31)
   Water(§10)   Products        Inventory     Weather(§12)
   Quality(§9)  Pets(§16)       Collection   Seasons(§13)
   Seasons                      Achieve(§24) Day/Night(§46)
        │                                       │
        ├──── ECONOMY ───────────┐              │
        │                        │              │
      Market(§19)            Processing(§21)    │
      Orders(§22)            Factory           │
      Prices(dynamic)        Storage(§20)       │
        │                        │              │
        ├──── PROGRESSION ───────┘              │
        │                                      │
      Land(§6)  Equipment(§14)  House(§17)     │
      Barn      Tractor         Decor(§18)     │
        │                                      │
        └──── CREATIVE & LIVE ─────────────────┘
                Decor · Pets · Rating(§41)
                Events(§40) · Parent(§36)
```

---

## 50. Scope Split — GDD vs Phased Plan

This GDD describes the **complete game**. What gets built when is owned by
`phased-development-plan.md`:

- **Phase 1 (V1):** §§1–10 core, chickens, dog, 8 crops, fixed prices, L1–10
- **Phase 2 (V2):** animals core, tractor, weather/seasons, fruits, manual processing
- **Phase 3 (V3):** factory, orders, dynamic prices, fishing, events framework
- **Phase 4 (V4):** dream house, wildlife, endgame, gems, rating chase, L31–50
- **Phase 5 (V5+):** parent mode, safety audit, monetization, live ops

---

## Appendix A — Balancing Methodology (how the v1 numbers were chosen)

1. **Profit ladder:** each crop tier earns ~1.3–1.6× the coins-per-plot-hour of the
   previous tier at equal quality — progression feels rewarding, never mandatory.
2. **Session math:** a 5-minute session should complete ≥1 full crop cycle at the
   player's tier; a 30-minute session should make visible progress toward the next
   unlock (land saving goals are sized to 3–10 sessions).
3. **Quality incentive:** ⭐⭐⭐ pays ~2.4× ⭐ per plot-hour (yield + price), making
   *care* the best strategy — the game's thesis in numbers.
4. **Sink pacing:** total coin sinks per 10 levels ≈ 1.2× total earnable coins in
   that band for an average player — always something to save for, never a wall.
5. **XP curve:** `100 × n^1.5` per level, retunable after playtest session data.
6. **Simulation gate (Phase 0):** the spreadsheet model must be run with three
   player archetypes (casual kid, optimizer teen, returning-after-a-week) and show
   no dead ends, no infinite loops, and no required IAP before any tuning is final.

## Appendix B — Open Design Questions (for Phase 0)

1. Tech stack → then: 2.5D presentation details, performance budgets.
2. Exact season length (7 days vs 14) after session-data modeling.
3. Pig inclusion — regional market review.
4. Gem IAP: default remains **no**; revisit only with parent-research data.
5. Launch languages (English + ?).
6. Price points for Unlock Full Farm and content packs.

---

*End of GDD v1.0 — the stable part is the systems; the numbers are tuning knobs.
Next: Phase 0 simulation + wireframes, then the tech decision.*
