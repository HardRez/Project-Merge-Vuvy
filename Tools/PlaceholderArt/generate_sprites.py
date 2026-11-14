#!/usr/bin/env python3
"""Generate the merge game's sprites.

Writes three groups of RGBA PNGs under Assets/MergeGame/Content/Sprites:
  Items/<chainId>/<chainId>_<LL>.png  - one per chain level, from seed_content.json `emoji`
  Icons/*.png                         - HUD / board / customer icons
  UI/*.png                            - white tintable shapes + an opaque background

Item and icon art is Microsoft Fluent Emoji (3D style, MIT licence), downloaded
from raw.githubusercontent.com and cached locally. UI shapes are drawn here.

Runs on Windows, macOS and Linux with Python 3.9+ and Pillow.
"""

from __future__ import annotations

import argparse
import json
import math
import random
import sys
import unicodedata
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter

REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_SEED = REPO_ROOT / "Assets" / "MergeGame" / "Editor" / "Seed" / "seed_content.json"
DEFAULT_OUT = REPO_ROOT / "Assets" / "MergeGame" / "Content" / "Sprites"
DEFAULT_CACHE = Path(__file__).resolve().parent / ".cache"  # git-ignored

FLUENT_RAW = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/main/assets/"
USER_AGENT = "merge-game-sprite-generator/1.0"

VS16 = "️"  # emoji presentation selector; Fluent metadata is inconsistent about it

# Emoji whose Fluent folder name differs from the Unicode character name
# (Fluent uses CLDR short names), plus every multi-codepoint sequence.
FLUENT_FOLDER_OVERRIDES = {
    "⭐": "Star",                          # WHITE MEDIUM STAR
    "\U0001F512": "Locked",                    # LOCK
    "✅": "Check mark button",             # WHITE HEAVY CHECK MARK
    "\U0001F3E0": "House",                     # HOUSE BUILDING
    "▶": "Play button",                   # BLACK RIGHT-POINTING TRIANGLE
    "\U0001F33E": "Sheaf of rice",             # EAR OF RICE
    "\U0001F475": "Old woman",                 # OLDER WOMAN
    "\U0001F3B0": "Shortcake",
    "\U0001F471‍♀": "Woman blonde hair",
    "\U0001F9D4‍♂": "Man beard",
    "\U0001F468‍\U0001F9B0": "Man red hair",
    "\U0001F469‍\U0001F9B1": "Woman curly hair",
    "\U0001F468‍\U0001F9B3": "Man white hair",
}

# Canvas layout for items and icons (256 x 256).
SPRITE_SIZE = 256
SPRITE_FIT = 204          # longest side of the trimmed art
SPRITE_CENTRE_Y = 122     # art is centred slightly above middle to leave room for the shadow
CONTACT_SHADOW_ALPHA = 0.30
CONTACT_SHADOW_BLUR = 6

# (file stem, emoji). Customers use the "Default" (yellow) skin tone variant.
ICONS = [
    ("star", "⭐"),
    ("coin", "\U0001FA99"),
    ("lock", "\U0001F512"),
    ("check", "✅"),
    ("home", "\U0001F3E0"),
    ("trash", "\U0001F5D1"),
    ("sparkles", "✨"),
    ("crown", "\U0001F451"),
    ("play", "▶"),
    ("customer_01", "\U0001F471‍♀️"),  # woman, blonde hair
    ("customer_02", "\U0001F9D4‍♂️"),  # man, beard
    ("customer_03", "\U0001F475"),                    # old woman
    ("customer_04", "\U0001F468‍\U0001F9B0"),    # man, red hair
    ("customer_05", "\U0001F467"),                    # girl
    ("customer_06", "\U0001F469‍\U0001F9B1"),    # woman, curly hair
]
# `cobweb.png` is drawn procedurally (see draw_cobweb) so it works as an overlay.

