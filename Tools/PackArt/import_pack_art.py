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
]

# (published file under Content/Audio, pack file)
AUDIO = [
    ("music_main.ogg", Path("Audios") / "Music" / "bgmScene.ogg"),
    ("button_click.ogg", Path("Audios") / "Effect" / "SfxButtonClick.ogg"),
    ("spawn.ogg", Path("Audios") / "Effect" / "SfxMergeSpawnManual.ogg"),
    ("pick_up.ogg", Path("Audios") / "Effect" / "sfxItemChoose.ogg"),
    ("land.ogg", Path("Audios") / "Effect" / "sfxItemLanding.ogg"),
    ("unlock.ogg", Path("Audios") / "Effect" / "sfxItemUnlock.ogg"),
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
        audio = copy_files(args.pack, AUDIO, args.out / "Audio")
    except ImportError_ as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1
    print(f"Items: {len(items)}  UI: {len(ui)}  Audio: {len(audio)}  -> {args.out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
