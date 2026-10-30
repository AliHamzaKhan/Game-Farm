# FarmQuest Economy & Monetization Design

**Model:** free-to-play + optional in-app purchases + optional rewarded ads.
**Rule #1:** a player who never pays and never watches an ad can finish every city
and every country. Money and ads buy *convenience, customization and speed*, never access.

The numbers live in data (`GameDataGenerator`, `StoreCatalog`, `CropData`, `ItemData`,
`EquipmentData`, `DecorData`). Run **FarmQuest → Generate → Economy Report** to write
`ECONOMY_TABLE.md`, a full price/benefit/unlock table built from the actual assets.

## 1. Currencies

| | Farm Coins | Farm Gems |
|---|---|---|
| Earned by | Selling crops, orders, contracts, missions | Cities (2), countries (10–45), login days 6–7, weekly challenges |
| Bought with money | Yes (Starter Coins) | Yes (Gem Packs) |
| Buys | Every normal seed, Basic supplies, machines, land, upgrades | Premium seeds, Advanced/Premium supplies, gem decor, gem machine models |

A free player earns about **60 gems a week** from login and weekly challenges alone, plus city
and country rewards. That's enough for a premium seed pack every few days, so premium content
is reachable without paying.

## 2. What money can and can't do

| Can buy (convenience, cosmetics, speed) | Never sold / never required |
|---|---|
| Gems, coin packs | Access to any city or country |
| Premium seeds (higher value, golden visuals) | Crops needed by contracts or orders: these are always normal coin seeds |
| Growth boosters (max 25% of a timer each) | Skipping a whole city |
| Machines early via gems (same level requirement) | Random paid rewards (no loot boxes) |
| Decorations and country packs (no stats) | Essential mechanics behind a subscription |

Enforced in code:
- Premium seeds can't be bought with coins (`MarketService.BuySeed`).
- Premium crops never appear in orders (`OrderService`) or city objectives (data).
- The growth booster is capped per use (`CropCareService`).

## 3. Supplies, by tier

- **Basic** (coins): Basic Fertilizer, Organic Compost, Pest Control Spray, Nitrogen/Potassium/Calcium Nutrients.
- **Advanced** (coins or gems): Liquid Nutrients (any nutrient need +10%), Premium Fertilizer (fertilize + 10% time).
- **Premium** (gems or free rewards): Growth Booster (−25% timer), Super Compost (all care in one).

Advanced and Premium items are also handed out free by the login calendar, weekly
challenges and city rewards.

## 4. Rewarded ads (optional, natural moments only)

| Moment | Offer | Cooldown |
|---|---|---|
| Crop growing (plot panel) | −60 s, finish stage, finish now (≤10 min left), +50% harvest | daily caps |
| Good harvest (4+) | Same amount again (max 20) | 5 min |
| Order delivered | +50% coins | 5 min |
| Mission claimed | +50% coins | 5 min |
| Out of fertilizer/compost/spray | 2 free | 10 min |
| Daily reward | Double today's gift | once a day |
| Market | 2 free fertilizer | daily caps |

Guardrails:
- There's a 90 s global gap between offers, so there's no ad after every plant or harvest.
- Ads are capped per day (`BalanceConfig`), and time reduction is capped per day.
- The parent switch turns all ads off.
- A reward is granted only on confirmed completion. Skipped, failed or crashed ads give nothing and block nothing.
- There are **no interstitial or banner ads.**

## 5. In-app purchases and child safety

FarmQuest targets kids and teens, so every real-money purchase must:
1. **Pass the parent gate** (an arithmetic question) right before buying, every time.
2. Be allowed by the **parent dashboard** "In-app purchases" switch.
3. Show the **store's real localized price**. The `$` labels in data are fallbacks only.
4. Be credited **once per store transaction id**, saved immediately, so restores and
   duplicate callbacks never double-grant and crashes never lose a purchase.

There are no loot boxes, no countdown pressure, and no "only for payers" contracts.
A missed login day pauses the streak instead of resetting it.
Check the platform rules before launch: Google Play Families policy, Apple Kids Category, COPPA/GDPR-K.

**Integrating a real store:** add Unity IAP (`com.unity.purchasing`), write an `IIapProvider`
that calls the store and reports success only for completed (ideally server-validated)
purchases, and pass it to `StoreService` in `Bootstrapper`. Product ids are in `StoreCatalog`.
Until then the editor simulates purchases, and builds show the store as unavailable.

## 6. Retention (all free)

- 7-day login calendar (coins, supplies, gems, premium seeds on day 7) + daily free seeds.
- Daily missions (existing), 5 weekly challenges (reset Monday UTC), seasonal live events (existing).
- Safety net: a player with no seeds, nothing growing, nothing to sell and too few coins gets 5 free seeds (once a day).

## 7. Balancing notes

- The coin scale is small (tomato seed 15, sells 12). Your example table (tomato seed 100)
  is about 7× this scale; multiply prices by the same factor if you want bigger numbers.
- No-ads simulation (real game code, 16 plots): Karachi takes about 17 minutes either way. A careful
  player ends with about 6,000 coins, a water-and-harvest-only player about 2,700.
- Tune in data, then re-run Generate All Game Data and the Economy Report.

## 8. Later: FarmQuest VIP (not built)

If added: a monthly subscription with cosmetics, a monthly premium seed pack, extra *optional*
daily missions and more customization slots. It must never gate farming mechanics. It needs the
parent gate, clear renewal terms and easy cancellation, and should launch only after the core
economy is proven.