UI_SIZE = 128             # existing UI sprites are 128 x 128; their .meta sprite rects depend on it
SUPERSAMPLE = 4


class ResolveError(RuntimeError):
    pass


# --------------------------------------------------------------------------- fetching

class FluentSource:
    def __init__(self, cache_dir: Path, offline: bool = False):
        self.cache_dir = cache_dir
        self.offline = offline

    def _get(self, rel_path: str) -> bytes | None:
        """Fetch a file from the Fluent repo `assets/` folder (cached). Returns None on 404."""
        cached = self.cache_dir / rel_path
        missing_marker = cached.with_name(cached.name + ".404")
        if cached.exists():
            return cached.read_bytes()
        if missing_marker.exists():
            return None
        if self.offline:
            raise ResolveError(f"'{rel_path}' is not cached and --offline was given")
        url = FLUENT_RAW + urllib.parse.quote(rel_path)
        request = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
        try:
            with urllib.request.urlopen(request, timeout=30) as response:
                data = response.read()
        except urllib.error.HTTPError as exc:
            if exc.code == 404:
                missing_marker.parent.mkdir(parents=True, exist_ok=True)
                missing_marker.write_bytes(b"")
                return None
            raise ResolveError(f"HTTP {exc.code} for {url}") from exc
        except urllib.error.URLError as exc:
            raise ResolveError(f"cannot reach {url}: {exc.reason}") from exc
        cached.parent.mkdir(parents=True, exist_ok=True)
        cached.write_bytes(data)
        return data

    @staticmethod
    def folder_for(emoji: str) -> str:
        key = emoji.replace(VS16, "")
        if key in FLUENT_FOLDER_OVERRIDES:
            return FLUENT_FOLDER_OVERRIDES[key]
        if len(key) != 1:
            raise ResolveError(f"{emoji!r} is a sequence; add it to FLUENT_FOLDER_OVERRIDES")
        try:
            name = unicodedata.name(key)
        except ValueError as exc:
            raise ResolveError(f"{emoji!r} has no Unicode name") from exc
        name = name.lower()
        return name[0].upper() + name[1:]

    def load(self, emoji: str) -> Image.Image:
        folder = self.folder_for(emoji)
        meta_raw = self._get(f"{folder}/metadata.json")
        if meta_raw is None:
            raise ResolveError(f"{emoji!r}: no Fluent folder 'assets/{folder}/' (add an override)")
        meta = json.loads(meta_raw.decode("utf-8"))
        if meta.get("glyph", "").replace(VS16, "") != emoji.replace(VS16, ""):
            raise ResolveError(
                f"{emoji!r}: folder 'assets/{folder}/' is for {meta.get('glyph')!r}, not this emoji")
        snake = folder.lower().replace(" ", "_")
        for rel in (f"{folder}/3D/{snake}_3d.png", f"{folder}/Default/3D/{snake}_3d_default.png"):
            data = self._get(rel)
            if data is not None:
                cached = self.cache_dir / rel
                return Image.open(cached).convert("RGBA")
        raise ResolveError(f"{emoji!r}: no 3D render in 'assets/{folder}/'")


# --------------------------------------------------------------------------- items / icons

