# Third-Party Notices

This project includes the following third-party material.

## Microsoft Fluent Emoji

- Source: https://github.com/microsoft/fluentui-emoji (3D style PNG renders)
- Licence: MIT
- Used for: icons (score, coin, lock, home, trash, sparkles, crown, play, customer avatars). The renders are trimmed, scaled, centred and given a drop shadow by `Tools/PlaceholderArt/generate_sprites.py`. The script can also render item sprites for chain levels that name an `emoji` in the seed; no shipped chain does.
- Files: `Assets/MergeGame/Content/Sprites/Icons/*.png` (except `Icons/cobweb.png` and `Icons/check.png`, which are drawn by the script).

```
MIT License

Copyright (c) Microsoft Corporation.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE
```

## Baloo 2 (font)

- Source: https://github.com/google/fonts/tree/main/ofl/baloo2 (upstream: https://github.com/EkType/Baloo2)
- Copyright 2019 The Baloo 2 Project Authors
- Licence: SIL Open Font License, Version 1.1 (no Reserved Font Name). Full text: `Assets/MergeGame/Content/Fonts/Baloo2-OFL.txt`.
- Used for: all UI text (TextMesh Pro font asset is built from it in Unity).
- File: `Assets/MergeGame/Content/Fonts/Baloo2-ExtraBold.ttf` — a static ExtraBold (wght 800) instance generated from the variable `Baloo2[wght].ttf` with `fontTools.varLib.instancer`; glyph outlines are otherwise unchanged.

## Spine Runtimes (spine-unity 3.8)

- Source: https://github.com/EsotericSoftware/spine-runtimes, branch `3.8` (spine-csharp and spine-unity), commit 8b4844bd4b193ba9e54487ed397a777993cbad56
- Copyright (c) 2013-2019, Esoteric Software LLC
- Licence: Spine Runtimes License Agreement. Using the runtime requires a Spine Editor licence for each user. Full text: `Packages/com.esotericsoftware.spine.spine-unity/LICENSE.md`.
- Used for: playing the animated customer portraits (`SkeletonGraphic`).
- Files: `Packages/com.esotericsoftware.spine.spine-unity/**`. Modified for Unity 6.6: the APIs it rejects (`Object.GetInstanceID`, `EditorUtility.InstanceIDToObject`, `EditorApplication.hierarchyWindowItemOnGUI`) are replaced with their `EntityId` counterparts, and the Spine settings asset lives at `Assets/MergeGame/Editor/SpineSettings.asset` instead of `Assets/Editor`. Every change is listed in `Packages/com.esotericsoftware.spine.spine-unity/UNITY6_PATCHES.md`.

## Customer characters

- Source: supplied by the project owner, who holds the licence to use them.
- Used for: the 13 animated customers on the order cards (Spine 3.8.99 exports: skeleton, atlas and texture).
- Files: `Assets/MergeGame/Content/Characters/**`.

## Licensed asset pack (item art, board tray, selection brackets, sound)

- Source: an asset pack supplied by the project owner, who holds the licence to use it. The pack is not part of the repository.
- Used for: the item sprites of all four chains, the blue board tray and the colours of its tiles, the corner brackets around the selected cell, the sparkle stars over a highlighted item, the ring that spreads out from a merge, the sound effects and the background music.
- Files: `Assets/MergeGame/Content/Sprites/Items/**/*.png`, `Assets/MergeGame/Content/Sprites/UI/selection_brackets.png`, `Assets/MergeGame/Content/Sprites/UI/board_tray.png`, `Assets/MergeGame/Content/Sprites/UI/sparkle_star.png`, `Assets/MergeGame/Content/Sprites/UI/merge_ring.png` and `Assets/MergeGame/Content/Audio/*.ogg`. They are copied from the pack by `Tools/PackArt/import_pack_art.py`, which lists every source file; item art is trimmed, scaled, centred on a 256×256 canvas and given a contact shadow, audio is copied unchanged.
