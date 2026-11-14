using System;
using System.Collections;
using System.IO;
using System.Linq;
using MergeGame.Core.Grid;
using MergeGame.PlayTests.Support;
using MergeGame.Presentation.Hints;
using MergeGame.Presentation.Notices;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MergeGame.PlayTests
{
    /// <summary>
    /// Development aid, not a regression test: plays through the scenes and saves screenshots of key states at phone
    /// and tablet resolutions. Runs only when the environment variable <c>MERGE_CAPTURE_DIR</c> names an output folder.
    /// </summary>
    [Category("Screenshots")]
    public sealed class ScreenshotCaptureTests
    {
        private const string OutputVariable = "MERGE_CAPTURE_DIR";
        private const int HighestRequestedLevel = 4;

        // The sand pour is filmed as an animation at this frame rate (assembled outside the project).
        private const float PourFramesPerSecond = 20f;
        private const int MaxPourFrames = 120;
        private const int PourMagnification = 3;

        private string directory;
        private GameScreenDriver game;

        [SetUp]
        public void RequireOutputFolder()
        {
            directory = Environment.GetEnvironmentVariable(OutputVariable);
            if (string.IsNullOrEmpty(directory))
                Assert.Ignore($"Set {OutputVariable} to a folder to capture screenshots.");
            Directory.CreateDirectory(directory);
        }

        [UnityTest]
        public IEnumerator CaptureMainMenu()
        {
            yield return GameScreenDriver.LoadScene(GameScreenDriver.MainMenuScene);
            yield return GameScreenDriver.WaitSeconds(1.5f);
            yield return Snapshot.CaptureAll(Camera.main, directory, "01_main_menu");
        }

        [UnityTest]
        public IEnumerator CaptureGameStates()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            yield return GameScreenDriver.WaitSeconds(0.8f);
            yield return Capture("02_game_opening_sand");

            game.Tap(game.Cells().First(game.IsCovered));
            yield return GameScreenDriver.WaitSeconds(0.5f);
            yield return Capture("03_game_sand_selected");

            // Each resolution re-triggers the one-shot pulse, so every capture catches it mid-pulse.
            yield return Capture("04_game_idle_hint_pulse", PulseMergeHint, 0.3f);

            yield return CaptureSandPour("05_game_sand_pour");
            yield return GameScreenDriver.WaitSeconds(1f);
            yield return Capture("05b_game_sand_revealed");

            yield return SpawnAndMergeThenSelect();
            yield return Capture("06_game_merged_selected");

            yield return CaptureDragOverMergeTarget("07_game_dragging_merge_shine");

            yield return CaptureLockedItemNotice("08_game_notice_locked");
            game.Notices.ShowNotice(Notice.MaxLevelReached);
            yield return Capture("09_game_notice_max_level");

            Assert.That(AutoPlayer.PlayUntilAnOrderIsDeliverable(game.Session, HighestRequestedLevel), Is.True, "No order became deliverable.");
            yield return GameScreenDriver.WaitSeconds(2.5f);
            game.Tap(game.OrderMatchedCells().First());
            yield return GameScreenDriver.WaitSeconds(0.5f);
            yield return Capture("10_game_order_ready_marked");

            GameScreenDriver.Click(FirstDeliverableCard().GetComponentInChildren<UnityEngine.UI.Button>());
            yield return GameScreenDriver.WaitSeconds(0.75f);
            yield return Capture("11_game_order_celebration");

            game.ClickSpawn();
            yield return GameScreenDriver.WaitSeconds(0.3f);
            yield return Capture("12_game_spawn_shine");

            yield return FillTheBoard();
            yield return Capture("13_game_board_full");
        }

        private IEnumerator Capture(string name, Action beforeRender = null, float settleSeconds = 0f) =>
            Snapshot.CaptureAll(game.Camera, directory, name, beforeRender, settleSeconds);

        /// <summary>Lets enough idle time pass for the next hint check, which pulses the hint once.</summary>
        private void PulseMergeHint()
        {
            var settings = game.Resolve<MergeHintSettings>();
            game.Resolve<MergeHintPresenter>().Advance(Mathf.Max(settings.IdleSeconds, settings.RepeatSeconds));
        }

        /// <summary>
        /// Opens a closed cell for real, then steps its sand pour at the pace of a 20 fps animation and films the cell and
        /// its neighbours, magnified: one frame of the resting sand first, then every frame of the pour.
        /// </summary>
        private IEnumerator CaptureSandPour(string name)
        {
            Assert.That(game.TryFindCoverOpeningMerge(out var from, out var to), Is.True);
            var closed = GameScreenDriver.Neighbours(to).First(game.IsCovered);
            yield return CroppedFrames.Capture(
                game.Camera, Snapshot.DeviceResolutions[0], directory, name + "_rest", 1, _ => false,
                () => CellNeighbourhoodInPixels(closed), PourMagnification);

            yield return game.DragAndDrop(from, to);
            // Past the reveal delay, so the item already sits behind the sand.
            yield return GameScreenDriver.WaitSeconds(0.4f);
            Assert.That(game.Board.TryGetCover(closed, out var cover), Is.True);
            var stepsPerFrame = cover.StepsPerSecond / PourFramesPerSecond;
            yield return CroppedFrames.Capture(
                game.Camera, Snapshot.DeviceResolutions[0], directory, name, MaxPourFrames,
                frame =>
                {
                    cover.ShowPourStep(Mathf.RoundToInt(frame * stepsPerFrame));
                    return cover.GrainCount > 0;
                },
                () => CellNeighbourhoodInPixels(closed), PourMagnification);
            cover.Finish();
        }

        /// <summary>The cell and half a cell around it, in pixels of the current render.</summary>
        private RectInt CellNeighbourhoodInPixels(GridPosition cell)
        {
            var center = game.ScreenPointOf(cell);
            var size = Mathf.RoundToInt(CellStep(cell).x * 2f);
            var x = Mathf.Clamp(Mathf.RoundToInt(center.x - size / 2f), 0, game.Camera.pixelWidth - size);
            var y = Mathf.Clamp(Mathf.RoundToInt(center.y - size / 2f), 0, game.Camera.pixelHeight - size);
            return new RectInt(x, y, size, size);
        }

        private IEnumerator SpawnAndMergeThenSelect()
        {
            for (var i = 0; i < 4; i++)
            {
                game.ClickSpawn();
                yield return GameScreenDriver.WaitSeconds(0.15f);
            }

            Assert.That(game.TryFindMergePair(out var from, out var to), Is.True);
            yield return game.DragAndDrop(from, to);
            yield return GameScreenDriver.WaitSeconds(1f);
            game.Tap(to);
            yield return GameScreenDriver.WaitSeconds(0.6f);
        }

        private IEnumerator CaptureDragOverMergeTarget(string name)
        {
            GridPosition from, to;
            for (var spawns = 0; !game.TryFindMergePair(out from, out to) && spawns < 20; spawns++)
            {
                game.ClickSpawn();
                yield return null;
            }

            yield return GameScreenDriver.WaitSeconds(0.5f);
            PointerGesture held = null;
            yield return game.DragOnto(from, to, gesture => held = gesture);
            yield return GameScreenDriver.WaitSeconds(0.3f);
            // Hover near the target's top-right corner, so the lifted item leaves most of the shining target visible.
            yield return Capture(name, () => held.MoveTo(game.ScreenPointOf(to) + CellStep(to) * new Vector2(0.4f, 0.4f)));

            // The screen size changed back after the capture; aim at the target again before dropping.
            yield return null;
            held.MoveTo(game.ScreenPointOf(to));
            held.Release();
            yield return GameScreenDriver.WaitSeconds(1f);
        }

        private IEnumerator CaptureLockedItemNotice(string name)
        {
            Assert.That(TryFindLockedItem(out var locked), Is.True, "No locked item left on the board.");
            yield return game.DragAndDrop(locked, locked);
            yield return GameScreenDriver.WaitSeconds(0.3f);
            yield return Capture(name);
        }

        private IEnumerator FillTheBoard()
        {
            for (var spawns = 0; game.Spawn.IsAvailable && spawns < 60; spawns++)
            {
                game.ClickSpawn();
                yield return GameScreenDriver.WaitSeconds(0.05f);
            }

            yield return GameScreenDriver.WaitSeconds(2.5f);
        }

        /// <summary>The on-screen size of one cell, measured at <paramref name="cell"/>.</summary>
        private Vector2 CellStep(GridPosition cell)
        {
            var width = (game.ScreenPointOf(new GridPosition(cell.X + 1, cell.Y)) - game.ScreenPointOf(cell)).x;
            return new Vector2(width, width);
        }

        private bool TryFindLockedItem(out GridPosition position)
        {
            foreach (var cell in game.Cells())
            {
                if (game.Board.TryGetItem(cell, out var item) && item.IsLocked && !item.IsHidden)
                {
                    position = cell;
                    return true;
                }
            }

            position = default;
            return false;
        }

        private Component FirstDeliverableCard()
        {
            for (var slot = 0; slot < game.Orders.CardCount; slot++)
            {
                if (game.Orders.GetCard(slot).CanDeliver)
                    return game.Orders.GetCard(slot);
            }

            Assert.Fail("No deliverable order card.");
            return null;
        }
    }
}
