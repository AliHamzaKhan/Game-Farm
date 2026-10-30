# 🌱 FarmQuest — Unity 2.5D Farming Game (Vertical Slice)

A polished, modular, offline-first farming sim for kids & teens.
**This scaffold implements the vertical slice (spec §54, Phase 1):**
prepare → plant → water → grow → harvest → sell → earn → replant,
with coins, XP/levels, inventory, market, save/load, and a data-driven
foundation for everything after.

## 1. Requirements

- **Unity 6 LTS** (tested target: `6000.0.23f1`) via Unity Hub
- No extra packages needed (uses built-in uGUI + legacy Input)

## 2. Open the project

1. Unity Hub → **Add** → select this `unity-project/` folder → Open.
2. Unity generates `Library/` on first open (may take a few minutes).

## 3. Generate game data (do this first!)

1. In Unity: menu **FarmQuest → Generate → All Game Data**.
2. This creates `Assets/Resources/Data/` with:
   - 8 crop assets (carrot, lettuce, tomato, onion, peas, potato, corn, pumpkin)
     with growth times/prices from the spec §8 tuning table
   - Seed + harvested-crop + compost + fertilizer `ItemData` assets
   - `CropDatabase`, `Progression` (levels 1–30), `BalanceConfig`, `FarmLayout`
3. **Assign** these assets on the Bootstrapper (or move them under a
   `Resources/Data/` folder — Bootstrapper falls back to `Resources.Load`).

## 4. Build the Farm scene (checklist)

Create a scene `Farm` and add:

| GameObject | Component | Notes |
|---|---|---|
| `Boot` | `Bootstrapper` | Assign the 4 data assets; `DontDestroyOnLoad` |
| `Input` | `InputService` | Auto-picks touch vs mouse/keyboard |
| `Audio` | `AudioService` | Optional for now; assign clips later |
| `Main Camera` | `FarmCameraController` | Orthographic set automatically |
| `Player` | `PlayerController` + `CharacterController` + `PlayerCustomization` | Assign ground layer mask |
| `FarmGrid` | (empty holder) | Tile views are spawned by a small spawner — see below |
| `FarmInteraction` | `FarmInteractionController` | Assign camera, tile layer, plot panel |
| `Canvas` | `HudController` + panels | Wire buttons/texts in inspector |
| `Canvas` | `MissionPanelController` | Daily missions + claim buttons |
| `Canvas` | `DialoguePanelController` | NPC tap-to-talk dialogue |
| `Canvas` | `TutorialPanelController` | FTUE hint bar + skip button |
| NPCs | `NpcView` per villager | Set `npcId`; place near market/farm |
| `Animals` | `AnimalViewManager` | Set `penOrigin`; spawns views per owned animal |
| `Decor` | `DecorViewManager` | Syncs placed-decoration visuals |
| `House` | `HouseView` | Assign `levelPrefabs`; swaps on upgrade |
| `Canvas` | `AnimalPanelController` | Barn UI: feed/pet/collect/buy |
| `Canvas` | `DecorPanelController` | Decor shop + tap-to-place mode |
| `Canvas` | `HousePanelController` | House upgrade panel |
| `Tractor` | `TractorController` + `TractorView` | Drivable tractor; tap to drive |
| `Canvas` | `TractorPanelController` | Mode buttons + exit while driving |
| `Canvas` | `EquipmentPanelController` | Equipment shop |
| `Canvas` | `OrchardPanelController` | Sapling shop + tree status |
| `Orchard` | `TreeViewManager` | Set `orchardOrigin`; spawns tree views |
| `Canvas` | `ProductionPanelController` | Production buildings: queues, recipes, collect |
| `Canvas` | `OrderBoardPanelController` | Delivery orders board |
| `Canvas` | `VillagePanelController` | Village shops (+ bank button) |
| `Canvas` | `BankPanelController` | Bank: deposit/withdraw, interest info |
| `Pond` | `FishingController` | Assign fish species; tap to cast/catch |
| Anywhere | `WildlifeService` | Ambient critter spawner |
| `Canvas` | `ParentGateController` | Arithmetic gate → opens dashboard |
| `Canvas` | `ParentDashboardController` | Stats, ad/analytics toggles, playtime limit |

**Tile views:** write a tiny `FarmGridBuilder` (or place manually for the slice):
for each `FarmService.AllTiles()`, instantiate a quad/cube with `FarmTileView`,
position at `FarmService.GridToWorld(pos)`, put on the tile layer, call `Bind(tile)`.
(Left as a scene-wiring step so you can choose your tile prefab art.)

