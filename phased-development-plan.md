# 🌱 Farm Life / Farm Quest — Phased Development Plan

> **Status:** Planning · **Technology:** Unity (C#, 2.5D isometric, orthographic camera) — decided 2026-10-08
> Unity vertical-slice scaffold: `unity-project/` (see its README + ARCHITECTURE)
> **Version:** 1.0 · **Date:** 2026-10-08
>
> This document turns the full game concept into a buildable, phased roadmap.
> Every one of the 51 concept sections is mapped to a phase so nothing gets lost —
> the mapping is noted at the end of each phase as `Covers: §n`.

---

## 1. How to read this plan

- **Phases are sequential and gated.** A phase is done only when its exit criteria are met
  *and* a playtest with real kids/teens passes. No phase starts half-finished work "in parallel."
- **Each phase ships something playable.** Even Phase 1 is a real game, just a small one.
- **Content scales with levels.** The level unlock table from the concept (§49) is the backbone:
  Phase 1 ≈ levels 1–10, Phase 2 ≈ 11–20, Phase 3 ≈ 21–30, Phase 4 ≈ 31–50.
- **Tech-agnostic.** Nothing here assumes Flutter, Unity, Godot, or anything else.
  The tech decision is an explicit Phase 0 deliverable.

---

## 2. Non-negotiable design principles (apply to every phase)

These came straight from the concept and are treated as hard constraints, not preferences:

1. **Kid-safe by architecture, not by patch.** No open chat, no unmoderated messaging,
   no public user-generated content, no location sharing, no unnecessary personal data (§37).
2. **No gambling mechanics, ever.** Dynamic prices follow visible, predictable rules
   and never touch real money (§19, §42).
3. **No aggressive monetization.** One-time unlock + optional content packs.
   Core progression is never paywalled (§26, §42).
4. **Offline-first.** Plant, water, harvest, animals, building, customization, and
   mini-games all work without internet (§43). Internet is for backup, events, updates, purchases.
5. **PLAN → FARM → CARE → EXPLORE → PRODUCE → SELL → UPGRADE → CUSTOMIZE → DISCOVER → EXPAND.**
   Never degrade into WAIT → COLLECT (§48). Every phase must leave the player with
   at least 3 meaningful things to choose between.
6. **Every marketing/feature claim stays inside what is actually built.**
   No invented stats, no invented features.
7. **Education is woven in, never lectured.** Composting facts, profit math, and water
   conservation appear inside gameplay, not as schoolwork (§35).

---

## 3. Phase map (overview)

| Phase | Name | Player levels | Core question it answers |
|-------|------|---------------|--------------------------|
| **0** | Pre-production & GDD | — | *What exactly are we building, with what, and how will we know it's balanced?* |
| **1** | Core Farming MVP (V1) | 1–10 | *Is the farming loop actually fun?* |
| **2** | Animals & Machines (V2) | 11–20 | *Does the farm feel alive and worth expanding?* |
| **3** | Business Depth (V3) | 21–30 | *Can the player run a real farm business?* |
| **4** | Living World & Dream Farm (V4) | 31–50 | *Is there a hundred-hour dream to chase?* |
| **5** | Launch Readiness & Live Ops (V5+) | 50+ / endgame | *Is it safe, sellable, and alive after launch?* |

---

## 4. Phase 0 — Pre-production & Game Design Document

**Goal:** Leave this phase with zero ambiguity about *what* to build. Code written
against a vague design is the most expensive code you'll ever write.

### Deliverables

- **Full GDD**, including the exact numbers the concept deliberately leaves open:
  - Economy spreadsheet: every crop's seed cost, growth time, water/quality needs,
    yield range, sell price, quality multipliers — simulated for exploits and dead ends.
  - XP table for levels 1–50 and the XP source breakdown (§25).
  - Land map: all 8+ plots, sizes, unlock order, costs (§4).
  - Buildings, animals, equipment tiers 0–5, missions, achievements, collection book contents.
  - UI screen list + user flows (farm view, market, inventory, dashboard §47, customization).
  - MVP vs V2/V3/V4 feature split = this document, ratified.
- **Technology decision document.** ✅ DECIDED 2026-10-08: **Unity** (C#, 2.5D isometric,
  orthographic camera; Android/iOS primary, Windows/macOS later). A complete Unity
  development specification exists; the vertical-slice scaffold is in `unity-project/`.
- **Art direction bible.** Cute stylized look (§44): palette, character proportions,
  tileset/prop approach, animation principles (§45), day/night lighting (§46).
- **Wireframes / clickable mock** of the core loop (plant → water → harvest → sell).
- **Safety & monetization policy.** Parental-gate design, one-time-unlock model,
  content-pack plan, and the list of things we will never build (§37, §42).
- **Risk register** (see §13 below, expanded with owners).

### Exit criteria
- [ ] GDD signed off; economy spreadsheet simulated with no infinite-money loops.
- [ ] Tech stack chosen and a "hello farm" prototype builds on target devices.
- [ ] Art style locked with 3+ approved sample assets.
- [ ] Wireframes exist for every Phase 1 screen.

**Covers:** §44, §47, §51 (the GDD step itself), plus the policy halves of §37, §42, §43.

---

## 5. Phase 1 — Core Farming MVP (V1, levels 1–10)

**Goal:** Prove the loop is fun. One small farm, a handful of crops, and a complete
journey from first seed to first expansion.

> **Honest scope note:** the concept's suggested V1 list is ambitious for a true MVP.
> So Phase 1 is split: **1A** is a vertical slice (prove fun fast, cheap to throw away),
> **1B** grows it into the full V1. If 1A isn't fun, we fix the design — not the content volume.

### Milestone 1A — Vertical slice (the fun test)
- One tiny farm: house, well, storage shed, **4 plots** (§4, land 1).
- **2 crops** (carrot, tomato): simplified soil prep (dig → compost → plant),
  watering can, growth stages, harvest (§1, §5, §6, §7).
- Sell at market for **fixed prices**; earn coins + XP; levels 1–3 (§18, §25, §26 coins only).
- Friendly NPC farmer tutorial (§2, §29-lite).
- Local save/load, offline from day one (§43).
- Core animations: plant sprout, growth stages, harvest pull (§45). Day/night visuals (§46).

**1A exit:** a new player can complete plant → water → harvest → sell three times
without guidance after the tutorial, and a kid playtest rates it "fun, want more."

### Milestone 1B — Full V1
- **8 vegetable crops** (§6): carrot, potato, tomato, onion, lettuce, corn, pumpkin, peas.
- Soil prep full 5-step flow (§5); compost + fertilizer + nutrients with one-line
  educational tips (§10, §35).
- Crop quality: ⭐ Normal → ⭐⭐⭐ Excellent based on care (§8).
- Watering progression: watering can → water pump → sprinkler (§9).
- **Chickens** (feed → eggs) as the first animal system (§14-lite).
- **One pet**: dog that follows the player (§15-lite).
- Equipment tier 0–1: hand tools, wheelbarrow, better watering can (§13-lite).
- Land: levels 1–2 (4 plots → +8 plots) (§4).
- Economy: inventory, market with fixed prices, basic storage shed (§18, §20).
- Progression: XP levels 1–10 with titles (§25), 10 starter achievements (§24),
  daily missions v1 (§23), collection book v1 (crops + equipment) (§27).
- Character customization v1: body type, skin tone, hair + color, shirt, pants, hat (§3).
  Nothing customization-related costs real money, ever.
- Village v1: seed seller + market owner NPCs, story chapter 1
  ("restore grandfather's farm") (§29-lite, §30-lite).
- One mini-game: harvest tap-timing (§32-lite).
- Player dashboard v1: level, coins, crops/animals counts, today's goals (§47).
- Sound: friendly music loop + core SFX.

### Exit criteria
- [ ] Player journey tutorial → level 10 → first land expansion → chickens, with no blockers.
- [ ] Save/load survives app kill; fully offline.
- [ ] Kid playtest (ages 6–12): understands loop without help; wants to come back tomorrow.
- [ ] No crashes on low-end target devices; 30 fps minimum.

**Covers:** §1, §2, §3 (basic), §4 (lands 1–2), §5, §6 (vegetables), §7, §8, §9 (basic),
§10, §14 (chickens), §15 (dog), §13 (tier 0–1), §18, §20 (basic), §23 (v1), §24 (starter),
§25 (1–10), §26 (coins), §27 (v1), §29 (2 NPCs), §30 (ch.1), §32 (one), §43, §45, §46, §47.

---

## 6. Phase 2 — Animals & Machines (V2, levels 11–20)

**Goal:** The farm feels alive. Animals become a second progression path,
machines make the player feel their growing power.

### Scope
- **Animals** (§14): cow (milk), sheep (wool), goat — with care loop
  (feed, brush, clean), animal area land (§4, land 5), small barn (§20 upgrade).
- **Pets** (§15): cat joins the dog; pet accessories; pets sleep at the house,
  react to the player.
- **Equipment tiers 1–2** (§13): better shovel, seeder, sprayer, **small tractor**
  (drives across fields preparing soil — the animation moment §45 calls for).
- **Watering** (§9): irrigation system unlock.
- **Fruits** (§6): strawberry, watermelon, apple, orange + **orchard land** (§4, land 4).
- **Weather v1** (§11): sunny / cloudy / rain / wind. Rain auto-waters (relief, never
  punishment). Storms are visual-only at this stage — *never frustrating for children.*
- **Seasons framework** (§12): spring/summer/autumn/winter with gentle crop bonuses
  and visual changes; winter favors greenhouse teaser.
- **House upgrades** (§16): level 1 → 2; **decorations set 1** (§17):
  fences, trees, flowers, lamps, paths.
- **Basic processing** (§21-lite): manual workstation — strawberry → jam,
  milk → cheese, wheat → flour. (Full factory comes in Phase 3.)
- **Basic delivery orders** (§22-lite): market order board ("we need 20 tomatoes").
- **Village grows** (§29): equipment dealer, animal doctor NPCs; story chapters 2–3;
  friendship-flavored NPC lines (§31-lite).
- **Educational layer** (§35): composting, crop cycles, profit math surfaced in quests.
- Collection book expands (animals, fruits); 15 more achievements; daily missions v2.

### Exit criteria
- [ ] Level 20 reachable in weeks of casual play; economy still balanced (no grind wall, no money fountain).
- [ ] Tractor + animals + weather all interact (rain waters crops, tractor preps soil).
- [ ] Teen playtest: "there's always something to do" — validates §48.

**Covers:** §4 (lands 4–5), §6 (fruits), §9 (mid), §11 (v1), §12 (framework), §13 (tier 1–2),
§14 (core herd), §15 (cat+), §16 (L1–2), §17 (set 1), §20 (barn), §21 (manual),
§22 (basic), §23 (v2), §24 (+15), §27 (expanded), §29 (+NPCs), §30 (ch. 2–3), §31 (lite), §35.

---

## 7. Phase 3 — Business Depth (V3, levels 21–30)

**Goal:** The player runs a real farm business — production chains, orders,
and market strategy (kid-safe, predictable, educational).

### Scope
- **Processing factory** (§21 full): tomato → sauce, apple → juice, honey → jar,
  multi-step recipes; factory building on its own land (§4, land 8).
- **Delivery system full** (§22): restaurant orders (multi-item, e.g. 50 tomatoes +
  20 onions + 10 peppers), timed order board, XP-heavy rewards.
- **Dynamic market prices** (§19): prices shift on visible, predictable in-game rules
  (season, weather, supply) with a shown trend arrow — "sell now or wait?" as a
  *thinking* mechanic, never gambling, never real money.
- **Full seasons** (§12): 4 seasons, seasonal crop bonuses, winter greenhouse farming.
- **Weather full** (§11): storms can threaten unprotected crops — with clear,
  cheap protection the player controls (greenhouse covers, storm shutters).
  Snow in winter (visual + greenhouse synergy).
- **Land 6–7** (§4): large crop field, factory plot; storage upgrades again (§20).
- **Equipment tiers 3–4** (§13): large tractor, automated planter, harvesting machine,
  automatic irrigation.
- **More animals** (§14): horse, bees (honey); **pets** (§15): rabbit, bird.
- **House level 3** (§16); **decorations set 2** (§17): windmill, fountain, picnic area, bridges.
- **Fishing** (§33): pond/river, species collection, equipment upgrades, challenges.
  Friendly, non-violent presentation.
- **Mini-games** (§32): planting timing, market packing, simple tractor driving,
  animal care brushing.
- **Events framework** (§40): seasonal event system + first event (Harvest Festival).
- **World map** (§28): forest + river nodes unlock; village full cast (§29);
  story chapters 4–6 (§30); NPC friendship progression (§31).
- **Farm rating** (§41): star rating from crops, animals, buildings, decorations,
  production, achievements — the long-term score.

### Exit criteria
- [ ] A level-30 player juggles crops + animals + factory + orders without dead time.
- [ ] Dynamic pricing is understood by teen playtesters and never feels random or unfair.
- [ ] Event framework proven with one full seasonal event.

**Covers:** §4 (lands 6–8), §9 (auto), §11 (full), §12 (full), §13 (tier 3–4), §14 (+horse/bees),
§15 (+rabbit/bird), §16 (L3), §17 (set 2), §19, §20 (upgrades), §21 (factory), §22 (full),
§28 (forest/river), §29 (full cast), §30 (ch. 4–6), §31, §32 (set), §33, §40 (framework), §41.

---

## 8. Phase 4 — Living World & Dream Farm (V4, levels 31–50)

**Goal:** The hundred-hour dream. Endgame depth, full creative expression,
and the "Master Farmer" summit.

### Scope
- **Remaining land + endgame plots** (§4): everything unlocked; large-scale layouts.
- **Dream farmhouse level 4** (§16) with full interior customization
  (walls, roof, doors, windows, furniture, kitchen, garden).
- **Decorations complete** (§17): playground, garden ornaments, night lighting —
  the creative sandbox younger players love.
- **Equipment tier 5** (§13): premium machinery; near-full automation for players
  who want to manage rather than tap.
- **Advanced production chains** (§21): multi-step recipes (e.g. wheat → flour →
  baked goods via order chains).
- **Nature & wildlife** (§34): birds, butterflies, bees, squirrels, frogs, fireflies —
  appearing based on season, weather, trees, flowers, and decorations. The farm feels magical.
- **Full world** (§28): mountain node; village complete; story chapter 7 —
  "become the valley's Master Farmer" (§30).
- **Second currency** (§26): gems / farm tokens — **earnable only** through
  achievements and special events; never required for core progression; never purchasable
  in a way that gates kids' progress. (Design finalized in Phase 0, implemented here.)
- **Collection book complete** (§27); **50+ achievements** (§24); farm rating 5-star chase (§41).
- **Leaderboards — optional and gated** (§39): weekly, opt-in, non-social challenges
  (most crops harvested); ships only after a child-safety design review.
- **§48 audit:** verify the endgame always offers 5+ concurrent goals.
- Performance pass, localization groundwork, accessibility review
  (colorblind-safe palettes, readable text, no reflex-only mechanics).

### Exit criteria
- [ ] Level 50 achievable; endgame loops (factory chains, orders, events, rating)
  sustain play without new content for 4+ weeks.
- [ ] Creative tools (house + decorations) validated with the younger playtest group.
- [ ] Content-complete: everything in the GDD's V4 column is in.

**Covers:** §4 (complete), §13 (tier 5), §16 (L4), §17 (complete), §21 (advanced),
§24 (50+), §26 (gems), §27 (complete), §28 (mountain), §30 (ch. 7), §34, §39 (optional),
§41 (chase), §48 (audit).

---

## 9. Phase 5 — Launch Readiness & Live Ops (V5+)

**Goal:** Ship safely, sell honestly, and keep the world alive after launch.

### Scope
- **Parent Mode** (§36): parental gate → play time, farming progress, achievements,
  "what your child learned today" (composting, profit math), purchase history, settings.
- **Child-safety audit** (§37): independent review against COPPA / GDPR-K style
  requirements; confirm: no open chat, no public UGC, no location, no behavioral ads,
  minimal data, clear privacy policy.
- **Monetization implementation** (§42):
  - Free: core farming, basic land, basic crops/animals, story intro.
  - One-time purchase: **Unlock Full Farm** (more land, crops, animals, buildings, customization; no ads).
  - Optional content packs: 🌴 Tropical Farm, 🏔️ Mountain Farm, 🐎 Horse Ranch, 🌸 Flower Garden.
  - All purchases through Apple/Google IAP with parental-gate requirements.
- **Offline-first completion** (§43): optional cloud backup, updates, events, purchases
  online; everything else offline.
- **Events calendar** (§40): monthly/seasonal events (Pumpkin Festival 🎃,
  Spring Garden 🌸, Snow Festival ❄️, Animal Day 🐄) with temporary cosmetics — no purchase required.
- **Analytics** (privacy-safe, no PII): retention, session length, progression funnels,
  economy health — to balance, not to surveil.
- **Multiplayer prep** (§38): architecture supports future farm visits
  (view-only + predefined friendly reactions); **not shipped** until safety-reviewed.
  Never unrestricted child-to-child communication.
- **Soft launch → playtest → tune → global launch.**

### Live ops cadence (post-launch)
- Monthly event, quarterly content pack, continuous economy balancing.
- Parent-mode reports improve with each learning-theme release.

### Exit criteria
- [ ] Passes platform kids-category review (Apple/Google) first submission.
- [ ] Parent-mode playtest: parents understand and trust it.
- [ ] Soft-launch metrics: D1/D7/D30 retention and session health meet GDD targets.

**Covers:** §36, §37 (audit), §38 (prep), §39 (if shipped), §40 (calendar), §42, §43 (complete).

---

## 10. Cross-cutting systems (built across phases)

| System | Phase 0–1 | Later phases |
|--------|-----------|--------------|
| Save system (offline-first, conflict-safe) | Architecture + local saves | Cloud backup opt-in (P5) |
| Audio | Music loop + core SFX | Per-season/event music, animal voices (P2–P4) |
| Animation | Core loop animations (§45) | Tractor, weather, wildlife, market truck (P2–P4) |
| Educational content (§35) | Compost/profit tips in tutorial | Water conservation, crop cycles, business math in quests (P2–P4) |
| Accessibility | Readable text, no twitch-only input | Colorblind pass, localization (P4–P5) |
| Art pipeline | Style bible, asset list per phase | Packs and event assets (P5) |

---

## 11. Playtest gates (after every phase)

Each gate uses two groups: **kids 6–12** (is it understandable, kind, fun?)
and **teens 13–16** (is it deep enough, is there always something to do?).

- **P0:** paper/clickable prototype of the core loop — do kids *get it* in 2 minutes?
- **P1:** full V1 — can they reach level 10 unaided? Do they return the next day?
- **P2:** animals + tractor — does the farm feel alive? Any frustration points?
- **P3:** factory + orders + dynamic prices — do teens strategize? Do kids still feel safe?
- **P4:** creative tools + endgame — do younger kids decorate for fun? Do teens chase mastery?
- **P5:** parent mode + purchase flow — do parents trust it? Is buying ever confusing?

A failed gate sends the phase back for rework. Gates are cheaper than relaunches.

---

## 12. Rough timeline (indicative)

Assumes a small team (2–3 developers, 1 artist, part-time designer).
These are ranges, not commitments — art scope and tech choice move them most.

| Phase | Indicative duration |
|-------|--------------------|
| 0 — Pre-production & GDD | 3–4 weeks |
| 1 — Core Farming MVP (1A + 1B) | 12–16 weeks |
| 2 — Animals & Machines | 8–10 weeks |
| 3 — Business Depth | 8–10 weeks |
| 4 — Living World & Dream Farm | 8–12 weeks |
| 5 — Launch Readiness | 6–8 weeks + ongoing live ops |
| **Total to global launch** | **~11–15 months** |

---

## 13. Risks & mitigations

| Risk | Mitigation |
|------|------------|
| Scope creep (the concept is *big*) | Phase gates; "not in this phase" list (§14); GDD owns the scope |
| Economy exploits / grind walls | Spreadsheet simulation in P0; analytics + rebalancing in live ops |
| Art cost explodes (cute 3D/2.5D is asset-hungry) | Style chosen for scalability in P0; decorations reuse a prop system |
| Kid-safety issue found late | Safety designed in P0, audited in P5 — never bolted on |
| Tech choice wrong for offline-first + low-end devices | P0 prototype must build and run on the cheapest target device |
| "Wait → collect" boredom (§48) | Every phase exit checks: ≥3 concurrent meaningful goals |
| Team tries to build multiplayer too early | §38 is prep-only until post-launch; written into the plan |

---

## 14. Deliberately deferred (not in V1–V4)

- Real-time multiplayer / farm visits beyond view-only prep (§38)
- Open chat or any unrestricted child communication (§37)
- Advertising-based monetization (§42)
- Any gambling-like mechanic (§19, §26)
- PvP, combat, or competitive pressure mechanics
- Console/VR ports — mobile-first, then evaluate

---

## 15. Open decisions for Phase 0

1. Technology stack (Flutter/Flame vs Unity vs Godot vs other).
2. 2D, 2.5D, or stylized 3D — art cost vs. "alive" feel (§44).
3. Exact crop/economy numbers (the GDD spreadsheet).
4. Launch platforms order (mobile-first? which stores? which age-rating category?).
5. Languages at launch (consider the PK/IN audience + Roman Urdu lessons from your other products).
6. One-time-unlock price point and content-pack pricing (§42).
7. Second-currency earn rates — generous by design (§26).

---

*Next step: Phase 0 — write the full Game Design Document using this plan as the skeleton.
Once the tech decision lands, each phase becomes a build milestone with the same exit criteria.*