def compose_sprite(art: Image.Image) -> Image.Image:
    """Trim, fit into SPRITE_FIT, centre on a 256 canvas and add a soft contact shadow."""
    bbox = art.getchannel("A").point(lambda a: 255 if a > 8 else 0).getbbox()
    if bbox is None:
        raise ResolveError("render is empty")
    art = art.crop(bbox)
    scale = SPRITE_FIT / max(art.size)
    w, h = max(1, round(art.width * scale)), max(1, round(art.height * scale))
    art = art.convert("RGBa").resize((w, h), Image.LANCZOS).convert("RGBA")

    x = (SPRITE_SIZE - w) // 2
    y = round(SPRITE_CENTRE_Y - h / 2)

    # Contact shadow: a flattened ellipse under the art's footprint.
    ss = SUPERSAMPLE
    shadow = Image.new("L", (SPRITE_SIZE * ss, SPRITE_SIZE * ss), 0)
    sw = w * 0.72
    sh = max(12.0, sw * 0.16)
    cx, cy = SPRITE_SIZE / 2, y + h - sh * 0.30
    ImageDraw.Draw(shadow).ellipse(
        ((cx - sw / 2) * ss, (cy - sh / 2) * ss, (cx + sw / 2) * ss, (cy + sh / 2) * ss), fill=255)
    shadow = shadow.resize((SPRITE_SIZE, SPRITE_SIZE), Image.LANCZOS)
    shadow = shadow.filter(ImageFilter.GaussianBlur(CONTACT_SHADOW_BLUR))
    shadow = shadow.point(lambda a: round(a * CONTACT_SHADOW_ALPHA))

    sprite = Image.new("RGBA", (SPRITE_SIZE, SPRITE_SIZE), (40, 30, 20, 0))
    sprite.putalpha(shadow)
    sprite.alpha_composite(art, (x, y))
    return sprite


def write_items(source: FluentSource, seed_path: Path, out_dir: Path) -> list[Path]:
    seed = json.loads(seed_path.read_text(encoding="utf-8"))
    written, errors = [], []
    for chain in seed["chains"]:
        chain_id = chain["id"]
        for index, level in enumerate(chain["levels"], start=1):
            try:
                sprite = compose_sprite(source.load(level["emoji"]))
            except ResolveError as exc:
                errors.append(f"chain '{chain_id}' level {index} ({level.get('name')}): {exc}")
                continue
            path = out_dir / "Items" / chain_id / f"{chain_id}_{index:02d}.png"
            path.parent.mkdir(parents=True, exist_ok=True)
            sprite.save(path)
            written.append(path)
    if errors:
        raise ResolveError("Unresolvable item emoji:\n  " + "\n  ".join(errors))
    return written


def draw_cobweb() -> Image.Image:
    """Semi-transparent white web, drawn over a locked item. Lines only, so the item stays visible."""
    ss = SUPERSAMPLE
    size = SPRITE_SIZE * ss
    c = size / 2
    spokes = 12
    rings = [0.16, 0.30, 0.45, 0.61, 0.78, 0.97]
    radius = size * 0.5
    angles = [2 * math.pi * i / spokes + 0.13 for i in range(spokes)]

    lines: list[list[tuple[float, float]]] = []
    for a in angles:  # spokes run to the corners/edges of the cell
        reach = radius * 1.42
        lines.append([(c, c), (c + math.cos(a) * reach, c + math.sin(a) * reach)])
    for r in rings:  # threads sag towards the centre between spokes
        rr = radius * r
        for i in range(spokes):
            a0, a1 = angles[i], angles[(i + 1) % spokes] + (2 * math.pi if i == spokes - 1 else 0)
            pts = []
            for k in range(13):
                t = k / 12
                a = a0 + (a1 - a0) * t
                sag = 1 - 0.13 * math.sin(math.pi * t)
                pts.append((c + math.cos(a) * rr * sag, c + math.sin(a) * rr * sag))
            lines.append(pts)

    def stroke(width: float) -> Image.Image:
        m = Image.new("L", (size, size), 0)
        d = ImageDraw.Draw(m)
        for pts in lines:
            d.line(pts, fill=255, width=round(width * ss), joint="curve")
        return m

    dark = stroke(5.0).filter(ImageFilter.GaussianBlur(2 * ss))   # soft dark halo for legibility
    light = stroke(2.2)

    # Fade the web towards the centre a little so the item stays readable.
    fade = Image.radial_gradient("L").resize((size, size))           # 0 centre -> 255 edge
    fade = fade.point(lambda v: round(150 + 105 * min(1.0, v / 160)))

    halo_a = ImageChops.multiply(dark, fade).point(lambda a: round(a * 0.35))
    line_a = ImageChops.multiply(light, fade).point(lambda a: round(a * 0.92))

    haze = Image.radial_gradient("L").resize((size, size)).point(lambda v: round(min(255, v) * 0.22))

    # Keep everything inside a rounded square so it sits within a rounded board cell.
    clip = Image.new("L", (size, size), 0)
    ImageDraw.Draw(clip).rounded_rectangle((6 * ss, 6 * ss, size - 6 * ss, size - 6 * ss), radius=48 * ss, fill=255)
    clip = clip.filter(ImageFilter.GaussianBlur(3 * ss))
    halo_a, line_a, haze = (ImageChops.multiply(m, clip) for m in (halo_a, line_a, haze))

    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    halo = Image.new("RGBA", (size, size), (60, 60, 80, 0))
    halo.putalpha(halo_a)
    mist = Image.new("RGBA", (size, size), (235, 238, 245, 0))
    mist.putalpha(haze)
    web = Image.new("RGBA", (size, size), (250, 250, 255, 0))
    web.putalpha(line_a)
    img.alpha_composite(mist)
    img.alpha_composite(halo)
    img.alpha_composite(web)
    return img.convert("RGBa").resize((SPRITE_SIZE, SPRITE_SIZE), Image.LANCZOS).convert("RGBA")


