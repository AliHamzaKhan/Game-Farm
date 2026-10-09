# FarmQuest — Architecture Notes

## Why this shape (§50, §51)

The spec demands: modular, scalable, no god-`GameManager`, and a strict
**DATA / LOGIC / VIEW / UI / SAVE** split. The implementation:

```
DATA   ScriptableObjects: CropData, ItemData, ProgressionData, FarmLayoutData, BalanceConfig
         Plain models:    FarmTile, CropInstance, InventorySlot
LOGIC  Plain C# services: FarmService, CropGrowthService, InventoryService,
                          EconomyService, ProgressionService, MarketService,
                          TimeAccelerationService, RewardedAdService
VIEW   MonoBehaviours that only render: FarmTileView, (later: AnimalView…)
UI     MonoBehaviours that only bind: HudController, PlotPanelController, …
SAVE   SaveData DTOs + SaveService + SaveMigrator + SaveCoordinator
```

**Services are plain C# classes**, constructed once in `Bootstrapper` and shared
via `ServiceLocator`. This avoids "unnecessary singletons" (§58) while keeping
code testable without scenes — every service has unit tests except the thin
MonoBehaviour views.

**Communication is event-driven** (`GameEvents` static bus): services raise,
views/UI subscribe. No service holds a reference to a view. (`§49`: no per-frame
`Update()` polling — growth is evaluated on a single 1-second tick.)

## The time model (§9)

`ITimeService.UtcNow` is the only clock. `CropInstance` stores `PlantedAtTicks`;
progress = elapsed / duration, computed on demand. Offline progression is therefore
*free*: closing the app just lets timestamps age. Clock jumps are clamped
(never negative, max 30 days) — kids sharing devices are never punished.

`TimeAccelerationService` reduces remaining time by shifting `PlantedAtTicks`
earlier — one mechanism serves waiting, items, ads, and equipment (§11).

## Saves (§41)

`SaveData` (versioned) → JSON in `Application.persistentDataPath`.
`SaveMigrator` steps old versions forward; unknown versions are stamped, never
crashed. `SaveCoordinator` pulls `CaptureState()` from each service and pushes
`RestoreState()` back. Autosave every 60s + on pause/quit.

## Input (§52) & Camera (§53)

`IInputService` abstracts tap/drag/pinch/long-press + WASD + wheel.
`TouchInputProvider` / `StandaloneInputProvider` are chosen at runtime —
gameplay code never sees `UnityEngine.Input.touches`. `FarmCameraController`
is orthographic, bounded, smoothed, with follow/focus modes.

## Economy seams (§19, §47)

- `IPriceStrategy`: `FixedPriceStrategy` now; dynamic pricing later, zero market-code changes.
- `IRemoteConfigProvider`: `LocalRemoteConfigProvider` wraps `BalanceConfig` now;
  a backend provider can override later.
- `IAdProvider`: `StubAdProvider` now; Unity Ads/AdMob in Phase 6. Limits
  (per-day, cooldown, per-activity, daily reduction cap) are enforced in
  `RewardedAdService` regardless of provider.

## Quality (§14) — the kindness rule in code

`CropInstance.CalculateQuality()` scores water + compost + nutrients + harvest
timing. Missing care lowers the grade; **nothing ever kills a crop**. This is
deliberate and must survive all future refactors.

## Phase mapping (spec §54 → this codebase)

| Spec phase | Status here |
|---|---|
| 1 — Vertical slice | ✅ Implemented: player, farm, camera, grid, dig/plant/water/growth/harvest, inventory, market, coins, XP, save/load |
| 2 — Compost/quality/missions/story | ✅ Done: compost, quality, daily missions, achievements, NPCs + dialogue, 10 story chapters, tutorial director, 14 crops |
| 3 — Animals/pets/barn/house/decor | ✅ Done: `AnimalInstance`/`AnimalService` (+view/manager), pets, buildings, decor placement |
| 4 — Tractor/equipment/orchard/irrigation | ✅ Done: `EquipmentService`, drivable `TractorController` + `TractorWorkService`, `OrchardService`, `IrrigationService` |
| 5 — Processing/orders/village/fishing/weather/seasons | ✅ Done: `ProductionService`, `OrderService`, `VillageService`/`BankService`, fishing, wildlife, `SeasonService`, `WeatherService` |
| 6 — Parent mode/cloud/events/ads/analytics/remote config | 🟡 Ad service + remote-config seams done; rest later |

## Suggested next code milestones

1. `FarmGridBuilder` scene spawner + tile/soil art pass (unblocks visual testing).
2. Compile in Unity 6 + run EditMode tests + Play Mode vertical-slice pass.
3. Achievements browse UI + story journal UI (data/services done).
4. Pet accessories UI (service done).
5. Launch ops: ad package setup, privacy policy, cloud deploy, store submission.
