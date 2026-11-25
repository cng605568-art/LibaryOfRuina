# Ruina Mobile

Play **Library of Ruina** on an Android phone from the PC game folder you already have, with controls and performance settings made for touch screens.

Ruina Mobile is a small plugin that runs **inside the real PC game** while an Android PC-game emulator (Winlator, GameHub or GameNative) runs `LibraryOfRuina.exe`. It adds on-screen combat buttons, bigger touch targets for speed dice, a 30/60 FPS mode that keeps the phone cooler, a lower-resolution battle stage with full-resolution UI, GPU presets, and a mod-folder check.

**This repository contains no game files.** Everything is loaded from your own copy of the game.

> **Status (v0.1.0):** the code compiles in GitHub Actions, but it has not yet been tested in-game on a phone. The parts most likely to need tuning on your device are listed under [Troubleshooting](#troubleshooting).

---

## Contents

- [Why there's no standalone launcher APK](#why-theres-no-standalone-launcher-apk)
- [What you get](#what-you-get)
- [Setup, step by step](#setup-step-by-step)
- [Using it](#using-it)
- [Mods](#mods)
- [Save files](#save-files)
- [Troubleshooting](#troubleshooting)
- [Building it yourself](#building-it-yourself)
- [How the original plan maps to this repo](#how-the-original-plan-maps-to-this-repo)

---

## Native route (experimental)

Winlator turned out far too slow on a budget Mali phone (1–6 FPS on a Dimensity 6020). The [`nx2android-profile/`](nx2android-profile/README.md) folder is a second route: a Library of Ruina profile for [nx2android](https://github.com/shirai91/nx2android). nx2android runs the **Switch** version's own ARM64 code natively on the phone, so there's no x86 translation and no Wine. You need your own Switch dump (base game + update 1.0.3) and `prod.keys`. It has not been tested on Android yet; see its README for the steps and the risks.

---

## Why there's no standalone launcher APK

The first plan was an APK that boots the PC game files on a built-in ARM64 Unity runtime. That can't work, for two reasons:

1. **Unity has no 64-bit ARM Mono runtime for Android.** Mono on Android exists only for 32-bit ARMv7, and ARM64 requires IL2CPP. IL2CPP converts game code to native code when the game is built, so it can't load the PC `Assembly-CSharp.dll` when the game starts. Many current phones also can't run 32-bit apps at all.
2. **The PC build's shaders are DirectX 11 only.** That includes Unity's built-in `Sprites/Default` and `UI/Default`, so there's nothing in the game folder an OpenGL ES or Vulkan Unity player could use as a fallback.

An emulator avoids both problems. Box64 runs the x86-64 Windows game, Wine provides Windows, and DXVK translates the game's own DirectX 11 shaders to Vulkan. Ruina Mobile adds the mobile features on top.

---

## What you get

| Feature | What it does |
|---|---|
| **On-screen buttons** | Translucent SPACE and ESC buttons, plus **+ / − / FIT** zoom buttons. You can move them anywhere and change their size, opacity and keys. |
| **Speed-die snap** | Makes the touch area around speed dice bigger, so dragging a page onto an enemy die in 5v5 fights doesn't miss. |
| **Smart FPS** | Runs story, menus and deckbuilding at 30 FPS, then switches to 60 (or your screen's refresh rate) while you touch the screen, the camera moves or effects play. |
| **Split-resolution stage** | Renders the battle stage at 60–100 % resolution while cards, dice numbers and menus stay at full resolution. |
| **GPU presets** | **Auto** detects Adreno, Mali, Xclipse or PowerVR. You can also pick **Performance**, **Balanced**, **Quality** or **Custom** yourself. |
| **Light render settings** | Turns off MSAA, anisotropic filtering, soft particles and reflection probes on weaker GPUs. |
| **Touch inspector** | Shows the name of whatever you tap, so you can fine-tune speed-die snap for your game version. |
| **Mod check** | Lists the mods it finds in the game's mod folders, flags broken ones (missing or misplaced `StageModInfo.xml`), and caches the result in `mod_index_cache.json`. |
| **In-game settings panel** | Large, touch-friendly settings screen. Every change is saved straight away. |

---

## Setup, step by step

### What you need

- An ARM64 Android phone or tablet. Snapdragon (Adreno) phones have the best emulator support. Mali / Xclipse phones work on newer emulator builds.
- **Winlator**, or **GameHub** / **GameNative**. The steps below use Winlator. The other two use the same ideas under different menu names.
- Your **`Library Of Ruina`** PC folder already copied to the phone, containing `LibraryOfRuina.exe` and `LibraryOfRuina_Data`.

### 1. Download the drop-in zip

1. Open this repository on GitHub and go to **Actions → Build Ruina Mobile**.
2. Open the newest run that has a green tick.
3. Under **Artifacts**, download **RuinaMobile-dropin** (you need to be signed in to GitHub).

If a version has been published under **Releases**, you can download `RuinaMobile-dropin.zip` from there instead.

### 2. Put it in your game folder

Extract everything from the zip into your `Library Of Ruina` folder, next to `LibraryOfRuina.exe`. When you're done, the folder should look like this:

```
Library Of Ruina/
├── LibraryOfRuina.exe
├── LibraryOfRuina_Data/
├── MonoBleedingEdge/
├── BepInEx/                 ← new (Ruina Mobile is in BepInEx/plugins/RuinaMobile)
├── winhttp.dll              ← new
├── doorstop_config.ini      ← new
└── RuinaMobile_INSTALL.txt  ← new (short version of this guide)
```

### 3. Set up the emulator (Winlator)

1. **Create a container** with the default Wine version.
2. **Graphics driver**
   - Snapdragon / Adreno: use **Turnip**.
   - Mali / Xclipse: use the Mali option your Winlator build offers. Winlator 11 added an OpenGL wrapper called Gladio for Mali GPUs. Older forks use VirGL.
3. **DirectX wrapper**: try **DXVK** first. If you get a black screen or a crash, switch to **WineD3D**.
4. **Environment variable (required)**: add
   ```
   WINEDLLOVERRIDES=winhttp=n,b
   ```
   to the container's environment variables, or to the game shortcut's. Without it, Wine uses its own `winhttp.dll` and the plugin never starts.
   *Plan B:* open **Wine Configuration (winecfg)** → **Libraries**, add `winhttp`, and set it to **native, builtin**.
5. **Drives**: map the folder that holds `Library Of Ruina` as a drive (for example **D:**).
6. **Screen size**: 1280×720 is a good start. Strong phones can use 1920×1080.
7. **Touch input**: use direct touch, so a tap clicks exactly where your finger is.

**Using GameHub or GameNative instead?** Add `LibraryOfRuina.exe` as a local game, then put the same `WINEDLLOVERRIDES` value in that game's environment-variable settings.

**About Steam:** Library of Ruina is a Steam game. If it closes right away or shows a Steam error, launch it through Steam inside the emulator. GameNative and GameHub can sign in to Steam for you, and Winlator can run the Windows Steam client. Ruina Mobile deliberately doesn't include a Steamworks stub, because that would skip Steam's ownership check.

### 4. First launch

1. Start the container, open drive D:, and run `LibraryOfRuina.exe`. The first launch is slower while BepInEx sets itself up.
2. A small **SET** button appears in the top-left corner.
3. No SET button? Open `BepInEx/LogOutput.log` in the game folder:
   - If the log doesn't exist, BepInEx never started. Re-check step 3.4.
   - If the log exists but doesn't contain `Ruina Mobile loaded`, check that `BepInEx/plugins/RuinaMobile/RuinaMobile.dll` exists.

---

## Using it

### On-screen buttons

| Button | Default | Notes |
|---|---|---|
| **SET** | top-left | Opens settings. Always visible. |
| **SPACE** (big) | right side | Sends the Space key. Can be changed to any key in Controls. |
| **ESC** (small) | right side, higher up | Sends Escape. Can be changed. |
| **+ / −** | left side | Zoom in or out by sending mouse-wheel notches. |
| **FIT** | left side | Puts the camera back where it started in the current scene. It can also send a key. |

To move the buttons, go to **SET → Controls → Move buttons**, drag them into place and tap **DONE**. A touch that starts on one of these buttons never reaches the game underneath.

Pinch-zoom and two-finger taps come from the emulator's own touch gestures. The **+ / −** buttons work regardless, because they send mouse-wheel input to the game directly.

### Graphics presets

| Preset | Battle stage | Render settings | Good for |
|---|---|---|---|
| **Auto** | Adreno or unrecognised GPU: 100 %<br>Mali / Xclipse / PowerVR: 80 % | full / light | Start here |
| **Performance** | 70 % | light | Hot phones, long sessions |
| **Balanced** | 85 % | full | Mid-range phones |
| **Quality** | 100 % | full | Flagships |
| **Custom** | your choice, 60–100 % in 5 % steps | your choice | Fine-tuning |

Some emulator setups report a fake desktop GPU name, such as "NVIDIA GeForce …". The Graphics tab tells you when that happens. Pick a preset yourself in that case.

### Smart FPS

With Smart FPS on, the game runs at **Idle FPS** (30 by default) unless something is happening. It switches to **Combat FPS** (60 by default, or **Native**) when:

- you touch the screen or press a key, or
- the camera moves or zooms (clash camera work), or
- more than 60 particles are alive, or
- the scene name contains one of your `BattleSceneKeywords` (scene names appear in the log as `Scene loaded: …`).

The boost lasts 1.5 seconds after the last trigger. Turn Smart FPS off to let the game control its own frame rate.

### Tuning speed-die snap

By default, the snap enlarges objects whose name contains **`SpeedDice`**. That name is a starting guess, so check it on your game version:

1. Go to **SET → Controls** and turn on **Touch inspector**.
2. Start a battle and tap a speed die. The bottom of the screen shows what was hit, for example `UI  BattleUI/…/SpeedDiceSlot  [Image, …]`.
3. Type a distinctive part of that name into **Name keywords**. You can enter several, separated by commas.
4. Turn the inspector off. The Controls tab shows how many touch pads and colliders were enlarged.

---

## Mods

Library of Ruina loads mods itself. Ruina Mobile only checks that the mod files are where the game looks for them.

1. On your PC, Workshop mods are in `steamapps/workshop/content/1256670/<item id>/`. Copy each item's folder into `Library Of Ruina/LibraryOfRuina_Data/Mods/` on the phone.
2. Turn mods on or off, and set their order, in the game's **Mods** menu on the title screen.
3. Open **SET → Mods** and tap **Rescan**. Each mod shows **OK** or **!!** with the reason, for example "StageModInfo.xml is inside 'X'. Move that inner folder up one level."

The mod check looks in `LibraryOfRuina_Data/Mods`, `<game>/Mods`, `<game>/Workshop`, the Steam Workshop folder (if the game sits in a normal Steam library) and any folders you add under `ExtraModFolders`. Results are cached in `BepInEx/config/RuinaMobile/mod_index_cache.json`.

> Some Workshop mods ship their own copy of Harmony, which can clash with the copy BepInEx ships. If a mod only breaks while BepInEx is installed, that's the likely cause. To test, rename `winhttp.dll` to `winhttp.dll.off`: this turns BepInEx and Ruina Mobile off without deleting anything.

---

## Save files

The game keeps saves in Unity's standard per-user folder inside the emulator. **SET → Info** shows the exact path. To carry over a PC save, copy the same folder from your PC (`%USERPROFILE%\AppData\LocalLow\…`) into that path in the container.

---

## Troubleshooting

| Problem | Fix |
|---|---|
| No SET button and no `BepInEx/LogOutput.log` | The `WINEDLLOVERRIDES=winhttp=n,b` variable is missing, or `winhttp.dll` isn't next to `LibraryOfRuina.exe`. |
| Log exists but no SET button | Check that `BepInEx/plugins/RuinaMobile/RuinaMobile.dll` exists, and look for red errors in the log. |
| Taps land in the wrong place during battles | Set **Stage resolution** to 100 % (or pick **Quality**) and open an issue with your log. |
| Battle stage is upside down | Turn on **Graphics → Flip stage image**. |
| Graphics tab says "Skipped here: camera …" | Stage scaling can't be used safely with that scene's camera setup (another camera draws before the stage or clears the screen after it), so it stays at 100 % there. Nothing is broken. |
| SPACE / ESC buttons do nothing | Look in the log for `Skipped patch Input.GetKeyDown`. As a fallback, map the same keys with your emulator's on-screen controls. |
| + / − do nothing | The game may read the mouse wheel through a method that couldn't be patched (shown as skipped in the log). Use the emulator's scroll gesture instead. |
| FIT does nothing | The game's own camera code overrides it. If the game has a camera-reset key, set it under **FIT also sends**. |
| Speed dice don't get bigger | Use the touch inspector to find the right name keyword (see [Tuning speed-die snap](#tuning-speed-die-snap)). |
| Phone gets hot | Use **Performance**, Idle FPS 20–30 and Combat FPS 45. |
| Black screen or crash at start | Try a different DXVK version, or WineD3D. To check whether the plugin is involved, rename `winhttp.dll` to `winhttp.dll.off` and launch again. |
| Need to see what happened | `BepInEx/LogOutput.log` lists every Ruina Mobile action and every patch it had to skip. |

Settings live in `BepInEx/config/com.cng605568.ruinamobile.cfg`. Delete that file to go back to the defaults.

---

## Building it yourself

GitHub does the building:

- Every push to `main` runs **Actions → Build Ruina Mobile**. The run compiles `RuinaMobile.dll`, downloads the latest official BepInEx 5 x64 release and uploads the **RuinaMobile-dropin** artifact.
- Pushing a tag like `v0.1.0` also publishes `RuinaMobile-dropin.zip` as a GitHub Release.

To build on your own PC (optional), install the .NET 8 SDK and run:

```
dotnet build src/RuinaMobile/RuinaMobile.csproj -c Release
```

The build compiles against reference packages for BepInEx 5, HarmonyX and the Unity 2019.4 engine API, pulled from nuget.org and the BepInEx NuGet feed (see `NuGet.Config`). It never touches `Assembly-CSharp.dll` or any other game file.

### Repository layout

```
LibaryOfRuina/
├── .github/workflows/build.yml     CI: build DLL → add BepInEx → drop-in zip (+ release on tags)
├── dist/INSTALL.txt                short install note copied into the zip
├── NuGet.Config                    nuget.org + BepInEx feed
├── src/RuinaMobile/
│   ├── RuinaMobile.csproj          net46 plugin project
│   ├── Plugin.cs                   BepInEx entry point, patch helper
│   ├── Settings.cs                 every config entry + preset logic
│   ├── GpuProfile.cs               GPU family detection
│   ├── GraphicsController.cs       light render settings
│   ├── StageScaler.cs              split-resolution stage + presenter camera
│   ├── CameraPatches.cs            keeps taps aligned when the stage is scaled
│   ├── SmartFps.cs                 adaptive 30/60 FPS
│   ├── VirtualInput.cs             virtual keys, virtual wheel, HUD touch gate
│   ├── InputPatches.cs             feeds virtual input to Unity Input / EventSystem
│   ├── CameraZoom.cs               FIT button
│   ├── DieSnap.cs                  larger speed-die touch areas
│   ├── Inspector.cs                touch inspector
│   ├── ModIndex.cs                 mod-folder check + JSON cache
│   ├── UiReflection.cs             runtime access to UnityEngine.UI
│   └── UI/ (Hud.cs, SettingsPanel.cs, Styles.cs, HudLayout.cs)
├── LICENSE                         MIT
└── THIRD_PARTY_NOTICES.md          BepInEx / Doorstop / HarmonyX licences
```

---

## How the original plan maps to this repo

| Originally asked for | Where it is now |
|---|---|
| Kotlin / Compose launcher APK, JNI boot bridge, embedded ARM64 Mono | Not possible (see [above](#why-theres-no-standalone-launcher-apk)). The emulator runs the PC build, and settings live in the in-game panel. |
| `EntryPointBootstrapper.cs` | `Plugin.cs`. BepInEx loads it before the first scene. |
| `SteamworksStubPatch.cs` | Left out on purpose. It would skip Steam's ownership check. Launch through Steam instead. |
| `ExternalStorageRedirectPatch.cs` | Not needed. The game already runs from its own folder. |
| `UniversalGraphicsPatch.cs` | `GpuProfile.cs`, `StageScaler.cs` + `CameraPatches.cs`, `SmartFps.cs`, `GraphicsController.cs`. No shader fallback is needed, because DXVK runs the original DirectX 11 shaders. |
| `WorkshopModLoaderPatch.cs` | `ModIndex.cs` (folder check + `mod_index_cache.json`). The game loads mod DLLs itself, so loading them again with `Assembly.LoadFrom()` would run them twice. |
| `TouchInputEnhancerPatch.cs` | `UI/Hud.cs`, `VirtualInput.cs`, `InputPatches.cs`, `DieSnap.cs`, `Inspector.cs`, `CameraZoom.cs`. |
| `build-apk.yml` | `build.yml`. Builds a drop-in zip instead of an APK. |

---

Ruina Mobile's code is MIT-licensed. Library of Ruina belongs to Project Moon. This is an unofficial fan project and isn't affiliated with Project Moon or any emulator project.
