#!/usr/bin/env python3
"""
Copies the art and audio the game uses from the licensed asset pack into Assets/MergeGame/Content.

  Sprites/Items/<chainId>/<chainId>_<LL>.png  - one per chain level whose seed entry names a pack `source`
  Sprites/UI/<name>.png                       - UI sprites listed in UI_SPRITES
  Audio/<name>.ogg                            - sound effects and music listed in AUDIO

The pack itself is not in the repository; pass the folder that holds its `Assets` content with --pack.
"""
from __future__ import annotations

import argparse
import json
import shutil
import sys
from pathlib import Path

from PIL import Image, ImageFilter

REPO_ROOT = Path(__file__).resolve().parents[2]
CONTENT = REPO_ROOT / "Assets" / "MergeGame" / "Content"
DEFAULT_SEED = REPO_ROOT / "Assets" / "MergeGame" / "Editor" / "Seed" / "seed_content.json"

ITEM_FOLDER = Path("Board") / "ItemMain" / "Item"

# Item sprites are centred on a canvas the size of the emoji sprites. The pack draws low levels smaller than high
# ones; the longest side is scaled by ITEM_GROWTH and clamped, so that progression survives but a level 1 item still
# reads clearly in its cell. A sprite of this canvas is drawn at about 60% on a phone, so the upscale stays sharp.
ITEM_CANVAS = 256
ITEM_GROWTH = 1.3
ITEM_MIN_SIDE = 160
ITEM_MAX_SIDE = 212
CONTACT_SHADOW_ALPHA = 0.22
CONTACT_SHADOW_BLUR = 6

# (published file under Content/Sprites/UI, pack file)
UI_SPRITES = [
    ("selection_brackets.png", Path("Board") / "Board" / "item_selected.png"),
    ("board_tray.png", Path("Board") / "Board" / "board.png"),
    ("sparkle_star.png", Path("Effects") / "Textures" / "t_xingxing_3.png"),
    ("merge_ring.png", Path("Effects") / "Textures" / "t_effect_1.png"),
    ("lock_bubble.png", Path("Board") / "ItemMain" / "Item" / "Bubble.png"),
    ("bubble_pop_sheet.png", Path("Effects") / "Textures" / "t_water_5.png"),
    ("bubble_droplet.png", Path("Effects") / "Textures" / "t_paopao1.png"),
    ("spawn_icon.png", Path("Hud") / "BaseSceneView" / "icon_cook.png"),  # whisk and spatula: making things
    ("home_button.png", Path("Hud") / "UIRoot" / "btn_setall_edit_hud.png"),
    ("order_tray.png", Path("Board") / "Board" / "order_tray.png"),  # the customers' orders stand on it
    ("counter_cloth.png", Path("Board") / "Board" / "order_counter.png"),  # hangs down the front of the counter
    ("deliver_button.png", Path("UI") / "Common" / "button_green_2.png"),  # appears when an order can be delivered
]

# (published file under Content/Audio, pack file)
AUDIO = [
    ("music_main.ogg", Path("Audios") / "Music" / "bgmScene.ogg"),
    ("button_click.ogg", Path("Audios") / "Effect" / "SfxButtonClick.ogg"),
    ("spawn.ogg", Path("Audios") / "Effect" / "SfxMergeSpawnManual.ogg"),
    ("pick_up.ogg", Path("Audios") / "Effect" / "sfxItemChoose.ogg"),
    ("land.ogg", Path("Audios") / "Effect" / "sfxItemLanding.ogg"),
    ("unlock.ogg", Path("Audios") / "Effect" / "SfxMergeBubbleBreak.ogg"),  # freeing a locked item pops its bubble
    ("sand_break.ogg", Path("Audios") / "Effect" / "sfxSandBreak.ogg"),
    ("discard.ogg", Path("Audios") / "Effect" / "sfxCommonDelete.ogg"),
    ("order_complete.ogg", Path("Audios") / "Effect" / "sfxOrderComplete.ogg"),
    ("notice.ogg", Path("Audios") / "Effect" / "sfxDialoguePop.ogg"),
] + [
    (f"merge_{level:02d}.ogg", Path("Audios") / "Effect" / f"SfxMergelv{level}.ogg") for level in range(2, 8)
]


class ImportError_(Exception):
    pass


