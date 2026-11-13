#!/usr/bin/env python3
"""Generate original placeholder sprites for the merge game.

Writes three groups of RGBA PNGs:
  Items/<chainId>/<chainId>_<LL>.png  - one per chain level, from seed_content.json emoji
  Icons/*.png                         - HUD / board / customer icons (emoji based)
  UI/*.png                            - white alpha shapes, tinted in Unity

Emoji glyphs come from Noto Color Emoji (SIL OFL 1.1). No other art source is used.
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_SEED = REPO_ROOT / "Assets/MergeGame/Editor/Seed/seed_content.json"
DEFAULT_OUT = REPO_ROOT / "Assets/MergeGame/Content/Sprites"

FONT_PATH = "/usr/share/fonts/truetype/noto/NotoColorEmoji.ttf"
FONT_SIZE = 109  # the only strike size available in the bitmap colour font

EMOJI_CANVAS = 256
EMOJI_FIT = 212
SHADOW_OFFSET = 4
SHADOW_BLUR = 5
SHADOW_ALPHA = 0.35

UI_SIZE = 128
SUPERSAMPLE = 4

# (file stem, emoji, fallback emoji used if the first one doesn't render as one glyph)
ICONS = [
    ("energy", "⚡", None),
    ("coin", "\U0001FA99", None),
    ("cobweb", "\U0001F578️", None),
    ("crate", "\U0001F4E6", None),
    ("check", "✅", None),
    ("lock", "\U0001F512", None),
    ("clock", "⏱️", None),
    ("star", "⭐", None),
    ("home", "\U0001F3E0", None),
    ("anchor", "⚓", None),
    ("customer_01", "\U0001F469‍\U0001F373", "\U0001F469"),  # woman cook -> woman
    ("customer_02", "\U0001F9D1‍\U0001F33E", "\U0001F9D1"),  # farmer -> person
    ("customer_03", "\U0001F475", None),                          # older woman
    ("customer_04", "\U0001F468‍\U0001F9B0", "\U0001F468"),  # man red hair -> man
    ("customer_05", "\U0001F9D2", None),                          # child
    ("customer_06", "\U0001F469‍\U0001F9B1", "\U0001F467"),  # woman curly hair -> girl
]


class GlyphError(RuntimeError):
    pass


# --------------------------------------------------------------------------- emoji

def load_font() -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(FONT_PATH, FONT_SIZE)


def render_glyph(font: ImageFont.FreeTypeFont, text: str) -> Image.Image:
    """Render an emoji string as one glyph run, trimmed to its alpha bounds."""
    left, top, right, bottom = font.getbbox(text)
    single_width = font.getbbox("⭐")[2]
    if right > single_width * 1.5:
        raise GlyphError(f"{text!r} renders as several glyphs (width {right}px)")

    pad = 32
    canvas = Image.new("RGBA", (right + pad * 2, bottom + pad * 2), (0, 0, 0, 0))
    ImageDraw.Draw(canvas).text((pad, pad), text, font=font, embedded_color=True)
    bbox = canvas.getchannel("A").getbbox()
    if bbox is None:
        raise GlyphError(f"{text!r} rendered empty (missing from font?)")
    return canvas.crop(bbox)


def compose_emoji_sprite(glyph: Image.Image) -> Image.Image:
    """Scale glyph into the fit box, centre it, and add a soft drop shadow."""
    scale = EMOJI_FIT / max(glyph.size)
    w, h = max(1, round(glyph.width * scale)), max(1, round(glyph.height * scale))
    glyph = glyph.resize((w, h), Image.LANCZOS)

    x = (EMOJI_CANVAS - w) // 2
    y = (EMOJI_CANVAS - h) // 2 - SHADOW_OFFSET // 2  # leave room for the shadow below

    shadow_alpha = Image.new("L", (EMOJI_CANVAS, EMOJI_CANVAS), 0)
    shadow_alpha.paste(glyph.getchannel("A"), (x, y + SHADOW_OFFSET))
    shadow_alpha = shadow_alpha.filter(ImageFilter.GaussianBlur(SHADOW_BLUR))
    shadow_alpha = shadow_alpha.point(lambda a: round(a * SHADOW_ALPHA))

    sprite = Image.new("RGBA", (EMOJI_CANVAS, EMOJI_CANVAS), (0, 0, 0, 0))
    sprite.putalpha(shadow_alpha)
    sprite.alpha_composite(glyph, (x, y))
    return sprite


def write_items(font, seed_path: Path, out_dir: Path) -> list[Path]:
    seed = json.loads(seed_path.read_text(encoding="utf-8"))
    written = []
    errors = []
    for chain in seed["chains"]:
        chain_id = chain["id"]
        for index, level in enumerate(chain["levels"], start=1):
            try:
                sprite = compose_emoji_sprite(render_glyph(font, level["emoji"]))
            except GlyphError as exc:
                errors.append(f"chain '{chain_id}' level {index} ({level.get('name')}): {exc}")
                continue
            path = out_dir / "Items" / chain_id / f"{chain_id}_{index:02d}.png"
            path.parent.mkdir(parents=True, exist_ok=True)
            sprite.save(path)
            written.append(path)
    if errors:
        raise GlyphError("Unrenderable item emoji:\n  " + "\n  ".join(errors))
    return written


def write_icons(font, out_dir: Path) -> tuple[list[Path], list[str]]:
    written, substitutions = [], []
    for stem, emoji, fallback in ICONS:
        try:
            glyph = render_glyph(font, emoji)
        except GlyphError as exc:
            if fallback is None:
                raise GlyphError(f"icon '{stem}': {exc}") from exc
            glyph = render_glyph(font, fallback)
            substitutions.append(f"{stem}: {emoji!r} -> {fallback!r} ({exc})")
        path = out_dir / "Icons" / f"{stem}.png"
        path.parent.mkdir(parents=True, exist_ok=True)
        compose_emoji_sprite(glyph).save(path)
        written.append(path)
    return written, substitutions


# --------------------------------------------------------------------------- UI shapes

def _big(px: float) -> int:
    return round(px * SUPERSAMPLE)


def _new_mask() -> Image.Image:
    return Image.new("L", (_big(UI_SIZE), _big(UI_SIZE)), 0)


def _rounded_mask(radius: float, inset: float = 0) -> Image.Image:
    mask = _new_mask()
    size = _big(UI_SIZE)
    i = _big(inset)
    ImageDraw.Draw(mask).rounded_rectangle((i, i, size - 1 - i, size - 1 - i), radius=_big(radius), fill=255)
    return mask


def _bottom_band(mask: Image.Image, height: float, brightness: float) -> Image.Image:
    """Greyscale luminance: white everywhere, darker band in the bottom `height` px of the shape."""
    lum = Image.new("L", mask.size, 255)
    bbox = mask.getbbox()
    band_top = bbox[3] - _big(height)
    ImageDraw.Draw(lum).rectangle((0, band_top, mask.width, mask.height), fill=round(255 * brightness))
    return lum


def _finish(mask: Image.Image, lum: Image.Image | None = None, color=(255, 255, 255)) -> Image.Image:
    """Build an RGBA image at supersampled size and downscale to UI_SIZE."""
    if lum is None:
        rgb = Image.new("RGB", mask.size, color)
    else:
        rgb = Image.merge("RGB", (lum, lum, lum))
    rgba = rgb.convert("RGBA")
    rgba.putalpha(mask)
    # premultiply-safe downscale: resize colour and alpha via premultiplied form
    return _downscale_premultiplied(rgba)


def _downscale_premultiplied(img: Image.Image) -> Image.Image:
    target = (UI_SIZE, UI_SIZE)
    premul = img.convert("RGBa").resize(target, Image.LANCZOS)
    return premul.convert("RGBA")


def ui_panel():
    return _finish(_rounded_mask(32))


def ui_cell():
    mask = _rounded_mask(28)
    return _finish(mask, _bottom_band(mask, 6, 0.92))


def ui_button():
    mask = _rounded_mask(40)
    return _finish(mask, _bottom_band(mask, 12, 0.80))


def ui_pill():
    return _finish(_rounded_mask(64))


def ui_circle():
    mask = _new_mask()
    size = _big(UI_SIZE)
    ImageDraw.Draw(mask).ellipse((0, 0, size - 1, size - 1), fill=255)
    return _finish(mask)


def ui_glow():
    size = UI_SIZE * SUPERSAMPLE
    centre = (size - 1) / 2
    mask = Image.new("L", (size, size), 0)
    px = mask.load()
    for yy in range(size):
        for xx in range(size):
            d = ((xx - centre) ** 2 + (yy - centre) ** 2) ** 0.5 / (size / 2)
            t = max(0.0, 1.0 - d)
            px[xx, yy] = round(255 * t * t * (3 - 2 * t))  # smoothstep falloff
    return _finish(mask)


def ui_shadow():
    blur = 7
    mask = _rounded_mask(40, inset=blur * 1.6)
    mask = mask.filter(ImageFilter.GaussianBlur(_big(blur)))
    mask = mask.point(lambda a: round(a * 0.60))
    return _finish(mask, color=(0, 0, 0))


def ui_ring():
    outer = _rounded_mask(28)
    inner = _rounded_mask(28 - 10, inset=10)
    return _finish(ImageChops.subtract(outer, inner))


UI_SHAPES = {
    "panel": ui_panel,
    "cell": ui_cell,
    "button": ui_button,
    "pill": ui_pill,
    "circle": ui_circle,
    "glow": ui_glow,
    "shadow": ui_shadow,
    "ring": ui_ring,
}


def write_ui(out_dir: Path) -> list[Path]:
    written = []
    for stem, build in UI_SHAPES.items():
        path = out_dir / "UI" / f"{stem}.png"
        path.parent.mkdir(parents=True, exist_ok=True)
        build().save(path)
        written.append(path)
    return written


# --------------------------------------------------------------------------- main

def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--seed", type=Path, default=DEFAULT_SEED, help="path to seed_content.json")
    parser.add_argument("--out", type=Path, default=DEFAULT_OUT, help="output Sprites folder")
    args = parser.parse_args(argv)

    font = load_font()
    try:
        items = write_items(font, args.seed, args.out)
        icons, substitutions = write_icons(font, args.out)
    except GlyphError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1
    ui = write_ui(args.out)

    print(f"Items: {len(items)}  Icons: {len(icons)}  UI: {len(ui)}  -> {args.out}")
    for line in substitutions:
        print(f"Substituted {line}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
