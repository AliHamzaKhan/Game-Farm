# FarmQuest — Child Safety Audit (Phase 6)

**Date:** 2026-10-09
**Scope:** Unity codebase (`Assets/`) vs the spec's child-safety rules (§10, §43, §46).
**Method:** rule-by-rule check against implemented code. Honest status only.

## 1. Content & kindness rules

| Rule | Status | Evidence |
|---|---|---|
| Crops never instantly die | ✅ Pass | `CropInstance`: missed care only slows growth / lowers quality |
| Missed care = slower growth, not punishment | ✅ Pass | Watering/quality system; no death state exists |
| Animals never harshly punished | ✅ Pass | `AnimalInstance`: low happiness only slows production (0.8×); no sickness/death |
| No violence, horror, or disturbing content | ✅ Pass | Cozy farming theme throughout; no combat systems exist |
| Education integrated naturally, never school-like | ⚠️ Partial | GDD education layer designed; in-code integration is light (season/climate lessons, production chains). Content pass needed before launch |

## 2. Communication & data rules

| Rule | Status | Evidence |
|---|---|---|
| No open chat / messaging | ✅ Pass | NPC dialogue is one-way scripted text (`NpcService`); no input field exists |
| No user-generated content sharing | ✅ Pass | No photo, text, or content upload anywhere |
| No location sharing / location APIs | ✅ Pass | No location code; weather/seasons use device clock only |
| No accounts required to play | ✅ Pass | Local-first; cloud save is optional and keyed by random install id |
| No PII collected | ✅ Pass | Analytics sends gameplay facts only (see §4); no names, emails, device IDs, ad IDs |
| Offline-first | ✅ Pass | All core systems timestamp-based; network failures are silent |

## 3. Advertising rules (§10, §46)

| Rule | Status | Evidence |
|---|---|---|
| Rewarded ads only — never forced interstitials | ✅ Pass | `RewardedAdService`: player-initiated placements only; no interstitial code exists |
| Reward only after provider-confirmed completion | ✅ Pass | `IAdProvider.Show(placement, onComplete)` — reward path requires `completed=true` |
| Configurable cooldowns + daily/per-activity limits | ✅ Pass | `BalanceConfig` (5/day, 5-min cooldown, 2/activity); backend-overridable via remote config |
| Core game fully playable without ads | ✅ Pass | Ads only grant time-reductions/bonuses; no progression gate needs them |
| Parent can disable ads entirely | ✅ Pass | Parent dashboard toggle (`ParentModeService.AdsAllowed`) |
| Real provider behind the seam | ⚠️ Setup required | `UnityAdsProvider` written; needs the Ads package + `FARMQUEST_UNITY_ADS` define + dashboard placement id (see its header docs) |

## 4. Analytics privacy (§46)

| Rule | Status | Evidence |
|---|---|---|
| Gameplay facts only | ✅ Pass | Events: `session_start`, `level_up`, `animal_bought`, `equipment_bought` (+ counts/levels) |
| No device IDs / ad identifiers | ✅ Pass | No `SystemInfo.deviceUniqueIdentifier`, no advertising-ID code anywhere |
| First-party only (our server) | ✅ Pass | `HttpAnalyticsProvider` posts to the FarmQuest cloud server, not a third-party SDK |
| Parent opt-out | ✅ Pass | Parent dashboard toggle; `AnalyticsService` drops events when off |
| Offline behavior | ✅ Pass | Queue dropped (not hoarded) when offline |

## 5. Parent mode (§43)

| Rule | Status | Evidence |
|---|---|---|
| Gated access (child can't open casually) | ✅ Pass | Arithmetic parent gate (`ParentGateController`) |
| Playtime visibility | ✅ Pass | Daily playtime in dashboard (`PlaytimeService`) |
| Optional daily limit with gentle reminders | ✅ Pass | Toast reminders, never a hard lockout |
| Progress visibility | ✅ Pass | Level, XP, coins, achievements in dashboard |
| Purchase visibility | ✅ Pass | No in-app purchases exist; bank/ads/achievement stats shown |

## 6. Store kids-category checklists

### Google Play — Designed for Families
- [ ] Declare target age groups (5 & under / 6–8 / 9–12) in Play Console
- [ ] Privacy policy URL (hosted) linked in store listing AND in-app (parent dashboard)
- [ ] Ads: use only Google Play Families-certified ad SDKs; complete the Families ads declaration. Unity Ads supports this via its dashboard "child-directed" settings — configure before launch
- [ ] No disturbing content (covered §1)
- [ ] App must not request location (covered §2)

### Apple — Kids Category
- [ ] **Third-party ads are NOT allowed in the Kids Category.** Ship the iOS Kids build with rewarded ads disabled (`AdsAllowed=false` default on iOS, or a build flag). Unity Ads may only be used in a non-Kids build
- [ ] **Third-party analytics are NOT allowed.** Our analytics is first-party (own server) with parent opt-out — acceptable; keep the toggle prominent
- [ ] No external links, no in-app purchase prompts to kids (none exist)
- [ ] Privacy policy + parental gate for any external section (parent dashboard covers settings)

## 7. Before-launch TODO (not code-complete)

1. Install + configure the real ad provider (`UnityAdsProvider` header has the steps)
2. Education content pass (GDD §36 lessons → in-game tips/NPC lines)
3. Host privacy policy; link it in the parent dashboard (UI task)
4. iOS Kids build: disable rewarded ads at build time
5. Run the cloud server (`cloud-server/`) behind HTTPS with `CLOUD_API_KEY` set
6. Independent playtest with the target age group; record findings here
