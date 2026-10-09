# 🗺️ FarmQuest — Phase-by-Phase Implementation Plan

Spec §54 defines 6 implementation phases. **Phase 1 (vertical slice) is DONE**
and playable. This document is the build order from here — each phase ends with
a playable milestone and a test gate.

**Golden rule (spec §54):** do not start a phase's systems until the previous
phase is playable and fun. Each phase below lists: goal → systems → new data →
done criteria.

---

## ✅ Phase 1 — Vertical Slice (DONE)

**Goal:** the core loop is fun end-to-end.

**Shipped:** player, farm grid, ortho camera, input abstraction, dig/prepare/plant/
water/growth/harvest, 8 crops (data-driven), inventory, market (fixed prices),
coins, XP/levels 1–30, versioned save/load with offline progression, HUD + plot/
market/seed/inventory UI, rewarded-ads service (stub), 20+ unit tests.

**Gate:** new player can plant → harvest → sell → replant unaided; save survives
app kill; crops grow while closed.

---

## ✅ Phase 2 — Depth of the Core (DONE)

**Goal:** reasons to return daily; guidance for new players; a village with a voice.

### Systems
- [x] **Missions** — daily mission pool, progress tracking via events, claimable
      rewards, day-rollover regeneration (`MissionData` + `MissionService`)
- [x] **Achievements** — one-time milestones (harvests, coins, sales, level, land),
      trophy toast + rewards, persisted unlocks (`AchievementData` + `AchievementService`)
- [x] **NPCs** — data-driven villagers, tap-to-talk dialogue UI (`NpcData` +
      `NpcService` + `NpcView` + `DialoguePanelController`); 8 villagers generated
- [x] **Story mode** — 10 chapters (§30) with objectives, rewards, chapter fanfare
      (`StoryChapterData` + `StoryService`)
- [x] **Tutorial director** — scripted FTUE hints reacting to real player actions
      (`TutorialService` + `TutorialPanelController`), persisted step
- [x] **More crops** — strawberry, watermelon, apple, orange, wheat, sunflower
      (14 crops total; pure data, systems unchanged)

### Already done in Phase 1
Compost, fertilizer, crop quality, land expansion (regions unlock).

### Gate
Day-2 retention hook exists (daily missions reset); tutorial completes without
confusion; story chapter 1–3 completable; all new tests pass.

---

## ✅ Phase 3 — Animals & Home (DONE)

**Goal:** the farm feels alive; a second progression path (animals) + creative
expression (house/decor).

### Systems
- [x] `AnimalData` (SO) + `AnimalInstance` + `AnimalService` — hunger/happiness/
      production/ready/sleeping states; feed/pet/collect (mirrors the crop
      pattern: DATA / LOGIC / VIEW / SAVE); offline production via timestamps
- [x] Animal products as `ItemData` (egg, milk, wool, goat milk, duck egg, honey)
      → inventory + market (sell filter now covers all sellable items)