def write_icons(source: FluentSource, out_dir: Path) -> list[Path]:
    written, errors = [], []
    icon_dir = out_dir / "Icons"
    icon_dir.mkdir(parents=True, exist_ok=True)
    for stem, emoji in ICONS:
        try:
            sprite = compose_sprite(source.load(emoji))
        except ResolveError as exc:
            errors.append(f"icon '{stem}': {exc}")
            continue
        path = icon_dir / f"{stem}.png"
        sprite.save(path)
        written.append(path)
    if errors:
        raise ResolveError("Unresolvable icon emoji:\n  " + "\n  ".join(errors))
    path = icon_dir / "cobweb.png"
    draw_cobweb().save(path)
    written.append(path)
    return written


# --------------------------------------------------------------------------- UI shapes
#
# All UI shapes are white/grey with alpha; Unity tints them by multiplying, so
# 255 = full tint colour and darker greys = shaded tint. Drawn at SUPERSAMPLE x
# and downscaled in premultiplied space for clean edges.

def _rr(size: tuple[int, int], box: tuple[float, float, float, float], radius: float) -> Image.Image:
    """Supersampled rounded-rect mask; box and radius in final pixels."""
    ss = SUPERSAMPLE
    m = Image.new("L", (size[0] * ss, size[1] * ss), 0)
    x0, y0, x1, y1 = box
    ImageDraw.Draw(m).rounded_rectangle(
        (round(x0 * ss), round(y0 * ss), round(x1 * ss) - 1, round(y1 * ss) - 1), radius=round(radius * ss), fill=255)
    return m


def _vgrad(size: tuple[int, int], top: float, bottom: float, y0: float = 0, y1: float | None = None) -> Image.Image:
    """Vertical luminance gradient (0..1 values) between rows y0 and y1 (final pixels)."""
    ss = SUPERSAMPLE
    w, h = size[0] * ss, size[1] * ss
    y1 = size[1] if y1 is None else y1
    col = Image.new("L", (1, h))
    for yy in range(h):
        t = min(1.0, max(0.0, (yy / ss - y0) / max(1e-6, (y1 - y0))))
        col.putpixel((0, yy), round(255 * (top + (bottom - top) * t)))
    return col.resize((w, h))


def _const(size: tuple[int, int], value: float) -> Image.Image:
    return Image.new("L", (size[0] * SUPERSAMPLE, size[1] * SUPERSAMPLE), round(255 * value))


def _over(base: Image.Image, top: Image.Image, mask: Image.Image) -> Image.Image:
    return Image.composite(top, base, mask)


