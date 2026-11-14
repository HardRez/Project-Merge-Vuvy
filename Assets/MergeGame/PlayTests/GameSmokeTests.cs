using System.Collections;
using MergeGame.Core.Grid;
using MergeGame.Data.Authoring;
using MergeGame.PlayTests.Support;
using MergeGame.View.MainMenu;
using MergeGame.View.Navigation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zenject;

namespace MergeGame.PlayTests
{
    /// <summary>End-to-end checks through the real scenes, composition root and views.</summary>
    public sealed class GameSmokeTests
    {
        private const float AnimationSettleSeconds = 0.9f;

        private GameScreenDriver game;

        [UnityTest]
        public IEnumerator PlayButton_OnMainMenu_LoadsTheGame()
        {
            yield return GameScreenDriver.LoadScene(GameScreenDriver.MainMenuScene);

            var menu = Object.FindAnyObjectByType<MainMenuView>();
            GameScreenDriver.Click(FindButton(menu.transform, "PlayButton"));
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == GameScreenDriver.GameScene);
        }

        [UnityTest]
        public IEnumerator Board_AtStart_ShowsTheConfiguredGridAndOpeningLayout()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);

            var config = ProjectContext.Instance.Container.Resolve<GameConfigAsset>();
            Assert.That(game.Board.CellCount, Is.EqualTo(config.BoardWidth * config.BoardHeight));
            Assert.That(game.Board.ItemCount, Is.EqualTo(config.InitialLayout.Placements.Count));
            foreach (var placement in config.InitialLayout.Placements)
            {
                Assert.That(game.Board.TryGetItem(new GridPosition(placement.X, placement.Y), out var item), Is.True, $"No item at ({placement.X}, {placement.Y}).");
                Assert.That(item.ItemId.ChainId, Is.EqualTo(placement.Chain.Id));
                Assert.That(item.ItemId.Level, Is.EqualTo(placement.Level));
                Assert.That(item.IsLocked, Is.EqualTo(placement.Locked));
            }
        }

        [UnityTest]
        public IEnumerator SpawnButton_WhenClicked_AddsAnItem()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            var before = game.Board.ItemCount;

            game.ClickSpawn();
            yield return null;

            Assert.That(game.Board.ItemCount, Is.EqualTo(before + 1));
        }

        [UnityTest]
        public IEnumerator DragOntoAnEqualItem_Merges_AndTheScoreGoesUp()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            Assert.That(game.TryFindMergePair(out var from, out var to), Is.True, "The opening layout has no mergeable pair.");
            game.Board.TryGetItem(to, out var target);
            var mergedLevel = target.ItemId.Level + 1;
            var itemsBefore = game.Board.ItemCount;

            yield return game.DragAndDrop(from, to);
            yield return GameScreenDriver.WaitSeconds(AnimationSettleSeconds);

            Assert.That(game.Board.ItemCount, Is.EqualTo(itemsBefore - 1));
            Assert.That(game.Board.TryGetItem(from, out _), Is.False);
            Assert.That(game.Board.TryGetItem(to, out var merged), Is.True);
            Assert.That(merged.ItemId.Level, Is.EqualTo(mergedLevel));
            Assert.That(merged.IsLocked, Is.False);
            Assert.That(game.Score.DisplayedScore, Is.GreaterThan(0));
            Assert.That(game.Score.DisplayedScore, Is.EqualTo(game.Session.Score));
        }

        [UnityTest]
        public IEnumerator DragOntoADifferentItem_ReturnsItToItsCell()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            Assert.That(game.TryFindPair((dragged, target) => dragged.ItemId != target.ItemId, out var from, out var to), Is.True);
            game.Board.TryGetItem(from, out var dragged);
            var draggedId = dragged.ItemId;
            var itemsBefore = game.Board.ItemCount;

            yield return game.DragAndDrop(from, to);
            yield return GameScreenDriver.WaitSeconds(AnimationSettleSeconds);

            Assert.That(game.Board.ItemCount, Is.EqualTo(itemsBefore));
            Assert.That(game.Board.TryGetItem(from, out var returned), Is.True);
            Assert.That(returned, Is.SameAs(dragged));
            Assert.That(returned.ItemId, Is.EqualTo(draggedId));
            Assert.That(returned.IsLifted, Is.False);
            Assert.That(Vector3.Distance(returned.WorldCenter, game.Board.GetCellWorldCenter(from)), Is.LessThan(0.01f));
            Assert.That(game.Board.IsDragging, Is.False);
        }

        [UnityTest]
        public IEnumerator SelectionMarker_WhileItsItemIsDragged_IsHiddenAndReturnsWithIt()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            Assert.That(game.TryFindPair((dragged, target) => dragged.ItemId != target.ItemId, out var from, out var to), Is.True);
            game.Tap(from);
            yield return null;
            Assert.That(game.Board.IsSelectionShown, Is.True);

            PointerGesture held = null;
            yield return game.DragOnto(from, to, gesture => held = gesture);
            Assert.That(game.Board.IsSelectionShown, Is.False, "The marker stayed on the emptied cell.");

            held.Release();
            yield return null;
            Assert.That(game.Board.IsSelectionShown, Is.True, "The marker did not come back with the returned item.");
        }

        [UnityTest]
        public IEnumerator BurstsOfCommands_InOneFrame_LeaveTheViewMatchingTheBoard()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);

            for (var i = 0; i < 8; i++)
                game.ClickSpawn();
            AutoPlayer.PlayMoves(game.Session, maxLevelToCreate: 5, moves: 300);
            yield return GameScreenDriver.WaitSeconds(AnimationSettleSeconds + 0.6f);

            var board = game.Session.Board;
            var itemCount = 0;
            foreach (var cell in game.Cells())
            {
                var hasItem = board.TryGetItem(cell, out var expected);
                Assert.That(game.Board.TryGetItem(cell, out var shown), Is.EqualTo(hasItem), $"Cell {cell}");
                if (!hasItem)
                    continue;

                itemCount++;
                Assert.That(shown.ItemId, Is.EqualTo(expected.ItemId), $"Cell {cell}");
                Assert.That(shown.IsLocked, Is.EqualTo(expected.IsLocked), $"Cell {cell}");
                Assert.That(shown.DisplayedIcon, Is.Not.Null, $"Cell {cell}");
                Assert.That(Vector3.Distance(shown.WorldCenter, game.Board.GetCellWorldCenter(cell)), Is.LessThan(0.01f), $"Cell {cell}");
            }

            Assert.That(game.Board.ItemCount, Is.EqualTo(itemCount));
            Assert.That(game.Score.DisplayedScore, Is.EqualTo(game.Session.Score));
        }

        [UnityTest]
        public IEnumerator BackButton_InTheGame_ReturnsToTheMainMenu()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);

            var menu = Object.FindAnyObjectByType<GameMenuView>();
            GameScreenDriver.Click(FindButton(menu.transform, "HomeButton"));
            yield return new WaitUntil(() => SceneManager.GetActiveScene().name == GameScreenDriver.MainMenuScene);
        }

        private static Button FindButton(Transform root, string name)
        {
            foreach (var button in root.GetComponentsInChildren<Button>(includeInactive: true))
            {
                if (button.name == name)
                    return button;
            }

            Assert.Fail($"No button named {name} under {root.name}.");
            return null;
        }
    }
}
