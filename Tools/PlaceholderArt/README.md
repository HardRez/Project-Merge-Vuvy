# Placeholder Art Generator

`generate_sprites.py` produces original placeholder sprites for the merge game:

- `Items/<chainId>/<chainId>_<LL>.png` – one 256×256 sprite per chain level, built from the `emoji` field in `seed_content.json` (centred in a 212 px box, soft drop shadow).
- `Icons/*.png` – 256×256 HUD/board icons and customer avatars, same emoji pipeline.
- `UI/*.png` – 128×128 white shapes with alpha (panel, cell, button, pill, circle, glow, shadow, ring), drawn at 4× and downscaled; tint them in Unity.

If an emoji is missing from the font, the script stops and names the chain/level so it can be replaced in the seed.

## Font licence

Emoji glyphs are rendered from **Noto Color Emoji**, licensed under the SIL Open Font License 1.1. That licence allows rendering glyphs into images and shipping them. No other art source is used.

## Run

Needs Python 3, Pillow (with libraqm, for ZWJ sequences) and the font at
`/usr/share/fonts/truetype/noto/NotoColorEmoji.ttf`.

```bash
python3 Tools/PlaceholderArt/generate_sprites.py
# or with explicit paths
python3 Tools/PlaceholderArt/generate_sprites.py \
  --seed Assets/MergeGame/Editor/Seed/seed_content.json \
  --out  Assets/MergeGame/Content/Sprites
```

Re-running overwrites existing PNGs. Unity creates the `.meta` files on import.