def compose_item(art: Image.Image) -> Image.Image:
    """Scales the art (see ITEM_GROWTH), centres it on the item canvas and puts a soft contact shadow under it."""
    art = art.crop(art.getbbox())
    longest = max(art.width, art.height)
    side = min(ITEM_MAX_SIDE, max(ITEM_MIN_SIDE, round(longest * ITEM_GROWTH)))
    scale = side / longest
    size = (max(1, round(art.width * scale)), max(1, round(art.height * scale)))
    # Resampled in premultiplied alpha, so transparent pixels do not bleed a dark fringe into the edges.
    art = art.convert("RGBa").resize(size, Image.LANCZOS).convert("RGBA")
    x = (ITEM_CANVAS - art.width) // 2
    y = (ITEM_CANVAS - art.height) // 2

    # The shadow is the art's own silhouette squashed into a flat ellipse at its foot.
    silhouette = art.getchannel("A").resize((art.width, max(1, art.height // 6)), Image.LANCZOS)
    shadow_alpha = Image.new("L", (ITEM_CANVAS, ITEM_CANVAS), 0)
    shadow_alpha.paste(silhouette, (x, min(ITEM_CANVAS - silhouette.height, y + art.height - silhouette.height // 2)))
    shadow_alpha = shadow_alpha.filter(ImageFilter.GaussianBlur(CONTACT_SHADOW_BLUR))
    shadow_alpha = shadow_alpha.point(lambda a: round(a * CONTACT_SHADOW_ALPHA))

    sprite = Image.new("RGBA", (ITEM_CANVAS, ITEM_CANVAS), (40, 30, 20, 0))
    sprite.putalpha(shadow_alpha)
    sprite.alpha_composite(art, (x, y))
    return sprite


def import_items(pack: Path, seed_path: Path, out: Path) -> list[Path]:
    seed = json.loads(seed_path.read_text(encoding="utf-8"))
    written, errors = [], []
    for chain in seed["chains"]:
        chain_id = chain["id"]
        for index, level in enumerate(chain["levels"], start=1):
            source = level.get("source")
            if not source:
                continue
            path = pack / ITEM_FOLDER / f"{source}.png"
            try:
                if not path.is_file():
                    raise ImportError_(f"no pack file {path}")
                sprite = compose_item(Image.open(path).convert("RGBA"))
            except ImportError_ as exc:
                errors.append(f"chain '{chain_id}' level {index} ({level.get('name')}): {exc}")
                continue
            target = out / "Sprites" / "Items" / chain_id / f"{chain_id}_{index:02d}.png"
            target.parent.mkdir(parents=True, exist_ok=True)
            sprite.save(target)
            written.append(target)
    if errors:
        raise ImportError_("Unresolvable item art:\n  " + "\n  ".join(errors))
    return written


ROUND_BUTTON = Path("Board") / "BaseUIBoard" / "UI" / "times_main_btn_emission5.png"  # the game's own "produce" button
ROUND_FACE_RADIUS = 68  # inside the red rim, in pixels of the 180 px art


def remove_tap_hand(art: Image.Image) -> Image.Image:
    """
    The pack's round button has a tap hand drawn on it. The hand (pale skin, its brown outline and drop shadow) is
    masked and the area filled by diffusion: each masked pixel repeatedly takes the average of its neighbours, so the
    face's gloss band and its orange-to-yellow gradient flow in smoothly. The gloss highlight at the top left stays.
    """
    import colorsys

    import numpy as np

    w, h = art.size
    rgba = np.asarray(art).astype(np.float64)
    ys, xs = np.mgrid[0:h, 0:w]
    inside_face = (xs - w / 2) ** 2 + (ys - h / 2) ** 2 < ROUND_FACE_RADIUS ** 2
    sat = np.zeros((h, w))
    val = np.zeros((h, w))
    for y in range(h):
        for x in range(w):
            _, sat[y, x], val[y, x] = colorsys.rgb_to_hsv(*(rgba[y, x, :3] / 255))

    skin = (sat < 0.5) & (val > 0.68)
    darker_than_face = rgba[..., 0] < 248  # the face's red channel is 253-255 everywhere
    hand_area = ((xs >= 58) | ((xs >= 44) & (ys >= 60))) & (xs <= 158) & (ys >= 18) & (ys <= 138)
    mask = inside_face & hand_area & (skin | darker_than_face)
    grown = Image.fromarray((mask * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(9))
    mask = (np.asarray(grown) > 0) & inside_face

    filled = rgba.copy()
    filled[mask] = rgba[~mask & inside_face].mean(axis=0)
    for _ in range(1500):
        average = (np.roll(filled, 1, 0) + np.roll(filled, -1, 0) + np.roll(filled, 1, 1) + np.roll(filled, -1, 1)) / 4
        filled[mask] = average[mask]

    result = Image.fromarray(np.clip(filled, 0, 255).astype(np.uint8), "RGBA")
    seam = Image.fromarray((mask * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(2))
    return Image.composite(result, art, seam)


def greyed(art: Image.Image) -> Image.Image:
    """A warm grey version of coloured button art, for its disabled look; keeps its light and shade."""
    grey = art.convert("LA").convert("RGBA")
    tint = Image.new("RGBA", art.size, (196, 186, 172, 255))
    grey = Image.blend(grey, Image.composite(tint, grey, grey.getchannel("A")), 0.35)
    grey.putalpha(art.getchannel("A"))
    return grey


def import_round_button(pack: Path, folder: Path) -> list[Path]:
    path = pack / ROUND_BUTTON
    if not path.is_file():
        raise ImportError_(f"Missing pack file: {path}")
    face = remove_tap_hand(Image.open(path).convert("RGBA"))
    folder.mkdir(parents=True, exist_ok=True)
    face.save(folder / "spawn_button.png")
    greyed(face).save(folder / "spawn_button_disabled.png")
    return [folder / "spawn_button.png", folder / "spawn_button_disabled.png"]


# The shop the customers stand in: a leaf wallpaper behind them, a wooden counter in front and a parquet floor under
# the board. Both textures are drawn tiled.
SHOP_WALL = Path("Texture2D") / "canteenwall_3.png"
# One repeat of the wallpaper above its wooden dado: two columns of leaves across, one leaf down. Measured; the
# texture's own 256 px width does not repeat cleanly.
SHOP_WALL_REPEAT = (5, 5, 178, 91)
SHOP_FLOOR = Path("Texture2D") / "patiofloor_1.png"
# The parquet is calmed so the board stays the busiest thing on screen: its contrast is pulled towards its average
# colour, then it is tinted towards a warm sand.
SHOP_FLOOR_CALM = 0.45
SHOP_FLOOR_TINT = ((236, 196, 150), 0.35)
COUNTER_TOP = Path("UI") / "Window" / "SpecialOrder" / "special_order_table.png"


def calmed(art: Image.Image, calm: float, tint: tuple[tuple[int, int, int], float]) -> Image.Image:
    """Pulls the colours towards their average by <calm>, then blends them towards <tint> colour by its weight."""
    import numpy as np

    rgba = np.asarray(art).astype(np.float64)
    mean = rgba[..., :3].reshape(-1, 3).mean(axis=0)
    rgba[..., :3] = rgba[..., :3] * (1 - calm) + mean * calm
    colour, weight = tint
    rgba[..., :3] = rgba[..., :3] * (1 - weight) + np.array(colour) * weight
    return Image.fromarray(np.clip(rgba, 0, 255).astype(np.uint8), "RGBA")


def import_shop(pack: Path, folder: Path) -> list[Path]:
    for path in (pack / SHOP_WALL, pack / SHOP_FLOOR, pack / COUNTER_TOP):
        if not path.is_file():
            raise ImportError_(f"Missing pack file: {path}")
    folder.mkdir(parents=True, exist_ok=True)
    wall = Image.open(pack / SHOP_WALL).convert("RGBA").crop(SHOP_WALL_REPEAT)
    wall.save(folder / "shop_wall.png")
    floor = calmed(Image.open(pack / SHOP_FLOOR).convert("RGBA"), SHOP_FLOOR_CALM, SHOP_FLOOR_TINT)
    floor.save(folder / "shop_floor.png")
    # The plank of the special order window's table, cut out of its empty margins (its soft shadow stays).
    table = Image.open(pack / COUNTER_TOP).convert("RGBA")
    table.crop(table.getbbox()).save(folder / "counter_top.png")
    return [folder / "shop_wall.png", folder / "shop_floor.png", folder / "counter_top.png"]


def copy_files(pack: Path, table: list[tuple[str, Path]], folder: Path) -> list[Path]:
    missing = [str(pack / source) for _, source in table if not (pack / source).is_file()]
    if missing:
        raise ImportError_("Missing pack files:\n  " + "\n  ".join(missing))
    folder.mkdir(parents=True, exist_ok=True)
    written = []
    for name, source in table:
        shutil.copyfile(pack / source, folder / name)
        written.append(folder / name)
    return written


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--pack", type=Path, required=True, help="the pack's Assets folder")
    parser.add_argument("--seed", type=Path, default=DEFAULT_SEED, help="path to seed_content.json")
    parser.add_argument("--out", type=Path, default=CONTENT, help="the game's Content folder")
    args = parser.parse_args(argv)

    try:
        items = import_items(args.pack, args.seed, args.out)
        ui = copy_files(args.pack, UI_SPRITES, args.out / "Sprites" / "UI")
        ui += import_round_button(args.pack, args.out / "Sprites" / "UI")
        ui += import_shop(args.pack, args.out / "Sprites" / "UI")
        audio = copy_files(args.pack, AUDIO, args.out / "Audio")
    except ImportError_ as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1
    print(f"Items: {len(items)}  UI: {len(ui)}  Audio: {len(audio)}  -> {args.out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
