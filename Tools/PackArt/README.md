# Asset Pack Importer

`import_pack_art.py` copies the parts of the licensed asset pack the game uses into `Assets/MergeGame/Content/`. The pack is not in the repository (see `THIRD-PARTY-NOTICES.md`); the imported files are, so the project opens and plays without it. Run the script only to change which pack files are used.

| Output | From | How |
|---|---|---|
| `Sprites/Items/<chainId>/<chainId>_<LL>.png` | `Board/ItemMain/Item/<source>.png`, one per chain level whose entry in `Assets/MergeGame/Editor/Seed/seed_content.json` has a `source` | Trimmed, scaled (longest side × 1.3, clamped to 160–212 px, so level 1 items stay readable while higher levels still grow), centred on a 256×256 canvas with a soft contact shadow, the same canvas as the placeholder emoji items |
| `Sprites/UI/selection_brackets.png` | `Board/Board/item_selected.png` | Copied unchanged; the scene builder slices it 9-ways (60 px corners) |
| `Sprites/UI/sparkle_star.png` | `Effects/Textures/t_xingxing_3.png` | Copied unchanged; the twinkling stars over a merge target or a spawned item, and the merge burst |
| `Sprites/UI/board_tray.png` | `Board/Board/board.png` | Copied unchanged; the blue tray inside the board's wooden frame, sliced 9-ways (30 px borders). The tile colours in `UiTheme` (`boardTileLight`, `boardTileDark`) are sampled from the pack's `tile_bg1.png` and `tile_bg2.png` |
| `Audio/*.ogg` | `Audios/Effect/*.ogg`, `Audios/Music/bgmScene.ogg` | Copied unchanged under the names in the `AUDIO` table; `SoundLibrary.asset` is created from these names |

## Run

Needs Python 3.9+ and Pillow (`python -m pip install pillow`). Pass the pack's `Assets` folder:

```bash
python Tools/PackArt/import_pack_art.py --pack "<path to the pack>/Assets"
```

Options: `--seed <json>`, `--out <Content folder>`. Re-running overwrites the outputs; the `.meta` files (GUIDs) are kept.

After changing item sources, run **Merge Game → Seed Content** so the chain assets pick up any new names; after adding audio, run **Merge Game → Build Scenes** (an existing `SoundLibrary.asset` is kept, so assign new clips to it in the Inspector).

## Item sources

| Chain | Levels 1–7 |
|---|---|
| `garden` (Bahçe) | OrangeTree01, 02, 03, 05, 06, 08, 10 |
| `cafe` (Kafe) | Coffee01, 03, 05, 07, 08, 09, 10 |
| `sea` (Deniz) | Seashell03, 04, 05, 06, 08, 10, 12 (starts at the orange scallop so no level 1 item is a pale oval like the garden seed) |
| `bakery` (Fırın) | Flour01, 02, 03, 05, 07, 09, 13 |