def _finish(size: tuple[int, int], lum: Image.Image, alpha: Image.Image, tint=(1.0, 1.0, 1.0)) -> Image.Image:
    rgb = [lum.point(lambda v, k=k: round(v * k)) for k in tint]
    img = Image.merge("RGBA", (*rgb, alpha))
    return img.convert("RGBa").resize(size, Image.LANCZOS).convert("RGBA")


def _blur(mask: Image.Image, px: float) -> Image.Image:
    return mask.filter(ImageFilter.GaussianBlur(px * SUPERSAMPLE))


def _shift(mask: Image.Image, dx: float, dy: float) -> Image.Image:
    return ImageChops.offset(mask, round(dx * SUPERSAMPLE), round(dy * SUPERSAMPLE))


def tactile(size, radius, depth, face_top, face_bottom, edge, highlight, highlight_band, outline=0.0):
    """Rounded body with a darker bottom 'depth' edge, a graded face and a soft top highlight.

    depth          - height in px of the darker band visible under the face
    face_top/bottom- face luminance at top/bottom
    edge           - luminance of the depth band
    highlight      - opacity of the top gloss (0..1)
    highlight_band - (top, bottom) rows the gloss occupies; keep it inside the top 9-slice border
    """
    w, h = size
    body = _rr(size, (0, 0, w, h), radius)
    face = _rr(size, (0, 0, w, h - depth), radius)

    lum = _vgrad(size, edge * 1.06, edge * 0.92, h - depth - radius, h)  # depth band, slightly shaded
    lum = _over(lum, _vgrad(size, face_top, face_bottom, 0, h - depth), face)

    if outline > 0:  # thin darker rim just inside the silhouette for definition
        inner = _rr(size, (outline, outline, w - outline, h - depth - outline * 0.5), max(1, radius - outline))
        rim = ImageChops.subtract(face, inner)
        lum = _over(lum, _const(size, face_bottom * 0.9), rim)

    if highlight > 0:
        hy0, hy1 = highlight_band
        inset = max(6, radius * 0.28)
        gloss = _rr(size, (inset, hy0, w - inset, hy1), (hy1 - hy0) / 2)
        gloss = _blur(gloss, 1.5)
        gloss_mask = ImageChops.multiply(gloss, _vgrad(size, 1.0, 0.25, hy0, hy1)).point(lambda a: round(a * highlight))
        lum = _over(lum, _const(size, 1.0), gloss_mask)
    return _finish(size, lum, body)


def ui_panel():
    return tactile((UI_SIZE, UI_SIZE), radius=36, depth=7, face_top=1.0, face_bottom=0.95, edge=0.80,
                   highlight=0.0, highlight_band=(0, 0))


def ui_button():
    return tactile((UI_SIZE, UI_SIZE), radius=40, depth=13, face_top=0.93, face_bottom=0.82, edge=0.58,
                   highlight=0.95, highlight_band=(5, 34))


def ui_pill():
    return tactile((UI_SIZE, UI_SIZE), radius=60, depth=8, face_top=0.95, face_bottom=0.86, edge=0.66,
                   highlight=0.8, highlight_band=(6, 30))


def ui_badge():
    s = 64
    return tactile((s, s), radius=22, depth=5, face_top=0.98, face_bottom=0.86, edge=0.62,
                   highlight=0.7, highlight_band=(4, 18))


def ui_cell():
    size = (UI_SIZE, UI_SIZE)
    shape = _rr(size, (2, 2, UI_SIZE - 2, UI_SIZE - 2), 26)
    # Inner shadow: darken where the shape, shifted down, no longer covers (top/side rims).
    shifted = _blur(_shift(shape, 0, 7), 6)
    inner = ImageChops.subtract(shape, shifted)
    lum = _vgrad(size, 0.95, 1.0)
    shade = _const(size, 0.72)
    lum = _over(lum, shade, inner.point(lambda a: round(a * 0.85)))
    # a faint light lip along the bottom inner edge
    lip = ImageChops.subtract(shape, _shift(shape, 0, -3))
    lum = _over(lum, _const(size, 1.0), _blur(lip, 0.6))
    return _finish(size, lum, shape)


