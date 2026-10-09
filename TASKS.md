# Farm Quest — Task Tracker

> **Rule:** every task's status lives here. When a task is done, in progress, or pending, update this file.
> Last updated: 2026-10-09

**Status marks:** ✅ Done · 🔄 In Progress · ⏳ Pending (needs user action) · 📋 Planned (future work)

---

## 1. Design & Planning

| # | Task | Status | Notes |
|---|------|--------|-------|
| 1.1 | Game Design Document v1.0 (971 lines, 51 sections) | ✅ Done | `game-design-document.md` |
| 1.2 | Phased development plan (Phase 0–5) | ✅ Done | `phased-development-plan.md` |
| 1.3 | 60-section Unity dev spec → 6 implementation phases | ✅ Done | All phase source written |
| 1.4 | Child-safety audit | ✅ Done | `unity-project/CHILD_SAFETY_AUDIT.md` |
| 1.5 | Crop economy / level pacing review vs GDD | 📋 Planned | Pacing drift flagged, not resolved |

## 2. Core Game Code (Unity, C#)

| # | Task | Status | Notes |
|---|------|--------|-------|
| 2.1 | Phase 1: farming MVP (grid, soil, crops, market, save) | ✅ Done | Source complete, uncompiled at write time |
| 2.2 | Phase 2: missions, achievements, NPCs, story, tutorial | ✅ Done | 14 crops, 8 NPCs, 10 chapters |
| 2.3 | Phase 3: animals, pets, buildings, decor | ✅ Done | 7 animals, barn/house/decor |
| 2.4 | Phase 4: equipment, tractor, irrigation, orchard | ✅ Done | 11 equipment, drivable tractor |
| 2.5 | Phase 5: production, orders, village, bank, fishing, wildlife, seasons, weather | ✅ Done | 8 buildings, 10 recipes |
| 2.6 | Phase 6: parent mode, cloud save, events, ads, analytics | ✅ Done | Parent gate + dashboard, FastAPI server |
| 2.7 | Static review pass (11 real issues fixed) | ✅ Done | 130 files parse, events balanced |

## 3. Unity Compile & Scene Fixes

| # | Task | Status | Notes |
|---|------|--------|-------|
| 3.1 | CharacterController removal (Unity 6 package split) | ✅ Done | Commit `e595bdd` |
| 3.2 | Audio module types (AudioClip/AudioSource) | ✅ Done | Commit `81ef389` |
| 3.3 | Unity 6 built-in packages in manifest (audio/physics/jsonserialize) | ✅ Done | Commit `bd915a0` |
| 3.4 | NUnit test compile errors | ✅ Done | Commit `4ff6e13` |
| 3.5 | Camera namespace collision in SceneSetup | ✅ Done | Commit `c736ca3` |
| 3.6 | Test framework package in manifest | ✅ Done | Commit `f3265eb` |
| 3.7 | Single-scene restructure (Main.unity) | ✅ Done | Commit `b3dea7a` |
| 3.8 | SceneSetup font (Arial.ttf removed in Unity 6) | ✅ Done | Commit `a89d1dd` |
| 3.9 | Camera follow sinking into ground | ✅ Done | Commit `6e7c032` |
| 3.10 | User re-pull + recompile after each fix | ⏳ Pending | Standing workflow: pull → change → push |

## 4. Test Scene — UI

| # | Task | Status | Notes |
|---|------|--------|-------|
| 4.1 | Scene generator (farm grid, player, camera, HUD, plot panel) | ✅ Done | `SceneSetup.cs` |
| 4.2 | All 18 panels built (Market, Inventory, SeedShop, Missions, Orders, Barn, Factory, Village+Bank, Equipment, Orchard, Decor, House, ParentGate+Dashboard, Dialogue, Tutorial, Tractor) | ✅ Done | `SceneSetupPanels.cs`, commit `a02b9d0` |
| 4.3 | Bottom navigation bar (13 buttons → all HUD toggles) | ✅ Done | Same commit |
| 4.4 | Shared row/seed-button prefab assets | ✅ Done | `Assets/Game/UI/Generated/` |
| 4.5 | Bootstrapper execution order −100 | ✅ Done | Fixes service init race |
| 4.6 | User: re-run Create Test Scenes + Play-test every panel | ⏳ Pending | Must be done after pulling `a02b9d0` |

