using System;
using System.Collections.Generic;
using System.Linq;
using MergeGame.Core.Items;
using MergeGame.Presentation.Chains;
using NUnit.Framework;

namespace MergeGame.Presentation.Tests
{
    public sealed class ChainsPresenterTests
    {
        [Test]
        public void Initialize_ShowsEveryChain_InTheCatalogsOrder_FromLevelOneToItsLast()
        {
            var catalog = new ItemCatalog(new[]
            {
                Chain("wood", 3),
                Chain("stone", 5)
            });
            var view = new FakeChainsView();

            new ChainsPresenter(view, catalog).Initialize();

            Assert.That(view.Shown, Has.Count.EqualTo(1));
            var chains = view.Shown[0];
            Assert.That(chains.Select(chain => chain.ChainId), Is.EqualTo(new[] { "wood", "stone" }));
            Assert.That(chains[0].Levels, Is.EqualTo(new[] { new ItemId("wood", 1), new ItemId("wood", 2), new ItemId("wood", 3) }));
            Assert.That(chains[1].Levels.Select(item => item.Level), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
            Assert.That(chains[1].Levels.All(item => item.ChainId == "stone"), Is.True);
        }

        [Test]
        public void Constructor_RejectsMissingDependencies()
        {
            var catalog = new ItemCatalog(new[] { Chain("wood", 2) });
            Assert.Throws<ArgumentNullException>(() => new ChainsPresenter(null, catalog));
            Assert.Throws<ArgumentNullException>(() => new ChainsPresenter(new FakeChainsView(), null));
        }

        private static ItemChainDefinition Chain(string id, int levels) =>
            new ItemChainDefinition(id, Enumerable.Range(1, levels).Select(level => new ItemLevelDefinition(level, $"{id} {level}")), 1, 1f);

        private sealed class FakeChainsView : IChainsView
        {
            public List<IReadOnlyList<ChainLevels>> Shown { get; } = new List<IReadOnlyList<ChainLevels>>();

            public void ShowChains(IReadOnlyList<ChainLevels> chains) => Shown.Add(chains);
        }
    }
}
