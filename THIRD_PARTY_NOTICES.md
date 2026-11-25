# Third-party notices

The drop-in package built by GitHub Actions bundles these unmodified third-party components.
Ruina Mobile's own code is MIT-licensed (see `LICENSE`).

| Component | What it does | License | Source |
|---|---|---|---|
| BepInEx 5 (Windows x64) | Plugin loader for Unity Mono games | LGPL-2.1 | https://github.com/BepInEx/BepInEx |
| UnityDoorstop (`winhttp.dll`, part of the BepInEx package) | Starts BepInEx when the game launches | LGPL-2.1 | https://github.com/NeighTools/UnityDoorstop |
| HarmonyX (`0Harmony.dll`, part of the BepInEx package) | Runtime method patching | MIT | https://github.com/BepInEx/HarmonyX |
| MonoMod (part of the BepInEx package) | Runtime detours used by HarmonyX | MIT | https://github.com/MonoMod/MonoMod |

The `nx2android-profile/0100E7A019246000/` folder (not part of the drop-in package) is copied from
[LoR-NX](https://github.com/Vastrilliant/LoR-NX) and is licensed under GPL-3.0 (`nx2android-profile/LICENSE-GPL-3.0.txt`);
its new `android.tsv` uses the same licence.

The package is downloaded from the official BepInEx GitHub releases during each build and is
redistributed as-is; its full source code is available at the links above.

**No Library of Ruina files are included.** Library of Ruina and all of its assets belong to
Project Moon. Ruina Mobile only runs inside a copy of the game that you already own.
