# Asset Pack Importer

`import_pack_art.py` copies the parts of the licensed asset pack the game uses into `Assets/MergeGame/Content/`. The pack is not in the repository (see `THIRD-PARTY-NOTICES.md`); the imported files are, so the project opens and plays without it. Run the script only to change which pack files are used.

| Output | From | How |
|---|---|---|
| `Sprites/Items/<chainId>/<chainId>_<LL>.png` | `Board/ItemMain/Item/<source>.png`, one per chain level whose entry in `Assets/MergeGame/Editor/Seed/seed_content.json` has a `source` | Trimmed, scaled (longest side × 1.3, clamped to 160–212 px, so level 1 items stay readable while higher levels still grow), centred on a 256×256 canvas with a soft contact shadow, the same canvas as the placeholder emoji items |
| `Sprites/UI/selection_brackets.png` | `Board/Board/item_selected.png` | Copied unchanged; the scene builder slices it 9-ways (60 px corners) |
| `Sprites/UI/sparkle_star.png` | `Effects/Textures/t_xingxing_3.png` | Copied unchanged; the twinkling stars over a merge target or a spawned item, and the glint at a merged item's corner |
| `Sprites/UI/bubble_pop_sheet.png` | `Effects/Textures/t_water_5.png` | Copied unchanged; a 4x4 sheet of a bubble's skin tearing apart, played as a flipbook when a bubble bursts |
| `Sprites/UI/spawn_button.png`, `Sprites/UI/spawn_button_disabled.png` | `Board/BaseUIBoard/UI/times_main_btn_emission5.png` | The game's own round "produce" button with its tap hand removed: the hand and its shadow are masked and the area is filled by diffusion from the face around it, keeping the gloss highlight. The disabled version is the same art in warm grey |
| `Sprites/UI/spawn_icon.png` | `Hud/BaseSceneView/icon_cook.png` | Copied unchanged; the whisk and spatula on the spawn button |
| `Sprites/UI/home_button.png` | `Hud/UIRoot/btn_setall_edit_hud.png` | Copied unchanged; the home button's glossy blue square, drawn at its own proportions (not sliced) |
| `Sprites/UI/bubble_droplet.png` | `Effects/Textures/t_paopao1.png` | Copied unchanged; the small bubbles sprayed out of a burst |
| `Sprites/UI/lock_bubble.png` | `Board/ItemMain/Item/Bubble.png` | Copied unchanged; the clear bubble around a locked item, which pops outwards when a merge frees it |
| `Sprites/UI/merge_ring.png` | `Effects/Textures/t_effect_1.png` | Copied unchanged; the ring that spreads out behind a merged item |
| `Sprites/UI/board_tray.png` | `Board/Board/board.png` | Copied unchanged; the blue tray inside the board's wooden frame, sliced 9-ways (30 px borders). The tile colours in `UiTheme` (`boardTileLight`, `boardTileDark`) are sampled from the pack's `tile_bg1.png` and `tile_bg2.png` |
| `Audio/*.ogg` | `Audios/Effect/*.ogg`, `Audios/Music/bgmScene.ogg` | Copied unchanged under the names in the `AUDIO` table; `SoundLibrary.asset` is created from these names |

## Run

Needs Python 3.9+, Pillow and NumPy (`python -m pip install pillow numpy`; NumPy only removes the hand from the round button). Pass the pack's `Assets` folder:

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
