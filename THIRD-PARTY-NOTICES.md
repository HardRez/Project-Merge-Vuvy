# Third-Party Notices

This project includes the following third-party material.

## Microsoft Fluent Emoji

- Source: https://github.com/microsoft/fluentui-emoji (3D style PNG renders)
- Licence: MIT
- Used for: item sprites and icons (score, coin, lock, check, home, trash, sparkles, crown, play, customer portraits). The renders are trimmed, scaled, centred and given a drop shadow by `Tools/PlaceholderArt/generate_sprites.py`.
- Files: `Assets/MergeGame/Content/Sprites/Items/**/*.png` and `Assets/MergeGame/Content/Sprites/Icons/*.png` (except `Icons/cobweb.png`, which is drawn by the script).

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