- [x] `PetData` + `PetService` + `PetView` — dog (Grandpa's gift) + cat (L14),
      follow/idle/sleep behaviors, naming, happy-bounce interaction
- [x] Barn + house as `BuildingData` + `BuildingService`; barn upgrades expand
      housing capacity; `HouseView` swaps prefabs per level (spec §60)
- [x] Decorations: `DecorData` + `DecorService` (buy → own → place on 1m grid),
      8 decor items, `DecorPanelController` + `DecorViewManager`

### Data
Chicken, cow, sheep, goat, duck, horse, bees; dog + cat pets; barn + 4-level house.

### Gate
Animal loop (feed → product → sell) is fun standalone; buying a cow visibly
changes the farm.

---

## ✅ Phase 4 — Machines (DONE)

**Goal:** power fantasy — the player feels their growing scale.

### Systems
- [x] Tractor as drivable arcade vehicle (tap-to-move + WASD, §23): work applied
      to tiles under the tractor in the mode's radius; camera follows; player
      controls pause while driving (`TractorController` + `TractorView`)
- [x] Area-work logic as pure testable C# (`TractorWorkService`): plow chains
      clear→dig→prepare in one pass; plant/water/fertilize/harvest reuse
      `FarmService` primitives (XP/inventory/events stay consistent)
- [x] Equipment tiers as data (`EquipmentData` SO, 11 items): 3 tractors,
      seeder/sprayer/harvester unlock modes, auto-planter/harvester widen radius,
      3 irrigation systems; `EquipmentService` with mode gating
- [x] Orchard (permanent trees — `TreeInstance` with regrow timers): 4 trees,
      sapling shop, tap-to-harvest, fruit uses crop yield tables
- [x] Advanced irrigation (`IrrigationService`): best owned system auto-waters
      thirsty crops on its timer through `FarmService.TryWater`
- [x] Nutrient application (`FarmService.TryApplyNutrient` + soil prep flags +
      PlotPanel button); sprayer/tractor Fertilize mode

### Gate
Tractor milestone feels earned; equipping it visibly changes field work speed.

---

## ✅ Phase 5 — Business & World (DONE)

**Goal:** the player runs a farm business in a living world.

### Systems
- [x] Processing (`RecipeData` SO + `ProductionService` with per-building queues,
      real-time batches, cancel-with-refund, offline-safe): 8 buildings, 10 recipes,
      9 product items
- [x] Delivery orders (`OrderService` + order board UI): generation every 4h,
      24h expiry, premium orders at 2.2×, fulfill from inventory
- [x] Village: NPC shops with open hours (`ShopData` + `VillageService`), village
      bank with 2% daily interest (`BankService`), weekly Market Day (+20% sell prices)
- [x] Fishing (`FishingController` timing minigame + 4 species as sellable items)
- [x] Wildlife ambience (`WildlifeService`: butterflies, birds, rare hedgehog gift)
- [x] Seasons (`SeasonService`: 4 × 30 days; out-of-season crops grow slower, never blocked)
- [x] Weather (`WeatherService`: sunny/cloudy/rainy/storm; rain auto-waters crops;
      storms are gentle ambience only)
- [x] `DayChanged` event on `GameTimeService` (bank interest, market day, seasons)

### Gate
A level-25 player juggles crops + animals + processing + orders with no dead time.

---

## ✅ Phase 6 — Launch Readiness (DONE, code)

**Goal:** safe, sellable, operable.

### Systems
- [x] Parent mode: arithmetic parent gate + dashboard (playtime, level/XP/coins/
      achievements, ads & analytics toggles, optional daily limit with gentle
      reminders — never a hard lockout)
- [x] Playtime tracking (`PlaytimeService`): session + daily totals, day rollover
- [x] Cloud save: `ICloudSaveProvider` seam + HTTP provider + `CloudSaveService`
      (local-first; latest-timestamp-wins; silent failures) + real FastAPI/PostgreSQL
      server in `workspace/farm-quest/cloud-server/`
- [x] Events framework: `GameEventData` + `LiveEventService`, 8-event monthly
      calendar; sell-price bonuses compose with Market Day; XP multiplier hook
- [x] Real ad provider behind `IAdProvider`: `UnityAdsProvider` (guarded compile;
      setup steps in its header); kid-safe limits already enforced + remote-config
      overridable
- [x] Analytics (privacy-safe): `AnalyticsService` + debug/HTTP providers;
      gameplay facts only, no device IDs, parent opt-out, first-party server
- [x] Remote config: generic key-value seam added to `IRemoteConfigProvider`;
      `HttpRemoteConfigProvider` with local fallback
- [x] Child-safety audit (`CHILD_SAFETY_AUDIT.md`) + store kids-category checklists

### Gate
Passes platform kids-category review; parents trust parent mode.

### Remaining (needs Unity editor / accounts — not code)
- Install & configure Unity Ads package (steps in `UnityAdsProvider`)
- Host privacy policy; link in parent dashboard
- iOS Kids build: disable rewarded ads at build time
- Deploy cloud server behind HTTPS with `CLOUD_API_KEY`
- Target-age playtest; education content pass

---

## Implementation notes

- Every phase follows the same pattern: **DATA (SO) → LOGIC (service) →
  VIEW → UI → SAVE → TESTS**. See ARCHITECTURE.md.
- New crops/items/levels never require code — only the generator or new assets.
- The `GameEvents` bus is the integration point: new systems subscribe, never
  modify existing services' internals.
- Spec §60 ("visible progression") is a per-phase checklist item: every unlock
  must change something the player can *see*.
