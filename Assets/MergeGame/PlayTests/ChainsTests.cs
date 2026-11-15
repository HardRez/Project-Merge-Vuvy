using System.Collections;
using System.Linq;
using MergeGame.Core.Items;
using MergeGame.Data.Visuals;
using MergeGame.PlayTests.Support;
using MergeGame.View.Chains;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;

namespace MergeGame.PlayTests
{
    /// <summary>The info button and the chains popup, through the real Game scene.</summary>
    public sealed class ChainsTests
    {
        private const float AnimationSeconds = 0.45f;

        [UnityTest]
        public IEnumerator TheInfoButton_OpensATabForEveryChain_AndEachTabShowsItsChainFromItsFirstLevelToItsLast()
        {
            GameScreenDriver game = null;
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            var chains = Object.FindAnyObjectByType<ChainsView>();
            Assert.That(chains, Is.Not.Null);
            Assert.That(chains.IsOpen, Is.False);

            GameScreenDriver.Click(chains.OpenButton);
            yield return GameScreenDriver.WaitSeconds(AnimationSeconds);
            Assert.That(chains.IsOpen, Is.True);

            var container = Object.FindAnyObjectByType<SceneContext>().Container;
            var catalog = container.Resolve<ItemCatalog>();
            var visuals = container.Resolve<IItemVisuals>();
            Assert.That(chains.Tabs.Select(tab => tab.LabelText),
                Is.EqualTo(catalog.Chains.Select(chain => visuals.GetChainDisplayName(chain.Id))), "One tab per chain, in the catalog's order.");
            Assert.That(chains.ChosenIndex, Is.Zero, "The first chain is shown at first.");

            for (var c = 0; c < catalog.Chains.Count; c++)
            {
                GameScreenDriver.Click(chains.Tabs[c].Button);
                yield return null;

                var chain = catalog.Chains[c];
                Assert.That(chains.Tabs.Select(tab => tab.IsChosen), Is.EqualTo(Enumerable.Range(0, chains.Tabs.Count).Select(i => i == c)),
                    $"Only the tab of '{chain.Id}' glows.");
                Assert.That(chains.ChainTitle, Is.EqualTo(visuals.GetChainDisplayName(chain.Id)));
                Assert.That(chains.Cells, Has.Count.EqualTo(chain.MaxLevel), $"'{chain.Id}' does not show all its levels.");

                var firstRowLength = (chain.MaxLevel + 1) / 2;
                for (var level = 1; level <= chain.MaxLevel; level++)
                {
                    var item = chain.ItemAtLevel(level);
                    var cell = chains.Cells[level - 1];
                    Assert.That(cell.gameObject.activeInHierarchy, Is.True);
                    Assert.That(cell.Icon, Is.SameAs(visuals.GetIcon(item)), $"{item} shows the wrong picture.");
                    Assert.That(cell.LevelText, Is.EqualTo(level == chain.MaxLevel ? ChainLevelCellView.MaxLevelText : level.ToString()),
                        "Every level shows its number, but the last reads MAX.");
                    Assert.That(cell.BadgeScale, level == chain.MaxLevel ? Is.GreaterThan(1f) : Is.EqualTo(1f), "Only the last level's badge is drawn larger.");
                    Assert.That(cell.NameText, Is.EqualTo(visuals.GetDisplayName(item)));
                    var endsRow = level == firstRowLength || level == chain.MaxLevel;
                    Assert.That(cell.ShowsArrow, Is.EqualTo(!endsRow), "Every level but the last of each row points on to the next.");
                }
            }

            GameScreenDriver.Click(chains.CloseButton);
            yield return GameScreenDriver.WaitSeconds(AnimationSeconds);
            Assert.That(chains.IsOpen, Is.False);
        }
    }
}
