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

            Assert.That(game.TryFindCoverOpeningMerge(out var from, out var to), Is.True);
            yield return game.DragAndDrop(from, to);
            yield return GameScreenDriver.WaitSeconds(0.45f);
            yield return CaptureOnce("05_game_sand_mid_reveal");
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

        /// <summary>One resolution only, for a moment that would be over before the other sizes are rendered.</summary>
        private IEnumerator CaptureOnce(string name) =>
            Snapshot.Capture(game.Camera, Path.Combine(directory, $"{name}_1080x1920.png"), Snapshot.DeviceResolutions[0], null, 0f);

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
