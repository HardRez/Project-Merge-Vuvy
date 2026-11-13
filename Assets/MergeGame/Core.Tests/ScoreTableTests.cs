using System;
using MergeGame.Core.Scoring;
using NUnit.Framework;

namespace MergeGame.Core.Tests
{
    public sealed class ScoreTableTests
    {
        [Test]
        public void GetPoints_IsOneBased()
        {
            var table = new ScoreTable(new[] { 1, 2, 3 });

            Assert.That(table.GetPoints(1), Is.EqualTo(1));
            Assert.That(table.GetPoints(3), Is.EqualTo(3));
        }

        [Test]
        public void Constructor_WithEmptyTable_Throws()
        {
            Assert.Throws<ArgumentException>(() => new ScoreTable(new int[0]));
        }

        [Test]
        public void Constructor_WithNegativePoints_Throws()
        {
            Assert.Throws<ArgumentException>(() => new ScoreTable(new[] { 0, -5 }));
        }

        [Test]
        public void Constructor_WithPointsDecreasingByLevel_Throws()
        {
            var exception = Assert.Throws<ArgumentException>(() => new ScoreTable(new[] { 0, 20, 10 }));
            Assert.That(exception.Message, Does.Contain("level 3"));
        }

        [Test]
        public void GetPoints_BeyondTheTable_Throws()
        {
            var table = new ScoreTable(new[] { 0, 10 });

            Assert.Throws<ArgumentOutOfRangeException>(() => table.GetPoints(3));
        }
    }
}
