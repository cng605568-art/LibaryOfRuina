# Library of Ruina, native on Android (experimental)

This folder lets [nx2android](https://github.com/shirai91/nx2android) build a native Android version of Library of Ruina from **your own Switch copy**. The Switch game's own ARM64 code runs directly on the phone's CPU and its graphics are replayed on Vulkan, so there is no x86 translation and no Wine. On a weak phone that should be much faster than Winlator.

**Status: not yet run on Android.** The game profile comes from [LoR-NX](https://github.com/Vastrilliant/LoR-NX). It plays on a Mac and was reported working on an iPhone. The only new file is `android.tsv`.

## Why it might work

- **The engine is already proven on Android.** Library of Ruina's Switch version is Unity 2021.3 on SDK 15.3.2, using the same `sdk` and `subsdk0` builds as **Blasphemous 2**. nx2android lists Blasphemous 2 as playable on Android at 60 fps.
- **Mali isn't blocked in code.** nx2android's installer refuses Mali only for games whose profile says `requires bcn`. When a GPU can't read BC-compressed textures, its Vulkan layer decodes them on the CPU instead (`gfx_vk_tex.c` `bc_decode`). This profile has no `requires bcn` row, so the installer lets a Mali phone through.

## What could stop it

- **Mali support is unproven.** nx2android's guide says Mali phones can't run its games, and the only Mali report so far (a Pixel 9 Pro) showed rendering glitches.
- **Memory.** Textures decoded on the CPU take 4 to 8 times the memory. Set the game's texture quality to **Low**.
- **Weak GPU.** The game is limited by GPU speed even on an iPhone, so this profile starts in handheld mode (1280×720) and holds 30 fps.

## What you need

- Your Library of Ruina Switch dump: the base game **and update 1.0.3** (v262144), in one folder. The profile's code fixes only match that update.
- `prod.keys` from the same Switch. Neither the keys nor game files are provided here, and I can't help get them.
- A Windows PC with about 30 GB free, a USB cable, and USB debugging on the phone. On vivo phones, go to **About phone → Software information** and tap **Software version** seven times to unlock Developer options.

## Steps

1. Download `nx2android-v0.4.0-windows-x64.zip` from [nx2android's releases](https://github.com/shirai91/nx2android/releases) and unzip it to `C:\nx2android`.
2. Copy the `0100E7A019246000` folder from here into `C:\nx2android\nxnative\dm\games\`.
3. Start `nx2android.exe`. On the **Setup** tab, click **Download missing tools**, set **prod.keys**, then click **Save and check**. Every line should show a green ✓.
4. On the **Port** tab, choose the folder with your Library of Ruina files. The card should name Library of Ruina as **experimental**.
5. Plug in the phone, tick **Install on the device when done**, and click **Port**. The first port takes a long time, because the game has about 16,800 shaders to translate.
6. Open Library of Ruina on the phone. In the game's settings, set texture quality to **Low**.

## If it fails

Copy the red lines from the bottom of the Port tab, or the app's `log.txt` from the phone, and send them over. They show whether the problem is the port itself, Vulkan on Mali, or memory.

## Credits and licence

- `0100E7A019246000/` except `android.tsv`: copied unchanged from [LoR-NX](https://github.com/Vastrilliant/LoR-NX) (commit `d3c38d0`), licensed under **GPL-3.0** (see `LICENSE-GPL-3.0.txt`). LoR-NX builds on [nxnative](https://github.com/seedds/nxnative) (MIT).
- `android.tsv` is new, follows nx2android's profile format, and is released under the same GPL-3.0.
- This folder contains no game files and no keys. A port you build contains the whole game, so it's for your own device only. Don't share the APK or its data folder.
