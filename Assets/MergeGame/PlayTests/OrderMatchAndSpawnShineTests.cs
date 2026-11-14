using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MergeGame.Core.Grid;
using MergeGame.PlayTests.Support;
using MergeGame.View.Board;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MergeGame.PlayTests
{
    /// <summary>The order-match mark and the spawn shine, through the real Game scene.</summary>
    public sealed class OrderMatchAndSpawnShineTests
    {
        private const int HighestRequestedLevel = 4;
        private const float SettleSeconds = 1.3f;

        private GameScreenDriver game;

        [UnityTest]
        public IEnumerator ItemsWantedByAnOrder_AreMarked_AndLoseTheMarkWhenDelivered()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            Assert.That(AutoPlayer.PlayUntilAnOrderIsDeliverable(game.Session, HighestRequestedLevel), Is.True);
            yield return GameScreenDriver.WaitSeconds(SettleSeconds);

            AssertMarksMatchTheSession();

            var order = game.Session.ActiveOrders.First(active => game.Session.CanDeliver(active.Id));
            var delivered = game.Session.GetRequirementMatches(order.Id).Select(cell => ItemAt(cell.Value)).ToList();
            Assert.That(delivered.Select(item => item.OrderMark.IsShown), Has.All.True);

            game.Session.DeliverOrder(order.Id);
            yield return null;

            Assert.That(delivered.Select(item => item.OrderMark.IsShown), Has.All.False, "A delivered item kept its mark.");
            yield return GameScreenDriver.WaitSeconds(SettleSeconds);
            AssertMarksMatchTheSession();
        }

        [UnityTest]
        public IEnumerator AMovedMarkedItem_KeepsItsMarkAtTheNewCell()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            Assert.That(AutoPlayer.PlayUntil(game.Session, HighestRequestedLevel, () => TryFindUniqueMarkedItem(out _)), Is.True);
            yield return GameScreenDriver.WaitSeconds(SettleSeconds);
            TryFindUniqueMarkedItem(out var from);
            var marked = ItemAt(from);
            var to = game.FirstEmptyCell();

            PointerGesture held = null;
            yield return game.DragOnto(from, to, gesture => held = gesture);
            Assert.That(marked.OrderMark.IsShown, Is.True, "The mark did not travel with the dragged item.");
            held.Release();
            Assert.That(marked.OrderMark.IsShown, Is.True, "The mark blinked off during the move.");

            yield return GameScreenDriver.WaitSeconds(SettleSeconds);
            Assert.That(ItemAt(to), Is.SameAs(marked));
            Assert.That(marked.OrderMark.IsShown, Is.True);
            AssertMarksMatchTheSession();
        }

        [UnityTest]
        public IEnumerator ASpawnedItem_ShinesBriefly_ThenRests()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            var before = OccupiedCells();

            game.ClickSpawn();
            var spawned = ItemAt(OccupiedCells().Except(before).Single());
            Assert.That(spawned.Accent.Current, Is.EqualTo(AccentKind.SpawnShine));

            yield return GameScreenDriver.WaitSeconds(SettleSeconds);
            Assert.That(spawned.Accent.Current, Is.EqualTo(AccentKind.None));
            Assert.That(spawned.Accent.transform.localScale, Is.EqualTo(Vector3.one));
        }

        [UnityTest]
        public IEnumerator OpeningLayoutItems_DoNotShine()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);

            foreach (var cell in OccupiedCells())
                Assert.That(ItemAt(cell).Accent.Current, Is.EqualTo(AccentKind.None), $"Cell {cell}");
        }

        /// <summary>Exactly the items the session would hand to a customer carry the mark.</summary>
        private void AssertMarksMatchTheSession()
        {
            var matched = game.OrderMatchedCells();
            foreach (var cell in OccupiedCells())
                Assert.That(ItemAt(cell).OrderMark.IsShown, Is.EqualTo(matched.Contains(cell)), $"Cell {cell}");
        }

        /// <summary>A marked item no other free item could stand in for, so moving it cannot hand its mark to a twin.</summary>
        private bool TryFindUniqueMarkedItem(out GridPosition cell)
        {
            var board = game.Session.Board;
            foreach (var candidate in game.OrderMatchedCells())
            {
                var itemId = board.GetItem(candidate).ItemId;
                var twins = board.Positions.Count(position => board.TryGetItem(position, out var other) && !other.IsLocked && other.ItemId == itemId);
                if (twins == 1)
                {
                    cell = candidate;
                    return true;
                }
            }

            cell = default;
            return false;
        }

        private ItemView ItemAt(GridPosition cell)
        {
            Assert.That(game.Board.TryGetItem(cell, out var item), Is.True, $"No item at {cell}.");
            return item;
        }

        private List<GridPosition> OccupiedCells() => game.Cells().Where(cell => game.Board.TryGetItem(cell, out _)).ToList();
    }
}
