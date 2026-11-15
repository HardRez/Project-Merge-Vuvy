using System.Collections;
using System.Linq;
using MergeGame.PlayTests.Support;
using MergeGame.View.Board;
using MergeGame.View.Common;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MergeGame.PlayTests
{
    /// <summary>How the buttons press and what a merge's points popup shows, through the real scenes.</summary>
    public sealed class ButtonAndPopupTests
    {
        private GameScreenDriver game;

        [UnityTest]
        public IEnumerator EveryTactileButton_OnBothScreens_IsPivotedOnItsCentre_SoAPressShrinksItTowardsItsMiddle()
        {
            yield return GameScreenDriver.LoadScene(GameScreenDriver.MainMenuScene);
            AssertCentred("main menu");

            yield return GameScreenDriver.OpenGame(driver => game = driver);
            AssertCentred("game");
        }

        [UnityTest]
        public IEnumerator AMergesPointsPopup_ShowsTheBillsBeforeItsPoints()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            Assert.That(game.TryFindMergePair(out var from, out var to), Is.True, "The opening layout has no mergeable pair.");

            yield return game.DragAndDrop(from, to);
            yield return GameScreenDriver.WaitSeconds(0.3f);

            var popup = Object.FindObjectsByType<PointsPopup>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault();
            Assert.That(popup, Is.Not.Null, "No points popup is showing after the merge.");
            var cash = popup.GetComponentsInChildren<Image>().FirstOrDefault(image => image.sprite != null && image.sprite.name == "score_cash");
            Assert.That(cash, Is.Not.Null, "The popup does not show the bills.");
            var label = popup.GetComponentInChildren<TMP_Text>();
            Assert.That(label.text, Does.StartWith("+"));
            Assert.That(cash.rectTransform.position.x, Is.LessThan(label.rectTransform.position.x), "The bills are not before the points.");
        }

        private static void AssertCentred(string screen)
        {
            var buttons = Object.FindObjectsByType<TactileButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(buttons, Is.Not.Empty, $"No tactile buttons on the {screen}.");
            foreach (var button in buttons)
            {
                var pivot = ((RectTransform)button.transform).pivot;
                Assert.That(Vector2.Distance(pivot, new Vector2(0.5f, 0.5f)), Is.LessThan(0.001f),
                    $"'{button.name}' on the {screen} is pivoted at {pivot}, so a press shrinks it towards that point.");
            }
        }
    }
}