def ui_circle():
    size = (UI_SIZE, UI_SIZE)
    ss = SUPERSAMPLE
    m = Image.new("L", (UI_SIZE * ss, UI_SIZE * ss), 0)
    ImageDraw.Draw(m).ellipse((0, 0, UI_SIZE * ss - 1, UI_SIZE * ss - 1), fill=255)
    return _finish(size, _vgrad(size, 1.0, 0.90), m)


def ui_glow():
    size = (UI_SIZE, UI_SIZE)
    g = Image.radial_gradient("L").resize((UI_SIZE * SUPERSAMPLE,) * 2)  # 0 centre -> 255 at radius
    alpha = g.point(lambda v: round(255 * max(0.0, 1 - v / 255) ** 2.2))
    return _finish(size, _const(size, 1.0), alpha)


def ui_shadow():
    size = (UI_SIZE, UI_SIZE)
    m = _rr(size, (12, 12, UI_SIZE - 12, UI_SIZE - 12), 32)
    m = _blur(m, 7).point(lambda a: round(a * 0.55))
    return _finish(size, _const(size, 0.0), m)


def ui_ring():
    size = (UI_SIZE, UI_SIZE)
    outer = _rr(size, (0, 0, UI_SIZE, UI_SIZE), 32)
    inner = _rr(size, (10, 10, UI_SIZE - 10, UI_SIZE - 10), 22)
    return _finish(size, _const(size, 1.0), ImageChops.subtract(outer, inner))


def ui_board_frame():
    """Thick rounded frame with a bevel: lighter top-left rim, darker inner lip; centre transparent."""
    size = (UI_SIZE, UI_SIZE)
    t = 14
    outer = _rr(size, (0, 0, UI_SIZE, UI_SIZE), 38)
    inner = _rr(size, (t, t, UI_SIZE - t, UI_SIZE - t), 26)
    ring = ImageChops.subtract(outer, inner)
    lum = _vgrad(size, 1.0, 0.86)
    # inner lip (darker) just outside the hole
    lip = ImageChops.subtract(_rr(size, (t - 4, t - 4, UI_SIZE - t + 4, UI_SIZE - t + 4), 29), inner)
    lum = _over(lum, _const(size, 0.70), _blur(lip, 0.8))
    # outer bottom depth
    depth = ImageChops.subtract(outer, _rr(size, (0, 0, UI_SIZE, UI_SIZE - 5), 38))
    lum = _over(lum, _const(size, 0.66), depth)
    return _finish(size, lum, ring)


def ui_speech_bubble():
    """White bubble, tail at bottom-left pointing down-left. Body occupies rows 0..100."""
    size = (UI_SIZE, UI_SIZE)
    ss = SUPERSAMPLE
    body_bottom = 100
    body = _rr(size, (0, 0, UI_SIZE, body_bottom), 32)
    tail = Image.new("L", body.size, 0)
    pts = [(22, body_bottom - 12), (50, body_bottom - 12), (46, body_bottom + 2), (12, UI_SIZE - 4)]
    ImageDraw.Draw(tail).polygon([(x * ss, y * ss) for x, y in pts], fill=255)
    # round the tail with a curved inner edge: subtract a circle from its right side
    cut = Image.new("L", body.size, 0)
    ImageDraw.Draw(cut).ellipse(((40) * ss, (body_bottom - 2) * ss, (80) * ss, (body_bottom + 40) * ss), fill=255)
    tail = ImageChops.subtract(tail, cut)
    tail = tail.filter(ImageFilter.GaussianBlur(0.8 * ss)).point(lambda a: 255 if a > 127 else 0)
    shape = ImageChops.lighter(body, tail)
    lum = _vgrad(size, 1.0, 0.94, 0, body_bottom)
    return _finish(size, lum, shape)


