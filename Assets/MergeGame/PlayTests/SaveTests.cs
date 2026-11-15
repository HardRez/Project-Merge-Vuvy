using System.Collections;
using System.IO;
using System.Linq;
using MergeGame.App.Saving;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Core.Orders;
using MergeGame.Core.Session;
using MergeGame.PlayTests.Support;
using MergeGame.View.Cheats;
using MergeGame.View.Settings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MergeGame.PlayTests
{
    /// <summary>
    /// The saved game: the JSON file store on its own, and through the real Game scene that progress survives reopening
    /// the game and that the cheats' reset starts it over. Runs in the test run's own save folder (<see cref="TestSaveFolder"/>).
    /// </summary>
    public sealed class SaveTests
    {
        private const float AnimationSeconds = 0.45f;

        private string storeFolder;

        [SetUp]
        public void SetUp() => storeFolder = Path.Combine(TestSaveFolder.Directory, "Store");

        [Test]
        public void TheStore_WritesJson_AndReadsBackTheSameGame()
        {
            var store = new JsonFileSaveStore(storeFolder);
            Assert.That(store.TryLoad(out _), Is.False, "No save yet.");

            store.Save(Snapshot(score: 42));

            Assert.That(File.ReadAllText(store.FilePath), Does.Contain("\"score\": 42").And.Contain("\"chain\": \"a\""));
            Assert.That(store.TryLoad(out var loaded), Is.True);
            Assert.That(loaded.Score, Is.EqualTo(42));
            Assert.That(loaded.NextOrderId, Is.EqualTo(5));
            Assert.That(loaded.Items.Select(item => (item.Position, item.ItemId, item.IsLocked, item.IsHidden)), Is.EqualTo(new[]
            {
                (new GridPosition(0, 0), new ItemId("a", 2), false, false),
                (new GridPosition(3, 1), new ItemId("b", 1), true, false),
                (new GridPosition(4, 1), new ItemId("b", 3), true, true)
            }));
            Assert.That(loaded.Orders.Single().Id, Is.EqualTo(4));
            Assert.That(loaded.Orders.Single().CustomerIndex, Is.EqualTo(2));
            Assert.That(loaded.Orders.Single().Requirements, Is.EqualTo(new[] { new ItemId("a", 1), new ItemId("b", 2) }));
        }

        [Test]
        public void ACorruptSave_FallsBackToThePreviousOne()
        {
            var store = new JsonFileSaveStore(storeFolder);
            store.Save(Snapshot(score: 1));
            store.Save(Snapshot(score: 2));

            File.WriteAllText(store.FilePath, "{ \"score\": 3, \"items\": [ { \"x\": ");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Ignoring unreadable save"));

            Assert.That(store.TryLoad(out var loaded), Is.True);
            Assert.That(loaded.Score, Is.EqualTo(1));
        }

        [Test]
        public void ASaveFromAnUnknownVersion_IsIgnored()
        {
            var store = new JsonFileSaveStore(storeFolder);
            Directory.CreateDirectory(storeFolder);
            File.WriteAllText(store.FilePath, "{ \"version\": 99, \"score\": 3 }");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("not supported"));

            Assert.That(store.TryLoad(out _), Is.False);
        }

        [Test]
        public void Delete_RemovesTheSaveAndItsBackup()
        {
            var store = new JsonFileSaveStore(storeFolder);
            store.Save(Snapshot(score: 1));
            store.Save(Snapshot(score: 2));
            Assert.That(File.Exists(store.BackupPath), Is.True);

            store.Delete();

            Assert.That(File.Exists(store.FilePath) || File.Exists(store.BackupPath), Is.False);
            Assert.That(store.TryLoad(out _), Is.False);
        }

        [UnityTest]
        public IEnumerator TheGame_IsSavedAsItIsPlayed_AndReopensWhereItStopped()
        {
            GameScreenDriver game = null;
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            game.ClickSpawn();
            game.ClickSpawn();
            yield return null;
            var session = game.Session;
            var played = session.CaptureSnapshot();

            yield return GameScreenDriver.OpenGame(driver => game = driver);
            Assert.That(game.Session, Is.Not.SameAs(session), "The game was opened anew.");
            var reopened = game.Session.CaptureSnapshot();

            AssertSameGame(reopened, played);
            Assert.That(game.Board.ItemCount, Is.EqualTo(played.Items.Count), "The board view shows the reopened game.");
        }

        [UnityTest]
        public IEnumerator ResetSave_InTheCheatsOfTheSettings_StartsTheGameOver()
        {
            GameScreenDriver game = null;
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            var definition = game.Resolve<GameDefinition>();
            game.ClickSpawn();
            yield return null;
            var played = game.Session;
            Assert.That(played.Board.Size.CellCount - played.Board.EmptyCellCount, Is.EqualTo(definition.InitialPlacements.Count + 1));

            var settings = Object.FindAnyObjectByType<SettingsView>();
            var cheats = Object.FindAnyObjectByType<CheatsView>();
            GameScreenDriver.Click(settings.OpenButton);
            yield return GameScreenDriver.WaitSeconds(AnimationSeconds);
            GameScreenDriver.Click(cheats.OpenButton);
            yield return GameScreenDriver.WaitSeconds(AnimationSeconds);
            Assert.That(cheats.IsOpen && settings.IsOpen, Is.True, "The cheats open over the settings.");

            GameScreenDriver.Click(cheats.ResetSaveButton);
            yield return new WaitUntil(() => cheats == null);
            yield return GameScreenDriver.OpenGame(driver => game = driver);

            Assert.That(game.Session, Is.Not.SameAs(played));
            Assert.That(game.Session.Score, Is.Zero);
            Assert.That(game.Board.ItemCount, Is.EqualTo(definition.InitialPlacements.Count), "The new game starts from the opening layout.");
        }

        private static void AssertSameGame(GameSnapshot actual, GameSnapshot expected)
        {
            Assert.That(actual.Score, Is.EqualTo(expected.Score));
            Assert.That(actual.Items.Select(item => (item.Position, item.ItemId, item.IsLocked, item.IsHidden)),
                Is.EqualTo(expected.Items.Select(item => (item.Position, item.ItemId, item.IsLocked, item.IsHidden))));
            Assert.That(actual.Orders.Select(order => (order.Id, order.CustomerIndex, string.Join(",", order.Requirements))),
                Is.EqualTo(expected.Orders.Select(order => (order.Id, order.CustomerIndex, string.Join(",", order.Requirements)))));
            Assert.That(actual.NextOrderId, Is.EqualTo(expected.NextOrderId));
        }

        private static GameSnapshot Snapshot(int score) =>
            new GameSnapshot(
                score,
                new[]
                {
                    new InitialPlacement(new GridPosition(0, 0), new ItemId("a", 2)),
                    new InitialPlacement(new GridPosition(3, 1), new ItemId("b", 1), isLocked: true),
                    InitialPlacement.Hidden(new GridPosition(4, 1), new ItemId("b", 3))
                },
                new[] { new Order(4, 2, new[] { new ItemId("a", 1), new ItemId("b", 2) }, 30) },
                5);
    }
}
