# Sprite Generator

`generate_sprites.py` builds every sprite under `Assets/MergeGame/Content/Sprites/`:

| Group | Path | Size | Source |
|---|---|---|---|
| Items | `Items/<chainId>/<chainId>_<LL>.png` | 256×256 | Microsoft Fluent Emoji (3D), one per chain level, from the `emoji` field in `Assets/MergeGame/Editor/Seed/seed_content.json` |
| Icons | `Icons/*.png` | 256×256 | Fluent Emoji (3D); `cobweb.png` is drawn by the script |
| UI | `UI/*.png` | see table | drawn by the script (including the sand cover variants) |

Items and icons are trimmed, scaled so the longest side is 204 px, centred on a transparent 256×256 canvas and given a soft contact shadow, so every sprite has the same padding.

Output file names are fixed: the committed `.meta` files (GUIDs, and 128×128 sprite rects for the existing UI shapes) depend on them. Unity creates `.meta` files for new outputs on import.

## Run

Needs Python 3.9+ and Pillow (`python -m pip install pillow`). Internet access is needed on the first run; downloads are cached in `Tools/PlaceholderArt/.cache/` (git-ignored).

```bash
# Windows (PowerShell or cmd)
python Tools\PlaceholderArt\generate_sprites.py

# macOS / Linux
python3 Tools/PlaceholderArt/generate_sprites.py
```

Options: `--seed <json>`, `--out <Sprites folder>`, `--cache <folder>`, `--offline` (use the cache only), `--ui-only` (redraw UI shapes, no downloads). Re-running overwrites the PNGs.

### How an emoji is resolved

The Fluent repo stores each emoji in `assets/<Name>/`, where `<Name>` is the CLDR short name with a capital first letter. The script derives `<Name>` from the Unicode character name (`🍎` RED APPLE → `Red apple`); emoji whose CLDR name differs, and all multi-codepoint sequences, are listed in `FLUENT_FOLDER_OVERRIDES`. It then downloads `metadata.json` and checks that its `glyph` is the requested emoji, and takes `3D/<name>_3d.png`, or `Default/3D/<name>_3d_default.png` for people with skin-tone variants.

If anything cannot be resolved, the script exits with code 1 and lists every failing chain/level (or icon). Fix it by adding an override or choosing another emoji in the seed.

## Icons

| File | Emoji | Fluent folder | Use |
|---|---|---|---|
| `star.png` | ⭐ | Star | score |
| `coin.png` | 🪙 | Coin | currency |
| `cobweb.png` | – | drawn by the script | semi-transparent web drawn over a locked item; clipped to a rounded square |
| `lock.png` | 🔒 | Locked | locked state |
| `check.png` | ✅ | Check mark button | order complete |
| `home.png` | 🏠 | House | home / menu |
| `trash.png` | 🗑️ | Wastebasket | discard |
| `sparkles.png` | ✨ | Sparkles | spawn button |
| `crown.png` | 👑 | Crown | max-level badge |
| `play.png` | ▶️ | Play button | menu |
| `customer_01.png` | 👱‍♀️ | Woman blonde hair (Default) | customer |
| `customer_02.png` | 🧔‍♂️ | Man beard (Default) | customer |
| `customer_03.png` | 👵 | Old woman (Default) | customer |
| `customer_04.png` | 👨‍🦰 | Man red hair (Default) | customer |
| `customer_05.png` | 👧 | Girl (Default) | customer |
| `customer_06.png` | 👩‍🦱 | Woman curly hair (Default) | customer |

## UI shapes

All UI shapes except `background.png` are white/grey with alpha and are meant to be tinted in Unity (tint multiplies, so white = full tint colour, grey = shaded tint). Tactile shapes have a darker bottom "depth" band and a soft top gloss; the gloss sits inside the top 9-slice border and the depth band inside the bottom border, so stretching keeps them crisp. Everything is drawn at 4× and downscaled in premultiplied alpha.

9-slice borders are in pixels, in Unity's order **(left, bottom, right, top)** = `spriteBorder (x, y, z, w)`:

| File | Size | Border L, B, R, T | Notes |
|---|---|---|---|
| `panel.png` | 128×128 | 40, 44, 40, 40 | radius 36, subtle 7 px depth edge |
| `button.png` | 128×128 | 44, 54, 44, 40 | radius 40, 13 px depth edge, top gloss; looks pressable in any tint |
| `pill.png` | 128×128 | 62, 68, 62, 60 | fully rounded; borders cover the full height, so stretch it horizontally only (use `pixelsPerUnitMultiplier` to make it shorter) |
| `cell.png` | 128×128 | 32, 32, 32, 32 | board cell, soft inner shadow on top edge |
| `circle.png` | 128×128 | 0, 0, 0, 0 | simple sprite, slight top-to-bottom shading |
| `glow.png` | 128×128 | 0, 0, 0, 0 | radial glow, simple sprite |
| `shadow.png` | 128×128 | 50, 50, 50, 50 | black soft drop shadow for panels |
| `ring.png` | 128×128 | 34, 34, 34, 34 | 10 px rounded outline (selection / highlight) |
| `board_frame.png` | 128×128 | 40, 40, 40, 40 | 14 px rounded frame with bevel; centre is transparent |
| `badge.png` | 64×64 | 24, 26, 24, 22 | small tactile badge (counts, level) |
| `speech_bubble.png` | 128×128 | 52, 60, 36, 36 | bubble with a tail at bottom-left pointing down-left; tail lies inside the bottom-left corner slice |
| `background.png` | 512×1024 | 0, 0, 0, 0 | opaque sky-to-sand gradient with faint dots; not tinted, stretch to fill the screen |
| `sand_01.png` … `sand_03.png` | 256×256 | 64, 64, 64, 64 | a closed board cell buried in sand: full colour (not tinted), same footprint and corner radius as `cell.png` at 2× (use half the cell's corner scale), sunk in by an inner rim shadow; ripples, grain and pebbles differ per variant so neighbouring cells do not look stamped |
| `sand_surface.png` | 256×96 | 0, 0, 0, 0 | the uneven top of sand draining out of an opened cell: transparent above a gently sloped, wavy sand line (about 40% from the top) with a lit crest and a soft shadow, opaque sand below that matches the light top of the sand tiles; not tinted, stretched to the cell width |

## Licences

- Item and icon art (except `cobweb.png`): **Microsoft Fluent Emoji**, © Microsoft Corporation, MIT licence, https://github.com/microsoft/fluentui-emoji. The images are scaled and given a drop shadow.
- `cobweb.png` and all UI shapes: drawn by this script, part of this project.

See `THIRD-PARTY-NOTICES.md` at the repository root for the full licence texts.