def ui_background():
    """Opaque 512x1024 sky-to-sand gradient with faint dots. Not tinted."""
    w, h = 512, 1024
    stops = [  # (t, rgb)
        (0.00, (118, 196, 240)),
        (0.45, (186, 228, 246)),
        (0.62, (232, 243, 236)),
        (0.80, (253, 232, 190)),
        (1.00, (246, 205, 150)),
    ]
    col = Image.new("RGB", (1, h))
    for y in range(h):
        t = y / (h - 1)
        for (t0, c0), (t1, c1) in zip(stops, stops[1:]):
            if t0 <= t <= t1:
                u = (t - t0) / (t1 - t0)
                u = u * u * (3 - 2 * u)
                col.putpixel((0, y), tuple(round(a + (b - a) * u) for a, b in zip(c0, c1)))
                break
    img = col.resize((w, h)).convert("RGBA")

    ss = 2
    dots = Image.new("L", (w * ss, h * ss), 0)
    d = ImageDraw.Draw(dots)
    step = 64
    for row, y in enumerate(range(0, h + step, step)):
        off = step // 2 if row % 2 else 0
        for x in range(-step, w + step, step):
            r = 9
            cx, cy = x + off, y
            d.ellipse(((cx - r) * ss, (cy - r) * ss, (cx + r) * ss, (cy + r) * ss), fill=255)
    dots = dots.resize((w, h), Image.LANCZOS).point(lambda a: round(a * 0.10))
    white = Image.new("RGBA", (w, h), (255, 255, 255, 0))
    white.putalpha(dots)
    img.alpha_composite(white)
    return img.convert("RGB")


SAND_SIZE = 256           # drawn at 2x the cell sprite, so the grain stays fine on large cells
SAND_VARIANTS = 3


def draw_sand(variant: int) -> Image.Image:
    """
    A closed board cell: the whole cell buried under sand. Same footprint and corner radius as `cell.png` at twice the
    resolution, sunk into the board by an inner rim shadow (no drop shadow: it is not an object lying on the cell).
    Variants differ in their ripples and grain, and some carry a pebble, so neighbouring cells do not look stamped.
    """
    rng = random.Random(1000 + variant)
    ss = SUPERSAMPLE
    n = SAND_SIZE
    size = (n, n)
    big = n * ss
    shape = _rr(size, (4, 4, n - 4, n - 4), 52)

    # Warm sand, a touch lighter at the top.
    sand = Image.new("RGB", (big, big))
    top, bottom = (240, 212, 152), (226, 190, 124)
    draw = ImageDraw.Draw(sand)
    for y in range(big):
        t = y / (big - 1)
        draw.line([(0, y), (big, y)], fill=tuple(round(a + (b - a) * t) for a, b in zip(top, bottom)))

    # Soft dune ripples: a light crest with a shaded trough just below it.
    crests = Image.new("L", (big, big), 0)
    troughs = Image.new("L", (big, big), 0)
    crest_draw, trough_draw = ImageDraw.Draw(crests), ImageDraw.Draw(troughs)
    for _ in range(2 + variant % 2):
        base = rng.uniform(0.22, 0.8) * big
        amplitude = rng.uniform(0.03, 0.06) * big
        phase = rng.uniform(0, math.tau)
        wavelength = rng.uniform(0.9, 1.4) * big
        points = [(x, base + amplitude * math.sin(phase + x / wavelength * math.tau)) for x in range(0, big + 8, 8)]
        crest_draw.line(points, fill=230, width=round(6 * ss))
        trough_draw.line([(x, y + 8 * ss) for x, y in points], fill=190, width=round(7 * ss))
    sand.paste((250, 228, 178), mask=_blur(crests, 3))
    sand.paste((205, 166, 100), mask=_blur(troughs, 4))

    # Fine grain: light and dark specks.
    grain = ImageDraw.Draw(sand)
    for _ in range(1800):
        x, y = rng.uniform(0, big), rng.uniform(0, big)
        r = rng.uniform(0.6, 1.5) * ss
        shade = rng.choice(((255, 238, 200), (196, 156, 94), (214, 176, 112)))
        grain.ellipse((x - r, y - r, x + r, y + r), fill=shade)

    # A half-buried pebble or two on some variants.
    for _ in range(variant):
        cx, cy = rng.uniform(0.25, 0.75) * big, rng.uniform(0.3, 0.75) * big
        rx, ry = rng.uniform(7, 11) * ss, rng.uniform(5, 7) * ss
        grain.ellipse((cx - rx, cy - ry + 3 * ss, cx + rx, cy + ry + 3 * ss), fill=(200, 160, 100))
        grain.ellipse((cx - rx, cy - ry, cx + rx, cy + ry), fill=(168, 150, 132))
        grain.ellipse((cx - rx * 0.5, cy - ry * 0.7, cx, cy - ry * 0.1), fill=(204, 190, 172))

    # Sunk into the board: shade the rim where the shape, shifted down and blurred, no longer covers.
    inner = ImageChops.subtract(shape, _blur(_shift(shape, 0, 10), 9))
    rim = ImageChops.subtract(shape, _blur(shape, 6))
    sand.paste((176, 136, 78), mask=inner.point(lambda a: round(a * 0.75)))
    sand.paste((196, 156, 92), mask=rim.point(lambda a: round(a * 0.5)))

    img = sand.convert("RGBA")
    img.putalpha(shape)
    return img.convert("RGBa").resize(size, Image.LANCZOS).convert("RGBA")


