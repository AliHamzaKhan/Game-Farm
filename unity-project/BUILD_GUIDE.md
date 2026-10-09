# Farm Quest — Build Guide (Android / iOS / WebGL)

How to go from this source to a running game on your devices.

## 1. Prerequisites

- **Unity 6** — version `6000.0.23f1` (see `ProjectSettings/ProjectVersion.txt`).
  Install via **Unity Hub**. Add modules:
  - ✅ Android Build Support (+ Android SDK & NDK via Hub, OpenJDK)
  - ✅ iOS Build Support (**building for iOS requires a Mac with Xcode** —
    Unity on Windows cannot produce an installable iOS app)
  - ✅ WebGL Build Support
- This project folder, opened as a Unity project.

> First open takes a while (Unity imports everything). That's normal.

## 2. One-time setup inside Unity

1. **Generate game data:** menu **FarmQuest → Generate → All Game Data**.
   Creates `Assets/Resources/Data/` (crops, items, animals, machines, events…).
2. **Create test scenes:** menu **FarmQuest → Setup → Create Test Scenes**.
   Creates `Assets/Scenes/Main.unity` (boot + farm in one scene) and adds
   it to Build Settings.
3. Open `Assets/Scenes/Main.unity` and press **Play**.
   - You should see a grass field of tiles, a HUD (🪙 coins, level, clock),
     and a farmer. **Tap a tile** → the plot panel opens →
     Clear → Dig → Prepare → Plant → Water → watch it grow → tap to harvest.
   - If you see a red boot error about missing data, you skipped step 1.

## 3. Player settings (all platforms)

`Edit → Project Settings → Player`:
- **Product Name:** `Farm Quest`
- **Version:** `0.1.0`
- Android tab → **Other Settings → Package Name:** `com.ahkstudios.farmquest`
  (also set **Minimum API Level** ≥ 24)
- iPhone tab → **Other Settings → Bundle Identifier:** `com.ahkstudios.farmquest`
- WebGL tab → **Resolution and Presentation** is fine at defaults.

## 4. Android

1. `File → Build Settings…` → select **Android** → **Switch Platform**.
2. ✅ **Development Build** for testing (faster builds, shows errors on device).
3. **Build** → produces an `.apk` (or **Build App Bundle** for Play Store later).
4. Install on your phone via USB (`adb install`) or copy the APK over.
5. Saves live in the app's private storage; cloud save/analytics are optional
   and off unless you configure the server (see `cloud-server/README.md`).

## 5. iOS (Mac + Xcode required)

1. On your **Mac**: `File → Build Settings…` → **iOS** → **Switch Platform** → **Build**.
   Unity produces an **Xcode project folder**, not an installable app.
2. Open the `.xcodeproj` in **Xcode**, select your **Team** (signing),
   plug in your iPhone, pick it as the run destination, and press **Run**.
3. For TestFlight/App Store later: Product → Archive in Xcode.

> Note: the rewarded-ads code path is stubbed until you install the Unity Ads
> package (steps are in `Assets/Game/Systems/Ads/UnityAdsProvider.cs`). The
> game is fully playable without ads.

## 6. WebGL

1. `File → Build Settings…` → **WebGL** → **Switch Platform** → **Build**.
2. Host the `Build/` folder output. Easiest options:
   - **itch.io** (free, upload the zip, set "HTML" project type), or
   - any static host (`npx serve`, Netlify, GitHub Pages).
3. Notes:
   - Saves use the browser's IndexedDB — progress persists per browser/site.
   - Serve over **HTTPS** if you enable the cloud server features.
   - First load is slow (Unity engine download); that's normal.

## 7. What to expect (honest)

This is a **playable code slice**, not a finished game:
- ✅ Full farm loop works: clear/dig/prepare/plant/water/harvest, coins, XP/levels,
  day/night clock, offline progress, saves.
- 🟨 **Programmer art**: tiles are colored quads, crops are green cubes that grow
  with stage. Final art replaces these (see `FarmTileView`).
- 🟨 Only the **plot panel + HUD** have real UI. Shop/mission/barn/etc. panels
  exist as code and need their Unity UI layouts built (checklist in `README.md`).
- 🟨 Animals, tractor, machines, fishing, and events are coded and saved, but
  their scene prefabs/views aren't placed yet.

## 8. Troubleshooting

| Symptom | Fix |
|---|---|
| Red error: data assets missing | Run **FarmQuest → Generate → All Game Data** |
| Black screen on Play | Open the **Main** scene and press Play |
| Tiles not tappable | Check `FarmInteractionController.tileLayer` matches the builder's layer (8) |
| UI buttons don't respond | The scene needs an **EventSystem** (the setup script adds one) |
| Android build fails on SDK | In Hub, install **Android SDK & NDK Tools** for this Unity version |