**UI:** create panels with the named children the controllers expect
(`MarketPanelController` documents its row prefab: children `Name`, `Price`, `Button`).

## 5. Play the vertical slice

Press Play → tap a grass tile → **Clear → Dig → Prepare** → open seed shop →
select carrot → tap the prepared plot to **plant** → **water** → wait 2 min
(or watch it grow through 6 stages) → tap the ripe crop to **harvest** →
open market → **sell** → coins + XP → buy more seeds. Save persists across runs;
crops keep growing while the app is closed (§9 offline progression).

## 6. Run the tests

**Window → General → Test Runner →** run EditMode tests (`FarmQuest.Tests`):
crop growth, quality, offline time, inventory stacking, economy, save migration.
All must pass before each phase milestone (§58).

## 7. Phase status (see PHASES.md)

| System | Status |
|---|---|
| Phase 2: missions/achievements/NPCs/story/tutorial/14 crops | ✅ Implemented |
| Phase 3: animals/pets/barn/house/decor | ✅ Implemented |
| Phase 4: tractor/equipment/orchard/irrigation | ✅ Implemented |
| Phase 5: processing/orders/village/fishing/wildlife/seasons/weather | ✅ Implemented |
| Phase 6: parent mode/cloud/events/ads/analytics/safety audit | ✅ Implemented (code; launch ops need editor+accounts) |
| Tractor/equipment | Phase 4 of spec |
| Processing/orders/village/fishing/weather/seasons | Phase 5 of spec |
| Rewarded ads | `RewardedAdService` interface + kid-safe limits done; real provider = Phase 6 |
| Parent mode / cloud save / analytics / remote config | Phase 6 of spec |
| Backend (FastAPI/PostgreSQL) | Optional; cloud-save only, never required offline |

## 8. Key design rules (enforced in code)

- **No gameplay logic in MonoBehaviours** that belongs in data/services (§51):
  `CropData` / `CropInstance` / `CropGrowthService` / `FarmTileView` / UI / save.
- **One clock**: `ITimeService` — every timer derives from it (§9).
- **Crops never die** — missed care lowers quality only (kid-safe §13).
- **Balance is data** (`CropData`, `BalanceConfig`, `ProgressionData`), never hardcoded (§47).
- **Ads are optional** and rewards fire only on provider-confirmed completion (§10).

See `ARCHITECTURE.md` for the full technical rationale.

## 9. World Tour: countries, cities, crop care

The game is a world farming tour. You start in **Level 1: Karachi, Pakistan**.
Each city is a level with its own contracts. Finishing every contract completes
the city, pays its reward and unlocks the next one. Finishing a country's last
city pays a country bonus and opens the next country. After the last authored
city the tour starts again with bigger targets and rewards (★2, ★3…), so it never ends.

**Play it**
1. **World → City** shows the current contracts: harvest crops, deliver items
   (press **GIVE**), sell items, or earn coins from sales.
2. Tap a plot. The panel shows the crop's stage (Seed → Sprouting → Growing leaves →
   Flowering → Fruit developing → Ready), time left, and what it needs right now.
   A coloured ball over a plot means attention: blue = water, yellow = fertilizer,
   purple = nutrient, red = pests, orange = can prune, gold = ready.
3. Buy fertilizer, pest spray and nutrients in **Market → Buy**.
4. Optional ad buttons in the plot panel speed growth up; they never block progress.
5. **World → Map** shows every country and city; **World → Farm** has upgrades and land.

**Care is crop-specific** (set per crop in `CropData`):
- `waterIntervalSeconds`: how long a watering lasts. Dry crops grow at
  `dryGrowthPenalty` speed, never die.
- `needsFertilizer`: needed from the Vegetative stage. Without it the harvest is 20% smaller.
- `requiredNutrient`: wanted from Flowering. Gives +15% harvest, or −10% if missing.
- `needsPruning`: prune during Vegetative/Flowering for +25% harvest.
- `pestChance`: chance of a pest attack. Unsprayed pests cost 40% of the harvest.
- `stageWeights`: relative length of the 5 growing stages.

Everything is timestamp-based, so it all resolves correctly after the app was closed.

**Add countries and cities (no code)**: edit `Assets/Resources/Data/WorldData.asset`
in the Inspector, or the `GenerateWorld()` table in `Editor/GameDataGenerator.cs`
and re-run **Generate → All Game Data**. Objective codes there are
`H:crop:n` harvest, `D:item:n` deliver, `S:item:n` sell, `E:n` earn from sales;
prices are `item:multiplier`.

Re-run **FarmQuest → Generate → All Game Data** and **FarmQuest → Setup → Create
Test Scenes** after pulling this update.