UI_SHAPES = {
    "panel": ui_panel,
    "cell": ui_cell,
    "button": ui_button,
    "pill": ui_pill,
    "circle": ui_circle,
    "glow": ui_glow,
    "shadow": ui_shadow,
    "ring": ui_ring,
    "board_frame": ui_board_frame,
    "badge": ui_badge,
    "speech_bubble": ui_speech_bubble,
    "background": ui_background,
}


def write_ui(out_dir: Path) -> list[Path]:
    written = []
    for stem, build in UI_SHAPES.items():
        path = out_dir / "UI" / f"{stem}.png"
        path.parent.mkdir(parents=True, exist_ok=True)
        build().save(path)
        written.append(path)
    for variant in range(SAND_VARIANTS):
        path = out_dir / "UI" / f"sand_{variant + 1:02d}.png"
        draw_sand(variant).save(path)
        written.append(path)
    return written


# --------------------------------------------------------------------------- main

def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--seed", type=Path, default=DEFAULT_SEED, help="path to seed_content.json")
    parser.add_argument("--out", type=Path, default=DEFAULT_OUT, help="output Sprites folder")
    parser.add_argument("--cache", type=Path, default=DEFAULT_CACHE, help="download cache folder")
    parser.add_argument("--offline", action="store_true", help="use only cached downloads")
    parser.add_argument("--ui-only", action="store_true", help="only (re)draw the UI shapes")
    args = parser.parse_args(argv)
    for stream in (sys.stdout, sys.stderr):  # Windows consoles may not be UTF-8
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(errors="backslashreplace")

    items, icons = [], []
    if not args.ui_only:
        source = FluentSource(args.cache, offline=args.offline)
        try:
            items = write_items(source, args.seed, args.out)
            icons = write_icons(source, args.out)
        except ResolveError as exc:
            print(f"ERROR: {exc}", file=sys.stderr)
            return 1
    ui = write_ui(args.out)
    print(f"Items: {len(items)}  Icons: {len(icons)}  UI: {len(ui)}  -> {args.out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
