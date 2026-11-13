using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Core.Rules;
using MergeGame.Core.Scoring;
using MergeGame.Core.Session;
using MergeGame.Core.Tests.Support;
using NUnit.Framework;

namespace MergeGame.Core.Tests
{
    public sealed class DataDrivenChainTests
    {
        [Test]
        public void NewChainDefinedOnlyAsData_SpawnsMergesAndScores()
        {
            var gems = new ItemChainDefinition(
                "gem",
                new[]
                {
                    new ItemLevelDefinition(1, "Shard"),
                    new ItemLevelDefinition(2, "Crystal"),
                    new ItemLevelDefinition(3, "Jewel"),
                    new ItemLevelDefinition(4, "Crown"),
                    new ItemLevelDefinition(5, "Treasure"),
                    new ItemLevelDefinition(6, "Hoard")
                },
                spawnWeight: 1,
                scoreMultiplier: 3f);
            var definition = new GameDefinition(
                new ItemCatalog(new[] { gems }),
                new BoardSize(2, 1),
                new ScoreTable(new[] { 0, 5, 10, 15, 20, 25 }));
            var session = new MergeGameSession(definition, new ScriptedRandom(0, 0, 0, 0));

            var first = session.Spawn().Placement;
            var second = session.Spawn().Placement;
            var outcome = session.Drop(first.Position, second.Position);

            Assert.That(first.ItemId, Is.EqualTo(new ItemId("gem", 1)));
            Assert.That(outcome.Kind, Is.EqualTo(DropOutcomeKind.Merge));
            Assert.That(session.Board.GetItem(second.Position).ItemId, Is.EqualTo(new ItemId("gem", 2)));
            Assert.That(session.Score, Is.EqualTo(15));
        }

        [Test]
        public void ChainLength_ComesFromData()
        {
            var catalog = TestContent.Catalog(TestContent.Chain("short", maxLevel: 2));
            var definition = TestContent.Definition(catalog: catalog);
            var session = new SessionBuilder()
                .WithDefinition(definition)
                .WithItem(0, 0, new ItemId("short", 2))
                .WithItem(1, 0, new ItemId("short", 2))
                .Build();

            var outcome = session.Drop(new GridPosition(0, 0), new GridPosition(1, 0));

            Assert.That(outcome.RejectReason, Is.EqualTo(DropRejectReason.MaxLevel));
        }
    }
}
