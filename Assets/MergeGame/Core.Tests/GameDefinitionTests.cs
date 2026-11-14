using System;
using MergeGame.Core.Grid;
using MergeGame.Core.Rules;
using MergeGame.Core.Scoring;
using MergeGame.Core.Session;
using NUnit.Framework;
using static MergeGame.Core.Tests.Support.TestContent;

namespace MergeGame.Core.Tests
{
    public sealed class GameDefinitionTests
    {
        [Test]
        public void Constructor_WithScoreTableShorterThanTheLongestChain_Throws()
        {
            var catalog = Catalog(Chain(Wood, maxLevel: 6));

            var exception = Assert.Throws<ArgumentException>(() => Definition(catalog: catalog, scoreTable: new ScoreTable(DefaultPoints)));
            Assert.That(exception.Message, Does.Contain("level 6"));
        }

        [Test]
        public void Constructor_WithEmptyRuleList_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                new GameDefinition(DefaultCatalog(), new BoardSize(5, 5), new ScoreTable(DefaultPoints), new IDropRule[0]));
        }

        [Test]
        public void Constructor_WithUnsetBoardSize_Throws()
        {
            Assert.Throws<ArgumentException>(() => new GameDefinition(DefaultCatalog(), default, new ScoreTable(DefaultPoints)));
        }

        [TestCase(0, 5)]
        [TestCase(5, 0)]
        public void BoardSize_WithNonPositiveDimension_Throws(int width, int height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoardSize(width, height));
        }

        [Test]
        public void Constructor_WithoutRules_UsesTheStandardRules()
        {
            var definition = Definition();

            Assert.That(definition.DropRules.Count, Is.EqualTo(4));
            Assert.That(definition.DropRules[0], Is.InstanceOf<LockedSourceRule>());
            Assert.That(definition.DropRules[1], Is.InstanceOf<HiddenTargetRule>());
            Assert.That(definition.DropRules[2], Is.InstanceOf<MoveToEmptyCellRule>());
            Assert.That(definition.DropRules[3], Is.InstanceOf<MergeRule>());
        }
    }
}