## 5. Test Scene — Living World

| # | Task | Status | Notes |
|---|------|--------|-------|
| 5.1 | House with 4 visible upgrade levels | ✅ Done | `SceneSetupWorld.cs`, commit `6fc7f42` |
| 5.2 | Tractor (tap to drive) | ✅ Done | Same commit |
| 5.3 | 8 NPC villagers with dialogue | ✅ Done | Same commit |
| 5.4 | Animal pen + fence, orchard, pet dog, fishing pond, wildlife | ✅ Done | Same commit |
| 5.5 | Tap-to-interact system (was dead code — nothing raycast IInteractable) | ✅ Done | PlayerController + FarmInteractionController |
| 5.6 | PlayerController self-registers in Awake (pet follow fix) | ✅ Done | Same commit |
| 5.7 | Fishing pond species assigned (carp, bass, catfish, koi) | ✅ Done | Same commit |
| 5.8 | User: re-run Create Test Scenes + walk around, tap villagers/pond/tractor | ⏳ Pending | Must be done after pulling `6fc7f42` |
| 5.9 | WildlifeService registered/wired (currently dead code) | 📋 Planned | MonoBehaviour exists, never registered |

## 6. Missing GDD Screens (no code yet)

| # | Task | Status | Notes |
|---|------|--------|-------|
| 6.1 | Splash / Title screen | 📋 Planned | GDD §47 screen |
| 6.2 | New game / Continue flow + character creator | 📋 Planned | First-session flow entry |
| 6.3 | Pause menu | 📋 Planned | |
| 6.4 | Settings (sound/music toggles) | 📋 Planned | |
| 6.5 | Achievements UI | 📋 Planned | Service exists, no UI |
| 6.6 | Collection book | 📋 Planned | GDD §47 screen |
| 6.7 | Story journal | 📋 Planned | Service exists, no UI |
| 6.8 | Pet screen | 📋 Planned | |
| 6.9 | Land map / World map | 📋 Planned | Expansion UI |
| 6.10 | IAP shop | 📋 Planned | Monetization SKUs defined |
| 6.11 | Events screen | 📋 Planned | LiveEventService exists, no UI |

## 7. Testing & Platform Builds

| # | Task | Status | Notes |
|---|------|--------|-------|
| 7.1 | Unity EditMode tests (Test Runner) | ⏳ Pending | Needs user in Unity |
| 7.2 | Unity PlayMode test + console null-ref check | ⏳ Pending | Needs user in Unity |
| 7.3 | WebGL build + browser test | ⏳ Pending | Build Profiles → Web |
| 7.4 | Android APK build + device test | ⏳ Pending | Android Build Support |
| 7.5 | iOS build (Mac + Xcode) | ⏳ Pending | Unity → Xcode project |
| 7.6 | Device test package (zip) delivered | ✅ Done | Link expires 2026-10-11 |

## 8. Backend & Launch

| # | Task | Status | Notes |
|---|------|--------|-------|
| 8.1 | Cloud server (FastAPI + PostgreSQL) source | ✅ Done | `cloud-server/` |
| 8.2 | Cloud server deployed | 📋 Planned | |
| 8.3 | Ad package setup (Unity Ads) | 📋 Planned | Provider stubbed, setup documented |
| 8.4 | Privacy policy | 📋 Planned | Required for store submission |
| 8.5 | Store submission (Google Play / Apple) | 📋 Planned | |
| 8.6 | Production art, animation, sound, effects | 📋 Planned | Placeholders only today |

## 9. Repo & Workflow

| # | Task | Status | Notes |
|---|------|--------|-------|
| 9.1 | GitHub repo `AliHamzaKhan/Game-Farm`, all files pushed | ✅ Done | Branch `main` |
| 9.2 | Standing rule: pull → change → push for every change | ✅ Done | Recorded 2026-10-09, followed always |
| 9.3 | This task tracker created | ✅ Done | `TASKS.md` |
| 9.4 | This task tracker pushed to repo | ✅ Done | Commit `b4d8819` |

---

### How to update this file
Change the status mark (✅/🔄/⏳/📋) and the Notes column, then commit + push.
One line per task — no essays.
