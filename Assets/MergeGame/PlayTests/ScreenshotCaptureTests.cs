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

        private const int MaxPourFrames = 400;

        /// <summary>
        /// The same pour filmed three ways, as frames of exact pour time (assembled into animations outside the project):
        /// a 3× close-up at 60 frames per second of pour time, the same at 200 for slow motion, and true on-device size.
        /// </summary>
        private static readonly (string suffix, float framesPerSecond, float cells, int magnify)[] PourViews =
        {
            ("", 60f, 2f, 3),
            ("_slow", 200f, 2f, 3),
            ("_1x", 60f, 3.2f, 1)
        };

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
            // Lets the customers come in behind the counter and settle.
            yield return GameScreenDriver.WaitSeconds(2.2f);
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

            // The settings popup, then with vibration off; the switch is put back as it was.
            var settings = UnityEngine.Object.FindAnyObjectByType<MergeGame.View.Settings.SettingsView>();
            GameScreenDriver.Click(settings.OpenButton);
            yield return GameScreenDriver.WaitSeconds(0.5f);
            yield return Capture("13b_game_settings");
            GameScreenDriver.Click(settings.VibrationButton);
            yield return GameScreenDriver.WaitSeconds(0.2f);
            yield return Capture("13c_game_settings_vibration_off");
            GameScreenDriver.Click(settings.VibrationButton);
            GameScreenDriver.Click(settings.CloseButton);
            yield return GameScreenDriver.WaitSeconds(0.3f);

            // The chains popup.
            var chains = UnityEngine.Object.FindAnyObjectByType<MergeGame.View.Chains.ChainsView>();
            GameScreenDriver.Click(chains.OpenButton);
            yield return GameScreenDriver.WaitSeconds(0.5f);
            yield return Capture("13d_game_chains");
            GameScreenDriver.Click(chains.CloseButton);
        }

        /// <summary>A drag-merge filmed at 30 frames per second of game time, cropped to the target and its neighbours.</summary>
        [UnityTest]
        public IEnumerator CaptureMergeFrames()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            for (var spawns = 0; !game.TryFindMergePair(out _, out _) && spawns < 20; spawns++)
            {
                game.ClickSpawn();
                yield return null;
            }

            Assert.That(game.TryFindMergePair(out var from, out var to), Is.True);
            yield return FilmMerge("14_merge", from, to);
        }

        /// <summary>A merge that frees a locked item, so its bubble swells and bursts; filmed like <see cref="CaptureMergeFrames"/>.</summary>
        [UnityTest]
        public IEnumerator CaptureBubblePopFrames()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            Assert.That(game.TryFindCoverOpeningMerge(out var from, out var to), Is.True);
            yield return FilmMerge("15_bubble_pop", from, to);
        }

        /// <summary>A locked item's bubble breathing at rest, filmed at 10 frames per second of game time.</summary>
        [UnityTest]
        public IEnumerator CaptureBubbleBreathFrames()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            var locked = game.Cells().First(cell => game.Board.TryGetItem(cell, out var item) && item.IsLocked && !item.IsHidden);
            Time.captureDeltaTime = 1f / 10f;
            try
            {
                yield return CroppedFrames.CaptureLive(
                    game.Camera, Snapshot.DeviceResolutions[0], directory, "16_bubble_breath", 25, () => { },
                    () => CellNeighbourhoodInPixels(locked, 1.6f), 1);
            }
            finally
            {
                Time.captureDeltaTime = 0f;
            }
        }

        /// <summary>
        /// Delivering an order, filmed at 30 frames per second of game time over the counter: the items fly onto the tray,
        /// the customer leaves and the next one arrives.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureCustomerLeaveFrames()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            Assert.That(AutoPlayer.PlayUntilAnOrderIsDeliverable(game.Session, HighestRequestedLevel), Is.True, "No order became deliverable.");
            yield return GameScreenDriver.WaitSeconds(1.3f);

            var screen = Snapshot.DeviceResolutions[0];
            var order = game.Session.ActiveOrders.First(active => game.Session.CanDeliver(active.Id));
            Time.captureDeltaTime = 1f / 30f;
            try
            {
                yield return CroppedFrames.CaptureLive(
                    game.Camera, screen, directory, "17_customer_leave", 75,
                    () => game.Session.DeliverOrder(order.Id),
                    () => new RectInt(0, Mathf.RoundToInt(screen.y * 0.6f), screen.x, Mathf.RoundToInt(screen.y * 0.4f)), 1);
            }
            finally
            {
                Time.captureDeltaTime = 0f;
            }
        }

        private IEnumerator FilmMerge(string name, GridPosition from, GridPosition to)
        {
            yield return GameScreenDriver.WaitSeconds(0.6f);
            PointerGesture held = null;
            yield return game.DragOnto(from, to, gesture => held = gesture);
            yield return GameScreenDriver.WaitSeconds(0.3f);

            Time.captureDeltaTime = 1f / 30f;
            try
            {
                yield return CroppedFrames.CaptureLive(
                    game.Camera, Snapshot.DeviceResolutions[0], directory, name, 32,
                    () =>
                    {
                        held.MoveTo(game.ScreenPointOf(to));
                        held.Release();
                    },
                    () => CellNeighbourhoodInPixels(to, 2f), 1);
            }
            finally
            {
                Time.captureDeltaTime = 0f;
            }
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
            foreach (var view in PourViews)
            {
                yield return CroppedFrames.Capture(
                    game.Camera, Snapshot.DeviceResolutions[0], directory, $"{name}{view.suffix}_rest", 1, _ => false,
                    () => CellNeighbourhoodInPixels(closed, view.cells), view.magnify);
            }

            yield return game.DragAndDrop(from, to);
            // Past the reveal delay, so the item already sits behind the sand.
            yield return GameScreenDriver.WaitSeconds(0.3f);
            Assert.That(game.Board.TryGetCover(closed, out var cover), Is.True);
            foreach (var view in PourViews)
            {
                yield return CroppedFrames.Capture(
                    game.Camera, Snapshot.DeviceResolutions[0], directory, name + view.suffix, MaxPourFrames,
                    frame =>
                    {
                        var seconds = frame / view.framesPerSecond;
                        cover.ShowPourAt(seconds);
                        return seconds < cover.PourSeconds;
                    },
                    () => CellNeighbourhoodInPixels(closed, view.cells), view.magnify);
            }

            cover.Finish();
        }

        /// <summary>A square of <paramref name="cells"/> cells' width around the cell, in pixels of the current render.</summary>
        private RectInt CellNeighbourhoodInPixels(GridPosition cell, float cells)
        {
            var center = game.ScreenPointOf(cell);
            var size = Mathf.RoundToInt(CellStep(cell).x * cells);
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
